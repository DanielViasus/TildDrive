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
            float brakeForceN = Mathf.Clamp01(input.brakeInput) * config.brakeForceN;
            float engineBrakeForceN = CalculateEngineBrakeForce(input, config, wheelRadius, speedAbsMS);

            float resistanceForceN =
                (aerodynamicDragForceN + rollingResistanceForceN + brakeForceN + engineBrakeForceN) *
                dragDirection;

            float netForceN = tractionForceN + resistanceForceN;
            float accelerationMS2 = netForceN / config.vehicleMassKg;
            float signedSpeedMS = previousSpeedMS + accelerationMS2 * deltaTime;

            if (Mathf.Abs(previousSpeedMS) > 0f &&
                Mathf.Sign(previousSpeedMS) != Mathf.Sign(signedSpeedMS) &&
                Mathf.Abs(tractionForceN) < Mathf.Abs(resistanceForceN))
            {
                signedSpeedMS = 0f;
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
            float simulationSpeed = config.speedDisplayUnit == VehicleSpeedUnit.MilesPerHour
                ? signedSpeedMPH
                : signedSpeedKMH;

            float powerKW = input.transmissionOutputTorqueNm * Mathf.Abs(wheelAngularVelocityRadS) / 1000f;
            float powerHP = powerKW * KwToHp;

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
            state.outputPowerKW = Mathf.Max(0f, powerKW);
            state.outputPowerHP = Mathf.Max(0f, powerHP);
            state.tractionForceN = tractionForceN;

            state.accelerationMS2 = accelerationMS2;
            state.brakeInput = Mathf.Clamp01(input.brakeInput);
            state.aerodynamicDragForceN = aerodynamicDragForceN;
            state.rollingResistanceForceN = rollingResistanceForceN;
            state.brakeForceN = brakeForceN;
            state.engineBrakeForceN = engineBrakeForceN;

            state.driveDirection = Mathf.Abs(signedSpeedMS) > 0.05f
                ? (int)Mathf.Sign(signedSpeedMS)
                : input.driveDirection;
            state.isMoving = absoluteSpeedKMH > 0.1f;
            state.lastUpdateTime = input.simulationTime;

            output.diagnosticMessage = state.isMoving ? "VEHICLE OUTPUT MOVING" : "VEHICLE OUTPUT REST";
            return output;
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
    }
}
