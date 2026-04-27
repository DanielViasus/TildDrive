using UnityEngine;

namespace TiltDrive.VehicleSystem
{
    public class DynamicVehicleOutput
    {
        private const float KmhToMph = 0.621371f;
        private const float KwToHp = 1.341022f;

        public VehicleOutputSimulationOutput Simulate(VehicleOutputSimulationInput input)
        {
            VehicleOutputSimulationOutput output = new VehicleOutputSimulationOutput();

            if (input == null || !input.IsValid())
            {
                output.state.ResetRuntime();
                output.diagnosticMessage = "INVALID VEHICLE OUTPUT INPUT";
                return output;
            }

            VehicleOutputConfig config = input.vehicleOutputConfig;
            config.ClampValues();

            VehicleOutputState state = output.state;
            state.ResetRuntime(config);

            float deltaTime = Mathf.Max(0.0001f, input.deltaTime);
            float wheelRadius = Mathf.Max(0.01f, config.wheelRadiusMeters);
            float wheelCircumference = 2f * Mathf.PI * wheelRadius;
            float signedOutputRPM = input.transmissionOutputRPM;
            float wheelAngularVelocityRadS = signedOutputRPM * 2f * Mathf.PI / 60f;

            float previousSpeedMS = input.previousSpeedMS;
            float previousDirection = GetMovementDirection(previousSpeedMS, input.driveDirection);
            float tractionDirection = input.driveDirection != 0 ? input.driveDirection : previousDirection;

            float tractionForceN = (input.transmissionOutputTorqueNm / wheelRadius) *
                tractionDirection *
                config.tractionForceScale;

            float dragDirection = GetOppositeDirection(previousSpeedMS);
            float speedAbsMS = Mathf.Abs(previousSpeedMS);
            float aerodynamicDragForceN = config.aerodynamicDragCoefficient * speedAbsMS * speedAbsMS;
            float rollingResistanceForceN = speedAbsMS > 0.05f
                ? config.rollingResistanceCoefficient * config.vehicleMassKg * Physics.gravity.magnitude
                : 0f;
            BrakeReport brake = CalculateBrakeReport(input, config, speedAbsMS, tractionForceN, deltaTime);
            float requestedBrakeForceN = brake.requestedBrakeForceN;
            float brakeForceN = brake.effectiveBrakeForceN;
            float engineBrakeForceN = CalculateEngineBrakeForce(input, config, wheelRadius, speedAbsMS);
            float brakeDirection = GetBrakeDirection(previousSpeedMS, tractionForceN);

            float passiveResistanceForceN =
                (aerodynamicDragForceN + rollingResistanceForceN + engineBrakeForceN) *
                dragDirection;
            float brakeResistanceForceN = brakeForceN * brakeDirection;
            float resistanceForceN = passiveResistanceForceN + brakeResistanceForceN;

            float netForceN = tractionForceN + resistanceForceN;
            float rawAccelerationMS2 = netForceN / config.vehicleMassKg;
            float signedSpeedMS = previousSpeedMS + rawAccelerationMS2 * deltaTime;

            if (Mathf.Abs(previousSpeedMS) > 0f &&
                Mathf.Sign(previousSpeedMS) != Mathf.Sign(signedSpeedMS) &&
                Mathf.Abs(tractionForceN) < Mathf.Abs(resistanceForceN))
            {
                signedSpeedMS = 0f;
            }

            if (ShouldSnapToFullStop(
                    signedSpeedMS,
                    tractionForceN,
                    brakeForceN,
                    input.brakeInput,
                    config))
            {
                signedSpeedMS = 0f;
            }

            float activeGearSpeedLimitMS = CalculateActiveGearSpeedLimitMS(input, config, wheelCircumference);
            bool gearSpeedLimited = false;
            if (activeGearSpeedLimitMS > 0f)
            {
                if (Mathf.Abs(previousSpeedMS) > activeGearSpeedLimitMS)
                {
                    float targetSpeedMS = Mathf.Sign(previousSpeedMS) * activeGearSpeedLimitMS;
                    signedSpeedMS = Mathf.MoveTowards(
                        signedSpeedMS,
                        targetSpeedMS,
                        config.overRevSpeedCorrectionDecelerationMS2 * deltaTime);
                    gearSpeedLimited = true;
                }
                else
                {
                    float limitedSpeedMS = Mathf.Clamp(signedSpeedMS, -activeGearSpeedLimitMS, activeGearSpeedLimitMS);
                    gearSpeedLimited = !Mathf.Approximately(limitedSpeedMS, signedSpeedMS);
                    signedSpeedMS = limitedSpeedMS;
                }
            }

            float maxSpeedMS = config.theoreticalMaxSpeedKmh / 3.6f;
            if (config.clampSpeedToTheoreticalMax)
            {
                signedSpeedMS = Mathf.Clamp(signedSpeedMS, -maxSpeedMS, maxSpeedMS);
            }

            float signedSpeedKMH = signedSpeedMS * 3.6f;
            float signedSpeedMPH = signedSpeedKMH * KmhToMph;

            if (config.clampSpeedToTheoreticalMax)
            {
                signedSpeedKMH = Mathf.Clamp(signedSpeedKMH, -config.theoreticalMaxSpeedKmh, config.theoreticalMaxSpeedKmh);
                signedSpeedMS = signedSpeedKMH / 3.6f;
                signedSpeedMPH = signedSpeedKMH * KmhToMph;
            }

            float absoluteSpeedKMH = Mathf.Abs(signedSpeedKMH);
            float speedPercent = Mathf.Clamp01(absoluteSpeedKMH / config.theoreticalMaxSpeedKmh) * 100f;
            float activeGearSpeedLimitKMH = activeGearSpeedLimitMS * 3.6f;
            float simulationSpeed = config.speedDisplayUnit == VehicleSpeedUnit.MilesPerHour
                ? signedSpeedMPH
                : signedSpeedKMH;

            float accelerationMS2 = (signedSpeedMS - previousSpeedMS) / deltaTime;
            float powerKW = input.transmissionOutputTorqueNm * Mathf.Abs(wheelAngularVelocityRadS) / 1000f;
            float powerHP = powerKW * KwToHp;
            SteeringReport steering = CalculateSteering(
                input,
                config,
                signedSpeedMS,
                deltaTime,
                brake.wheelsLocked);

            state.simulationSpeed = simulationSpeed;
            state.simulationSpeedUnit = config.speedDisplayUnit;
            state.motorSpeedPercent = speedPercent;
            state.engineRPM = input.engineRPM;
            state.engineOn = input.engineOn;
            state.currentGear = input.currentGear;

            state.transmissionOutputTorqueNm = Mathf.Max(0f, input.transmissionOutputTorqueNm);
            state.transmissionOutputRPM = signedOutputRPM;

            state.wheelRadiusMeters = wheelRadius;
            state.wheelCircumferenceMeters = wheelCircumference;
            state.wheelAngularVelocityRadS = wheelAngularVelocityRadS;

            state.finalSpeedMS = signedSpeedMS;
            state.finalSpeedKMH = signedSpeedKMH;
            state.finalSpeedMPH = signedSpeedMPH;
            state.absoluteSpeedKMH = absoluteSpeedKMH;

            state.driveSpeedPercent = speedPercent;
            state.activeGearSpeedLimitKMH = activeGearSpeedLimitKMH;
            state.gearSpeedLimited = gearSpeedLimited;
            state.outputPowerKW = Mathf.Max(0f, powerKW);
            state.outputPowerHP = Mathf.Max(0f, powerHP);
            state.tractionForceN = tractionForceN;

            state.accelerationMS2 = accelerationMS2;
            state.brakeInput = Mathf.Clamp01(input.brakeInput);
            state.requestedBrakeForceN = requestedBrakeForceN;
            state.aerodynamicDragForceN = aerodynamicDragForceN;
            state.rollingResistanceForceN = rollingResistanceForceN;
            state.brakeForceN = brakeForceN;
            state.engineBrakeForceN = engineBrakeForceN;

            state.handbrakeActive = false;
            state.absActive = brake.absActive;
            state.wheelsLocked = brake.wheelsLocked;
            state.hasBrakeWarning = brake.hasWarning;
            state.brakeSeverity = brake.severity;
            state.brakeDemandDecelerationMS2 = brake.demandDecelerationMS2;
            state.brakeTemperatureC = brake.temperatureC;
            state.brakeThermalEfficiency = brake.thermalEfficiency;
            state.brakeOverheated = brake.overheated;
            state.brakeFadeActive = brake.fadeActive;
            state.lastBrakeWarningCode = brake.warningCode;
            state.lastBrakeWarningMessage = brake.warningMessage;

            state.steerInput = Mathf.Clamp(input.steerInput, -1f, 1f);
            state.effectiveSteerInput = steering.effectiveSteerInput;
            state.steeringAngleDegrees = steering.steeringAngleDegrees;
            state.yawRateDegreesPerSecond = steering.yawRateDegreesPerSecond;
            state.headingDegrees = steering.headingDegrees;
            state.turnRadiusMeters = steering.turnRadiusMeters;
            state.steeringControlLost = steering.steeringControlLost;
            state.hasSteeringWarning = steering.hasWarning;
            state.steeringSeverity = steering.severity;
            state.steeringInputDelta = steering.inputDelta;
            state.lastSteeringWarningCode = steering.warningCode;
            state.lastSteeringWarningMessage = steering.warningMessage;

            state.driveDirection = Mathf.Abs(signedSpeedMS) > 0.05f
                ? (int)Mathf.Sign(signedSpeedMS)
                : input.driveDirection;
            state.isMoving = absoluteSpeedKMH > 0.1f;
            state.lastUpdateTime = input.simulationTime;

            if (brake.wheelsLocked)
            {
                output.diagnosticMessage = "WHEEL LOCK UNDER BRAKING";
            }
            else if (brake.absActive)
            {
                output.diagnosticMessage = "ABS ACTIVE";
            }
            else if (brake.hasWarning)
            {
                output.diagnosticMessage = brake.fadeActive || brake.overheated
                    ? "BRAKE TEMPERATURE WARNING"
                    : "AGGRESSIVE BRAKING WARNING";
            }
            else if (steering.hasWarning)
            {
                output.diagnosticMessage = "AGGRESSIVE STEERING WARNING";
            }
            else if (gearSpeedLimited)
            {
                output.diagnosticMessage = "ACTIVE GEAR RPM SPEED LIMIT";
            }
            else
            {
                output.diagnosticMessage = state.isMoving ? "VEHICLE OUTPUT MOVING" : "VEHICLE OUTPUT REST";
            }

            return output;
        }

