using System;
using UnityEngine;

namespace TiltDrive.ElectricalSystem
{
    public class VehicleElectricalStore : MonoBehaviour
    {
        public static VehicleElectricalStore Instance { get; private set; }

        [Header("Configuracion")]
        [SerializeField] private VehicleElectricalConfig config = new VehicleElectricalConfig();

        [Header("Estado Actual")]
        [SerializeField] private VehicleElectricalState current = new VehicleElectricalState();

        public VehicleElectricalConfig Config => config;
        public VehicleElectricalState Current => current;

        public event Action<VehicleElectricalState> OnStateChanged;
        public event Action<VehicleElectricalConfig> OnConfigChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            config ??= new VehicleElectricalConfig();
            current ??= new VehicleElectricalState();

            config.ClampValues();
            current.InitializeFromConfig(config);

            NotifyConfigChanged();
            NotifyStateChanged();
        }

        public void SetConfig(VehicleElectricalConfig newConfig, bool resetRuntime = true)
        {
            if (newConfig == null)
            {
                Debug.LogWarning("[TiltDrive][VehicleElectricalStore] Se intento asignar un VehicleElectricalConfig nulo.");
                return;
            }

            config = newConfig;
            config.ClampValues();

            if (resetRuntime)
            {
                current.InitializeFromConfig(config);
                NotifyStateChanged();
            }

            NotifyConfigChanged();
        }

        public void ApplyStateSnapshot(VehicleElectricalState snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[TiltDrive][VehicleElectricalStore] Se intento aplicar un VehicleElectricalState nulo.");
                return;
            }

            current.CopyFrom(snapshot);
            NotifyStateChanged();
        }

        public void ResetRuntime()
        {
            current.InitializeFromConfig(config);
            NotifyStateChanged();
        }

        public void SetBatteryChargePercent(float value)
        {
            config.ClampValues();
            float clamped = Mathf.Clamp(value, 0f, 100f);
            current.batteryChargePercent = clamped;
            current.batteryChargeAh = config.batteryCapacityAh * (clamped / 100f);
            current.batteryVoltage = VehicleElectricalState.CalculateOpenCircuitVoltage(config, clamped);
            current.systemVoltage = current.batteryVoltage;
            NotifyStateChanged();
        }

        public void ForceRefresh()
        {
            config.ClampValues();
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
