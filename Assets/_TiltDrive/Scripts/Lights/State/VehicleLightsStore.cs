using System;
using UnityEngine;

namespace TiltDrive.LightingSystem
{
    public class VehicleLightsStore : MonoBehaviour
    {
        public static VehicleLightsStore Instance { get; private set; }

        [Header("Configuracion")]
        [SerializeField] private VehicleLightsConfig config = new VehicleLightsConfig();

        [Header("Estado Actual")]
        [SerializeField] private VehicleLightsState current = new VehicleLightsState();

        public VehicleLightsConfig Config => config;
        public VehicleLightsState Current => current;

        public event Action<VehicleLightsState> OnStateChanged;
        public event Action<VehicleLightsConfig> OnConfigChanged;

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
                config = new VehicleLightsConfig();
            }

            if (current == null)
            {
                current = new VehicleLightsState();
            }

            config.ClampValues();
            current.ResetRuntime();

            NotifyConfigChanged();
            NotifyStateChanged();
        }

        public void SetConfig(VehicleLightsConfig newConfig, bool resetRuntime = true)
        {
            if (newConfig == null)
            {
                Debug.LogWarning("[TiltDrive][VehicleLightsStore] Se intento asignar un VehicleLightsConfig nulo.");
                return;
            }

            config = newConfig;
            config.ClampValues();

            if (resetRuntime)
            {
                current.ResetRuntime();
                NotifyStateChanged();
            }

            NotifyConfigChanged();
        }

        public void ApplyStateSnapshot(VehicleLightsState snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[TiltDrive][VehicleLightsStore] Se intento aplicar un VehicleLightsState nulo.");
                return;
            }

            current.CopyFrom(snapshot);
            NotifyStateChanged();
        }

        public void ResetRuntime()
        {
            current.ResetRuntime();
            NotifyStateChanged();
        }

        public void ForceRefresh()
        {
            config.ClampValues();
            NotifyConfigChanged();
            NotifyStateChanged();
        }

        public void SetLowBeams(bool value)
        {
            if (current.lowBeamsOn == value) return;

            current.lowBeamsOn = value;

            if (!value && config.highBeamsTurnOffWithLowBeams)
            {
                current.highBeamsOn = false;
            }

            NotifyStateChanged();
        }

        public void ToggleLowBeams()
        {
            SetLowBeams(!current.lowBeamsOn);
        }

        public void SetHighBeams(bool value)
        {
            bool changed = current.highBeamsOn != value;
            current.highBeamsOn = value;

            if (value && config.highBeamsRequireLowBeams && !current.lowBeamsOn)
            {
                current.lowBeamsOn = true;
                changed = true;
            }

            if (changed)
            {
                NotifyStateChanged();
            }
        }

        public void ToggleHighBeams()
        {
            SetHighBeams(!current.highBeamsOn);
        }

        public void SetFogLights(bool value)
        {
            if (current.fogLightsOn == value) return;

            current.fogLightsOn = value;
            NotifyStateChanged();
        }

        public void ToggleFogLights()
        {
            SetFogLights(!current.fogLightsOn);
        }

        public void SetOtherLights(bool value)
        {
            if (current.otherLightsOn == value) return;

            current.otherLightsOn = value;
            NotifyStateChanged();
        }

        public void ToggleOtherLights()
        {
            SetOtherLights(!current.otherLightsOn);
        }

        public void SetHazardLights(bool value)
        {
            if (current.hazardLightsOn == value) return;

            current.hazardLightsOn = value;
            NotifyStateChanged();
        }

        public void ToggleHazardLights()
        {
            SetHazardLights(!current.hazardLightsOn);
        }

        public void SetLeftTurnSignal(bool value)
        {
            bool changed = current.leftTurnSignalRequested != value ||
                (value && current.rightTurnSignalRequested);

            current.leftTurnSignalRequested = value;

            if (value)
            {
                current.rightTurnSignalRequested = false;
            }

            if (changed)
            {
                NotifyStateChanged();
            }
        }

        public void ToggleLeftTurnSignal()
        {
            SetLeftTurnSignal(!current.leftTurnSignalRequested);
        }

        public void SetRightTurnSignal(bool value)
        {
            bool changed = current.rightTurnSignalRequested != value ||
                (value && current.leftTurnSignalRequested);

            current.rightTurnSignalRequested = value;

            if (value)
            {
                current.leftTurnSignalRequested = false;
            }

            if (changed)
            {
                NotifyStateChanged();
            }
        }

        public void ToggleRightTurnSignal()
        {
            SetRightTurnSignal(!current.rightTurnSignalRequested);
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
