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

        private DynamicVehicleOutput dynamicVehicleOutput;
        private VehicleOutputSimulationInput simulationInput;

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
    }
}
