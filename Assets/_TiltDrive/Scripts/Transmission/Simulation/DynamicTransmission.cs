using UnityEngine;
using TiltDrive.Simulation;

namespace TiltDrive.TransmissionSystem
{
    public class DynamicTransmission
    {
        public TransmissionSimulationOutput Simulate(TransmissionSimulationInput input)
        {
            TransmissionSimulationOutput output = new TransmissionSimulationOutput();

            if (input == null || !input.IsValid())
            {
                output.Reset();
                output.diagnosticMessage = "INVALID TRANSMISSION INPUT";
                return output;
            }

            TransmissionConfig config = input.transmissionConfig;
            config.ClampValues();
            bool isAutomatic = config.transmissionType == TransmissionType.Automatic;
            bool requiresClutchForShift = config.requiresClutch &&
                config.transmissionType == TransmissionType.Manual &&
                !isAutomatic;

            output.transmissionType = config.transmissionType;
            output.currentGear = input.currentGear;
            output.requestedGear = input.requestedGear;
            output.shiftInProgress = input.shiftInProgress;
            output.shiftTimer = Mathf.Max(0f, input.currentShiftTimer);

            output.clutchInput = Mathf.Clamp01(input.clutchInput);
            output.clutchEngagement = CalculateClutchEngagement(output.clutchInput);
            if (isAutomatic)
            {
                output.clutchInput = 0f;
                output.clutchEngagement = 1f;
            }

            output.inputTorqueNm = Mathf.Max(0f, input.engineTorqueNm);
            output.inputRPM = Mathf.Max(0f, input.engineRPM);
            output.componentHealthPercent = Mathf.Clamp(input.componentHealthPercent, 0f, 100f);
            output.accumulatedDamagePercent = Mathf.Max(0f, input.accumulatedDamagePercent);

            output.simulationTick = 0;
            output.lastUpdateTime = input.simulationTime;

            // --------------------------------------------------
            // 1. VALIDAR SI SE PERMITEN CAMBIOS
            // --------------------------------------------------
            bool canRequestShift = input.shiftingAllowed;

            if (requiresClutchForShift)
            {
                bool clutchPressedEnough = input.clutchInput >= 0.7f;
                canRequestShift = canRequestShift && clutchPressedEnough;
            }

            output.shiftAllowed = canRequestShift;

            // --------------------------------------------------
            // 2. INICIO DE SOLICITUD DE CAMBIO
            // --------------------------------------------------
            // Si no está cambiando, evaluamos nuevas solicitudes.
            if (!output.shiftInProgress && canRequestShift)
            {
                int targetGear = output.currentGear;
                bool hasShiftRequest = false;

                if (input.useDirectGearSelection &&
                    config.allowDirectGearSelection &&
                    input.directGearRequest != 0 &&
                    (!isAutomatic || input.directGearRequest <= 0))
                {
                    targetGear = SanitizeRequestedGear(input.directGearRequest, config);
                    hasShiftRequest = targetGear != output.currentGear;
                }
                else if (isAutomatic)
                {
                    targetGear = ResolveAutomaticGear(output.currentGear, input, config);
                    hasShiftRequest = targetGear != output.currentGear;
                }
                else if (input.gearUpPressed)
                {
                    targetGear = ResolveGearUp(output.currentGear, config);
                    hasShiftRequest = targetGear != output.currentGear;
                }
                else if (input.gearDownPressed)
                {
                    targetGear = ResolveGearDown(output.currentGear, config);
                    hasShiftRequest = targetGear != output.currentGear;
                }

                if (hasShiftRequest)
                {
                    output.requestedGear = targetGear;
                    output.shiftInProgress = true;
                    output.shiftTimer = 0f;
                }
            }

            // --------------------------------------------------
            // 3. PROCESO DE CAMBIO
            // --------------------------------------------------
            if (output.shiftInProgress)
            {
                output.shiftTimer += Mathf.Max(0.0001f, input.deltaTime);

                // Durante el cambio dejamos el clutch muy desacoplado virtualmente
                // para evitar una entrega demasiado brusca.
                float shiftDisengagementFactor = 0.1f;
                output.clutchEngagement *= shiftDisengagementFactor;

                if (output.shiftTimer >= config.shiftDuration)
                {
                    int previousGear = output.currentGear;
                    int targetGear = SanitizeRequestedGear(output.requestedGear, config);
                    DriveMisuseDiagnostics.MisuseReport misuseReport =
                        DriveMisuseDiagnostics.AnalyzeGearChange(
                            previousGear,
                            targetGear,
                            input.vehicleSpeedMS,
                            input.wheelCircumferenceMeters,
                            input.engineMaxRPM,
                            config,
                            input.damagePenaltyConfig);

                    ApplyMisuseReport(output, misuseReport);

                    output.currentGear = targetGear;
                    output.shiftInProgress = false;
                    output.shiftTimer = 0f;
                }
            }
            else
            {
                output.requestedGear = output.currentGear;
            }

            // --------------------------------------------------
            // 4. RELACIONES ACTIVAS
            // --------------------------------------------------
            output.currentGearRatio = config.GetGearRatio(output.currentGear);
            output.finalDriveRatio = config.finalDriveRatio;
            output.totalDriveRatio = output.currentGearRatio > 0f
                ? output.currentGearRatio * output.finalDriveRatio
                : 0f;

            // --------------------------------------------------
            // 5. FLAGS Y DIRECCION
            // --------------------------------------------------
            output.EvaluateFlags(config);

            DriveMisuseDiagnostics.MisuseReport highGearStrainReport =
                DriveMisuseDiagnostics.AnalyzeHighGearEngineStrain(
                    output.currentGear,
                    input.vehicleSpeedMS,
                    input.throttleInput,
                    output.clutchEngagement,
                    input.engineRPM,
                    input.damagePenaltyConfig);

            if (highGearStrainReport.hasFault)
            {
                ApplyMisuseReport(output, highGearStrainReport);
            }

            // --------------------------------------------------
            // 6. MOTOR APAGADO
            // --------------------------------------------------
            if (!input.engineOn)
            {
                output.outputTorqueNm = 0f;
                output.outputRPM = 0f;
                output.transmittedTorqueNm = 0f;
                output.diagnosticMessage = "ENGINE OFF / NO TORQUE";
                return output;
            }

            // --------------------------------------------------
            // 7. NEUTRO O SIN RELACION ACTIVA
            // --------------------------------------------------
            if (output.isNeutral || output.currentGearRatio <= 0f)
            {
                output.outputTorqueNm = 0f;
                output.outputRPM = 0f;
                output.transmittedTorqueNm = 0f;

                if (output.shiftInProgress)
                {
                    output.diagnosticMessage = "SHIFTING IN PROGRESS";
                }
                else
                {
                    output.diagnosticMessage = "NEUTRAL / NO TORQUE TO WHEELS";
                }

                return output;
            }

            // --------------------------------------------------
            // 8. TORQUE TRANSMITIDO MAS NATURAL
            // --------------------------------------------------
            float efficiency = Mathf.Clamp(config.transmissionEfficiency, 0.1f, 1f);

            // Throttle assist:
            // incluso con throttle bajo puede existir un poco de arrastre útil,
            // pero la entrega principal sube con el acelerador.

            // Si el clutch está prácticamente desacoplado, el torque a ruedas debe ser muy bajo.
            float engagement = Mathf.Clamp01(output.clutchEngagement);
            float totalRatio = Mathf.Max(0.0001f, output.totalDriveRatio);

            // Durante cambio reducimos de forma muy marcada la entrega.
            float outputTorque =
                output.inputTorqueNm *
                totalRatio *
                efficiency *
                engagement *
                Mathf.Clamp01(output.componentHealthPercent / 100f);

            float outputRPM =
                (output.inputRPM / totalRatio) *
                engagement *
                output.driveDirection;

            output.outputTorqueNm = Mathf.Max(0f, outputTorque);
            output.outputRPM = outputRPM;
            output.transmittedTorqueNm = output.outputTorqueNm;

            // --------------------------------------------------
            // 9. DIAGNOSTICO
            // --------------------------------------------------
            if (output.shiftInProgress)
            {
                output.diagnosticMessage = $"SHIFTING TO GEAR {output.requestedGear}";
            }
            else if (output.isReverse)
            {
                output.diagnosticMessage = "REVERSE GEAR ENGAGED";
            }
            else if (output.currentGear > 0)
            {
                output.diagnosticMessage = $"FORWARD GEAR {output.currentGear} ENGAGED";
            }
            else
            {
                output.diagnosticMessage = "TRANSMISSION ACTIVE";
            }

            return output;
        }