        private struct SteeringReport
        {
            public float effectiveSteerInput;
            public float steeringAngleDegrees;
            public float yawRateDegreesPerSecond;
            public float headingDegrees;
            public float turnRadiusMeters;
            public bool steeringControlLost;
            public bool hasWarning;
            public float severity;
            public float inputDelta;
            public string warningCode;
            public string warningMessage;
        }

        private struct BrakeReport
        {
            public float requestedBrakeForceN;
            public float effectiveBrakeForceN;
            public float demandDecelerationMS2;
            public bool absActive;
            public bool wheelsLocked;
            public bool hasWarning;
            public float severity;
            public float temperatureC;
            public float thermalEfficiency;
            public bool fadeActive;
            public bool overheated;
            public string warningCode;
            public string warningMessage;
        }

        private static float CalculateActiveGearSpeedLimitMS(
            VehicleOutputSimulationInput input,
            VehicleOutputConfig config,
            float wheelCircumference)
        {
            if (!config.clampSpeedToActiveGearRPM ||
                input.currentGear == 0 ||
                input.totalDriveRatio <= 0f ||
                input.engineMaxRPM <= 0f ||
                input.clutchEngagement <= 0.05f)
            {
                return 0f;
            }

            float wheelRPMAtEngineLimit = input.engineMaxRPM / input.totalDriveRatio;
            return Mathf.Max(0f, wheelRPMAtEngineLimit * wheelCircumference / 60f);
        }

