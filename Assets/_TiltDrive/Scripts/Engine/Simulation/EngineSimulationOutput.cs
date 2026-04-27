using System;
using UnityEngine;

namespace TiltDrive.EngineSystem
{
    [Serializable]
    public class EngineSimulationOutput
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

        [Tooltip("Texto corto de diagnóstico del tick actual.")]
        public string diagnosticMessage = string.Empty;

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
                hasLaunchWarning ||
                launchStallRisk;
        }

        public void Reset()
        {
            engineOn = false;
            engineStarting = false;
            engineShuttingDown = false;
            engineStalled = false;
            engineStartHoldSeconds = 0f;
            requiredStartHoldSeconds = 0f;

            currentRPM = 0f;
            targetRPM = 0f;

            idleRPM = 1200f;
            stallRPM = 500f;
            maxRPM = 10000f;
            criticalRPM = 10500f;

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

            diagnosticMessage = string.Empty;

            simulationTick = 0;
            lastUpdateTime = 0f;
        }

        public EngineState ToEngineState()
        {
            EngineState state = new EngineState
            {
                engineOn = engineOn,
                engineStarting = engineStarting,
                engineShuttingDown = engineShuttingDown,
                engineStalled = engineStalled,
                engineStartHoldSeconds = Mathf.Max(0f, engineStartHoldSeconds),
                requiredStartHoldSeconds = Mathf.Max(0f, requiredStartHoldSeconds),

                currentRPM = Mathf.Max(0f, currentRPM),
                targetRPM = Mathf.Max(0f, targetRPM),

                idleRPM = Mathf.Max(0f, idleRPM),
                stallRPM = Mathf.Max(0f, stallRPM),
                maxRPM = Mathf.Max(0f, maxRPM),
                criticalRPM = Mathf.Max(0f, criticalRPM),

                componentHealthPercent = Mathf.Clamp(componentHealthPercent, 0f, 100f),
                accumulatedDamagePercent = Mathf.Max(0f, accumulatedDamagePercent),
                engineLoad = Mathf.Max(0f, engineLoad),
                engineTorqueNm = engineTorqueNm,
                engineBrakeTorqueNm = Mathf.Max(0f, engineBrakeTorqueNm),
                engineTemperatureC = Mathf.Max(0f, engineTemperatureC),
                thermalEfficiency = Mathf.Clamp01(thermalEfficiency),
                engineTemperatureWarning = engineTemperatureWarning,
                engineOverheated = engineOverheated,
                engineThermalDerateActive = engineThermalDerateActive,
                lastTemperatureWarningCode = lastTemperatureWarningCode,
                lastTemperatureWarningMessage = lastTemperatureWarningMessage,
                lastTemperatureSeverity = Mathf.Max(0f, lastTemperatureSeverity),

                revLimiterActive = revLimiterActive,
                revLimiterTorqueFactor = Mathf.Clamp01(revLimiterTorqueFactor),
                starterOveruseWarning = starterOveruseWarning,
                hasLaunchWarning = hasLaunchWarning,
                hasLaunchMisuse = hasLaunchMisuse,
                launchStallRisk = launchStallRisk,
                clutchFrictionRPMDrop = Mathf.Max(0f, clutchFrictionRPMDrop),
                clutchFrictionLoad = Mathf.Clamp01(clutchFrictionLoad),
                idleLaunchAssistFactor = Mathf.Clamp01(idleLaunchAssistFactor),
                lastLaunchWarningCode = lastLaunchWarningCode,
                lastLaunchWarningMessage = lastLaunchWarningMessage,
                lastLaunchSeverity = Mathf.Max(0f, lastLaunchSeverity),
                lastStarterWarningCode = lastStarterWarningCode,
                lastStarterWarningMessage = lastStarterWarningMessage,
                lastStarterSeverity = Mathf.Max(0f, lastStarterSeverity),

                simulationTick = Mathf.Max(0, simulationTick),
                lastUpdateTime = Mathf.Max(0f, lastUpdateTime)
            };

            state.EvaluateFlags();
            return state;
        }

        public void CopyFromState(EngineState state)
        {
            if (state == null) return;

            engineOn = state.engineOn;
            engineStarting = state.engineStarting;
            engineShuttingDown = state.engineShuttingDown;
            engineStalled = state.engineStalled;
            engineStartHoldSeconds = Mathf.Max(0f, state.engineStartHoldSeconds);
            requiredStartHoldSeconds = Mathf.Max(0f, state.requiredStartHoldSeconds);

            currentRPM = Mathf.Max(0f, state.currentRPM);
            targetRPM = Mathf.Max(0f, state.targetRPM);

            idleRPM = Mathf.Max(0f, state.idleRPM);
            stallRPM = Mathf.Max(0f, state.stallRPM);
            maxRPM = Mathf.Max(0f, state.maxRPM);
            criticalRPM = Mathf.Max(0f, state.criticalRPM);

            componentHealthPercent = Mathf.Clamp(state.componentHealthPercent, 0f, 100f);
            accumulatedDamagePercent = Mathf.Max(0f, state.accumulatedDamagePercent);
            engineLoad = Mathf.Max(0f, state.engineLoad);
            engineTorqueNm = state.engineTorqueNm;
            engineBrakeTorqueNm = Mathf.Max(0f, state.engineBrakeTorqueNm);
            engineTemperatureC = Mathf.Max(0f, state.engineTemperatureC);
            thermalEfficiency = Mathf.Clamp01(state.thermalEfficiency);
            engineTemperatureWarning = state.engineTemperatureWarning;
            engineOverheated = state.engineOverheated;
            engineThermalDerateActive = state.engineThermalDerateActive;
            lastTemperatureWarningCode = state.lastTemperatureWarningCode;
            lastTemperatureWarningMessage = state.lastTemperatureWarningMessage;
            lastTemperatureSeverity = Mathf.Max(0f, state.lastTemperatureSeverity);

            isBelowIdle = state.isBelowIdle;
            isInCriticalZone = state.isInCriticalZone;
            isOverRevving = state.isOverRevving;
            revLimiterActive = state.revLimiterActive;
            revLimiterTorqueFactor = Mathf.Clamp01(state.revLimiterTorqueFactor);
            starterOveruseWarning = state.starterOveruseWarning;
            hasLaunchWarning = state.hasLaunchWarning;
            hasLaunchMisuse = state.hasLaunchMisuse;
            launchStallRisk = state.launchStallRisk;
            clutchFrictionRPMDrop = Mathf.Max(0f, state.clutchFrictionRPMDrop);
            clutchFrictionLoad = Mathf.Clamp01(state.clutchFrictionLoad);
            idleLaunchAssistFactor = Mathf.Clamp01(state.idleLaunchAssistFactor);
            lastLaunchWarningCode = state.lastLaunchWarningCode;
            lastLaunchWarningMessage = state.lastLaunchWarningMessage;
            lastLaunchSeverity = Mathf.Max(0f, state.lastLaunchSeverity);
            lastStarterWarningCode = state.lastStarterWarningCode;
            lastStarterWarningMessage = state.lastStarterWarningMessage;
            lastStarterSeverity = Mathf.Max(0f, state.lastStarterSeverity);
            hasEngineWarning = state.hasEngineWarning;

            simulationTick = Mathf.Max(0, state.simulationTick);
            lastUpdateTime = Mathf.Max(0f, state.lastUpdateTime);
        }
    }
}
