using UnityEngine;
using TiltDrive.ElectricalSystem;

namespace TiltDrive.DebugSystem
{
    public class VehicleElectricalDebugView : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private VehicleElectricalStore store;

        [Header("Logs")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logConfigChanges = false;
        [SerializeField] private bool logWarnings = true;

        private string previousStateSignature = string.Empty;
        private string previousWarningCode = string.Empty;

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
                Debug.LogError("[TiltDrive][VehicleElectricalDebugView] No se encontro VehicleElectricalStore.");
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
                store = VehicleElectricalStore.Instance;
            }

            if (store == null)
            {
                store = GetComponent<VehicleElectricalStore>();
            }

            if (store == null)
            {
                store = FindFirstObjectByType<VehicleElectricalStore>();
            }
        }

        private void HandleStateChanged(VehicleElectricalState state)
        {
            if (state == null) return;

            if (logWarnings && state.hasElectricalWarning && state.lastWarningCode != previousWarningCode)
            {
                Debug.LogWarning(
                    $"[TiltDrive][ElectricalWarning]" +
                    $" | Code={state.lastWarningCode}" +
                    $" | Voltage={state.systemVoltage:F2}V" +
                    $" | Sag={state.voltageSag:F2}V" +
                    $" | Battery={state.batteryChargePercent:F1}%" +
                    $" | Load={state.loadCurrentAmps:F1}A" +
                    $" | Message={state.lastWarningMessage}");
            }

            previousWarningCode = state.hasElectricalWarning
                ? state.lastWarningCode
                : string.Empty;

            if (!logStateChanges) return;

            string stateSignature =
                $"{state.batteryChargePercent:F0}|{state.systemVoltage:F1}|{state.alternatorActive}|" +
                $"{state.starterActive}|{state.starterHoldSeconds:F0}|{state.ignitionAvailable}|{state.lightsPowerAvailable}|" +
                $"{state.loadCurrentAmps:F0}|{state.alternatorChargeAmps:F0}";

            if (stateSignature == previousStateSignature)
            {
                return;
            }

            previousStateSignature = stateSignature;

            Debug.Log(
                $"[TiltDrive][ElectricalState]" +
                $" | Voltage={state.systemVoltage:F2}V" +
                $" | Sag={state.voltageSag:F2}V" +
                $" | Battery={state.batteryChargePercent:F1}%" +
                $" | Load={state.loadCurrentAmps:F1}A" +
                $" | Charge={state.alternatorChargeAmps:F1}A" +
                $" | Alternator={state.alternatorActive}" +
                $" | Starter={state.starterActive}" +
                $" | StarterHold={state.starterHoldSeconds:F2}s" +
                $" | Ignition={state.ignitionAvailable}" +
                $" | LightsPower={state.lightsPowerAvailable}" +
                $" | Brightness={state.lightsBrightnessFactor:F2}");
        }

        private void HandleConfigChanged(VehicleElectricalConfig config)
        {
            if (!logConfigChanges || config == null) return;

            Debug.Log(
                $"[TiltDrive][ElectricalConfig]" +
                $" | Name={config.profileName}" +
                $" | Capacity={config.batteryCapacityAh:F1}Ah" +
                $" | Alternator={config.alternatorMaxChargeAmps:F1}A" +
                $" | MinIgnition={config.minimumIgnitionVoltage:F1}V");
        }
    }
}