        private static float CalculateEngineBrakeForce(
            VehicleOutputSimulationInput input,
            VehicleOutputConfig config,
            float wheelRadius,
            float speedAbsMS)
        {
            if (speedAbsMS <= 0.05f || input.currentGear == 0 || input.totalDriveRatio <= 0f)
            {
                return 0f;
            }

            float drivetrainEngagement = Mathf.Clamp01(input.clutchEngagement);
            float brakeTorqueAtWheels =
                input.engineBrakeTorqueNm *
                input.totalDriveRatio *
                drivetrainEngagement *
                config.engineBrakeMultiplier;

            return Mathf.Max(0f, brakeTorqueAtWheels / wheelRadius);
        }

        private static BrakeReport CalculateBrakeReport(
            VehicleOutputSimulationInput input,
            VehicleOutputConfig config,
            float speedAbsMS,
            float tractionForceN,
            float deltaTime)
        {
            float brakeInput = Mathf.Clamp01(input.brakeInput);
            float requestedBrakeForceN = brakeInput * config.brakeForceN;
            BrakeTemperatureReport temperature = CalculateBrakeTemperature(input, config, speedAbsMS, requestedBrakeForceN, deltaTime);
            float thermallyLimitedBrakeForceN = requestedBrakeForceN * temperature.thermalEfficiency;
            float demandDecelerationMS2 = requestedBrakeForceN / Mathf.Max(1f, config.vehicleMassKg);
            bool movingFastEnoughForLock = speedAbsMS >= config.wheelLockMinSpeedMS;
            bool aggressiveByInput = brakeInput >= config.aggressiveBrakeInputThreshold;
            bool aggressiveByDecel = demandDecelerationMS2 >= config.aggressiveBrakeDecelerationMS2;
            bool aggressiveBraking = movingFastEnoughForLock && (aggressiveByInput || aggressiveByDecel);
            bool lockRisk =
                aggressiveBraking &&
                brakeInput >= config.wheelLockBrakeInputThreshold &&
                thermallyLimitedBrakeForceN > Mathf.Abs(tractionForceN) + config.fullStopTractionForceThresholdN;

            BrakeReport report = new BrakeReport
            {
                requestedBrakeForceN = requestedBrakeForceN,
                effectiveBrakeForceN = thermallyLimitedBrakeForceN,
                demandDecelerationMS2 = demandDecelerationMS2,
                temperatureC = temperature.temperatureC,
                thermalEfficiency = temperature.thermalEfficiency,
                fadeActive = temperature.fadeActive,
                overheated = temperature.overheated,
                warningCode = string.Empty,
                warningMessage = string.Empty
            };

            ApplyBrakeTemperatureWarning(ref report, config);

            if (!aggressiveBraking)
            {
                return report;
            }

            float inputSeverity = Mathf.InverseLerp(
                config.aggressiveBrakeInputThreshold,
                1f,
                brakeInput);
            float decelSeverity = Mathf.InverseLerp(
                config.aggressiveBrakeDecelerationMS2,
                Mathf.Max(config.aggressiveBrakeDecelerationMS2 + 0.01f, config.aggressiveBrakeDecelerationMS2 * 1.75f),
                demandDecelerationMS2);
            report.severity = Mathf.Clamp01(Mathf.Max(report.severity, Mathf.Max(inputSeverity, decelSeverity)));
            report.hasWarning = true;
            if (string.IsNullOrEmpty(report.warningCode))
            {
                report.warningCode = "AGGRESSIVE_BRAKING";
                report.warningMessage = "Brake pedal applied too aggressively; wheel lock risk is increasing.";
            }

            if (!lockRisk)
            {
                return report;
            }

            if (config.enableABS)
            {
                float pulse = Mathf.PingPong(input.simulationTime * config.absPulseFrequency * 2f, 1f);
                float absMultiplier = Mathf.Lerp(config.absMinBrakeMultiplier, 1f, pulse);

                report.absActive = true;
                report.effectiveBrakeForceN = thermallyLimitedBrakeForceN * absMultiplier;
                report.warningCode = "ABS_ACTIVE";
                report.warningMessage = "ABS is modulating brake pressure to prevent wheel lock.";
                return report;
            }

            report.wheelsLocked = true;
            report.effectiveBrakeForceN = thermallyLimitedBrakeForceN * config.lockedWheelBrakeMultiplier;
            report.warningCode = "WHEEL_LOCK_STEERING_LOSS";
            report.warningMessage = "Wheels locked by excessive brake pressure while moving; steering control is temporarily lost.";
            report.severity = Mathf.Max(report.severity, 0.85f);

            return report;
        }

