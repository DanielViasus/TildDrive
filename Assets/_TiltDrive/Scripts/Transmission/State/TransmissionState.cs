using System;
using UnityEngine;

namespace TiltDrive.TransmissionSystem
{
    [Serializable]
    public class TransmissionState
    {
        [Header("Estado Lógico")]
        public TransmissionType transmissionType = TransmissionType.Manual;

        [Tooltip("Marcha actual real. Convención: -1=R, 0=N, 1..N=forward")]
        public int currentGear = 0;

        [Tooltip("Marcha solicitada por input o sistema.")]
        public int requestedGear = 0;

        [Tooltip("Indica si la transmisión está ejecutando un cambio.")]
        public bool shiftInProgress = false;

        [Tooltip("Tiempo acumulado del cambio actual.")]
        [Min(0f)] public float shiftTimer = 0f;

        [Header("Clutch")]
        [Range(0f, 1f)] public float clutchInput = 0f;

        [Tooltip("Grado efectivo de acople del clutch. 0=desacoplado, 1=acoplado")]
        [Range(0f, 1f)] public float clutchEngagement = 0f;

        [Tooltip("True cuando el clutch está prácticamente desacoplado.")]
        public bool clutchDisengaged = true;

        [Header("Relaciones")]
        [Min(0f)] public float currentGearRatio = 0f;
        [Min(0f)] public float finalDriveRatio = 0f;
        [Min(0f)] public float totalDriveRatio = 0f;

        [Header("Entrada")]
        [Min(0f)] public float inputTorqueNm = 0f;

        [Tooltip("RPM de entrada provenientes del motor.")]
        [Min(0f)] public float inputRPM = 0f;

        [Header("Salida")]
        [Tooltip("Torque disponible a la salida de la transmision, despues de relaciones y eficiencia.")]
        [Min(0f)] public float outputTorqueNm = 0f;

        [Tooltip("RPM de salida de la transmision. Negativo indica reversa.")]
        public float outputRPM = 0f;

        [Header("Compatibilidad")]
        [Min(0f)] public float transmittedTorqueNm = 0f;

        [Tooltip("Dirección lógica de avance: -1 reversa, 0 neutro, 1 avance")]
        public int driveDirection = 0;

        [Header("Flags")]
        public bool isNeutral = true;
        public bool isReverse = false;
        public bool shiftAllowed = true;
        public bool hasTransmissionWarning = false;

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
            totalDriveRatio = currentGearRatio > 0f
                ? currentGearRatio * finalDriveRatio
                : 0f;

            EvaluateFlags(config);
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
                if (currentGear < -1)
                {
                    hasTransmissionWarning = true;
                }

                if (currentGear > config.forwardGearCount)
                {
                    hasTransmissionWarning = true;
                }

                if (currentGear == -1 && !config.hasReverse)
                {
                    hasTransmissionWarning = true;
                }

                if (currentGear == 0 && !config.hasNeutral)
                {
                    hasTransmissionWarning = true;
                }
            }
        }

        public void UpdateGearRatios(TransmissionConfig config)
        {
            if (config == null) return;

            config.ClampValues();

            currentGearRatio = config.GetGearRatio(currentGear);
            finalDriveRatio = config.finalDriveRatio;
            totalDriveRatio = currentGearRatio > 0f
                ? currentGearRatio * finalDriveRatio
                : 0f;
        }

        public void ResetRuntime(TransmissionConfig config = null)
        {
            currentGear = 0;
            requestedGear = 0;
            shiftInProgress = false;
            shiftTimer = 0f;

            clutchInput = 0f;
            clutchEngagement = 0f;
            clutchDisengaged = true;

            inputTorqueNm = 0f;
            inputRPM = 0f;
            outputTorqueNm = 0f;
            outputRPM = 0f;
            transmittedTorqueNm = 0f;

            shiftAllowed = true;
            simulationTick = 0;
            lastUpdateTime = 0f;

            if (config != null)
            {
                ApplyConfig(config);
            }
            else
            {
                currentGearRatio = 0f;
                finalDriveRatio = 0f;
                totalDriveRatio = 0f;

                isNeutral = true;
                isReverse = false;
                driveDirection = 0;
                shiftAllowed = true;
                hasTransmissionWarning = false;
            }
        }

        public void InitializeFromConfig(TransmissionConfig config, int startGear = 0)
        {
            if (config == null)
            {
                ResetRuntime();
                return;
            }

            config.ClampValues();

            transmissionType = config.transmissionType;
            currentGear = startGear;
            requestedGear = startGear;
            shiftInProgress = false;
            shiftTimer = 0f;

            clutchInput = 0f;
            clutchEngagement = 0f;
            clutchDisengaged = true;

            inputTorqueNm = 0f;
            inputRPM = 0f;
            outputTorqueNm = 0f;
            outputRPM = 0f;
            transmittedTorqueNm = 0f;

            shiftAllowed = true;
            simulationTick = 0;
            lastUpdateTime = 0f;

            UpdateGearRatios(config);
            EvaluateFlags(config);
        }
    }
}
