using UnityEngine;
using TiltDrive.EngineSystem;
using TiltDrive.VehicleSystem;

namespace TiltDrive.CoolingSystem
{
    public class RadiatorSimulationRunner : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private RadiatorStore radiatorStore;
        [SerializeField] private EngineStore engineStore;
        [SerializeField] private VehicleOutputStore vehicleOutputStore;

        [Header("Simulacion")]
        [SerializeField] private bool runSimulation = true;
        [SerializeField] private bool autoFindReferences = true;
        [SerializeField] private bool logRadiatorDiagnostics = true;

        [Header("Debug Interno")]
        [SerializeField] [Min(0)] private int simulationTick = 0;

        private string previousWarningCode = string.Empty;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Update()
        {
            if (!runSimulation) return;

            EnsureReferences();
            if (radiatorStore == null || radiatorStore.Config == null || radiatorStore.Current == null)
            {
                return;
            }

            RadiatorState next = new RadiatorState();
            next.CopyFrom(radiatorStore.Current);
            Simulate(next, radiatorStore.Config, Time.deltaTime, Time.time);
            radiatorStore.ApplyStateSnapshot(next);
            LogDiagnostics(next);
            simulationTick++;
        }

        private void EnsureReferences()
        {
            if (!autoFindReferences) return;

            radiatorStore ??= RadiatorStore.Instance != null ? RadiatorStore.Instance : FindFirstObjectByType<RadiatorStore>();
            engineStore ??= EngineStore.Instance != null ? EngineStore.Instance : FindFirstObjectByType<EngineStore>();
            vehicleOutputStore ??= VehicleOutputStore.Instance != null ? VehicleOutputStore.Instance : FindFirstObjectByType<VehicleOutputStore>();
        }