        private static void ApplyMisuseReport(
            TransmissionSimulationOutput output,
            DriveMisuseDiagnostics.MisuseReport report)
        {
            output.hasMisuseWarning = report.hasFault;
            output.lastMisuseCode = report.code;
            output.lastMisuseMessage = report.message;
            output.lastMisuseSeverity = report.severity;
            output.lastRequiredEngineRPM = report.requiredEngineRPM;
            output.lastMisuseEngineRPM = report.currentEngineRPM;
            output.lastMisuseThrottleInput = report.throttleInput;
            output.engineDamageThisTickPercent = report.engineDamagePercent;
            output.transmissionDamageThisTickPercent = report.transmissionDamagePercent;

            if (!report.hasFault)
            {
                return;
            }

            output.componentHealthPercent = Mathf.Clamp(
                output.componentHealthPercent - report.transmissionDamagePercent,
                0f,
                100f);
            output.accumulatedDamagePercent += report.transmissionDamagePercent;
            output.hasTransmissionWarning = true;
        }

        private float CalculateClutchEngagement(float clutchInput)
        {
            // clutchInput:
            // 0 = pedal suelto
            // 1 = pedal al fondo
            //
            // clutchEngagement:
            // 1 = acoplado
            // 0 = desacoplado
            //
            // Curva progresiva:
            // engagement = (1 - clutchInput)^2
            // Esto hace más suave el punto de acople.
            float normalized = Mathf.Clamp01(1f - clutchInput);
            return normalized * normalized;
        }

