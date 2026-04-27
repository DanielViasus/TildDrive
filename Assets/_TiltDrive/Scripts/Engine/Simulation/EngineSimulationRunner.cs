using UnityEngine;
using TiltDrive.State;
using TiltDrive.TransmissionSystem;
using TiltDrive.VehicleSystem;
using TiltDrive.Simulation;
using TiltDrive.ElectricalSystem;
using TiltDrive.CoolingSystem;

namespace TiltDrive.EngineSystem
{
    public class EngineSimulationRunner : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputStore inputStore;
        [SerializeField] private EngineStore engineStore;
        [SerializeField] private TransmissionStore transmissionStore;
        [SerializeField] private VehicleOutputStore vehicleOutputStore;
        [SerializeField] private VehicleElectricalStore electricalStore;
        [SerializeField] private RadiatorStore radiatorStore;

        [Header("Simulación")]
        [SerializeField] private bool runSimulation = true;
        [SerializeField] private bool autoFindReferences = true;

        [Header("Cargas Externas V1")]
        [Tooltip("Carga abstracta proveniente de transmisión o drivetrain.")]
        [SerializeField] [Min(0f)] private float transmissionLoad = 0f;

        [Tooltip("Masa estimada del vehículo en kilogramos.")]
        [SerializeField] [Min(0f)] private float vehicleMassKg = 1200f;

        [Tooltip("Ángulo de la pendiente. Positivo=subida, negativo=bajada.")]
        [SerializeField] private float slopeAngleDegrees = 0f;

        [Tooltip("Resistencia adicional abstracta.")]
        [SerializeField] [Min(0f)] private float additionalResistance = 0f;

        [Header("Control de Simulación")]
        [SerializeField] private bool ignitionAllowed = true;
        [SerializeField] private bool canStall = true;
        [SerializeField] private bool useExternalLoad = true;
        [SerializeField] private bool useDrivetrainRPMCoupling = true;

        [Header("Diagnostico de Arranque")]
        [SerializeField] private DriveLaunchDiagnosticsConfig launchDiagnosticsConfig = new DriveLaunchDiagnosticsConfig();

        [Header("Debug Interno")]
        [SerializeField] [Min(0)] private int simulationTick = 0;
        [SerializeField] private bool logLaunchDiagnostics = true;

        private DynamicEngine dynamicEngine;
        private EngineSimulationInput simulationInput;
        private string previousLaunchWarningCode = string.Empty;
        private bool previousLaunchStallRisk = false;
        private string previousStarterWarningCode = string.Empty;
        private string previousTemperatureWarningCode = string.Empty;

        public DriveLaunchDiagnosticsConfig LaunchDiagnosticsConfig => launchDiagnosticsConfig;

        private void Awake()
        {
            EnsureReferences();

            dynamicEngine = new DynamicEngine();
            simulationInput = new EngineSimulationInput();
        }

        private void Update()
        {
            if (!runSimulation) return;

            EnsureReferences();

            if (inputStore == null || engineStore == null)
            {
                return;
            }

            if (engineStore.Config == null || engineStore.Current == null)
            {
                return;
            }

            BuildSimulationInput(Time.deltaTime, Time.time);

            EngineSimulationOutput output = dynamicEngine.Simulate(simulationInput);
            output.simulationTick = simulationTick;
            output.lastUpdateTime = Time.time;

            engineStore.ApplyStateSnapshot(output.ToEngineState());
            LogLaunchDiagnostics(output);
            LogStarterDiagnostics(output);
            LogTemperatureDiagnostics(output);

            simulationTick++;
        }

        private void EnsureReferences()
        {
            if (!autoFindReferences) return;

            if (inputStore == null)
            {
                inputStore = InputStore.Instance;

                if (inputStore == null)
                {
                    inputStore = FindFirstObjectByType<InputStore>();
                }
            }

            if (engineStore == null)
            {
                engineStore = EngineStore.Instance;

                if (engineStore == null)
                {
                    engineStore = FindFirstObjectByType<EngineStore>();
                }
            }

            if (transmissionStore == null)
            {
                transmissionStore = TransmissionStore.Instance;

                if (transmissionStore == null)
                {
                    transmissionStore = FindFirstObjectByType<TransmissionStore>();
                }
            }

            if (vehicleOutputStore == null)
            {
                vehicleOutputStore = VehicleOutputStore.Instance;

                if (vehicleOutputStore == null)
                {
                    vehicleOutputStore = FindFirstObjectByType<VehicleOutputStore>();
                }
            }

            if (electricalStore == null)
            {
                electricalStore = VehicleElectricalStore.Instance;

                if (electricalStore == null)
                {
                    electricalStore = FindFirstObjectByType<VehicleElectricalStore>();
                }
            }

            if (radiatorStore == null)
            {
                radiatorStore = RadiatorStore.Instance;

                if (radiatorStore == null)
                {
                    radiatorStore = FindFirstObjectByType<RadiatorStore>();
                }
            }
        }

