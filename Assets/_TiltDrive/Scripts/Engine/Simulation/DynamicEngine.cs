using UnityEngine;
using TiltDrive.Simulation;

namespace TiltDrive.EngineSystem
{
    public class DynamicEngine
    {
        public EngineSimulationOutput Simulate(EngineSimulationInput input)
        {
            EngineSimulationOutput output = new EngineSimulationOutput();

            if (input == null || !input.IsValid())
            {
                output.Reset();
                output.diagnosticMessage = "INVALID ENGINE INPUT";
                return output;
            }

            EngineConfig config = input.engineConfig;
            config.ClampValues();
            DriveLaunchDiagnosticsConfig launchConfig =
                input.launchDiagnosticsConfig ?? new DriveLaunchDiagnosticsConfig();
            launchConfig.ClampValues();

            output.ApplyConfig(config);

            output.engineOn = input.engineOn;
            output.engineStarting = input.engineStarting;
            output.engineShuttingDown = input.engineShuttingDown;
            output.engineStalled = input.engineStalled;
            output.currentRPM = Mathf.Max(0f, input.currentRPM);
            output.targetRPM = output.currentRPM;
            output.componentHealthPercent = Mathf.Clamp(input.componentHealthPercent, 0f, 100f);
            output.accumulatedDamagePercent = Mathf.Max(0f, input.accumulatedDamagePercent);
            output.engineTemperatureC = input.engineTemperatureC > 0f
                ? input.engineTemperatureC
                : config.engineInitialTemperatureC;
            output.thermalEfficiency = Mathf.Clamp01(input.thermalEfficiency);
            output.engineStartHoldSeconds = Mathf.Max(0f, input.engineStartHoldSeconds);
            output.requiredStartHoldSeconds = CalculateRequiredStartHoldSeconds(config, output.componentHealthPercent);
            output.simulationTick = 0;
            output.lastUpdateTime = input.simulationTime;

            float deltaTime = Mathf.Max(0.0001f, input.deltaTime);

            // --------------------------------------------------
            // 1. TOGGLE DE ENCENDIDO / APAGADO
            // --------------------------------------------------
            // Regla:
            // - Si está en transición, ignoramos nuevas pulsaciones.
            // - Si está apagado y se pulsa, inicia arranque.
            // - Si está encendido y se pulsa, inicia apagado.
            if (!input.engineStarting && !input.engineShuttingDown)
            {
                if (!input.engineOn && input.engineStartHeld && input.ignitionAllowed)
                {
                    output.engineOn = false;
                    output.engineStarting = true;
                    output.engineShuttingDown = false;
                    output.engineStalled = false;
                    output.engineStartHoldSeconds = 0f;
                    output.requiredStartHoldSeconds = CalculateRequiredStartHoldSeconds(config, output.componentHealthPercent);

                    // Si viene de motor totalmente apagado, arrancamos desde 0.
                    output.currentRPM = Mathf.Max(0f, input.currentRPM);
                    output.targetRPM = config.idleRPM;
                }
                else if (input.engineStartPressed && input.engineOn)
                {
                    output.engineOn = false;
                    output.engineStarting = false;
                    output.engineShuttingDown = true;
                    output.engineStartHoldSeconds = 0f;
                    output.targetRPM = 0f;
                }
            }

            // --------------------------------------------------
            // 2. TRANSICION DE ARRANQUE
            // --------------------------------------------------
            if (output.engineStarting)
            {
                output.targetRPM = config.idleRPM;
                output.requiredStartHoldSeconds = CalculateRequiredStartHoldSeconds(config, output.componentHealthPercent);

                if (!input.engineStartHeld)
                {
                    output.engineStarting = false;
                    output.engineOn = false;
                    output.engineShuttingDown = false;
                    output.engineStartHoldSeconds = 0f;
                    output.currentRPM = Mathf.MoveTowards(
                        output.currentRPM,
                        0f,
                        config.engineShutdownRPMSpeed * deltaTime);
                    output.engineLoad = 0f;
                    output.engineTorqueNm = 0f;
                    output.engineBrakeTorqueNm = 0f;
                    ApplyEngineTemperature(output, input, config, deltaTime);
                    output.diagnosticMessage = "ENGINE START ABORTED";
                    output.EvaluateFlags();
                    return output;
                }

                output.engineStartHoldSeconds += deltaTime;
                ApplyStarterOveruseWarning(output, config);

                float healthFactor = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(output.componentHealthPercent / 100f));
                float startStep = config.engineStartRPMSpeed * healthFactor * deltaTime;
                output.currentRPM = Mathf.MoveTowards(output.currentRPM, config.idleRPM, startStep);

                output.engineLoad = 0f;
                output.engineTorqueNm = 0f;
                output.engineBrakeTorqueNm = 0f;

                if (output.currentRPM >= config.idleRPM - 1f &&
                    output.engineStartHoldSeconds >= output.requiredStartHoldSeconds)
                {
                    output.currentRPM = config.idleRPM;
                    output.targetRPM = config.idleRPM;
                    output.engineStarting = false;
                    output.engineOn = true;
                    output.engineShuttingDown = false;
                    output.engineStalled = false;
                    output.engineStartHoldSeconds = 0f;
                    output.diagnosticMessage = "ENGINE ON / IDLE";
                }
                else
                {
                    output.diagnosticMessage = output.starterOveruseWarning
                        ? "STARTER OVERUSE WARNING"
                        : "ENGINE CRANKING";
                }

                ApplyEngineTemperature(output, input, config, deltaTime);
                if (TryApplyThermalProtectionShutdown(output))
                {
                    return output;
                }
                output.EvaluateFlags();
                return output;
            }

