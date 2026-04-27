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

        [Header("Régimen")]
        [Min(0f)] public float currentRPM = 0f;
        [Min(0f)] public float targetRPM = 0f;

        [Min(0f)] public float idleRPM = 850f;
        [Min(0f)] public float stallRPM = 500f;
        [Min(0f)] public float maxRPM = 6500f;
        [Min(0f)] public float criticalRPM = 7000f;

        [Header("Esfuerzo y Entrega")]
        [Min(0f)] public float engineLoad = 0f;
        public float engineTorqueNm = 0f;
        [Min(0f)] public float engineBrakeTorqueNm = 0f;

        [Header("Diagnóstico")]
        public bool isBelowIdle = false;
        public bool isInCriticalZone = false;
        public bool isOverRevving = false;
        public bool hasEngineWarning = false;

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
            hasEngineWarning = isBelowIdle || isInCriticalZone || isOverRevving || engineStalled;
        }

        public void Reset()
        {
            engineOn = false;
            engineStarting = false;
            engineShuttingDown = false;
            engineStalled = false;

            currentRPM = 0f;
            targetRPM = 0f;

            idleRPM = 850f;
            stallRPM = 500f;
            maxRPM = 6500f;
            criticalRPM = 7000f;

            engineLoad = 0f;
            engineTorqueNm = 0f;
            engineBrakeTorqueNm = 0f;

            isBelowIdle = false;
            isInCriticalZone = false;
            isOverRevving = false;
            hasEngineWarning = false;

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

                currentRPM = Mathf.Max(0f, currentRPM),
                targetRPM = Mathf.Max(0f, targetRPM),

                idleRPM = Mathf.Max(0f, idleRPM),
                stallRPM = Mathf.Max(0f, stallRPM),
                maxRPM = Mathf.Max(0f, maxRPM),
                criticalRPM = Mathf.Max(0f, criticalRPM),

                engineLoad = Mathf.Max(0f, engineLoad),
                engineTorqueNm = engineTorqueNm,
                engineBrakeTorqueNm = Mathf.Max(0f, engineBrakeTorqueNm),

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

            currentRPM = Mathf.Max(0f, state.currentRPM);
            targetRPM = Mathf.Max(0f, state.targetRPM);

            idleRPM = Mathf.Max(0f, state.idleRPM);
            stallRPM = Mathf.Max(0f, state.stallRPM);
            maxRPM = Mathf.Max(0f, state.maxRPM);
            criticalRPM = Mathf.Max(0f, state.criticalRPM);

            engineLoad = Mathf.Max(0f, state.engineLoad);
            engineTorqueNm = state.engineTorqueNm;
            engineBrakeTorqueNm = Mathf.Max(0f, state.engineBrakeTorqueNm);

            isBelowIdle = state.isBelowIdle;
            isInCriticalZone = state.isInCriticalZone;
            isOverRevving = state.isOverRevving;
            hasEngineWarning = state.hasEngineWarning;

            simulationTick = Mathf.Max(0, state.simulationTick);
            lastUpdateTime = Mathf.Max(0f, state.lastUpdateTime);
        }
    }
}