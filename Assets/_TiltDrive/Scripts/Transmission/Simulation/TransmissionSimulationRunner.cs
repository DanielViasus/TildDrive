using UnityEngine;
using TiltDrive.State;
using TiltDrive.EngineSystem;
using TiltDrive.VehicleSystem;
using TiltDrive.Simulation;

namespace TiltDrive.TransmissionSystem
{
    public class TransmissionSimulationRunner : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputStore inputStore;
        [SerializeField] private EngineStore engineStore;
        [SerializeField] private TransmissionStore transmissionStore;
        [SerializeField] private VehicleOutputStore vehicleOutputStore;

        [Header("Simulación")]
        [SerializeField] private bool runSimulation = true;
        [SerializeField] private bool autoFindReferences = true;

        [Header("Control de Simulación")]
        [SerializeField] private bool shiftingAllowed = true;
        [SerializeField] private bool useDirectGearSelection = true;
        [SerializeField] private bool bufferShiftRequests = true;
        [SerializeField] [Min(0.05f)] private float shiftRequestBufferSeconds = 0.75f;

        [Header("Diagnostico y Danio")]
        [SerializeField] private DriveDamagePenaltyConfig damagePenaltyConfig = new DriveDamagePenaltyConfig();
        [SerializeField] [Range(0f, 1f)] private float looseClutchShiftInputThreshold = 0.7f;
        [SerializeField] [Range(0f, 5f)] private float looseClutchShiftTransmissionDamagePercent = 0.25f;

        [Header("Debug Interno")]
        [SerializeField] [Min(0)] private int simulationTick = 0;
        [SerializeField] private bool logShiftDiagnostics = true;

        private DynamicTransmission dynamicTransmission;
        private TransmissionSimulationInput simulationInput;
        private int bufferedDirectGearRequest = 0;
        private int bufferedSequentialShift = 0;
        private float bufferedShiftTimer = 0f;
        private int previousCurrentGear = 0;
        private int previousRequestedGear = 0;
        private bool previousShiftInProgress = false;
        private string previousMisuseWarningCode = string.Empty;
        private float nextMisuseWarningLogTime = 0f;
        private bool pendingLooseClutchShiftPenalty = false;
        private string pendingLooseClutchShiftReason = string.Empty;

        public DriveDamagePenaltyConfig DamagePenaltyConfig => damagePenaltyConfig;

        private void Awake()
        {
            EnsureReferences();

            dynamicTransmission = new DynamicTransmission();
            simulationInput = new TransmissionSimulationInput();
        }

        private void Update()
        {
            if (!runSimulation) return;

            EnsureReferences();

            if (inputStore == null || engineStore == null || transmissionStore == null)
            {
                return;
            }

            if (transmissionStore.Config == null || transmissionStore.Current == null)
            {
                return;
            }

            if (engineStore.Current == null)
            {
                return;
            }

            BuildSimulationInput(Time.deltaTime, Time.time);

            TransmissionSimulationOutput output = dynamicTransmission.Simulate(simulationInput);
            output.simulationTick = simulationTick;
            output.lastUpdateTime = Time.time;

            transmissionStore.ApplyStateSnapshot(output.ToTransmissionState());
            ApplyPendingLooseClutchShiftPenalty();
            ApplyMisuseDamage(output);
            LogShiftOutput(output);
            LogMisuseDiagnostics(output);
            ClearAcceptedShiftRequest(output);

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
        }

        private void BuildSimulationInput(float deltaTime, float simulationTime)
        {
            simulationInput.Reset();

            simulationInput.SetTime(deltaTime, simulationTime);
            simulationInput.SetConfig(transmissionStore.Config);
            simulationInput.SetDamagePenaltyConfig(damagePenaltyConfig);
            simulationInput.SetTransmissionState(transmissionStore.Current);
            simulationInput.SetEngineState(engineStore.Current);
            simulationInput.SetVehicleState(vehicleOutputStore != null ? vehicleOutputStore.Current : null);
            simulationInput.SetUserInput(inputStore.Current);
            ApplyBufferedShiftRequest(deltaTime);

            simulationInput.shiftingAllowed = shiftingAllowed;
            simulationInput.useDirectGearSelection = useDirectGearSelection;
        }

        private void ApplyBufferedShiftRequest(float deltaTime)
        {
            if (!bufferShiftRequests || inputStore == null || inputStore.Current == null)
            {
                return;
            }

            CaptureLatestShiftRequest();

            if (bufferedShiftTimer > 0f)
            {
                bufferedShiftTimer = Mathf.Max(0f, bufferedShiftTimer - Mathf.Max(0.0001f, deltaTime));
            }

            if (bufferedShiftTimer <= 0f)
            {
                ClearBufferedShiftRequest();
                return;
            }

            if (bufferedDirectGearRequest != 0)
            {
                simulationInput.directGearRequest = bufferedDirectGearRequest;
                simulationInput.gearUpPressed = false;
                simulationInput.gearDownPressed = false;
                return;
            }

            if (bufferedSequentialShift > 0)
            {
                simulationInput.gearUpPressed = true;
                simulationInput.gearDownPressed = false;
            }
            else if (bufferedSequentialShift < 0)
            {
                simulationInput.gearUpPressed = false;
                simulationInput.gearDownPressed = true;
            }
        }