            // --------------------------------------------------
            // 3. TRANSICION DE APAGADO
            // --------------------------------------------------
            if (output.engineShuttingDown)
            {
                output.targetRPM = 0f;

                float shutdownStep = config.engineShutdownRPMSpeed * deltaTime;
                output.currentRPM = Mathf.MoveTowards(output.currentRPM, 0f, shutdownStep);

                output.engineLoad = 0f;
                output.engineTorqueNm = 0f;
                output.engineBrakeTorqueNm = 0f;

                if (output.currentRPM <= 1f)
                {
                    output.currentRPM = 0f;
                    output.targetRPM = 0f;
                    output.engineOn = false;
                    output.engineStarting = false;
                    output.engineShuttingDown = false;
                    output.engineStalled = false;
                    output.diagnosticMessage = "ENGINE OFF";
                }
                else
                {
                    output.diagnosticMessage = "ENGINE SHUTTING DOWN";
                }

                ApplyEngineTemperature(output, input, config, deltaTime);
                output.EvaluateFlags();
                return output;
            }

            // --------------------------------------------------
            // 4. MOTOR APAGADO SIN TRANSICION
            // --------------------------------------------------
            if (!output.engineOn)
            {
                output.currentRPM = 0f;
                output.targetRPM = 0f;
                output.engineLoad = 0f;
                output.engineTorqueNm = 0f;
                output.engineBrakeTorqueNm = 0f;
                ApplyEngineTemperature(output, input, config, deltaTime);
                output.EvaluateFlags();
                output.diagnosticMessage = "ENGINE OFF";
                return output;
            }

            // --------------------------------------------------
            // 5. CARGA EXTERNA ABSTRACTA
            // --------------------------------------------------
            float externalLoad = 0f;

            if (input.useExternalLoad)
            {
                float transmissionLoad = input.transmissionLoad * config.loadSensitivity;
                float massLoad = (input.vehicleMassKg / 1000f) * config.massInfluence;
                float slopeLoad = Mathf.Max(0f, input.slopeAngleDegrees / 45f) * config.slopeSensitivity;
                float resistanceLoad = input.additionalResistance * config.loadSensitivity;

                externalLoad = transmissionLoad + massLoad + slopeLoad + resistanceLoad;
            }

            output.engineLoad = Mathf.Max(0f, externalLoad);

            // --------------------------------------------------
            // 6. RPM OBJETIVO
            // --------------------------------------------------
            float throttleFactor = CalculateThrottleFactor(input.throttleInput, config.throttleResponsiveness);
            float unclampedTargetRPM = config.idleRPM + (config.maxRPM - config.idleRPM) * throttleFactor;

            float loadRPMPenalty = externalLoad * 350f;
            float targetRPM = unclampedTargetRPM - loadRPMPenalty;
            targetRPM = Mathf.Max(0f, targetRPM);

            if (input.throttleInput <= 0.01f)
            {
                targetRPM = Mathf.Max(config.idleRPM - (externalLoad * 150f), 0f);
            }

            bool drivetrainCoupled = TryCalculateDrivetrainCoupledRPM(input, config, out float drivetrainCoupledRPM);
            float drivetrainTargetRPM = targetRPM;
            if (drivetrainCoupled)
            {
                drivetrainTargetRPM = CalculateDrivetrainTargetRPM(
                    input,
                    config,
                    launchConfig,
                    drivetrainCoupledRPM);
                targetRPM = Mathf.Lerp(targetRPM, drivetrainTargetRPM, Mathf.Clamp01(input.clutchEngagement));
            }

