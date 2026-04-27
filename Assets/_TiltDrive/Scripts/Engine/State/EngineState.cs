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

        public void ResetRuntime()
        {
            engineOn = false;
            engineStarting = false;
            engineShuttingDown = false;
            engineStalled = false;

            currentRPM = 0f;
            targetRPM = 0f;

            engineLoad = 0f;
            engineTorqueNm = 0f;
            engineBrakeTorqueNm = 0f;

            isBelowIdle = false;
            isInCriticalZone = false;
            isOverRevving = false;
            hasEngineWarning = false;

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

            currentRPM = idleRPM;
            targetRPM = idleRPM;

            engineLoad = 0f;
            engineTorqueNm = 0f;
            engineBrakeTorqueNm = 0f;

            EvaluateFlags();
        }
    }
}