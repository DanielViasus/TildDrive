using System;
using UnityEngine;

namespace TiltDrive.TransmissionSystem
{
    [Serializable]
    public class TransmissionSimulationOutput
    {
        [Header("Estado Lógico")]
        public TransmissionType transmissionType = TransmissionType.Manual;
        public int currentGear = 0;
        public int requestedGear = 0;
        public bool shiftInProgress = false;

        [Header("Cambio")]
        [Min(0f)] public float shiftTimer = 0f;

        [Header("Clutch")]
        [Range(0f, 1f)] public float clutchInput = 0f;
        [Range(0f, 1f)] public float clutchEngagement = 0f;
        public bool clutchDisengaged = true;

        [Header("Relaciones")]
        [Min(0f)] public float currentGearRatio = 0f;
        [Min(0f)] public float finalDriveRatio = 0f;
        [Min(0f)] public float totalDriveRatio = 0f;

        [Header("Entrada")]
        [Min(0f)] public float inputTorqueNm = 0f;
        [Min(0f)] public float inputRPM = 0f;

        [Header("Salida")]
        [Min(0f)] public float outputTorqueNm = 0f;
        public float outputRPM = 0f;

        [Header("Compatibilidad")]
        [Min(0f)] public float transmittedTorqueNm = 0f;
        public int driveDirection = 0;

        [Header("Flags")]
        public bool isNeutral = true;
        public bool isReverse = false;
        public bool shiftAllowed = true;
        public bool hasTransmissionWarning = false;

        [Tooltip("Texto corto de diagnóstico del tick actual.")]
        public string diagnosticMessage = string.Empty;

        [Header("Trazabilidad")]
        [Min(0)] public int simulationTick = 0;
        [Min(0f)] public float lastUpdateTime = 0f;

        public void ApplyConfig(TransmissionConfig config)
        {
            if (config == null) return;

            config.ClampValues();

            transmissionType = config.transmissionType;
            finalDriveRatio = config.finalDriveRatio;
            currentGearRatio = config.GetGearRatio(currentGear);
            totalDriveRatio = currentGearRatio > 0f ? currentGearRatio * finalDriveRatio : 0f;
        }

        public void EvaluateFlags(TransmissionConfig config)
        {
            isNeutral = currentGear == 0;
            isReverse = currentGear == -1;

            driveDirection = 0;
            if (currentGear > 0) driveDirection = 1;
            else if (currentGear < 0) driveDirection = -1;

            clutchDisengaged = clutchEngagement <= 0.05f;

            hasTransmissionWarning = false;

            if (config != null)
            {
                if (currentGear < -1) hasTransmissionWarning = true;
                if (currentGear > config.forwardGearCount) hasTransmissionWarning = true;
                if (currentGear == -1 && !config.hasReverse) hasTransmissionWarning = true;
                if (currentGear == 0 && !config.hasNeutral) hasTransmissionWarning = true;
            }
        }

        public void Reset()
        {
            transmissionType = TransmissionType.Manual;
            currentGear = 0;
            requestedGear = 0;
            shiftInProgress = false;

            shiftTimer = 0f;

            clutchInput = 0f;
            clutchEngagement = 0f;
            clutchDisengaged = true;

            currentGearRatio = 0f;
            finalDriveRatio = 0f;
            totalDriveRatio = 0f;

            inputTorqueNm = 0f;
            inputRPM = 0f;
            outputTorqueNm = 0f;
            outputRPM = 0f;
            transmittedTorqueNm = 0f;
            driveDirection = 0;

            isNeutral = true;
            isReverse = false;
            shiftAllowed = true;
            hasTransmissionWarning = false;

            diagnosticMessage = string.Empty;

            simulationTick = 0;
            lastUpdateTime = 0f;
        }

        public TransmissionState ToTransmissionState()
        {
            TransmissionState state = new TransmissionState
            {
                transmissionType = transmissionType,
                currentGear = currentGear,
                requestedGear = requestedGear,
                shiftInProgress = shiftInProgress,

                shiftTimer = Mathf.Max(0f, shiftTimer),

                clutchInput = Mathf.Clamp01(clutchInput),
                clutchEngagement = Mathf.Clamp01(clutchEngagement),
                clutchDisengaged = clutchDisengaged,

                currentGearRatio = Mathf.Max(0f, currentGearRatio),
                finalDriveRatio = Mathf.Max(0f, finalDriveRatio),
                totalDriveRatio = Mathf.Max(0f, totalDriveRatio),

                inputTorqueNm = Mathf.Max(0f, inputTorqueNm),
                inputRPM = Mathf.Max(0f, inputRPM),
                outputTorqueNm = Mathf.Max(0f, outputTorqueNm),
                outputRPM = outputRPM,
                transmittedTorqueNm = Mathf.Max(0f, transmittedTorqueNm),

                driveDirection = driveDirection,

                isNeutral = isNeutral,
                isReverse = isReverse,
                shiftAllowed = shiftAllowed,
                hasTransmissionWarning = hasTransmissionWarning,

                simulationTick = Mathf.Max(0, simulationTick),
                lastUpdateTime = Mathf.Max(0f, lastUpdateTime)
            };

            return state;
        }

        public void CopyFromState(TransmissionState state)
        {
            if (state == null) return;

            transmissionType = state.transmissionType;
            currentGear = state.currentGear;
            requestedGear = state.requestedGear;
            shiftInProgress = state.shiftInProgress;

            shiftTimer = Mathf.Max(0f, state.shiftTimer);

            clutchInput = Mathf.Clamp01(state.clutchInput);
            clutchEngagement = Mathf.Clamp01(state.clutchEngagement);
            clutchDisengaged = state.clutchDisengaged;

            currentGearRatio = Mathf.Max(0f, state.currentGearRatio);
            finalDriveRatio = Mathf.Max(0f, state.finalDriveRatio);
            totalDriveRatio = Mathf.Max(0f, state.totalDriveRatio);

            inputTorqueNm = Mathf.Max(0f, state.inputTorqueNm);
            inputRPM = Mathf.Max(0f, state.inputRPM);
            outputTorqueNm = Mathf.Max(0f, state.outputTorqueNm);
            outputRPM = state.outputRPM;
            transmittedTorqueNm = Mathf.Max(0f, state.transmittedTorqueNm);

            driveDirection = state.driveDirection;

            isNeutral = state.isNeutral;
            isReverse = state.isReverse;
            shiftAllowed = state.shiftAllowed;
            hasTransmissionWarning = state.hasTransmissionWarning;

            simulationTick = Mathf.Max(0, state.simulationTick);
            lastUpdateTime = Mathf.Max(0f, state.lastUpdateTime);
        }
    }
}