        private int ResolveGearUp(int currentGear, TransmissionConfig config)
        {
            if (currentGear < 0)
            {
                return config.hasNeutral ? 0 : 1;
            }

            if (currentGear == 0)
            {
                return 1;
            }

            return Mathf.Min(currentGear + 1, config.forwardGearCount);
        }

        private int ResolveGearDown(int currentGear, TransmissionConfig config)
        {
            if (currentGear > 1)
            {
                return currentGear - 1;
            }

            if (currentGear == 1)
            {
                return config.hasNeutral ? 0 : (config.hasReverse ? -1 : 1);
            }

            if (currentGear == 0)
            {
                return config.hasReverse ? -1 : 0;
            }

            return -1;
        }

        private int ResolveAutomaticGear(int currentGear, TransmissionSimulationInput input, TransmissionConfig config)
        {
            if (!input.engineOn)
            {
                return config.hasNeutral ? 0 : Mathf.Clamp(currentGear, 1, config.forwardGearCount);
            }

            if (currentGear <= 0)
            {
                return config.automaticEngageFirstFromNeutral ? 1 : currentGear;
            }

            if (input.engineRPM >= config.automaticUpshiftRPM && currentGear < config.forwardGearCount)
            {
                return currentGear + 1;
            }

            if (input.engineRPM <= config.automaticDownshiftRPM && currentGear > 1)
            {
                return currentGear - 1;
            }

            return currentGear;
        }

        private int SanitizeRequestedGear(int requestedGear, TransmissionConfig config)
        {
            if (requestedGear == -1)
            {
                return config.hasReverse ? -1 : (config.hasNeutral ? 0 : 1);
            }

            if (requestedGear == 0)
            {
                return config.hasNeutral ? 0 : 1;
            }

            if (requestedGear > 0)
            {
                return Mathf.Clamp(requestedGear, 1, config.forwardGearCount);
            }

            return config.hasNeutral ? 0 : 1;
        }
    }
}
