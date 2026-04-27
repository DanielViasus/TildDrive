using System;
using UnityEngine;
using TiltDrive.State;
using TiltDrive.TransmissionSystem;
using TiltDrive.VehicleSystem;
using TiltDrive.Simulation;
using TiltDrive.CoolingSystem;

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
        [Range(0f, 1f)] public float brakeInput = 0f;
        [Range(0f, 1f)] public float clutchInput = 0f;
        public bool engineStartPressed = false;
        public bool engineStartHeld = false;

        [Header("Estado Actual del Motor")]
        public bool engineOn = false;
        public bool engineStarting = false;
        public bool engineShuttingDown = false;
        public bool engineStalled = false;
        [Min(0f)] public float engineStartHoldSeconds = 0f;
        [Min(0f)] public float requiredStartHoldSeconds = 0f;
        [Min(0f)] public float currentRPM = 0f;
        [Range(0f, 100f)] public float componentHealthPercent = 100f;
        [Min(0f)] public float accumulatedDamagePercent = 0f;
        [Min(0f)] public float engineTemperatureC = 0f;
        [Range(0f, 1f)] public float thermalEfficiency = 1f;
        [Range(0f, 1.5f)] public float radiatorCoolingEfficiency = 1f;
        [Range(0f, 1.5f)] public float radiatorAirflowEfficiency = 1f;

        [Header("Configuración")]
        public EngineConfig engineConfig;
        public DriveLaunchDiagnosticsConfig launchDiagnosticsConfig;

        [Header("Cargas Externas")]
        [Tooltip("Carga proveniente de transmisión o drivetrain. V1: valor abstracto.")]
        [Min(0f)] public float transmissionLoad = 0f;

        [Tooltip("Masa estimada del vehículo en kg.")]
        [Min(0f)] public float vehicleMassKg = 0f;

        [Tooltip("Ángulo de pendiente. Positivo = subida, negativo = bajada.")]
        public float slopeAngleDegrees = 0f;

        [Tooltip("Resistencia adicional abstracta: viento, fricción, arrastre, etc.")]
        [Min(0f)] public float additionalResistance = 0f;

        [Header("Acople Drivetrain")]
        public bool useDrivetrainRPMCoupling = true;
        public int currentGear = 0;
        [Min(0f)] public float totalDriveRatio = 0f;
        [Range(0f, 1f)] public float clutchEngagement = 0f;
        public float vehicleSpeedMS = 0f;
        [Min(0f)] public float wheelCircumferenceMeters = 0f;

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
            brakeInput = Mathf.Clamp01(inputState.brake);
            clutchInput = Mathf.Clamp01(inputState.clutch);
            engineStartPressed = inputState.engineStartPressed;
            engineStartHeld = inputState.engineStartHeld;
        }

        public void SetEngineState(EngineState engineState)
        {
            if (engineState == null) return;

            engineOn = engineState.engineOn;
            engineStarting = engineState.engineStarting;
            engineShuttingDown = engineState.engineShuttingDown;
            engineStalled = engineState.engineStalled;
            engineStartHoldSeconds = Mathf.Max(0f, engineState.engineStartHoldSeconds);
            requiredStartHoldSeconds = Mathf.Max(0f, engineState.requiredStartHoldSeconds);
            currentRPM = Mathf.Max(0f, engineState.currentRPM);
            componentHealthPercent = Mathf.Clamp(engineState.componentHealthPercent, 0f, 100f);
            accumulatedDamagePercent = Mathf.Max(0f, engineState.accumulatedDamagePercent);
            engineTemperatureC = Mathf.Max(0f, engineState.engineTemperatureC);
            thermalEfficiency = Mathf.Clamp01(engineState.thermalEfficiency);
        }

        public void SetConfig(EngineConfig config)
        {
            engineConfig = config;
        }

        public void SetRadiatorState(RadiatorState radiatorState)
        {
            if (radiatorState == null)
            {
                radiatorCoolingEfficiency = 1f;
                radiatorAirflowEfficiency = 1f;
                return;
            }

            radiatorCoolingEfficiency = Mathf.Clamp(radiatorState.coolingEfficiency, 0f, 1.5f);
            radiatorAirflowEfficiency = Mathf.Clamp(radiatorState.airflowEfficiency, 0f, 1.5f);
        }

        public void SetLaunchDiagnosticsConfig(DriveLaunchDiagnosticsConfig config)
        {
            launchDiagnosticsConfig = config;
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

        public void SetDrivetrainState(
            TransmissionState transmissionState,
            VehicleOutputState vehicleOutputState)
        {
            if (transmissionState != null)
            {
                currentGear = transmissionState.currentGear;
                totalDriveRatio = Mathf.Max(0f, transmissionState.totalDriveRatio);
                clutchEngagement = Mathf.Clamp01(transmissionState.clutchEngagement);
            }

            if (vehicleOutputState != null)
            {
                vehicleSpeedMS = vehicleOutputState.finalSpeedMS;
                wheelCircumferenceMeters = Mathf.Max(0f, vehicleOutputState.wheelCircumferenceMeters);
            }
        }

        public void Reset()
        {
            deltaTime = 0.016f;
            simulationTime = 0f;

            throttleInput = 0f;
            brakeInput = 0f;
            clutchInput = 0f;
            engineStartPressed = false;
            engineStartHeld = false;

            engineOn = false;
            engineStarting = false;
            engineShuttingDown = false;
            engineStalled = false;
            engineStartHoldSeconds = 0f;
            requiredStartHoldSeconds = 0f;
            currentRPM = 0f;
            componentHealthPercent = 100f;
            accumulatedDamagePercent = 0f;
            engineTemperatureC = 0f;
            thermalEfficiency = 1f;
            radiatorCoolingEfficiency = 1f;
            radiatorAirflowEfficiency = 1f;

            engineConfig = null;
            launchDiagnosticsConfig = null;

            transmissionLoad = 0f;
            vehicleMassKg = 0f;
            slopeAngleDegrees = 0f;
            additionalResistance = 0f;

            useDrivetrainRPMCoupling = true;
            currentGear = 0;
            totalDriveRatio = 0f;
            clutchEngagement = 0f;
            vehicleSpeedMS = 0f;
            wheelCircumferenceMeters = 0f;

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
