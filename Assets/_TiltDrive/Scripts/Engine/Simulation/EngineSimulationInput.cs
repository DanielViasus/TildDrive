using System;
using UnityEngine;
using TiltDrive.State;

namespace TiltDrive.EngineSystem
{
    [Serializable]
    public class EngineSimulationInput
    {
        [Header("Tiempo")]
        [Min(0.0001f)] public float deltaTime = 0.016f;
        [Min(0f)] public float simulationTime = 0f;

        [Header("Input del Usuario")]
        [Range(0f, 1f)] public float throttleInput = 0f;
        [Range(0f, 1f)] public float clutchInput = 0f;
        public bool engineStartPressed = false;

        [Header("Estado Actual del Motor")]
        public bool engineOn = false;
        public bool engineStarting = false;
        public bool engineShuttingDown = false;
        public bool engineStalled = false;
        [Min(0f)] public float currentRPM = 0f;

        [Header("Configuración")]
        public EngineConfig engineConfig;

        [Header("Cargas Externas")]
        [Tooltip("Carga proveniente de transmisión o drivetrain. V1: valor abstracto.")]
        [Min(0f)] public float transmissionLoad = 0f;

        [Tooltip("Masa estimada del vehículo en kg.")]
        [Min(0f)] public float vehicleMassKg = 0f;

        [Tooltip("Ángulo de pendiente. Positivo = subida, negativo = bajada.")]
        public float slopeAngleDegrees = 0f;

        [Tooltip("Resistencia adicional abstracta: viento, fricción, arrastre, etc.")]
        [Min(0f)] public float additionalResistance = 0f;

        [Header("Control de Simulación")]
        public bool ignitionAllowed = true;
        public bool canStall = true;
        public bool useExternalLoad = true;

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
            engineStartPressed = inputState.engineStartPressed;
        }

        public void SetEngineState(EngineState engineState)
        {
            if (engineState == null) return;

            engineOn = engineState.engineOn;
            engineStarting = engineState.engineStarting;
            engineShuttingDown = engineState.engineShuttingDown;
            engineStalled = engineState.engineStalled;
            currentRPM = Mathf.Max(0f, engineState.currentRPM);
        }

        public void SetConfig(EngineConfig config)
        {
            engineConfig = config;
        }

        public void SetExternalLoad(
            float newTransmissionLoad,
            float newVehicleMassKg,
            float newSlopeAngleDegrees,
            float newAdditionalResistance)
        {
            transmissionLoad = Mathf.Max(0f, newTransmissionLoad);
            vehicleMassKg = Mathf.Max(0f, newVehicleMassKg);
            slopeAngleDegrees = newSlopeAngleDegrees;
            additionalResistance = Mathf.Max(0f, newAdditionalResistance);
        }

        public void Reset()
        {
            deltaTime = 0.016f;
            simulationTime = 0f;

            throttleInput = 0f;
            clutchInput = 0f;
            engineStartPressed = false;

            engineOn = false;
            engineStarting = false;
            engineShuttingDown = false;
            engineStalled = false;
            currentRPM = 0f;

            engineConfig = null;

            transmissionLoad = 0f;
            vehicleMassKg = 0f;
            slopeAngleDegrees = 0f;
            additionalResistance = 0f;

            ignitionAllowed = true;
            canStall = true;
            useExternalLoad = true;
        }

        public bool IsValid()
        {
            return engineConfig != null && deltaTime > 0f;
        }
    }
}