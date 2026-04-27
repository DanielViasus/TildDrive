using System;
using UnityEngine;

namespace TiltDrive.EngineSystem
{
    public class EngineStore : MonoBehaviour
    {
        public static EngineStore Instance { get; private set; }

        [Header("Configuración")]
        [SerializeField] private EngineConfig config = new EngineConfig();

        [Header("Estado Actual")]
        [SerializeField] private EngineState current = new EngineState();

        public EngineConfig Config => config;
        public EngineState Current => current;

        public event Action<EngineState> OnStateChanged;
        public event Action<EngineConfig> OnConfigChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (config == null)
            {
                config = new EngineConfig();
            }

            if (current == null)
            {
                current = new EngineState();
            }

            config.ClampValues();
            current.InitializeFromConfig(config, true);
            NotifyStateChanged();
            NotifyConfigChanged();
        }

        // --------------------------------------------------
        // CONFIG
        // --------------------------------------------------

        public void SetConfig(EngineConfig newConfig, bool reinitializeState = true, bool startEngineOff = true)
        {
            if (newConfig == null)
            {
                Debug.LogWarning("[TiltDrive][EngineStore] Se intentó asignar un EngineConfig nulo.");
                return;
            }

            config = newConfig;
            config.ClampValues();

            if (reinitializeState)
            {
                current.InitializeFromConfig(config, startEngineOff);
                current.EvaluateFlags();
                NotifyStateChanged();
            }

            NotifyConfigChanged();
        }

        public void ReapplyConfigToState()
        {
            if (config == null || current == null) return;

            config.ClampValues();
            current.ApplyConfig(config);
            current.EvaluateFlags();

            NotifyConfigChanged();
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // INICIALIZACION / RESET
        // --------------------------------------------------

        public void InitializeFromConfig(bool startEngineOff = true)
        {
            if (config == null)
            {
                Debug.LogWarning("[TiltDrive][EngineStore] No hay EngineConfig para inicializar.");
                return;
            }

            current.InitializeFromConfig(config, startEngineOff);
            current.EvaluateFlags();
            NotifyStateChanged();
        }

        public void ResetRuntime()
        {
            current.ResetRuntime();
            current.ApplyConfig(config);
            current.EvaluateFlags();
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // ESTADO LOGICO
        // --------------------------------------------------

        public void SetEngineOn(bool value)
        {
            if (current.engineOn == value) return;

            current.engineOn = value;
            current.EvaluateFlags();
            NotifyStateChanged();
        }

        public void SetEngineStarting(bool value)
        {
            if (current.engineStarting == value) return;

            current.engineStarting = value;
            current.EvaluateFlags();
            NotifyStateChanged();
        }

        public void SetEngineStalled(bool value)
        {
            if (current.engineStalled == value) return;

            current.engineStalled = value;
            current.EvaluateFlags();
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // REGIMEN
        // --------------------------------------------------

        public void SetCurrentRPM(float value)
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(current.currentRPM, clamped)) return;

            current.currentRPM = clamped;
            current.EvaluateFlags();
            NotifyStateChanged();
        }

        public void SetTargetRPM(float value)
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(current.targetRPM, clamped)) return;

            current.targetRPM = clamped;
            current.EvaluateFlags();
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // ESFUERZO Y ENTREGA
        // --------------------------------------------------

        public void SetEngineLoad(float value)
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(current.engineLoad, clamped)) return;

            current.engineLoad = clamped;
            NotifyStateChanged();
        }

        public void SetEngineTorqueNm(float value)
        {
            if (Mathf.Approximately(current.engineTorqueNm, value)) return;

            current.engineTorqueNm = value;
            NotifyStateChanged();
        }

        public void SetEngineBrakeTorqueNm(float value)
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(current.engineBrakeTorqueNm, clamped)) return;

            current.engineBrakeTorqueNm = clamped;
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // TRAZABILIDAD
        // --------------------------------------------------

        public void SetSimulationTick(int value)
        {
            int clamped = Mathf.Max(0, value);
            if (current.simulationTick == clamped) return;

            current.simulationTick = clamped;
            NotifyStateChanged();
        }

        public void SetLastUpdateTime(float value)
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(current.lastUpdateTime, clamped)) return;

            current.lastUpdateTime = clamped;
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // SNAPSHOT / APLICACION MASIVA
        // --------------------------------------------------

        public void ApplyStateSnapshot(EngineState snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[TiltDrive][EngineStore] Se intentó aplicar un EngineState nulo.");
                return;
            }

            current.engineOn = snapshot.engineOn;
            current.engineStarting = snapshot.engineStarting;
            current.engineShuttingDown = snapshot.engineShuttingDown;
            current.engineStalled = snapshot.engineStalled;

            current.currentRPM = Mathf.Max(0f, snapshot.currentRPM);
            current.targetRPM = Mathf.Max(0f, snapshot.targetRPM);

            current.idleRPM = Mathf.Max(0f, snapshot.idleRPM);
            current.stallRPM = Mathf.Max(0f, snapshot.stallRPM);
            current.maxRPM = Mathf.Max(0f, snapshot.maxRPM);
            current.criticalRPM = Mathf.Max(0f, snapshot.criticalRPM);

            current.engineLoad = Mathf.Max(0f, snapshot.engineLoad);
            current.engineTorqueNm = snapshot.engineTorqueNm;
            current.engineBrakeTorqueNm = Mathf.Max(0f, snapshot.engineBrakeTorqueNm);

            current.simulationTick = Mathf.Max(0, snapshot.simulationTick);
            current.lastUpdateTime = Mathf.Max(0f, snapshot.lastUpdateTime);

            current.EvaluateFlags();
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // UTILS
        // --------------------------------------------------

        public void ForceRefresh()
        {
            current.EvaluateFlags();
            NotifyConfigChanged();
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke(current);
        }

        private void NotifyConfigChanged()
        {
            OnConfigChanged?.Invoke(config);
        }
    }
}
