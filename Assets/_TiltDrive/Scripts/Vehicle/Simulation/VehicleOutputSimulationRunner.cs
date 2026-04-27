using UnityEngine;
using TiltDrive.EngineSystem;
using TiltDrive.State;
using TiltDrive.TransmissionSystem;

namespace TiltDrive.VehicleSystem
{
    public class VehicleOutputSimulationRunner : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputStore inputStore;
        [SerializeField] private EngineStore engineStore;
        [SerializeField] private TransmissionStore transmissionStore;
        [SerializeField] private VehicleOutputStore vehicleOutputStore;

        [Header("Simulacion")]
        [SerializeField] private bool runSimulation = true;
        [SerializeField] private bool autoFindReferences = true;

        [Header("Debug Interno")]
        [SerializeField] [Min(0)] private int simulationTick = 0;
        [SerializeField] private bool logBrakeDiagnostics = true;

        private DynamicVehicleOutput dynamicVehicleOutput;
        private VehicleOutputSimulationInput simulationInput;
        private string previousBrakeWarningCode = string.Empty;
        private bool previousABSActive = false;
        private bool previousWheelsLocked = false;
        private string previousSteeringWarningCode = string.Empty;

        private void Awake()
        {
            EnsureReferences();

            dynamicVehicleOutput = new DynamicVehicleOutput();
            simulationInput = new VehicleOutputSimulationInput();
        }

        private void Update()
        {
            if (!runSimulation) return;

            EnsureReferences();

            if (inputStore == null || engineStore == null || transmissionStore == null || vehicleOutputStore == null)
            {
                return;
            }

            if (engineStore.Current == null || transmissionStore.Current == null || vehicleOutputStore.Config == null)
            {
                return;
            }

            BuildSimulationInput(Time.deltaTime, Time.time);

            VehicleOutputSimulationOutput output = dynamicVehicleOutput.Simulate(simulationInput);
            VehicleOutputState state = output.ToVehicleOutputState();
            state.simulationTick = simulationTick;
            state.lastUpdateTime = Time.time;

            vehicleOutputStore.ApplyStateSnapshot(state);
            LogBrakeDiagnostics(state);
            LogSteeringDiagnostics(state);

            simulationTick++;
        }

        private void EnsureReferences()
        {
            if (!autoFindReferences) return;

            if (transmissionStore == null)
            {
                transmissionStore = TransmissionStore.Instance;

                if (transmissionStore == null)
                {
                    transmissionStore = FindFirstObjectByType<TransmissionStore>();
                }
            }

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

            if (vehicleOutputStore == null)
            {
                vehicleOutputStore = VehicleOutputStore.Instance;

                if (vehicleOutputStore == null)
                {
                    vehicleOutputStore = FindFirstObjectByType<VehicleOutputStore>();
                }
            }
        }

        private void BuildSimulationInput(float deltaTime, float simulationTime)
        {
            simulationInput.Reset();
            simulationInput.SetTime(deltaTime, simulationTime);
            simulationInput.SetConfig(vehicleOutputStore.Config);
            simulationInput.SetPreviousState(vehicleOutputStore.Current);
            simulationInput.SetUserInput(inputStore.Current);
            simulationInput.SetEngineState(engineStore.Current);
            simulationInput.SetTransmissionState(transmissionStore.Current);
        }

        public void SetRunSimulation(bool value)
        {
            runSimulation = value;
        }

        public void ResetSimulationTick()
        {
            simulationTick = 0;
        }

        private void LogBrakeDiagnostics(VehicleOutputState state)
        {
            if (!logBrakeDiagnostics || state == null)
            {
                return;
            }

            if (!state.hasBrakeWarning)
            {
                previousBrakeWarningCode = string.Empty;
                previousABSActive = false;
                previousWheelsLocked = false;
                return;
            }

            bool changed = state.lastBrakeWarningCode != previousBrakeWarningCode ||
                state.absActive != previousABSActive ||
                state.wheelsLocked != previousWheelsLocked;
            if (!changed)
            {
                return;
            }

            string alertType = state.wheelsLocked || state.steeringControlLost
                ? "BrakeMisuse"
                : "BrakeWarning";

            Debug.LogWarning(
                $"[TiltDrive][{alertType}]" +
                $" | Code={state.lastBrakeWarningCode}" +
                $" | Severity={state.brakeSeverity:F2}" +
                $" | ABS={state.absActive}" +
                $" | WheelsLocked={state.wheelsLocked}" +
                $" | SteeringLost={state.steeringControlLost}" +
                $" | BrakeInput={state.brakeInput:F2}" +
                $" | DemandDecel={state.brakeDemandDecelerationMS2:F2}m/s2" +
                $" | RequestedN={state.requestedBrakeForceN:F0}" +
                $" | EffectiveN={state.brakeForceN:F0}" +
                $" | TempC={state.brakeTemperatureC:F0}" +
                $" | ThermalEff={state.brakeThermalEfficiency:F2}" +
                $" | SpeedKMH={state.absoluteSpeedKMH:F1}" +
                $" | Steer={state.steerInput:F2}" +
                $" | EffectiveSteer={state.effectiveSteerInput:F2}" +
                $" | Message={state.lastBrakeWarningMessage}");

            previousBrakeWarningCode = state.lastBrakeWarningCode;
            previousABSActive = state.absActive;
            previousWheelsLocked = state.wheelsLocked;
        }

        private void LogSteeringDiagnostics(VehicleOutputState state)
        {
            if (!logBrakeDiagnostics || state == null)
            {
                return;
            }

            if (!state.hasSteeringWarning)
            {
                previousSteeringWarningCode = string.Empty;
                return;
            }

            if (state.lastSteeringWarningCode == previousSteeringWarningCode)
            {
                return;
            }

            Debug.LogWarning(
                $"[TiltDrive][SteeringMisuse]" +
                $" | Code={state.lastSteeringWarningCode}" +
                $" | Severity={state.steeringSeverity:F2}" +
                $" | SpeedKMH={state.absoluteSpeedKMH:F1}" +
                $" | Steer={state.steerInput:F2}" +
                $" | EffectiveSteer={state.effectiveSteerInput:F2}" +
                $" | SteerDelta={state.steeringInputDelta:F2}" +
                $" | SteeringAngle={state.steeringAngleDegrees:F1}" +
                $" | Message={state.lastSteeringWarningMessage}");

            previousSteeringWarningCode = state.lastSteeringWarningCode;
        }
    }
}
