using UnityEngine;

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

            output.ApplyConfig(config);

            output.engineOn = input.engineOn;
            output.engineStarting = input.engineStarting;
            output.engineShuttingDown = input.engineShuttingDown;
            output.engineStalled = input.engineStalled;
            output.currentRPM = Mathf.Max(0f, input.currentRPM);
            output.targetRPM = output.currentRPM;
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
            if (input.engineStartPressed && !input.engineStarting && !input.engineShuttingDown)
            {
                if (!input.engineOn && input.ignitionAllowed)
                {
                    output.engineOn = false;
                    output.engineStarting = true;
                    output.engineShuttingDown = false;
                    output.engineStalled = false;

                    // Si viene de motor totalmente apagado, arrancamos desde 0.
                    output.currentRPM = Mathf.Max(0f, input.currentRPM);
                    output.targetRPM = config.idleRPM;
                }
                else if (input.engineOn)
                {
                    output.engineOn = false;
                    output.engineStarting = false;
                    output.engineShuttingDown = true;
                    output.targetRPM = 0f;
                }
            }

            // --------------------------------------------------
            // 2. TRANSICION DE ARRANQUE
            // --------------------------------------------------
            if (output.engineStarting)
            {
                output.targetRPM = config.idleRPM;

                float startStep = config.engineStartRPMSpeed * deltaTime;
                output.currentRPM = Mathf.MoveTowards(output.currentRPM, config.idleRPM, startStep);

                output.engineLoad = 0f;
                output.engineTorqueNm = 0f;
                output.engineBrakeTorqueNm = 0f;

                if (output.currentRPM >= config.idleRPM - 1f)
                {
                    output.currentRPM = config.idleRPM;
                    output.targetRPM = config.idleRPM;
                    output.engineStarting = false;
                    output.engineOn = true;
                    output.engineShuttingDown = false;
                    output.engineStalled = false;
                    output.diagnosticMessage = "ENGINE ON / IDLE";
                }
                else
                {
                    output.diagnosticMessage = "ENGINE STARTING";
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
            float throttleFactor = Mathf.Clamp01(input.throttleInput) * config.throttleResponsiveness;
            float unclampedTargetRPM = config.idleRPM + (config.maxRPM - config.idleRPM) * Mathf.Clamp01(throttleFactor);

            float loadRPMPenalty = externalLoad * 350f;
            float targetRPM = unclampedTargetRPM - loadRPMPenalty;
            targetRPM = Mathf.Max(0f, targetRPM);

            if (input.throttleInput <= 0.01f)
            {
                targetRPM = Mathf.Max(config.idleRPM - (externalLoad * 150f), 0f);
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

            newRPM = Mathf.Max(0f, newRPM);
            output.currentRPM = newRPM;
            output.engineBrakeTorqueNm = Mathf.Max(0f, engineBrakeTorque);

            // --------------------------------------------------
            // 8. TORQUE BASE APROXIMADO
            // --------------------------------------------------
            float rpmNormalizedToPeak =
                1f - Mathf.Clamp01(Mathf.Abs(newRPM - config.peakTorqueRPM) / Mathf.Max(1f, config.maxRPM));

            float torqueCurveFactor = Mathf.Clamp01(0.35f + rpmNormalizedToPeak);
            float throttleTorqueFactor = Mathf.Clamp01(input.throttleInput);
            float rawTorque = config.baseTorqueNm * torqueCurveFactor * throttleTorqueFactor;

            float loadTorquePenalty = externalLoad * 15f;
            float finalTorque = Mathf.Max(0f, rawTorque - loadTorquePenalty);

            output.engineTorqueNm = finalTorque;

            // --------------------------------------------------
            // 9. APAGADO POR RPM MINIMAS
            // --------------------------------------------------
            if (input.canStall && output.currentRPM > 0f && output.currentRPM < config.stallRPM)
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
            else if (output.isOverRevving)
            {
                output.diagnosticMessage = "OVERREV WARNING";
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
    }
}