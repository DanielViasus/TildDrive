using UnityEngine;
using TiltDrive.State;
using TiltDrive.TransmissionSystem;
using TiltDrive.VehicleSystem;
using TiltDrive.ElectricalSystem;

namespace TiltDrive.LightingSystem
{
    public class VehicleLightsSimulationRunner : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputStore inputStore;
        [SerializeField] private VehicleOutputStore vehicleOutputStore;
        [SerializeField] private TransmissionStore transmissionStore;
        [SerializeField] private VehicleElectricalStore electricalStore;
        [SerializeField] private VehicleLightsStore lightsStore;

        [Header("Simulacion")]
        [SerializeField] private bool runSimulation = true;
        [SerializeField] private bool autoFindReferences = true;

        [Header("Debug Interno")]
        [SerializeField] [Min(0)] private int simulationTick = 0;

        private readonly VehicleLightsState workingState = new VehicleLightsState();
        private float blinkClock = 0f;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Update()
        {
            if (!runSimulation) return;

            EnsureReferences();

            if (lightsStore == null || lightsStore.Config == null || lightsStore.Current == null)
            {
                return;
            }

            VehicleLightsConfig config = lightsStore.Config;
            config.ClampValues();

            workingState.CopyFrom(lightsStore.Current);
            ApplyInputToggles(workingState, config);
            ApplyAutomaticLights(workingState, config);
            ApplyBlinkPhase(workingState, config, Time.deltaTime);
            ApplyElectricalAvailability(workingState);
            ApplyFunctionalFailures(workingState);

            workingState.simulationTick = simulationTick;
            workingState.lastUpdateTime = Time.time;

            lightsStore.ApplyStateSnapshot(workingState);
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

            if (vehicleOutputStore == null)
            {
                vehicleOutputStore = VehicleOutputStore.Instance;

                if (vehicleOutputStore == null)
                {
                    vehicleOutputStore = FindFirstObjectByType<VehicleOutputStore>();
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

            if (lightsStore == null)
            {
                lightsStore = VehicleLightsStore.Instance;

                if (lightsStore == null)
                {
                    lightsStore = GetComponent<VehicleLightsStore>();
                }

                if (lightsStore == null)
                {
                    lightsStore = FindFirstObjectByType<VehicleLightsStore>();
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
        }

        private void ApplyInputToggles(VehicleLightsState state, VehicleLightsConfig config)
        {
            if (inputStore == null || inputStore.Current == null)
            {
                return;
            }

            InputState input = inputStore.Current;

            if (input.lightsLowPressed)
            {
                state.lowBeamsOn = !state.lowBeamsOn;

                if (!state.lowBeamsOn && config.highBeamsTurnOffWithLowBeams)
                {
                    state.highBeamsOn = false;
                }
            }

            if (input.lightsHighPressed)
            {
                state.highBeamsOn = !state.highBeamsOn;

                if (state.highBeamsOn && config.highBeamsRequireLowBeams)
                {
                    state.lowBeamsOn = true;
                }
            }

            if (input.hazardPressed)
            {
                state.hazardLightsOn = !state.hazardLightsOn;
            }

            if (input.leftBlinkerPressed)
            {
                bool nextValue = !state.leftTurnSignalRequested;
                state.leftTurnSignalRequested = nextValue;

                if (nextValue)
                {
                    state.rightTurnSignalRequested = false;
                }
            }

            if (input.rightBlinkerPressed)
            {
                bool nextValue = !state.rightTurnSignalRequested;
                state.rightTurnSignalRequested = nextValue;

                if (nextValue)
                {
                    state.leftTurnSignalRequested = false;
                }
            }
        }

        private void ApplyAutomaticLights(VehicleLightsState state, VehicleLightsConfig config)
        {
            if (config.autoBrakeLights)
            {
                float brakeInput = 0f;

                if (inputStore != null && inputStore.Current != null)
                {
                    brakeInput = Mathf.Max(brakeInput, inputStore.Current.brake);
                }

                if (vehicleOutputStore != null && vehicleOutputStore.Current != null)
                {
                    brakeInput = Mathf.Max(brakeInput, vehicleOutputStore.Current.brakeInput);
                }

                state.brakeLightsOn = brakeInput >= config.brakeLightInputThreshold;
            }

            if (config.autoReverseLights)
            {
                bool reverseByTransmission =
                    transmissionStore != null &&
                    transmissionStore.Current != null &&
                    transmissionStore.Current.currentGear < 0;

                bool reverseByVehicle =
                    vehicleOutputStore != null &&
                    vehicleOutputStore.Current != null &&
                    vehicleOutputStore.Current.currentGear < 0;

                state.reverseLightsOn = reverseByTransmission || reverseByVehicle;
            }
        }

        private void ApplyBlinkPhase(VehicleLightsState state, VehicleLightsConfig config, float deltaTime)
        {
            bool hasBlinker =
                state.hazardLightsOn ||
                state.leftTurnSignalRequested ||
                state.rightTurnSignalRequested;

            if (!hasBlinker)
            {
                blinkClock = 0f;
                state.blinkPhaseOn = false;
                state.leftTurnSignalOn = false;
                state.rightTurnSignalOn = false;
                return;
            }

            blinkClock += Mathf.Max(0.0001f, deltaTime);
            state.blinkPhaseOn = Mathf.Repeat(blinkClock * config.turnSignalFrequencyHz, 1f) < 0.5f;
            state.leftTurnSignalOn = state.blinkPhaseOn &&
                (state.hazardLightsOn || state.leftTurnSignalRequested);
            state.rightTurnSignalOn = state.blinkPhaseOn &&
                (state.hazardLightsOn || state.rightTurnSignalRequested);
        }

        private void ApplyElectricalAvailability(VehicleLightsState state)
        {
            if (electricalStore == null || electricalStore.Current == null)
            {
                state.electricalPowerAvailable = true;
                state.brightnessFactor = 1f;
                return;
            }

            state.electricalPowerAvailable = electricalStore.Current.lightsPowerAvailable;
            state.brightnessFactor = electricalStore.Current.lightsBrightnessFactor;
        }

        private void ApplyFunctionalFailures(VehicleLightsState state)
        {
            float healthFactor = Mathf.Clamp01(state.lightingSystemHealthPercent / 100f);
            state.brightnessFactor *= healthFactor;

            if (healthFactor <= 0.01f)
            {
                state.leftTurnSignalOn = false;
                state.rightTurnSignalOn = false;
                state.lowBeamsOn = false;
                state.highBeamsOn = false;
                state.fogLightsOn = false;
                state.brakeLightsOn = false;
                state.reverseLightsOn = false;
                state.otherLightsOn = false;
                return;
            }

            if (!state.leftTurnSignalFunctional)
            {
                state.leftTurnSignalRequested = false;
                state.leftTurnSignalOn = false;
            }

            if (!state.rightTurnSignalFunctional)
            {
                state.rightTurnSignalRequested = false;
                state.rightTurnSignalOn = false;
            }

            if (!state.lowBeamsFunctional)
            {
                state.lowBeamsOn = false;
            }

            if (!state.highBeamsFunctional)
            {
                state.highBeamsOn = false;
            }

            if (!state.fogLightsFunctional)
            {
                state.fogLightsOn = false;
            }

            if (!state.brakeLightsFunctional)
            {
                state.brakeLightsOn = false;
            }

            if (!state.reverseLightsFunctional)
            {
                state.reverseLightsOn = false;
            }

            if (!state.otherLightsFunctional)
            {
                state.otherLightsOn = false;
            }
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