        private void BuildSimulationInput(float deltaTime, float simulationTime)
        {
            simulationInput.Reset();

            simulationInput.SetTime(deltaTime, simulationTime);
            simulationInput.SetConfig(engineStore.Config);
            simulationInput.SetLaunchDiagnosticsConfig(launchDiagnosticsConfig);
            simulationInput.SetEngineState(engineStore.Current);
            simulationInput.SetUserInput(inputStore.Current);
            simulationInput.SetRadiatorState(radiatorStore != null ? radiatorStore.Current : null);
            simulationInput.SetDrivetrainState(
                transmissionStore != null ? transmissionStore.Current : null,
                vehicleOutputStore != null ? vehicleOutputStore.Current : null);

            simulationInput.SetExternalLoad(
                transmissionLoad,
                vehicleMassKg,
                slopeAngleDegrees,
                additionalResistance
            );

            bool electricalIgnitionAllowed = electricalStore == null ||
                electricalStore.Current == null ||
                electricalStore.Current.ignitionAvailable;
            simulationInput.ignitionAllowed = ignitionAllowed && electricalIgnitionAllowed;
            simulationInput.canStall = canStall;
            simulationInput.useExternalLoad = useExternalLoad;
            simulationInput.useDrivetrainRPMCoupling = useDrivetrainRPMCoupling;
        }

        // --------------------------------------------------
        // SETTERS PUBLICOS PARA PRUEBAS / FUTURO
        // --------------------------------------------------

        public void SetRunSimulation(bool value)
        {
            runSimulation = value;
        }

        public void SetTransmissionLoad(float value)
        {
            transmissionLoad = Mathf.Max(0f, value);
        }

        public void SetVehicleMassKg(float value)
        {
            vehicleMassKg = Mathf.Max(0f, value);
        }

        public void SetSlopeAngleDegrees(float value)
        {
            slopeAngleDegrees = value;
        }

        public void SetAdditionalResistance(float value)
        {
            additionalResistance = Mathf.Max(0f, value);
        }

        public void SetIgnitionAllowed(bool value)
        {
            ignitionAllowed = value;
        }

        public void SetCanStall(bool value)
        {
            canStall = value;
        }

        public void SetUseExternalLoad(bool value)
        {
            useExternalLoad = value;
        }

        public void SetUseDrivetrainRPMCoupling(bool value)
        {
            useDrivetrainRPMCoupling = value;
        }

        public void ResetSimulationTick()
        {
            simulationTick = 0;
        }

        private void LogLaunchDiagnostics(EngineSimulationOutput output)
        {
            if (!logLaunchDiagnostics || output == null)
            {
                return;
            }

            if (!output.hasLaunchWarning)
            {
                previousLaunchWarningCode = string.Empty;
                previousLaunchStallRisk = false;
                return;
            }

            bool changed = output.lastLaunchWarningCode != previousLaunchWarningCode ||
                output.launchStallRisk != previousLaunchStallRisk;
            if (!changed)
            {
                return;
            }

            string level = output.hasLaunchMisuse ? "Misuse" : "Warning";
            Debug.LogWarning(
                $"[TiltDrive][Launch{level}]" +
                $" | Code={output.lastLaunchWarningCode}" +
                $" | Severity={output.lastLaunchSeverity:F2}" +
                $" | StallRisk={output.launchStallRisk}" +
                $" | EngineRPM={output.currentRPM:F0}" +
                $" | Message={output.lastLaunchWarningMessage}");

            previousLaunchWarningCode = output.lastLaunchWarningCode;
            previousLaunchStallRisk = output.launchStallRisk;
        }

        private void LogStarterDiagnostics(EngineSimulationOutput output)
        {
            if (!logLaunchDiagnostics || output == null)
            {
                return;
            }

            if (!output.starterOveruseWarning)
            {
                previousStarterWarningCode = string.Empty;
                return;
            }

            if (output.lastStarterWarningCode == previousStarterWarningCode)
            {
                return;
            }

            Debug.LogWarning(
                $"[TiltDrive][StarterMisuse]" +
                $" | Code={output.lastStarterWarningCode}" +
                $" | Severity={output.lastStarterSeverity:F2}" +
                $" | Hold={output.engineStartHoldSeconds:F2}s" +
                $" | Required={output.requiredStartHoldSeconds:F2}s" +
                $" | EngineHealth={output.componentHealthPercent:F1}%" +
                $" | Message={output.lastStarterWarningMessage}");

            previousStarterWarningCode = output.lastStarterWarningCode;
        }

        private void LogTemperatureDiagnostics(EngineSimulationOutput output)
        {
            if (!logLaunchDiagnostics || output == null)
            {
                return;
            }

            if (!output.engineTemperatureWarning)
            {
                previousTemperatureWarningCode = string.Empty;
                return;
            }

            if (output.lastTemperatureWarningCode == previousTemperatureWarningCode)
            {
                return;
            }

            string level = output.engineOverheated ? "EngineMisuse" : "EngineWarning";
            Debug.LogWarning(
                $"[TiltDrive][{level}]" +
                $" | Code={output.lastTemperatureWarningCode}" +
                $" | Severity={output.lastTemperatureSeverity:F2}" +
                $" | TempC={output.engineTemperatureC:F1}" +
                $" | ThermalEff={output.thermalEfficiency:F2}" +
                $" | RadiatorEff={(radiatorStore != null && radiatorStore.Current != null ? radiatorStore.Current.coolingEfficiency : 1f):F2}" +
                $" | RPM={output.currentRPM:F0}" +
                $" | Load={output.engineLoad:F2}" +
                $" | RevLimiter={output.revLimiterActive}" +
                $" | Message={output.lastTemperatureWarningMessage}");

            previousTemperatureWarningCode = output.lastTemperatureWarningCode;
        }
    }
}
