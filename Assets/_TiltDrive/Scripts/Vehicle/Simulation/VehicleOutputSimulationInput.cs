using System;
using UnityEngine;
using TiltDrive.EngineSystem;
using TiltDrive.State;
using TiltDrive.TransmissionSystem;

namespace TiltDrive.VehicleSystem
{
    [Serializable]
    public class VehicleOutputSimulationInput
    {
        [Header("Tiempo")]
        [Min(0.0001f)] public float deltaTime = 0.016f;
        [Min(0f)] public float simulationTime = 0f;

        [Header("Entrada de Transmision")]
        [Min(0f)] public float transmissionOutputTorqueNm = 0f;
        public float transmissionOutputRPM = 0f;
        public int driveDirection = 0;
        public int currentGear = 0;
        [Min(0f)] public float totalDriveRatio = 0f;
        [Range(0f, 1f)] public float clutchEngagement = 0f;

        [Header("Entrada de Motor")]
        [Min(0f)] public float engineRPM = 0f;
        [Min(0f)] public float engineBrakeTorqueNm = 0f;
        public bool engineOn = false;

        [Header("Entrada del Usuario")]
        [Range(0f, 1f)] public float brakeInput = 0f;

        [Header("Estado Previo")]
        public float previousSpeedMS = 0f;

        [Header("Configuracion")]
        public VehicleOutputConfig vehicleOutputConfig;

        public void SetTime(float newDeltaTime, float newSimulationTime)
        {
            deltaTime = Mathf.Max(0.0001f, newDeltaTime);
            simulationTime = Mathf.Max(0f, newSimulationTime);
        }

        public void SetTransmissionState(TransmissionState transmissionState)
        {
            if (transmissionState == null) return;

            transmissionOutputTorqueNm = Mathf.Max(0f, transmissionState.outputTorqueNm);
            transmissionOutputRPM = transmissionState.outputRPM;
            driveDirection = transmissionState.driveDirection;
            currentGear = transmissionState.currentGear;
            totalDriveRatio = Mathf.Max(0f, transmissionState.totalDriveRatio);
            clutchEngagement = Mathf.Clamp01(transmissionState.clutchEngagement);
        }

        public void SetEngineState(EngineState engineState)
        {
            if (engineState == null) return;

            engineRPM = Mathf.Max(0f, engineState.currentRPM);
            engineBrakeTorqueNm = Mathf.Max(0f, engineState.engineBrakeTorqueNm);
            engineOn = engineState.engineOn;
        }

        public void SetUserInput(InputState inputState)
        {
            if (inputState == null) return;

            brakeInput = Mathf.Clamp01(inputState.brake);
        }

        public void SetPreviousState(VehicleOutputState state)
        {
            if (state == null) return;

            previousSpeedMS = state.finalSpeedMS;
        }

        public void SetConfig(VehicleOutputConfig config)
        {
            vehicleOutputConfig = config;
        }

        public void Reset()
        {
            deltaTime = 0.016f;
            simulationTime = 0f;
            transmissionOutputTorqueNm = 0f;
            transmissionOutputRPM = 0f;
            driveDirection = 0;
            currentGear = 0;
            totalDriveRatio = 0f;
            clutchEngagement = 0f;
            engineRPM = 0f;
            engineBrakeTorqueNm = 0f;
            engineOn = false;
            brakeInput = 0f;
            previousSpeedMS = 0f;
            vehicleOutputConfig = null;
        }

        public bool IsValid()
        {
            return vehicleOutputConfig != null && deltaTime > 0f;
        }
    }
}
