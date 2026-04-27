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
        [SerializeField] private bool logConfigChanges = true;

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
                $" | SimulationSpeed={state.simulationSpeed:F2} {GetSpeedUnitLabel(state.simulationSpeedUnit)}" +
                $" | MotorSpeedPercent={state.motorSpeedPercent:F1}" +
                $" | EngineRPM={state.engineRPM:F0}" +
                $" | EngineOn={state.engineOn}" +
                $" | CurrentGear={FormatGear(state.currentGear)}" +
                $" | AccelMS2={state.accelerationMS2:F2}" +
                $" | Brake={state.brakeInput:F2}" +
                $" | EngineBrakeN={state.engineBrakeForceN:F0}");
        }

        private void HandleConfigChanged(VehicleOutputConfig config)
        {
            if (!logConfigChanges || config == null) return;

            Debug.Log(
                $"[TiltDrive][VehicleOutputConfig]" +
                $" | Name={config.outputName}" +
                $" | SpeedUnit={GetSpeedUnitLabel(config.speedDisplayUnit)}" +
                $" | WheelRadiusM={config.wheelRadiusMeters:F2}" +
                $" | MassKg={config.vehicleMassKg:F0}" +
                $" | Drag={config.aerodynamicDragCoefficient:F2}" +
                $" | Rolling={config.rollingResistanceCoefficient:F3}" +
                $" | BrakeForceN={config.brakeForceN:F0}" +
                $" | TheoreticalMaxSpeedKMH={config.theoreticalMaxSpeedKmh:F2}" +
                $" | ClampSpeed={config.clampSpeedToTheoreticalMax}");
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