            LaunchClutchFrictionReport clutchFriction = CalculateLaunchClutchFriction(
                input,
                config,
                launchConfig,
                targetRPM,
                drivetrainCoupledRPM,
                drivetrainCoupled);

            DriveMisuseDiagnostics.LaunchReport launchReport =
                DriveMisuseDiagnostics.AnalyzeLaunchOperation(
                    input.currentGear,
                    input.vehicleSpeedMS,
                    input.throttleInput,
                    input.brakeInput,
                    input.clutchInput,
                    input.clutchEngagement,
                    output.currentRPM,
                    config.idleRPM,
                    config.stallRPM,
                    launchConfig);
            ApplyLaunchReport(output, launchReport);

            targetRPM = Mathf.Max(0f, targetRPM - clutchFriction.rpmDrop);
            drivetrainTargetRPM = Mathf.Max(0f, drivetrainTargetRPM - clutchFriction.rpmDrop);
            output.clutchFrictionRPMDrop = clutchFriction.rpmDrop;
            output.clutchFrictionLoad = clutchFriction.load;
            output.idleLaunchAssistFactor = clutchFriction.idleAssistFactor;
            output.engineLoad = Mathf.Max(0f, externalLoad + clutchFriction.load);

            if (launchReport.hasWarning)
            {
                targetRPM = Mathf.Max(0f, targetRPM - launchReport.rpmPenalty);
                drivetrainTargetRPM = Mathf.Max(0f, drivetrainTargetRPM - launchReport.rpmPenalty);
            }

            if (clutchFriction.idleAssistFactor > 0.01f &&
                input.throttleInput <= launchConfig.idleOnlyThrottleThreshold &&
                Mathf.Abs(input.vehicleSpeedMS) <= launchConfig.launchSpeedThresholdMS)
            {
                float maxNoThrottleRPM = Mathf.Max(
                    config.stallRPM,
                    input.currentRPM - clutchFriction.rpmDrop * Mathf.Clamp01(input.clutchEngagement));
                targetRPM = Mathf.Min(targetRPM, maxNoThrottleRPM);
            }

            output.targetRPM = targetRPM;

            // --------------------------------------------------
            // 7. INERCIA DE SUBIDA Y BAJADA
            // --------------------------------------------------
            float currentRPM = Mathf.Max(0f, input.currentRPM);
            float inertia = Mathf.Max(0.01f, config.engineInertia);

            float riseRate = config.rpmRiseSpeed / inertia;
            float fallRate = config.rpmFallSpeed / inertia;

            float newRPM;

            if (targetRPM > currentRPM)
            {
                newRPM = Mathf.MoveTowards(currentRPM, targetRPM, riseRate * deltaTime);
            }
            else
            {
                newRPM = Mathf.MoveTowards(currentRPM, targetRPM, fallRate * deltaTime);
            }

            float engineBrakeTorque = 0f;
            if (input.throttleInput <= 0.01f && newRPM > config.idleRPM)
            {
                engineBrakeTorque = config.engineBrakeStrength *
                                    ((newRPM - config.idleRPM) / Mathf.Max(1f, config.maxRPM - config.idleRPM));

                newRPM -= engineBrakeTorque * deltaTime * 10f;
            }

            if (drivetrainCoupled)
            {
                float coupling = Mathf.Clamp01(input.clutchEngagement);
                newRPM = coupling >= 0.95f
                    ? drivetrainTargetRPM
                    : Mathf.Lerp(newRPM, drivetrainTargetRPM, coupling);
            }

            if (clutchFriction.idleAssistFactor > 0.01f &&
                input.throttleInput <= launchConfig.idleOnlyThrottleThreshold &&
                Mathf.Abs(input.vehicleSpeedMS) <= launchConfig.launchSpeedThresholdMS)
            {
                float maxNoThrottleRPM = Mathf.Max(
                    config.stallRPM,
                    input.currentRPM - clutchFriction.rpmDrop * Mathf.Clamp01(input.clutchEngagement));
                newRPM = Mathf.Min(newRPM, maxNoThrottleRPM);
            }

            bool revLimiterActive = TryApplyRevLimiter(
                input,
                config,
                drivetrainCoupled,
                drivetrainCoupledRPM,
                ref newRPM,
                out float revLimiterTorqueFactor);