        private struct BrakeTemperatureReport
        {
            public float temperatureC;
            public float thermalEfficiency;
            public bool fadeActive;
            public bool overheated;
        }

        private static BrakeTemperatureReport CalculateBrakeTemperature(
            VehicleOutputSimulationInput input,
            VehicleOutputConfig config,
            float speedAbsMS,
            float requestedBrakeForceN,
            float deltaTime)
        {
            float previousTemperature = input.previousBrakeTemperatureC > 0f
                ? input.previousBrakeTemperatureC
                : config.brakeInitialTemperatureC;
            float speedFactor = Mathf.Clamp01(speedAbsMS / Mathf.Max(0.1f, config.theoreticalMaxSpeedKmh / 3.6f));
            float brakeLoadFactor = Mathf.Clamp01(requestedBrakeForceN / Mathf.Max(1f, config.brakeForceN));
            float heatGain =
                brakeLoadFactor *
                Mathf.Lerp(0.25f, 1.25f, speedFactor) *
                config.brakeHeatGainPerSecond *
                deltaTime;
            float cooling =
                (config.brakeBaseCoolingPerSecond + speedAbsMS * config.brakeSpeedCoolingPerSecond) *
                deltaTime;
            float nextTemperature = Mathf.Clamp(
                previousTemperature + heatGain - cooling,
                config.brakeAmbientTemperatureC,
                config.brakeMaxTemperatureC);

            float fadeSeverity = Mathf.InverseLerp(
                config.brakeFadeStartTemperatureC,
                config.brakeCriticalTemperatureC,
                nextTemperature);
            float thermalEfficiency = Mathf.Lerp(
                1f,
                config.brakeMinThermalEfficiency,
                Mathf.Clamp01(fadeSeverity));

            return new BrakeTemperatureReport
            {
                temperatureC = nextTemperature,
                thermalEfficiency = Mathf.Clamp01(thermalEfficiency),
                fadeActive = nextTemperature >= config.brakeFadeStartTemperatureC,
                overheated = nextTemperature >= config.brakeCriticalTemperatureC
            };
        }