        private void Simulate(RadiatorState state, RadiatorConfig config, float deltaTime, float simulationTime)
        {
            config.ClampValues();

            EngineState engine = engineStore != null ? engineStore.Current : null;
            VehicleOutputState vehicle = vehicleOutputStore != null ? vehicleOutputStore.Current : null;
            float engineTemperature = engine != null && engine.engineTemperatureC > 0f
                ? engine.engineTemperatureC
                : config.coolantAmbientTemperatureC;
            bool engineActive = engine != null && (engine.engineOn || engine.engineStarting || engine.currentRPM > 1f);
            float speedMS = vehicle != null ? Mathf.Abs(vehicle.finalSpeedMS) : 0f;

            state.coolantType = config.coolantType;

            if (state.hasPerforation && state.leakRatePercentPerSecond > 0f)
            {
                state.coolantLevelPercent = Mathf.Clamp(
                    state.coolantLevelPercent - state.leakRatePercentPerSecond * deltaTime,
                    0f,
                    100f);
            }

            float coolantTarget = engineActive
                ? Mathf.Lerp(state.coolantTemperatureC, engineTemperature, 0.55f)
                : config.coolantAmbientTemperatureC;
            float coolantMoveSpeed = engineActive ? 18f : 9f;
            state.coolantTemperatureC = Mathf.MoveTowards(
                state.coolantTemperatureC,
                coolantTarget,
                coolantMoveSpeed * deltaTime);

            float tolerance = config.GetCoolantPressureToleranceMultiplier();
            float warningPressure = config.warningPressureKpa * tolerance;
            float damagePressure = config.pressureDamageStartKpa * tolerance;
            float criticalPressure = config.criticalPressureKpa * tolerance;
            float pressureHeatFactor = Mathf.InverseLerp(92f, 138f, Mathf.Max(engineTemperature, state.coolantTemperatureC));
            state.systemPressureKpa = config.basePressureKpa + pressureHeatFactor * (criticalPressure - config.basePressureKpa);

            state.radiatorFanActive = engineActive && state.coolantTemperatureC >= config.fanActivationTemperatureC;
            state.lowCoolant = state.coolantLevelPercent <= config.lowCoolantWarningPercent;
            state.radiatorOverPressurized = state.systemPressureKpa >= warningPressure;
            state.pressureDamageActive = state.systemPressureKpa >= damagePressure;

            float healthFactor = Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(state.radiatorHealthPercent / 100f));
            float levelFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, config.lowCoolantWarningPercent, state.coolantLevelPercent));
            float coolantFactor = config.GetCoolantEfficiencyMultiplier();
            float pressurePenalty = state.radiatorOverPressurized
                ? Mathf.Lerp(1f, 0.68f, Mathf.InverseLerp(warningPressure, criticalPressure, state.systemPressureKpa))
                : 1f;
            float fanFactor = state.radiatorFanActive ? config.fanCoolingEfficiency : 0f;
            float airflowFactor = Mathf.Clamp01(speedMS / 24f) * config.airflowCoolingEfficiency;
            float rawEfficiency = (config.baseCoolingEfficiency + fanFactor + airflowFactor) *
                healthFactor *
                levelFactor *
                coolantFactor *
                pressurePenalty;

            state.coolingEfficiency = Mathf.Clamp(
                rawEfficiency,
                config.minimumCoolingEfficiency,
                1.5f);
            state.airflowEfficiency = Mathf.Clamp(
                (config.airflowCoolingEfficiency + fanFactor) * healthFactor * levelFactor,
                config.minimumCoolingEfficiency,
                1.5f);

            float pressureSeverity = Mathf.InverseLerp(damagePressure, criticalPressure, state.systemPressureKpa);
            bool pressureDamage = state.pressureDamageActive && pressureSeverity > 0f;
            if (pressureDamage)
            {
                float damage = pressureSeverity * config.pressureDamagePerSecond * deltaTime;
                state.radiatorHealthPercent = Mathf.Clamp(state.radiatorHealthPercent - damage, 0f, 100f);
                state.accumulatedDamagePercent += damage;
                state.lastDamageReason = "Cooling system overpressure";

                if (pressureSeverity >= 0.95f)
                {
                    state.hasPerforation = true;
                    state.leakRatePercentPerSecond = Mathf.Max(
                        state.leakRatePercentPerSecond,
                        config.perforationLeakRatePercentPerSecond);
                }
            }

            if (engine != null && engine.engineOverheated)
            {
                float damage = config.overheatDamagePerSecond * deltaTime;
                state.radiatorHealthPercent = Mathf.Clamp(state.radiatorHealthPercent - damage, 0f, 100f);
                state.accumulatedDamagePercent += damage;
                state.lastDamageReason = "Engine overheat pressure stress";
            }

            ApplyWarningState(state, config, warningPressure, damagePressure, criticalPressure);
            state.simulationTick = simulationTick;
            state.lastUpdateTime = simulationTime;
        }

        private static void ApplyWarningState(
            RadiatorState state,
            RadiatorConfig config,
            float warningPressure,
            float damagePressure,
            float criticalPressure)
        {
            state.ClearWarning();

            if (state.coolantLevelPercent <= config.criticalCoolantPercent)
            {
                state.hasRadiatorWarning = true;
                state.lastRadiatorWarningCode = "RADIATOR_COOLANT_CRITICAL";
                state.lastRadiatorWarningMessage = "Coolant level is critical; engine cooling is heavily compromised.";
                state.lastRadiatorSeverity = 1f;
                return;
            }

            if (state.hasPerforation)
            {
                state.hasRadiatorWarning = true;
                state.lastRadiatorWarningCode = "RADIATOR_PERFORATED";
                state.lastRadiatorWarningMessage = "Radiator has a perforation and is losing coolant.";
                state.lastRadiatorSeverity = Mathf.Max(0.7f, Mathf.InverseLerp(100f, 0f, state.coolantLevelPercent));
                return;
            }

            if (state.systemPressureKpa >= criticalPressure)
            {
                state.hasRadiatorWarning = true;
                state.lastRadiatorWarningCode = "RADIATOR_PRESSURE_CRITICAL";
                state.lastRadiatorWarningMessage = "Cooling system pressure is critical; radiator damage or perforation is likely.";
                state.lastRadiatorSeverity = 1f;
                return;
            }

            if (state.systemPressureKpa >= damagePressure)
            {
                state.hasRadiatorWarning = true;
                state.lastRadiatorWarningCode = "RADIATOR_PRESSURE_DAMAGE";
                state.lastRadiatorWarningMessage = "Cooling system pressure is damaging the radiator.";
                state.lastRadiatorSeverity = Mathf.Max(0.65f, Mathf.InverseLerp(damagePressure, criticalPressure, state.systemPressureKpa));
                return;
            }

            if (state.systemPressureKpa >= warningPressure)
            {
                state.hasRadiatorWarning = true;
                state.lastRadiatorWarningCode = "RADIATOR_PRESSURE_HIGH";
                state.lastRadiatorWarningMessage = "Cooling system pressure is high; sustained overheating can damage the radiator.";
                state.lastRadiatorSeverity = Mathf.Max(0.35f, Mathf.InverseLerp(warningPressure, criticalPressure, state.systemPressureKpa));
                return;
            }

            if (state.lowCoolant)
            {
                state.hasRadiatorWarning = true;
                state.lastRadiatorWarningCode = "RADIATOR_COOLANT_LOW";
                state.lastRadiatorWarningMessage = "Coolant level is low; cooling efficiency is reduced.";
                state.lastRadiatorSeverity = Mathf.Max(0.35f, Mathf.InverseLerp(config.lowCoolantWarningPercent, config.criticalCoolantPercent, state.coolantLevelPercent));
            }
        }

        private void LogDiagnostics(RadiatorState state)
        {
            if (!logRadiatorDiagnostics || state == null)
            {
                return;
            }

            if (!state.hasRadiatorWarning)
            {
                previousWarningCode = string.Empty;
                return;
            }

            if (state.lastRadiatorWarningCode == previousWarningCode)
            {
                return;
            }

            Debug.LogWarning(
                $"[TiltDrive][RadiatorWarning]" +
                $" | Code={state.lastRadiatorWarningCode}" +
                $" | Severity={state.lastRadiatorSeverity:F2}" +
                $" | Health={state.radiatorHealthPercent:F1}%" +
                $" | Coolant={state.coolantType}" +
                $" | Level={state.coolantLevelPercent:F1}%" +
                $" | TempC={state.coolantTemperatureC:F1}" +
                $" | PressureKpa={state.systemPressureKpa:F0}" +
                $" | CoolingEff={state.coolingEfficiency:F2}" +
                $" | Perforated={state.hasPerforation}" +
                $" | Message={state.lastRadiatorWarningMessage}");

            previousWarningCode = state.lastRadiatorWarningCode;
        }
    }
}