            newRPM = Mathf.Max(0f, newRPM);
            output.currentRPM = newRPM;
            output.engineBrakeTorqueNm = Mathf.Max(0f, engineBrakeTorque);
            output.revLimiterActive = revLimiterActive;
            output.revLimiterTorqueFactor = revLimiterTorqueFactor;
            ApplyEngineTemperature(output, input, config, deltaTime);
            if (TryApplyThermalProtectionShutdown(output))
            {
                return output;
            }

            if (input.canStall && launchReport.forceStall)
            {
                output.engineOn = false;
                output.engineStarting = false;
                output.engineShuttingDown = false;
                output.engineStalled = true;
                output.currentRPM = 0f;
                output.targetRPM = 0f;
                output.engineTorqueNm = 0f;
                output.engineBrakeTorqueNm = 0f;
                output.EvaluateFlags();
                output.diagnosticMessage = $"STALL BY BAD LAUNCH: {launchReport.code}";
                return output;
            }

            // --------------------------------------------------
            // 8. TORQUE BASE APROXIMADO
            // --------------------------------------------------
            float rpmNormalizedToPeak =
                1f - Mathf.Clamp01(Mathf.Abs(newRPM - config.peakTorqueRPM) / Mathf.Max(1f, config.maxRPM));

            float torqueCurveFactor = Mathf.Clamp01(0.35f + rpmNormalizedToPeak);
            float throttleTorqueFactor = Mathf.Max(
                Mathf.Clamp01(input.throttleInput),
                clutchFriction.idleAssistFactor);
            float rawTorque = config.baseTorqueNm * torqueCurveFactor * throttleTorqueFactor;

            float loadTorquePenalty = externalLoad * 15f;
            float finalTorque = Mathf.Max(0f, rawTorque - loadTorquePenalty);
            finalTorque *= revLimiterTorqueFactor;
            finalTorque *= Mathf.Clamp01(output.componentHealthPercent / 100f);
            finalTorque *= Mathf.Clamp01(output.thermalEfficiency);

            output.engineTorqueNm = finalTorque;

            // --------------------------------------------------
            // 9. APAGADO POR RPM MINIMAS
            // --------------------------------------------------
            if (input.canStall && output.engineOn && output.currentRPM < config.stallRPM)
            {
                output.engineOn = false;
                output.engineStarting = false;
                output.engineShuttingDown = false;
                output.engineStalled = true;
                output.currentRPM = 0f;
                output.targetRPM = 0f;
                output.engineTorqueNm = 0f;
                output.engineBrakeTorqueNm = 0f;
                output.EvaluateFlags();
                output.diagnosticMessage = "STALL BY LOW RPM";
                return output;
            }

            // --------------------------------------------------
            // 10. LIMITES Y ALERTAS
            // --------------------------------------------------
            output.currentRPM = Mathf.Min(output.currentRPM, config.criticalRPM * 1.15f);
            output.EvaluateFlags();

            if (output.isInCriticalZone)
            {
                output.diagnosticMessage = "CRITICAL RPM";
            }
            else if (output.engineOverheated)
            {
                output.diagnosticMessage = "ENGINE OVERHEAT CRITICAL";
            }
            else if (output.engineThermalDerateActive)
            {
                output.diagnosticMessage = "ENGINE THERMAL DERATE";
            }
            else if (output.engineTemperatureWarning)
            {
                output.diagnosticMessage = "ENGINE TEMPERATURE WARNING";
            }
            else if (output.isOverRevving)
            {
                output.diagnosticMessage = "OVERREV WARNING";
            }
            else if (revLimiterActive)
            {
                output.diagnosticMessage = "REV LIMITER ACTIVE";
            }
            else if (launchReport.hasWarning)
            {
                output.diagnosticMessage = launchReport.stallRisk
                    ? "LAUNCH STALL RISK"
                    : "LAUNCH TECHNIQUE WARNING";
            }
            else if (clutchFriction.idleAssistFactor > 0.01f && input.throttleInput <= launchConfig.idleOnlyThrottleThreshold)
            {
                output.diagnosticMessage = "IDLE CLUTCH LAUNCH";
            }
            else if (clutchFriction.load > 0.01f)
            {
                output.diagnosticMessage = "CLUTCH FRICTION LOAD";
            }
            else if (drivetrainCoupled)
            {
                output.diagnosticMessage = "ENGINE RPM DRIVETRAIN COUPLED";
            }
            else if (input.throttleInput > 0.01f)
            {
                output.diagnosticMessage = "ENGINE ACCELERATING";
            }
            else
            {
                output.diagnosticMessage = "ENGINE IDLE / COAST";
            }