        private void CaptureLatestShiftRequest()
        {
            if (inputStore.Current.directGearRequest != 0)
            {
                bufferedDirectGearRequest = inputStore.Current.directGearRequest;
                bufferedSequentialShift = 0;
                bufferedShiftTimer = shiftRequestBufferSeconds;
                LogShiftRequest($"DirectGear={FormatGear(bufferedDirectGearRequest)}");
                return;
            }

            if (inputStore.Current.gearUpPressed)
            {
                bufferedDirectGearRequest = 0;
                bufferedSequentialShift = 1;
                bufferedShiftTimer = shiftRequestBufferSeconds;
                LogShiftRequest("GearUp");
                return;
            }

            if (inputStore.Current.gearDownPressed)
            {
                bufferedDirectGearRequest = 0;
                bufferedSequentialShift = -1;
                bufferedShiftTimer = shiftRequestBufferSeconds;
                LogShiftRequest("GearDown");
            }
        }

        private void ClearAcceptedShiftRequest(TransmissionSimulationOutput output)
        {
            if (!bufferShiftRequests || output == null)
            {
                return;
            }

            if (output.shiftInProgress)
            {
                ClearBufferedShiftRequest();
            }
        }

        private void ClearBufferedShiftRequest()
        {
            bufferedDirectGearRequest = 0;
            bufferedSequentialShift = 0;
            bufferedShiftTimer = 0f;
        }

        private void LogShiftRequest(string requestLabel)
        {
            if (inputStore == null || transmissionStore == null)
            {
                return;
            }

            InputState input = inputStore.Current;
            TransmissionState state = transmissionStore.Current;
            TransmissionConfig config = transmissionStore.Config;

            if (input == null || state == null || config == null)
            {
                return;
            }

            bool clutchRequiredNow = config.requiresClutch && config.transmissionType == TransmissionType.Manual;
            bool clutchReady = !clutchRequiredNow || input.clutch >= looseClutchShiftInputThreshold;
            bool positiveDirectIgnoredByAutomatic = config.transmissionType == TransmissionType.Automatic &&
                input.directGearRequest > 0;

            if (clutchRequiredNow && !clutchReady)
            {
                string reason =
                    $"Bad shift request with clutch too released. Request={requestLabel}, " +
                    $"ClutchInput={input.clutch:F2}, Required={looseClutchShiftInputThreshold:F2}.";
                pendingLooseClutchShiftPenalty = true;
                pendingLooseClutchShiftReason = reason;
                if (logShiftDiagnostics)
                {
                    Debug.LogWarning(
                        $"[TiltDrive][DriveMisuse] | Code=SHIFT_WITH_CLUTCH_RELEASED | " +
                        $"Severity={Mathf.Clamp01(1f - input.clutch):F2} | " +
                        $"TransmissionDamage={looseClutchShiftTransmissionDamagePercent:F2}% | " +
                        $"Gear={FormatGear(state.currentGear)} | Message={reason}");
                }
            }

            if (!logShiftDiagnostics)
            {
                return;
            }

            Debug.Log(
                $"[TiltDrive][TransmissionShiftInput]" +
                $" | Request={requestLabel}" +
                $" | Type={config.transmissionType}" +
                $" | CurrentGear={FormatGear(state.currentGear)}" +
                $" | RequestedGear={FormatGear(state.requestedGear)}" +
                $" | ClutchInput={input.clutch:F2}" +
                $" | RequiresClutch={clutchRequiredNow}" +
                $" | ClutchReady={clutchReady}" +
                $" | PositiveDirectIgnoredByAutomatic={positiveDirectIgnoredByAutomatic}" +
                $" | RunnerEnabled={isActiveAndEnabled}" +
                $" | BufferSeconds={shiftRequestBufferSeconds:F2}");
        }

