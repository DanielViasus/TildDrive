using System;
using UnityEngine;

namespace TiltDrive.TransmissionSystem
{
    public class TransmissionStore : MonoBehaviour
    {
        public static TransmissionStore Instance { get; private set; }

        [Header("Configuración")]
        [SerializeField] private TransmissionConfig config = new TransmissionConfig();

        [Header("Estado Actual")]
        [SerializeField] private TransmissionState current = new TransmissionState();

        public TransmissionConfig Config => config;
        public TransmissionState Current => current;

        public event Action<TransmissionState> OnStateChanged;
        public event Action<TransmissionConfig> OnConfigChanged;

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
                config = new TransmissionConfig();
            }

            if (current == null)
            {
                current = new TransmissionState();
            }

            config.ClampValues();
            current.InitializeFromConfig(config, 0);

            NotifyStateChanged();
            NotifyConfigChanged();
        }

        // --------------------------------------------------
        // CONFIG
        // --------------------------------------------------

        public void SetConfig(TransmissionConfig newConfig, bool reinitializeState = true, int startGear = 0)
        {
            if (newConfig == null)
            {
                Debug.LogWarning("[TiltDrive][TransmissionStore] Se intentó asignar un TransmissionConfig nulo.");
                return;
            }

            config = newConfig;
            config.ClampValues();

            if (reinitializeState)
            {
                current.InitializeFromConfig(config, startGear);
                NotifyStateChanged();
            }

            NotifyConfigChanged();
        }

        public void ReapplyConfigToState()
        {
            if (config == null || current == null) return;

            config.ClampValues();
            current.ApplyConfig(config);

            NotifyConfigChanged();
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // INICIALIZACION / RESET
        // --------------------------------------------------

        public void InitializeFromConfig(int startGear = 0)
        {
            if (config == null)
            {
                Debug.LogWarning("[TiltDrive][TransmissionStore] No hay TransmissionConfig para inicializar.");
                return;
            }

            current.InitializeFromConfig(config, startGear);
            NotifyStateChanged();
        }

        public void ResetRuntime()
        {
            current.ResetRuntime(config);
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // MARCHA / CAMBIO
        // --------------------------------------------------

        public void SetCurrentGear(int gear)
        {
            if (current.currentGear == gear) return;

            current.currentGear = gear;
            current.UpdateGearRatios(config);
            current.EvaluateFlags(config);
            NotifyStateChanged();
        }

        public void SetRequestedGear(int gear)
        {
            if (current.requestedGear == gear) return;

            current.requestedGear = gear;
            current.EvaluateFlags(config);
            NotifyStateChanged();
        }

        public void SetShiftInProgress(bool value)
        {
            if (current.shiftInProgress == value) return;

            current.shiftInProgress = value;
            current.EvaluateFlags(config);
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // CLUTCH
        // --------------------------------------------------

        public void SetClutchInput(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(current.clutchInput, clamped)) return;

            current.clutchInput = clamped;
            NotifyStateChanged();
        }

        public void SetClutchEngagement(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(current.clutchEngagement, clamped)) return;

            current.clutchEngagement = clamped;
            current.clutchDisengaged = clamped <= 0.05f;
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // RELACIONES
        // --------------------------------------------------

        public void RefreshGearRatios()
        {
            current.UpdateGearRatios(config);
            current.EvaluateFlags(config);
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // ENTREGA
        // --------------------------------------------------

        public void SetInputTorqueNm(float value)
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(current.inputTorqueNm, clamped)) return;

            current.inputTorqueNm = clamped;
            NotifyStateChanged();
        }

        public void SetTransmittedTorqueNm(float value)
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(current.transmittedTorqueNm, clamped)) return;

            current.transmittedTorqueNm = clamped;
            current.outputTorqueNm = clamped;
            NotifyStateChanged();
        }

        public void SetInputRPM(float value)
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(current.inputRPM, clamped)) return;

            current.inputRPM = clamped;
            NotifyStateChanged();
        }

        public void SetOutput(float torqueNm, float rpm)
        {
            float torqueClamped = Mathf.Max(0f, torqueNm);
            bool changed = false;

            if (!Mathf.Approximately(current.outputTorqueNm, torqueClamped))
            {
                current.outputTorqueNm = torqueClamped;
                current.transmittedTorqueNm = torqueClamped;
                changed = true;
            }

            if (!Mathf.Approximately(current.outputRPM, rpm))
            {
                current.outputRPM = rpm;
                changed = true;
            }

            if (changed)
            {
                NotifyStateChanged();
            }
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

        public void ApplyStateSnapshot(TransmissionState snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[TiltDrive][TransmissionStore] Se intentó aplicar un TransmissionState nulo.");
                return;
            }

            current.transmissionType = snapshot.transmissionType;

            current.currentGear = snapshot.currentGear;
            current.requestedGear = snapshot.requestedGear;
            current.shiftInProgress = snapshot.shiftInProgress;
            current.shiftTimer = Mathf.Max(0f, snapshot.shiftTimer);

            current.clutchInput = Mathf.Clamp01(snapshot.clutchInput);
            current.clutchEngagement = Mathf.Clamp01(snapshot.clutchEngagement);
            current.clutchDisengaged = snapshot.clutchDisengaged;

            current.currentGearRatio = Mathf.Max(0f, snapshot.currentGearRatio);
            current.finalDriveRatio = Mathf.Max(0f, snapshot.finalDriveRatio);
            current.totalDriveRatio = Mathf.Max(0f, snapshot.totalDriveRatio);

            current.componentHealthPercent = Mathf.Clamp(snapshot.componentHealthPercent, 0f, 100f);
            current.accumulatedDamagePercent = Mathf.Max(0f, snapshot.accumulatedDamagePercent);
            current.inputTorqueNm = Mathf.Max(0f, snapshot.inputTorqueNm);
            current.inputRPM = Mathf.Max(0f, snapshot.inputRPM);
            current.outputTorqueNm = Mathf.Max(0f, snapshot.outputTorqueNm);
            current.outputRPM = snapshot.outputRPM;
            current.transmittedTorqueNm = Mathf.Max(0f, snapshot.transmittedTorqueNm);

            current.driveDirection = snapshot.driveDirection;

            current.isNeutral = snapshot.isNeutral;
            current.isReverse = snapshot.isReverse;
            current.shiftAllowed = snapshot.shiftAllowed;
            current.hasTransmissionWarning = snapshot.hasTransmissionWarning;
            current.hasMisuseWarning = snapshot.hasMisuseWarning;
            current.lastMisuseCode = snapshot.lastMisuseCode;
            current.lastMisuseMessage = snapshot.lastMisuseMessage;
            current.lastMisuseSeverity = Mathf.Max(0f, snapshot.lastMisuseSeverity);
            current.lastRequiredEngineRPM = Mathf.Max(0f, snapshot.lastRequiredEngineRPM);
            current.lastMisuseEngineRPM = Mathf.Max(0f, snapshot.lastMisuseEngineRPM);
            current.lastMisuseThrottleInput = Mathf.Clamp01(snapshot.lastMisuseThrottleInput);

            current.simulationTick = Mathf.Max(0, snapshot.simulationTick);
            current.lastUpdateTime = Mathf.Max(0f, snapshot.lastUpdateTime);

            current.EvaluateFlags(config);
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // UTILS
        // --------------------------------------------------

        public void ForceRefresh()
        {
            current.UpdateGearRatios(config);
            current.EvaluateFlags(config);
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
