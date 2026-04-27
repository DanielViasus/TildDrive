using UnityEngine;
using TiltDrive.EngineSystem;
using TiltDrive.LightingSystem;
using TiltDrive.State;

namespace TiltDrive.ElectricalSystem
{
    public class VehicleElectricalSimulationRunner : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputStore inputStore;
        [SerializeField] private EngineStore engineStore;
        [SerializeField] private VehicleLightsStore lightsStore;
        [SerializeField] private VehicleElectricalStore electricalStore;

        [Header("Simulacion")]
        [SerializeField] private bool runSimulation = true;
        [SerializeField] private bool autoFindReferences = true;

        [Header("Debug Interno")]
        [SerializeField] [Min(0)] private int simulationTick = 0;

        private readonly VehicleElectricalState workingState = new VehicleElectricalState();

        private void Awake()
        {
            EnsureReferences();
        }

        private void Update()
        {
            if (!runSimulation) return;

            EnsureReferences();

            if (electricalStore == null || electricalStore.Config == null || electricalStore.Current == null)
            {
                return;
            }

            VehicleElectricalConfig config = electricalStore.Config;
            config.ClampValues();

            workingState.CopyFrom(electricalStore.Current);
            SimulateElectricalState(workingState, config, Time.deltaTime, Time.time);
            workingState.simulationTick = simulationTick;
            workingState.lastUpdateTime = Time.time;

            electricalStore.ApplyStateSnapshot(workingState);
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

            if (lightsStore == null)
            {
                lightsStore = VehicleLightsStore.Instance;

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
                    electricalStore = GetComponent<VehicleElectricalStore>();
                }

                if (electricalStore == null)
                {
                    electricalStore = FindFirstObjectByType<VehicleElectricalStore>();
                }
            }
        }

        private void SimulateElectricalState(
            VehicleElectricalState state,
            VehicleElectricalConfig config,
            float deltaTime,
            float simulationTime)
        {
            float loadAmps = CalculateBaseLoad(config);
            loadAmps += CalculateLightsLoad(config);

            bool starterActive = IsStarterActive();
            if (starterActive)
            {
                loadAmps += config.starterCurrentAmps;
            }

            state.starterHoldSeconds = starterActive
                ? state.starterHoldSeconds + Mathf.Max(0.0001f, deltaTime)
                : 0f;

            float engineRPM = engineStore != null && engineStore.Current != null
                ? engineStore.Current.currentRPM
                : 0f;
            bool engineOn = engineStore != null &&
                engineStore.Current != null &&
                engineStore.Current.engineOn &&
                !engineStore.Current.engineStalled;
            bool alternatorActive = engineOn && engineRPM >= config.alternatorActiveMinRPM;
            float alternatorChargeAmps = 0f;

            if (alternatorActive)
            {
                float rpmFactor = Mathf.InverseLerp(
                    config.alternatorActiveMinRPM,
                    config.alternatorFullChargeRPM,
                    engineRPM);
                alternatorChargeAmps = config.alternatorMaxChargeAmps *
                    Mathf.Clamp01(Mathf.Max(0.25f, rpmFactor));
            }

            float netCurrentAmps = alternatorChargeAmps - loadAmps;
            float nextChargeAh = state.batteryChargeAh +
                netCurrentAmps * Mathf.Max(0.0001f, deltaTime) * config.chargeSimulationScale / 3600f;
            state.batteryChargeAh = Mathf.Clamp(nextChargeAh, 0f, config.batteryCapacityAh);
            state.batteryChargePercent = Mathf.Clamp01(state.batteryChargeAh / config.batteryCapacityAh) * 100f;
            state.batteryVoltage = VehicleElectricalState.CalculateOpenCircuitVoltage(config, state.batteryChargePercent);

            float loadVoltageDrop = loadAmps * config.voltageDropPerAmp;
            float starterVoltageSag = starterActive
                ? config.starterVoltageSagPer100A * (config.starterCurrentAmps / 100f)
                : 0f;
            float chargeVoltage = alternatorActive
                ? Mathf.Lerp(state.batteryVoltage, config.alternatorVoltage, Mathf.Clamp01(alternatorChargeAmps / Mathf.Max(1f, config.alternatorMaxChargeAmps)))
                : state.batteryVoltage;
            state.voltageSag = loadVoltageDrop + starterVoltageSag;
            state.systemVoltage = Mathf.Max(0f, chargeVoltage - state.voltageSag);

            state.loadCurrentAmps = loadAmps;
            state.alternatorChargeAmps = alternatorChargeAmps;
            state.netCurrentAmps = netCurrentAmps;
            state.alternatorActive = alternatorActive;
            state.starterActive = starterActive;

            state.electricalAvailable = state.systemVoltage > config.noPowerVoltage &&
                state.batteryChargePercent > 0.1f;
            state.ignitionAvailable = state.electricalAvailable &&
                state.systemVoltage >= config.minimumIgnitionVoltage &&
                state.batteryChargePercent >= config.minimumIgnitionChargePercent;
            state.lightsPowerAvailable = state.electricalAvailable &&
                state.systemVoltage >= config.minimumLightsVoltage;
            state.lightsBrightnessFactor = state.lightsPowerAvailable
                ? Mathf.InverseLerp(config.minimumLightsVoltage, config.fullBrightnessVoltage, state.systemVoltage)
                : 0f;

            ApplyWarnings(state, config, simulationTime);
        }

        private float CalculateBaseLoad(VehicleElectricalConfig config)
        {
            bool engineActive = engineStore != null &&
                engineStore.Current != null &&
                (engineStore.Current.engineOn || engineStore.Current.engineStarting);

            if (engineActive)
            {
                return config.engineElectronicsAmps;
            }

            return config.ignitionStandbyAmps;
        }

        private float CalculateLightsLoad(VehicleElectricalConfig config)
        {
            if (lightsStore == null || lightsStore.Current == null)
            {
                return 0f;
            }

            VehicleLightsState lights = lightsStore.Current;
            float amps = 0f;

            if (lights.lowBeamsOn) amps += config.lowBeamsAmps;
            if (lights.highBeamsOn) amps += config.highBeamsAmps;
            if (lights.fogLightsOn) amps += config.fogLightsAmps;
            if (lights.brakeLightsOn) amps += config.brakeLightsAmps;
            if (lights.reverseLightsOn) amps += config.reverseLightsAmps;
            if (lights.otherLightsOn) amps += config.otherLightsAmps;
            if (lights.leftTurnSignalOn) amps += config.singleTurnSignalAmps;
            if (lights.rightTurnSignalOn) amps += config.singleTurnSignalAmps;

            return amps;
        }

        private bool IsStarterActive()
        {
            bool engineStarting = engineStore != null &&
                engineStore.Current != null &&
                engineStore.Current.engineStarting;
            bool engineOff = engineStore == null ||
                engineStore.Current == null ||
                (!engineStore.Current.engineOn && !engineStore.Current.engineShuttingDown);
            bool startPressed = inputStore != null &&
                inputStore.Current != null &&
                inputStore.Current.engineStartHeld;

            return engineStarting || (engineOff && startPressed);
        }

        private static void ApplyWarnings(
            VehicleElectricalState state,
            VehicleElectricalConfig config,
            float simulationTime)
        {
            state.hasElectricalWarning = false;
            state.lowVoltageWarning = false;
            state.criticalVoltageWarning = false;
            state.lastWarningCode = string.Empty;
            state.lastWarningMessage = string.Empty;

            if (state.systemVoltage <= config.criticalVoltage)
            {
                state.hasElectricalWarning = true;
                state.lowVoltageWarning = true;
                state.criticalVoltageWarning = true;
                state.lastWarningCode = "CRITICAL_ELECTRICAL_VOLTAGE";
                state.lastWarningMessage = "Electrical system voltage is critically low; ignition and lights may fail.";
                return;
            }

            if (state.systemVoltage <= config.lowChargeVoltage)
            {
                state.hasElectricalWarning = true;
                state.lowVoltageWarning = true;
                state.lastWarningCode = "LOW_ELECTRICAL_VOLTAGE";
                state.lastWarningMessage = "Electrical system voltage is low; battery charge is dropping.";
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