            return output;
        }

        private struct LaunchClutchFrictionReport
        {
            public float load;
            public float rpmDrop;
            public float idleAssistFactor;
        }

        private static void ApplyLaunchReport(
            EngineSimulationOutput output,
            DriveMisuseDiagnostics.LaunchReport report)
        {
            output.hasLaunchWarning = report.hasWarning;
            output.hasLaunchMisuse = report.hasMisuse;
            output.launchStallRisk = report.stallRisk;
            output.lastLaunchWarningCode = report.code;
            output.lastLaunchWarningMessage = report.message;
            output.lastLaunchSeverity = report.severity;
        }

        private static float CalculateRequiredStartHoldSeconds(EngineConfig config, float healthPercent)
        {
            float healthFactor = Mathf.Clamp01(healthPercent / 100f);
            return config.minimumStartHoldSeconds +
                config.damagedEngineStartExtraSeconds * (1f - healthFactor);
        }

        private static void ApplyStarterOveruseWarning(
            EngineSimulationOutput output,
            EngineConfig config)
        {
            output.starterOveruseWarning = false;
            output.lastStarterWarningCode = string.Empty;
            output.lastStarterWarningMessage = string.Empty;
            output.lastStarterSeverity = 0f;

            if (output.engineStartHoldSeconds < config.starterOveruseWarningSeconds)
            {
                return;
            }

            output.starterOveruseWarning = true;
            output.lastStarterWarningCode = "STARTER_HELD_TOO_LONG";
            output.lastStarterWarningMessage = "Engine starter is being held too long; release the start button to avoid starter and battery stress.";
            output.lastStarterSeverity = Mathf.InverseLerp(
                config.starterOveruseWarningSeconds,
                config.starterOveruseCriticalSeconds,
                output.engineStartHoldSeconds);
        }

        private static void ApplyEngineTemperature(
            EngineSimulationOutput output,
            EngineSimulationInput input,
            EngineConfig config,
            float deltaTime)
        {
            float previousTemperature = input.engineTemperatureC > 0f
                ? input.engineTemperatureC
                : config.engineInitialTemperatureC;
            float speedAbsMS = Mathf.Abs(input.vehicleSpeedMS);
            bool engineProducingHeat = output.engineOn || output.engineStarting || output.currentRPM > 1f;

            float heatGain = 0f;
            if (engineProducingHeat)
            {
                float rpmFactor = Mathf.InverseLerp(
                    config.idleRPM,
                    Mathf.Max(config.idleRPM + 1f, config.maxRPM),
                    output.currentRPM);
                float highRPMFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, rpmFactor));
                float throttleFactor = Mathf.Clamp01(input.throttleInput);
                float normalizedExternalLoad = Mathf.Clamp01(output.engineLoad / 3f);
                float loadFactor = Mathf.Clamp01(
                    normalizedExternalLoad * Mathf.Lerp(0.08f, 1f, throttleFactor) +
                    output.clutchFrictionLoad);
                float overRevFactor = Mathf.InverseLerp(
                    config.maxRPM,
                    Mathf.Max(config.maxRPM + 1f, config.criticalRPM),
                    output.currentRPM);
                float limiterHeatFactor = output.revLimiterActive ? 0.45f : 0f;
                float launchMisuseHeat = output.hasLaunchMisuse ? output.lastLaunchSeverity * 0.35f : 0f;
                float starterHeat = output.engineStarting ? 0.25f : 0f;
                bool lowStressIdle =
                    throttleFactor <= 0.02f &&
                    highRPMFactor <= 0.01f &&
                    loadFactor <= 0.08f &&
                    !output.revLimiterActive &&
                    overRevFactor <= 0.01f;

                if (lowStressIdle)
                {
                    float idleThermalMoveSpeed = previousTemperature > config.engineNormalOperatingTemperatureC
                        ? config.engineBaseCoolingPerSecond * Mathf.Clamp(input.radiatorCoolingEfficiency, 0.18f, 1.5f)
                        : config.engineBaseHeatPerSecond;
                    float idleTemperature = Mathf.MoveTowards(
                        previousTemperature,
                        config.engineNormalOperatingTemperatureC,
                        idleThermalMoveSpeed * deltaTime);
                    ApplyEngineTemperatureStatus(output, config, idleTemperature);
                    return;
                }

