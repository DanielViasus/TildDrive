using UnityEngine;
using TiltDrive.VehicleSystem;

namespace TiltDrive.DebugSystem
{
    public class VehicleOutputDebugView : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private VehicleOutputStore store;

        [Header("Logs")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logConfigChanges = false;

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
                Debug.LogError("[TiltDrive][VehicleOutputDebugView] No se encontro VehicleOutputStore.");
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
                store = VehicleOutputStore.Instance;
            }

            if (store == null)
            {
                store = GetComponent<VehicleOutputStore>();
            }

            if (store == null)
            {
                store = FindFirstObjectByType<VehicleOutputStore>();
            }
        }

        private void HandleStateChanged(VehicleOutputState state)
        {
            if (!logStateChanges || state == null) return;

            Debug.Log(
                $"[TiltDrive][VehicleOutputState]" +
                $" | Direction={state.effectiveSteerInput:F2}" +
                $" | FinalSpeedKMH={state.finalSpeedKMH:F1}" +
                $" | EngineRPM={state.engineRPM:F0}" +
                $" | Gear={FormatGear(state.currentGear)}" +
                $" | HandBrake={state.handbrakeActive}" +
                $" | EngineOn={state.engineOn}" +
                $" | MotorSpeed={state.motorSpeedPercent:F1}%");
        }

        private void HandleConfigChanged(VehicleOutputConfig config)
        {
            if (!logConfigChanges || config == null) return;

            Debug.Log(
                $"[TiltDrive][VehicleOutputConfig]" +
                $" | Name={config.outputName}" +
                $" | SpeedUnit={GetSpeedUnitLabel(config.speedDisplayUnit)}");
        }

        private static string GetSpeedUnitLabel(VehicleSpeedUnit speedUnit)
        {
            return speedUnit == VehicleSpeedUnit.MilesPerHour ? "mph" : "km/h";
        }

        private static string FormatGear(int gear)
        {
            if (gear == 0) return "N";
            if (gear < 0) return "R";
            return gear.ToString();
        }
    }
}