        private static void ApplyBrakeTemperatureWarning(ref BrakeReport report, VehicleOutputConfig config)
        {
            if (report.temperatureC < config.brakeWarningTemperatureC)
            {
                return;
            }

            float severity = Mathf.InverseLerp(
                config.brakeWarningTemperatureC,
                config.brakeCriticalTemperatureC,
                report.temperatureC);

            report.hasWarning = true;
            report.severity = Mathf.Clamp01(Mathf.Max(report.severity, severity));

            if (report.overheated)
            {
                report.warningCode = "BRAKE_OVERHEAT_CRITICAL";
                report.warningMessage = "Brake temperature is critical; braking force is severely reduced.";
                report.severity = 1f;
                return;
            }

            if (report.fadeActive)
            {
                report.warningCode = "BRAKE_FADE";
                report.warningMessage = "Brake fade is active due to high temperature; braking force is reduced.";
                report.severity = Mathf.Max(report.severity, 0.65f);
                return;
            }

            report.warningCode = "BRAKE_TEMPERATURE_HIGH";
            report.warningMessage = "Brake temperature is high; sustained braking can reduce brake performance.";
            report.severity = Mathf.Max(report.severity, 0.35f);
        }

        private static float GetMovementDirection(float speedMS, int fallbackDirection)
        {
            if (Mathf.Abs(speedMS) > 0.05f)
            {
                return Mathf.Sign(speedMS);
            }

            if (fallbackDirection != 0)
            {
                return fallbackDirection;
            }

            return 1f;
        }

        private static float GetOppositeDirection(float speedMS)
        {
            if (Mathf.Abs(speedMS) <= 0.05f)
            {
                return 0f;
            }

            return -Mathf.Sign(speedMS);
        }

        private static float GetBrakeDirection(float speedMS, float tractionForceN)
        {
            if (Mathf.Abs(speedMS) > 0.001f)
            {
                return -Mathf.Sign(speedMS);
            }

            if (Mathf.Abs(tractionForceN) > 0.01f)
            {
                return -Mathf.Sign(tractionForceN);
            }

            return 0f;
        }

        private static bool ShouldSnapToFullStop(
            float signedSpeedMS,
            float tractionForceN,
            float brakeForceN,
            float brakeInput,
            VehicleOutputConfig config)
        {
            if (Mathf.Abs(signedSpeedMS) > config.fullStopSpeedThresholdMS)
            {
                return false;
            }

            bool brakingCanHold =
                brakeInput >= config.fullStopBrakeInputThreshold &&
                brakeForceN >= Mathf.Abs(tractionForceN);
            bool noMeaningfulTraction =
                Mathf.Abs(tractionForceN) <= config.fullStopTractionForceThresholdN;

            return brakingCanHold || noMeaningfulTraction;
        }

