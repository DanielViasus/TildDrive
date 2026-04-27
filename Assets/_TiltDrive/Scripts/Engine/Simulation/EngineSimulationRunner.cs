using UnityEngine;
using TiltDrive.State;

namespace TiltDrive.EngineSystem
{
    public class EngineSimulationRunner : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputStore inputStore;
        [SerializeField] private EngineStore engineStore;

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

        [Header("Debug Interno")]
        [SerializeField] [Min(0)] private int simulationTick = 0;

        private DynamicEngine dynamicEngine;
        private EngineSimulationInput simulationInput;

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
        }

        private void BuildSimulationInput(float deltaTime, float simulationTime)
        {
            simulationInput.Reset();

            simulationInput.SetTime(deltaTime, simulationTime);
            simulationInput.SetConfig(engineStore.Config);
            simulationInput.SetEngineState(engineStore.Current);
            simulationInput.SetUserInput(inputStore.Current);

            simulationInput.SetExternalLoad(
                transmissionLoad,
                vehicleMassKg,
                slopeAngleDegrees,
                additionalResistance
            );

            simulationInput.ignitionAllowed = ignitionAllowed;
            simulationInput.canStall = canStall;
            simulationInput.useExternalLoad = useExternalLoad;
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

        public void ResetSimulationTick()
        {
            simulationTick = 0;
        }
    }
}