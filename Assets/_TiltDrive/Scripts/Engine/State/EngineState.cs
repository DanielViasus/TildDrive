using System;
using UnityEngine;

namespace TiltDrive.EngineSystem
{
    [Serializable]
    public class EngineState
    {
        [Header("Estado Lógico")]
        public bool engineOn = false;
        public bool engineStarting = false;
        public bool engineShuttingDown = false;
        public bool engineStalled = false;
        [Min(0f)] public float engineStartHoldSeconds = 0f;
        [Min(0f)] public float requiredStartHoldSeconds = 0f;

        [Header("Régimen")]
        [Min(0f)] public float currentRPM = 0f;
        [Min(0f)] public float targetRPM = 0f;

        [Min(0f)] public float idleRPM = 1200f;
        [Min(0f)] public float stallRPM = 500f;
        [Min(0f)] public float maxRPM = 10000f;
        [Min(0f)] public float criticalRPM = 10500f;

        [Header("Esfuerzo y Entrega")]
        [Range(0f, 100f)] public float componentHealthPercent = 100f;
        [Min(0f)] public float accumulatedDamagePercent = 0f;
        [Min(0f)] public float engineLoad = 0f;
        public float engineTorqueNm = 0f;
        [Min(0f)] public float engineBrakeTorqueNm = 0f;

        [Header("Temperatura")]
        [Min(0f)] public float engineTemperatureC = 0f;
        [Range(0f, 1f)] public float thermalEfficiency = 1f;
        public bool engineTemperatureWarning = false;
        public bool engineOverheated = false;
        public bool engineThermalDerateActive = false;
        public string lastTemperatureWarningCode = string.Empty;
        public string lastTemperatureWarningMessage = string.Empty;
        [Min(0f)] public float lastTemperatureSeverity = 0f;

        [Header("Diagnóstico")]
        public bool isBelowIdle = false;
        public bool isInCriticalZone = false;
        public bool isOverRevving = false;
        public bool revLimiterActive = false;
        [Range(0f, 1f)] public float revLimiterTorqueFactor = 1f;
        public bool hasEngineWarning = false;
        public bool starterOveruseWarning = false;
        public bool hasLaunchWarning = false;
        public bool hasLaunchMisuse = false;
        public bool launchStallRisk = false;
        [Min(0f)] public float clutchFrictionRPMDrop = 0f;
        [Range(0f, 1f)] public float clutchFrictionLoad = 0f;
        [Range(0f, 1f)] public float idleLaunchAssistFactor = 0f;
        public string lastLaunchWarningCode = string.Empty;
        public string lastLaunchWarningMessage = string.Empty;
        [Min(0f)] public float lastLaunchSeverity = 0f;
        public string lastStarterWarningCode = string.Empty;
        public string lastStarterWarningMessage = string.Empty;
        [Min(0f)] public float lastStarterSeverity = 0f;

        [Header("Trazabilidad")]
        [Min(0)] public int simulationTick = 0;
        [Min(0f)] public float lastUpdateTime = 0f;

        public void ApplyConfig(EngineConfig config)
        {
            if (config == null) return;

            config.ClampValues();

            idleRPM = config.idleRPM;
            stallRPM = config.stallRPM;
            maxRPM = config.maxRPM;
            criticalRPM = config.criticalRPM;
        }

        public void EvaluateFlags()
        {
            isBelowIdle = currentRPM > 0f && currentRPM < idleRPM;
            isInCriticalZone = currentRPM >= criticalRPM;
            isOverRevving = currentRPM > maxRPM;
            hasEngineWarning = isBelowIdle ||
                isInCriticalZone ||
                isOverRevving ||
                engineStalled ||
                engineTemperatureWarning ||
                engineOverheated ||
                engineThermalDerateActive ||
                starterOveruseWarning ||
                hasLaunchWarning ||
                launchStallRisk;
        }

        public void ResetRuntime()
        {
            engineOn = false;
            engineStarting = false;
            engineShuttingDown = false;
            engineStalled = false;
            engineStartHoldSeconds = 0f;
            requiredStartHoldSeconds = 0f;

            currentRPM = 0f;
            targetRPM = 0f;

            componentHealthPercent = 100f;
            accumulatedDamagePercent = 0f;
            engineLoad = 0f;
            engineTorqueNm = 0f;
            engineBrakeTorqueNm = 0f;
            engineTemperatureC = 0f;
            thermalEfficiency = 1f;
            engineTemperatureWarning = false;
            engineOverheated = false;
            engineThermalDerateActive = false;
            lastTemperatureWarningCode = string.Empty;
            lastTemperatureWarningMessage = string.Empty;
            lastTemperatureSeverity = 0f;

            isBelowIdle = false;
            isInCriticalZone = false;
            isOverRevving = false;
            revLimiterActive = false;
            revLimiterTorqueFactor = 1f;
            hasEngineWarning = false;
            starterOveruseWarning = false;
            hasLaunchWarning = false;
            hasLaunchMisuse = false;
            launchStallRisk = false;
            clutchFrictionRPMDrop = 0f;
            clutchFrictionLoad = 0f;
            idleLaunchAssistFactor = 0f;
            lastLaunchWarningCode = string.Empty;
            lastLaunchWarningMessage = string.Empty;
            lastLaunchSeverity = 0f;
            lastStarterWarningCode = string.Empty;
            lastStarterWarningMessage = string.Empty;
            lastStarterSeverity = 0f;

            simulationTick = 0;
            lastUpdateTime = 0f;
        }

        public void InitializeFromConfig(EngineConfig config, bool startEngineOff = true)
        {
            ApplyConfig(config);

            if (startEngineOff)
            {
                ResetRuntime();
                return;
            }

            engineOn = true;
            engineStarting = false;
            engineShuttingDown = false;
            engineStalled = false;
            engineStartHoldSeconds = 0f;
            requiredStartHoldSeconds = 0f;

            currentRPM = idleRPM;
            targetRPM = idleRPM;

            componentHealthPercent = 100f;
            accumulatedDamagePercent = 0f;
            engineLoad = 0f;
            engineTorqueNm = 0f;
            engineBrakeTorqueNm = 0f;
            engineTemperatureC = config != null ? config.engineInitialTemperatureC : 45f;
            thermalEfficiency = 1f;
            engineTemperatureWarning = false;
            engineOverheated = false;
            engineThermalDerateActive = false;
            lastTemperatureWarningCode = string.Empty;
            lastTemperatureWarningMessage = string.Empty;
            lastTemperatureSeverity = 0f;
            revLimiterActive = false;
            revLimiterTorqueFactor = 1f;
            starterOveruseWarning = false;
            hasLaunchWarning = false;
            hasLaunchMisuse = false;
            launchStallRisk = false;
            clutchFrictionRPMDrop = 0f;
            clutchFrictionLoad = 0f;
            idleLaunchAssistFactor = 0f;
            lastLaunchWarningCode = string.Empty;
            lastLaunchWarningMessage = string.Empty;
            lastLaunchSeverity = 0f;
            lastStarterWarningCode = string.Empty;
            lastStarterWarningMessage = string.Empty;
            lastStarterSeverity = 0f;

            EvaluateFlags();
        }
    }
}