                heatGain =
                    config.engineBaseHeatPerSecond * Mathf.Clamp01(1f - Mathf.InverseLerp(config.engineAmbientTemperatureC, config.engineNormalOperatingTemperatureC, previousTemperature)) +
                    Mathf.Pow(Mathf.Clamp01(highRPMFactor), 1.25f) * config.engineRPMHeatPerSecond +
                    loadFactor * config.engineLoadHeatPerSecond +
                    Mathf.Max(overRevFactor, limiterHeatFactor, launchMisuseHeat, starterHeat) * config.engineOverRevHeatPerSecond;
            }

            float cooling =
                config.engineBaseCoolingPerSecond * Mathf.Clamp(input.radiatorCoolingEfficiency, 0.18f, 1.5f) +
                speedAbsMS * config.engineSpeedCoolingPerSecond * Mathf.Clamp(input.radiatorAirflowEfficiency, 0.12f, 1.5f) +
                Mathf.Max(0f, previousTemperature - config.engineNormalOperatingTemperatureC) * 0.22f;
            float targetFloor = engineProducingHeat
                ? config.engineNormalOperatingTemperatureC
                : config.engineAmbientTemperatureC;
            float nextTemperature = previousTemperature + (heatGain - cooling) * deltaTime;

            if (engineProducingHeat && nextTemperature < targetFloor)
            {
                nextTemperature = Mathf.MoveTowards(
                    previousTemperature,
                    targetFloor,
                    config.engineBaseHeatPerSecond * deltaTime);
            }

            if (!engineProducingHeat)
            {
                nextTemperature = Mathf.MoveTowards(
                    previousTemperature,
                    config.engineAmbientTemperatureC,
                    cooling * deltaTime);
            }

            nextTemperature = Mathf.Clamp(
                nextTemperature,
                config.engineAmbientTemperatureC,
                config.engineMaxTemperatureC);

            float derateSeverity = Mathf.InverseLerp(
                config.engineEfficiencyDropStartTemperatureC,
                config.engineCriticalTemperatureC,
                nextTemperature);
            float thermalEfficiency = Mathf.Lerp(
                1f,
                config.engineMinThermalEfficiency,
                Mathf.Clamp01(derateSeverity));

            output.engineTemperatureC = nextTemperature;
            output.thermalEfficiency = Mathf.Clamp01(thermalEfficiency);
            output.engineThermalDerateActive = nextTemperature >= config.engineEfficiencyDropStartTemperatureC;
            output.engineOverheated = nextTemperature >= config.engineCriticalTemperatureC;
            output.engineTemperatureWarning = nextTemperature >= config.engineWarningTemperatureC;
            output.lastTemperatureWarningCode = string.Empty;
            output.lastTemperatureWarningMessage = string.Empty;
            output.lastTemperatureSeverity = 0f;

            if (!output.engineTemperatureWarning)
            {
                return;
            }

            output.lastTemperatureSeverity = Mathf.Clamp01(Mathf.InverseLerp(
                config.engineWarningTemperatureC,
                config.engineCriticalTemperatureC,
                nextTemperature));

            if (output.engineOverheated)
            {
                output.lastTemperatureWarningCode = "ENGINE_OVERHEAT_CRITICAL";
                output.lastTemperatureWarningMessage = "Engine temperature is critical; torque output is severely reduced.";
                output.lastTemperatureSeverity = 1f;
                return;
            }

            if (output.engineThermalDerateActive)
            {
                output.lastTemperatureWarningCode = "ENGINE_THERMAL_DERATE";
                output.lastTemperatureWarningMessage = "Engine is losing efficiency due to high temperature.";
                output.lastTemperatureSeverity = Mathf.Max(output.lastTemperatureSeverity, 0.65f);
                return;
            }

            output.lastTemperatureWarningCode = "ENGINE_TEMPERATURE_HIGH";
            output.lastTemperatureWarningMessage = "Engine temperature is high; sustained abuse can reduce performance.";
            output.lastTemperatureSeverity = Mathf.Max(output.lastTemperatureSeverity, 0.35f);
        }

        private static void ApplyEngineTemperatureStatus(
            EngineSimulationOutput output,
            EngineConfig config,
            float temperatureC)
        {
            float derateSeverity = Mathf.InverseLerp(
                config.engineEfficiencyDropStartTemperatureC,
                config.engineCriticalTemperatureC,
                temperatureC);
            float thermalEfficiency = Mathf.Lerp(
                1f,
                config.engineMinThermalEfficiency,
                Mathf.Clamp01(derateSeverity));

            output.engineTemperatureC = Mathf.Clamp(
                temperatureC,
                config.engineAmbientTemperatureC,
                config.engineMaxTemperatureC);
            output.thermalEfficiency = Mathf.Clamp01(thermalEfficiency);
            output.engineThermalDerateActive = output.engineTemperatureC >= config.engineEfficiencyDropStartTemperatureC;
            output.engineOverheated = output.engineTemperatureC >= config.engineCriticalTemperatureC;
            output.engineTemperatureWarning = output.engineTemperatureC >= config.engineWarningTemperatureC;
            output.lastTemperatureWarningCode = string.Empty;
            output.lastTemperatureWarningMessage = string.Empty;
            output.lastTemperatureSeverity = 0f;

            if (!output.engineTemperatureWarning)
            {
                return;
            }

            output.lastTemperatureSeverity = Mathf.Clamp01(Mathf.InverseLerp(
                config.engineWarningTemperatureC,
                config.engineCriticalTemperatureC,
                output.engineTemperatureC));

            if (output.engineOverheated)
            {
                output.lastTemperatureWarningCode = "ENGINE_OVERHEAT_CRITICAL";
                output.lastTemperatureWarningMessage = "Engine temperature is critical; torque output is severely reduced.";
                output.lastTemperatureSeverity = 1f;
                return;
            }

            if (output.engineThermalDerateActive)
            {
                output.lastTemperatureWarningCode = "ENGINE_THERMAL_DERATE";
                output.lastTemperatureWarningMessage = "Engine is losing efficiency due to high temperature.";
                output.lastTemperatureSeverity = Mathf.Max(output.lastTemperatureSeverity, 0.65f);
                return;
            }

            output.lastTemperatureWarningCode = "ENGINE_TEMPERATURE_HIGH";
            output.lastTemperatureWarningMessage = "Engine temperature is high; sustained abuse can reduce performance.";
            output.lastTemperatureSeverity = Mathf.Max(output.lastTemperatureSeverity, 0.35f);
        }

        private static bool TryApplyThermalProtectionShutdown(EngineSimulationOutput output)
        {
            if (!output.engineOverheated)
            {
                return false;
            }

            output.engineOn = false;
            output.engineStarting = false;
            output.engineShuttingDown = true;
            output.engineStalled = false;
            output.targetRPM = 0f;
            output.engineTorqueNm = 0f;
            output.engineBrakeTorqueNm = 0f;
            output.diagnosticMessage = "ENGINE OVERHEAT PROTECTION SHUTDOWN";
            output.EvaluateFlags();
            return true;
        }

        private static bool TryCalculateDrivetrainCoupledRPM(
            EngineSimulationInput input,
            EngineConfig config,
            out float coupledRPM)
        {
            coupledRPM = 0f;

            if (!input.useDrivetrainRPMCoupling ||
                input.currentGear == 0 ||
                input.totalDriveRatio <= 0f ||
                input.clutchEngagement <= 0.05f ||
                input.wheelCircumferenceMeters <= 0f)
            {
                return false;
            }

            float wheelRPM = Mathf.Abs(input.vehicleSpeedMS) / input.wheelCircumferenceMeters * 60f;
            coupledRPM = Mathf.Min(wheelRPM * input.totalDriveRatio, config.criticalRPM * 1.15f);
            return true;
        }

        private static float CalculateDrivetrainTargetRPM(
            EngineSimulationInput input,
            EngineConfig config,
            DriveLaunchDiagnosticsConfig launchConfig,
            float drivetrainCoupledRPM)
        {
            if (drivetrainCoupledRPM >= config.idleRPM ||
                !IsLaunchClutchActive(input, launchConfig))
            {
                return Mathf.Max(config.idleRPM, drivetrainCoupledRPM);
            }

            float lockupFactor = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(
                    launchConfig.idleLockupEngagementStart,
                    1f,
                    Mathf.Clamp01(input.clutchEngagement)));

            return Mathf.Lerp(config.idleRPM, drivetrainCoupledRPM, lockupFactor);
        }

        private static LaunchClutchFrictionReport CalculateLaunchClutchFriction(
            EngineSimulationInput input,
            EngineConfig config,
            DriveLaunchDiagnosticsConfig launchConfig,
            float targetRPM,
            float drivetrainCoupledRPM,
            bool drivetrainCoupled)
        {
            LaunchClutchFrictionReport report = new LaunchClutchFrictionReport();

            if (!drivetrainCoupled || !IsLaunchClutchActive(input, launchConfig))
            {
                return report;
            }

            float engagement = Mathf.Clamp01(input.clutchEngagement);
            float speedAbsMS = Mathf.Abs(input.vehicleSpeedMS);
            float speedRelief = 1f - Mathf.Clamp01(
                speedAbsMS / Mathf.Max(0.01f, launchConfig.launchSpeedThresholdMS));
            float biteProgress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(
                    launchConfig.biteEngagementMin,
                    launchConfig.clutchDumpEngagement,
                    engagement));
            float throttleRelief = Mathf.Clamp01(
                input.throttleInput / Mathf.Max(0.01f, launchConfig.minThrottleForClutchDump));
            float rpmMismatch = Mathf.Max(0f, targetRPM - drivetrainCoupledRPM);
            float rpmMismatchFactor = Mathf.Clamp01(rpmMismatch / Mathf.Max(1f, config.idleRPM));

            report.load = Mathf.Clamp01(
                biteProgress *
                speedRelief *
                rpmMismatchFactor *
                Mathf.Lerp(1f, 0.35f, throttleRelief));
            bool suitableIdleLaunchGear =
                input.currentGear < 0 || input.currentGear <= launchConfig.maxRecommendedLaunchGear;
            bool idleOnlyAttempt = launchConfig.allowIdleOnlyLaunch &&
                suitableIdleLaunchGear &&
                input.throttleInput <= launchConfig.idleOnlyThrottleThreshold;

            if (idleOnlyAttempt)
            {
                float gripWindow = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        launchConfig.biteEngagementMin,
                        launchConfig.biteEngagementMax,
                        engagement));
                float rpmReserve = Mathf.InverseLerp(
                    config.stallRPM,
                    Mathf.Max(config.stallRPM + 1f, config.idleRPM),
                    input.currentRPM);
                float idleOnlyLoad = gripWindow *
                    speedRelief *
                    Mathf.Lerp(0.45f, 1f, engagement) *
                    Mathf.Lerp(0.55f, 1f, rpmReserve);

                report.load = Mathf.Max(report.load, Mathf.Clamp01(idleOnlyLoad));

                report.idleAssistFactor = Mathf.Clamp01(
                    launchConfig.idleLaunchTorqueFactor *
                    gripWindow *
                    speedRelief *
                    rpmReserve);
            }

            report.rpmDrop = launchConfig.maxClutchFrictionRPMDrop * report.load;

            return report;
        }

        private static bool IsLaunchClutchActive(
            EngineSimulationInput input,
            DriveLaunchDiagnosticsConfig launchConfig)
        {
            return input.currentGear != 0 &&
                input.totalDriveRatio > 0f &&
                Mathf.Abs(input.vehicleSpeedMS) <= launchConfig.launchSpeedThresholdMS &&
                Mathf.Clamp01(input.clutchEngagement) >= launchConfig.biteEngagementMin;
        }

        private static float CalculateThrottleFactor(float throttleInput, float throttleResponsiveness)
        {
            float normalizedThrottle = Mathf.Clamp01(throttleInput);
            float curve = Mathf.Max(0.1f, throttleResponsiveness);
            return Mathf.Clamp01(Mathf.Pow(normalizedThrottle, 1f / curve));
        }

        private static bool TryApplyRevLimiter(
            EngineSimulationInput input,
            EngineConfig config,
            bool drivetrainCoupled,
            float drivetrainCoupledRPM,
            ref float currentRPM,
            out float torqueFactor)
        {
            torqueFactor = 1f;

            if (!config.enableRevLimiter || input.throttleInput <= 0.01f)
            {
                return false;
            }

            if (drivetrainCoupled && drivetrainCoupledRPM > config.maxRPM + 1f)
            {
                return false;
            }

            float limiterReferenceRPM = drivetrainCoupled
                ? drivetrainCoupledRPM
                : currentRPM;

            if (limiterReferenceRPM < config.maxRPM - 1f)
            {
                return false;
            }

            float pulse = Mathf.PingPong(input.simulationTime * config.revLimiterPulseFrequency * 2f, 1f);
            float limiterRPM = config.maxRPM - (config.revLimiterDropRPM * pulse);

            currentRPM = Mathf.Clamp(limiterRPM, config.idleRPM, config.maxRPM);
            torqueFactor = Mathf.Lerp(1f, config.revLimiterTorqueMultiplier, pulse);
            return true;
        }
    }
}