        private void LogShiftOutput(TransmissionSimulationOutput output)
        {
            if (!logShiftDiagnostics || output == null)
            {
                return;
            }

            bool gearChanged = output.currentGear != previousCurrentGear;
            bool requestedChanged = output.requestedGear != previousRequestedGear;
            bool shiftStateChanged = output.shiftInProgress != previousShiftInProgress;
            bool requestBuffered = bufferedDirectGearRequest != 0 || bufferedSequentialShift != 0;
            bool blockedBufferedRequest = requestBuffered && !output.shiftAllowed;

            if (gearChanged || requestedChanged || shiftStateChanged || blockedBufferedRequest)
            {
                Debug.Log(
                    $"[TiltDrive][TransmissionShiftState]" +
                    $" | Type={output.transmissionType}" +
                    $" | CurrentGear={FormatGear(output.currentGear)}" +
                    $" | RequestedGear={FormatGear(output.requestedGear)}" +
                    $" | ShiftInProgress={output.shiftInProgress}" +
                    $" | ShiftTimer={output.shiftTimer:F2}" +
                    $" | ShiftAllowed={output.shiftAllowed}" +
                    $" | ClutchInput={output.clutchInput:F2}" +
                    $" | ClutchEngagement={output.clutchEngagement:F2}" +
                    $" | BufferedRequest={GetBufferedRequestLabel()}" +
                    $" | Diagnostic={output.diagnosticMessage}");
            }

            previousCurrentGear = output.currentGear;
            previousRequestedGear = output.requestedGear;
            previousShiftInProgress = output.shiftInProgress;
        }

        private void ApplyMisuseDamage(TransmissionSimulationOutput output)
        {
            if (output == null || output.engineDamageThisTickPercent <= 0f || engineStore == null)
            {
                return;
            }

            engineStore.ApplyDamagePercent(output.engineDamageThisTickPercent, output.lastMisuseMessage);
        }

        private void LogMisuseDiagnostics(TransmissionSimulationOutput output)
        {
            if (output == null || !output.hasMisuseWarning)
            {
                previousMisuseWarningCode = string.Empty;
                return;
            }

            bool repeatedWarning = output.lastMisuseCode == previousMisuseWarningCode;
            bool canLogRepeatedWarning = Time.time >= nextMisuseWarningLogTime;
            bool shouldThrottleWarning = output.lastMisuseCode.StartsWith("HIGH_GEAR_ENGINE_STRAIN");

            if (shouldThrottleWarning && repeatedWarning && !canLogRepeatedWarning)
            {
                return;
            }

            Debug.LogWarning(
                $"[TiltDrive][DriveMisuse]" +
                $" | Code={output.lastMisuseCode}" +
                $" | Severity={output.lastMisuseSeverity:F2}" +
                $" | RequiredRPM={output.lastRequiredEngineRPM:F0}" +
                $" | EngineRPM={output.lastMisuseEngineRPM:F0}" +
                $" | Throttle={output.lastMisuseThrottleInput:F2}" +
                $" | Gear={FormatGear(output.currentGear)}" +
                $" | EngineDamage={output.engineDamageThisTickPercent:F2}%" +
                $" | TransmissionDamage={output.transmissionDamageThisTickPercent:F2}%" +
                $" | TransmissionHealth={output.componentHealthPercent:F1}%" +
                $" | Message={output.lastMisuseMessage}");

            previousMisuseWarningCode = output.lastMisuseCode;
            if (shouldThrottleWarning)
            {
                float interval = damagePenaltyConfig != null
                    ? damagePenaltyConfig.highGearStrainLogIntervalSeconds
                    : 0.75f;
                nextMisuseWarningLogTime = Time.time + Mathf.Max(0.1f, interval);
            }
            else
            {
                nextMisuseWarningLogTime = 0f;
            }
        }

        private void ApplyPendingLooseClutchShiftPenalty()
        {
            if (!pendingLooseClutchShiftPenalty || transmissionStore == null)
            {
                return;
            }

            transmissionStore.ApplyDamagePercent(looseClutchShiftTransmissionDamagePercent, pendingLooseClutchShiftReason);
            pendingLooseClutchShiftPenalty = false;
            pendingLooseClutchShiftReason = string.Empty;
        }

        private string GetBufferedRequestLabel()
        {
            if (bufferedDirectGearRequest != 0)
            {
                return $"DirectGear={FormatGear(bufferedDirectGearRequest)}";
            }

            if (bufferedSequentialShift > 0)
            {
                return "GearUp";
            }

            if (bufferedSequentialShift < 0)
            {
                return "GearDown";
            }

            return "None";
        }

        private static string FormatGear(int gear)
        {
            if (gear == 0) return "N";
            if (gear < 0) return "R";
            return gear.ToString();
        }

        // --------------------------------------------------
        // SETTERS PUBLICOS PARA PRUEBAS / FUTURO
        // --------------------------------------------------

        public void SetRunSimulation(bool value)
        {
            runSimulation = value;
        }

        public void SetShiftingAllowed(bool value)
        {
            shiftingAllowed = value;
        }

        public void SetUseDirectGearSelection(bool value)
        {
            useDirectGearSelection = value;
        }

        public void ResetSimulationTick()
        {
            simulationTick = 0;
        }
    }
}
