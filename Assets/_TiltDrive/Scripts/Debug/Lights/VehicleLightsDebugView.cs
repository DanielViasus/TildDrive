using UnityEngine;
using TiltDrive.LightingSystem;

namespace TiltDrive.DebugSystem
{
    public class VehicleLightsDebugView : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private VehicleLightsStore store;

        [Header("Logs")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logConfigChanges = false;

        private string previousStateSignature = string.Empty;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (store != null)
            {
                store.OnStateChanged += HandleStateChanged;
                store.OnConfigChanged += HandleConfigChanged;
            }
            else
            {
                Debug.LogError("[TiltDrive][VehicleLightsDebugView] No se encontro VehicleLightsStore.");
            }
        }

        private void OnDisable()
        {
            if (store != null)
            {
                store.OnStateChanged -= HandleStateChanged;
                store.OnConfigChanged -= HandleConfigChanged;
            }
        }

        private void EnsureReferences()
        {
            if (store == null)
            {
                store = VehicleLightsStore.Instance;
            }

            if (store == null)
            {
                store = GetComponent<VehicleLightsStore>();
            }

            if (store == null)
            {
                store = FindFirstObjectByType<VehicleLightsStore>();
            }
        }

        private void HandleStateChanged(VehicleLightsState state)
        {
            if (!logStateChanges || state == null) return;

            string stateSignature =
                $"{state.leftTurnSignalOn}|{state.rightTurnSignalOn}|{state.lowBeamsOn}|{state.highBeamsOn}|" +
                $"{state.fogLightsOn}|{state.brakeLightsOn}|{state.reverseLightsOn}|{state.otherLightsOn}|" +
                $"{state.hazardLightsOn}|{state.electricalPowerAvailable}|{state.brightnessFactor:F2}";

            if (stateSignature == previousStateSignature)
            {
                return;
            }

            previousStateSignature = stateSignature;

            Debug.Log(
                $"[TiltDrive][VehicleLightsState]" +
                $" | Left={state.leftTurnSignalOn}" +
                $" | Right={state.rightTurnSignalOn}" +
                $" | Low={state.lowBeamsOn}" +
                $" | High={state.highBeamsOn}" +
                $" | Fog={state.fogLightsOn}" +
                $" | Brake={state.brakeLightsOn}" +
                $" | Reverse={state.reverseLightsOn}" +
                $" | Other={state.otherLightsOn}" +
                $" | Hazard={state.hazardLightsOn}" +
                $" | Power={state.electricalPowerAvailable}" +
                $" | Brightness={state.brightnessFactor:F2}");
        }

        private void HandleConfigChanged(VehicleLightsConfig config)
        {
            if (!logConfigChanges || config == null) return;

            Debug.Log(
                $"[TiltDrive][VehicleLightsConfig]" +
                $" | Name={config.profileName}" +
                $" | BlinkHz={config.turnSignalFrequencyHz:F2}" +
                $" | BrakeThreshold={config.brakeLightInputThreshold:F2}");
        }
    }
}
