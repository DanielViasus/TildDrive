using System;
using UnityEngine;
using TiltDrive.State;
using TiltDrive.EngineSystem;

namespace TiltDrive.TransmissionSystem
{
    [Serializable]
    public class TransmissionSimulationInput
    {
        [Header("Tiempo")]
        [Min(0.0001f)] public float deltaTime = 0.016f;
        [Min(0f)] public float simulationTime = 0f;

        [Header("Input del Usuario")]
        [Range(0f, 1f)] public float throttleInput = 0f;
        [Range(0f, 1f)] public float clutchInput = 0f;
        public bool gearUpPressed = false;
        public bool gearDownPressed = false;
        public int directGearRequest = 0;

        [Header("Estado Actual de la Transmisión")]
        public int currentGear = 0;
        public int requestedGear = 0;
        public bool shiftInProgress = false;
        [Min(0f)] public float currentShiftTimer = 0f;
        [Range(0f, 1f)] public float currentClutchEngagement = 0f;

        [Header("Estado del Motor")]
        public bool engineOn = false;
        [Min(0f)] public float engineRPM = 0f;
        [Min(0f)] public float engineTorqueNm = 0f;

        [Header("Configuración")]
        public TransmissionConfig transmissionConfig;

        [Header("Control de Simulación")]
        public bool shiftingAllowed = true;
        public bool useDirectGearSelection = true;

        public void SetTime(float newDeltaTime, float newSimulationTime)
        {
            deltaTime = Mathf.Max(0.0001f, newDeltaTime);
            simulationTime = Mathf.Max(0f, newSimulationTime);
        }

        public void SetUserInput(InputState inputState)
        {
            if (inputState == null) return;

            throttleInput = Mathf.Clamp01(inputState.throttle);
            clutchInput = Mathf.Clamp01(inputState.clutch);
            gearUpPressed = inputState.gearUpPressed;
            gearDownPressed = inputState.gearDownPressed;
            directGearRequest = inputState.directGearRequest;
        }

        public void SetTransmissionState(TransmissionState transmissionState)
        {
            if (transmissionState == null) return;

            currentGear = transmissionState.currentGear;
            requestedGear = transmissionState.requestedGear;
            shiftInProgress = transmissionState.shiftInProgress;
            currentShiftTimer = Mathf.Max(0f, transmissionState.shiftTimer);
            currentClutchEngagement = Mathf.Clamp01(transmissionState.clutchEngagement);
        }

        public void SetEngineState(EngineState engineState)
        {
            if (engineState == null) return;

            engineOn = engineState.engineOn;
            engineRPM = Mathf.Max(0f, engineState.currentRPM);
            engineTorqueNm = Mathf.Max(0f, engineState.engineTorqueNm);
        }

        public void SetConfig(TransmissionConfig config)
        {
            transmissionConfig = config;
        }

        public void Reset()
        {
            deltaTime = 0.016f;
            simulationTime = 0f;

            throttleInput = 0f;
            clutchInput = 0f;
            gearUpPressed = false;
            gearDownPressed = false;
            directGearRequest = 0;

            currentGear = 0;
            requestedGear = 0;
            shiftInProgress = false;
            currentShiftTimer = 0f;
            currentClutchEngagement = 0f;

            engineOn = false;
            engineRPM = 0f;
            engineTorqueNm = 0f;

            transmissionConfig = null;

            shiftingAllowed = true;
            useDirectGearSelection = true;
        }

        public bool IsValid()
        {
            return transmissionConfig != null && deltaTime > 0f;
        }
    }
}