        private static SteeringReport CalculateSteering(
            VehicleOutputSimulationInput input,
            VehicleOutputConfig config,
            float signedSpeedMS,
            float deltaTime,
            bool wheelsLocked)
        {
            SteeringReport report = new SteeringReport
            {
                headingDegrees = NormalizeAngleDegrees(input.previousHeadingDegrees)
            };

            float speedAbsMS = Mathf.Abs(signedSpeedMS);
            float steerInput = Mathf.Clamp(input.steerInput, -1f, 1f);
            float inputDelta = Mathf.Abs(steerInput - Mathf.Clamp(input.previousSteerInput, -1f, 1f));
            float speedReduction = 1f / (1f + speedAbsMS * config.steeringSpeedSensitivity);
            float effectiveSteerInput = steerInput;
            float steeringAngle = steerInput * config.maxSteeringAngleDegrees * speedReduction;

            report.inputDelta = inputDelta;

            if (wheelsLocked && config.forceSteeringLossOnWheelLock)
            {
                steeringAngle = Mathf.Clamp(
                    steeringAngle * config.lockedWheelSteeringMultiplier,
                    -config.lockedWheelMaxSteeringAngleDegrees,
                    config.lockedWheelMaxSteeringAngleDegrees);

                float maxAvailableAngle = Mathf.Max(0.0001f, config.maxSteeringAngleDegrees * speedReduction);
                effectiveSteerInput = Mathf.Clamp(steeringAngle / maxAvailableAngle, -1f, 1f);
                report.steeringControlLost = true;
            }
            else if (wheelsLocked)
            {
                steeringAngle *= config.lockedWheelSteeringMultiplier;
                float maxAvailableAngle = Mathf.Max(0.0001f, config.maxSteeringAngleDegrees * speedReduction);
                effectiveSteerInput = Mathf.Clamp(steeringAngle / maxAvailableAngle, -1f, 1f);
                report.steeringControlLost = true;
            }

            report.effectiveSteerInput = effectiveSteerInput;
            report.steeringAngleDegrees = steeringAngle;
            ApplySteeringMisuseWarning(ref report, input, config, speedAbsMS, steerInput, inputDelta);

            if (speedAbsMS <= config.minSpeedForSteeringMS ||
                Mathf.Abs(steeringAngle) <= 0.01f)
            {
                report.yawRateDegreesPerSecond = 0f;
                report.turnRadiusMeters = 0f;
                return report;
            }

            float steeringAngleRad = steeringAngle * Mathf.Deg2Rad;
            float turnRadius = config.wheelBaseMeters / Mathf.Tan(Mathf.Abs(steeringAngleRad));
            float yawRateRadS = (signedSpeedMS / Mathf.Max(0.1f, config.wheelBaseMeters)) *
                Mathf.Tan(steeringAngleRad);

            report.yawRateDegreesPerSecond = yawRateRadS * Mathf.Rad2Deg;
            report.headingDegrees = NormalizeAngleDegrees(
                report.headingDegrees + report.yawRateDegreesPerSecond * deltaTime);
            report.turnRadiusMeters = Mathf.Abs(turnRadius);

            return report;
        }

        private static void ApplySteeringMisuseWarning(
            ref SteeringReport report,
            VehicleOutputSimulationInput input,
            VehicleOutputConfig config,
            float speedAbsMS,
            float steerInput,
            float inputDelta)
        {
            if (speedAbsMS < config.aggressiveSteeringMinSpeedMS)
            {
                return;
            }

            if (Mathf.Abs(steerInput) < config.aggressiveSteeringInputThreshold ||
                inputDelta < config.aggressiveSteeringDeltaThreshold)
            {
                return;
            }

            float speedSeverity = Mathf.InverseLerp(
                config.aggressiveSteeringMinSpeedMS,
                Mathf.Max(config.aggressiveSteeringMinSpeedMS + 0.01f, config.theoreticalMaxSpeedKmh / 3.6f),
                speedAbsMS);
            float inputSeverity = Mathf.InverseLerp(
                config.aggressiveSteeringInputThreshold,
                1f,
                Mathf.Abs(steerInput));
            float deltaSeverity = Mathf.InverseLerp(
                config.aggressiveSteeringDeltaThreshold,
                2f,
                inputDelta);

            report.hasWarning = true;
            report.severity = Mathf.Clamp01(Mathf.Max(speedSeverity, inputSeverity, deltaSeverity));
            report.warningCode = "AGGRESSIVE_STEERING_HIGH_SPEED";
            report.warningMessage = "Abrupt steering input at high speed; vehicle stability risk is increasing.";
        }

        private static float NormalizeAngleDegrees(float angle)
        {
            float normalized = Mathf.Repeat(angle + 180f, 360f) - 180f;
            return Mathf.Approximately(normalized, -180f) ? 180f : normalized;
        }
    }
}
