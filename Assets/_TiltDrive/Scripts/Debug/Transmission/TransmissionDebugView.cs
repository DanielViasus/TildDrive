using UnityEngine;
using TiltDrive.TransmissionSystem;

namespace TiltDrive.DebugSystem
{
    public class TransmissionDebugView : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TransmissionStore store;

        [Header("Logs")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logConfigChanges = true;

        [Header("Campos del Estado")]
        [SerializeField] private bool showLogicalState = true;
        [SerializeField] private bool showClutch = true;
        [SerializeField] private bool showRatios = true;
        [SerializeField] private bool showTorque = true;
        [SerializeField] private bool showFlags = true;
        [SerializeField] private bool showTrace = false;

        [Header("Campos de Config")]
        [SerializeField] private bool showIdentity = true;
        [SerializeField] private bool showStructure = true;
        [SerializeField] private bool showRatiosConfig = true;
        [SerializeField] private bool showShiftSettings = true;

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
                Debug.LogError("[TiltDrive][TransmissionDebugView] No se encontró TransmissionStore.");
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
                store = TransmissionStore.Instance;
            }

            if (store == null)
            {
                store = GetComponent<TransmissionStore>();
            }

            if (store == null)
            {
                store = FindFirstObjectByType<TransmissionStore>();
            }
        }

        private void HandleStateChanged(TransmissionState state)
        {
            if (!logStateChanges || state == null) return;

            string message = "[TiltDrive][TransmissionState]";

            if (showLogicalState)
            {
                message +=
                    $" | Type={state.transmissionType}" +
                    $" | CurrentGear={state.currentGear}" +
                    $" | RequestedGear={state.requestedGear}" +
                    $" | ShiftInProgress={state.shiftInProgress}" +
                    $" | DriveDirection={state.driveDirection}";
            }

            if (showClutch)
            {
                message +=
                    $" | ClutchInput={state.clutchInput:F2}" +
                    $" | ClutchEngagement={state.clutchEngagement:F2}" +
                    $" | ClutchDisengaged={state.clutchDisengaged}";
            }

            if (showRatios)
            {
                message +=
                    $" | GearRatio={state.currentGearRatio:F2}" +
                    $" | FinalDrive={state.finalDriveRatio:F2}" +
                    $" | TotalDriveRatio={state.totalDriveRatio:F2}";
            }

            if (showTorque)
            {
                message +=
                    $" | Health={state.componentHealthPercent:F1}" +
                    $" | Damage={state.accumulatedDamagePercent:F1}" +
                    $" | InputTorqueNm={state.inputTorqueNm:F2}" +
                    $" | InputRPM={state.inputRPM:F0}" +
                    $" | OutputTorqueNm={state.outputTorqueNm:F2}" +
                    $" | OutputRPM={state.outputRPM:F0}" +
                    $" | TransmittedTorqueNm={state.transmittedTorqueNm:F2}";
            }

            if (showFlags)
            {
                message +=
                    $" | IsNeutral={state.isNeutral}" +
                    $" | IsReverse={state.isReverse}" +
                    $" | ShiftAllowed={state.shiftAllowed}" +
                    $" | Warning={state.hasTransmissionWarning}" +
                    $" | Misuse={state.hasMisuseWarning}" +
                    $" | MisuseCode={state.lastMisuseCode}" +
                    $" | RequiredRPM={state.lastRequiredEngineRPM:F0}";
            }

            if (showTrace)
            {
                message +=
                    $" | Tick={state.simulationTick}" +
                    $" | LastUpdateTime={state.lastUpdateTime:F3}";
            }

            Debug.Log(message);
        }

        private void HandleConfigChanged(TransmissionConfig config)
        {
            if (!logConfigChanges || config == null) return;

            string message = "[TiltDrive][TransmissionConfig]";

            if (showIdentity)
            {
                message +=
                    $" | Name={config.transmissionName}" +
                    $" | Type={config.transmissionType}";
            }

            if (showStructure)
            {
                message +=
                    $" | ForwardGearCount={config.forwardGearCount}" +
                    $" | HasNeutral={config.hasNeutral}" +
                    $" | HasReverse={config.hasReverse}";
            }

            if (showRatiosConfig)
            {
                message +=
                    $" | FinalDriveRatio={config.finalDriveRatio:F2}" +
                    $" | TransmissionEfficiency={config.transmissionEfficiency:F2}" +
                    $" | ReverseGearRatio={config.reverseGearRatio:F2}";

                if (config.forwardGearRatios != null && config.forwardGearRatios.Length > 0)
                {
                    message += " | ForwardRatios=[";
                    for (int i = 0; i < config.forwardGearRatios.Length; i++)
                    {
                        message += config.forwardGearRatios[i].ToString("F2");

                        if (i < config.forwardGearRatios.Length - 1)
                        {
                            message += ", ";
                        }
                    }
                    message += "]";
                }
            }

            if (showShiftSettings)
            {
                message +=
                    $" | ShiftDuration={config.shiftDuration:F2}" +
                    $" | RequiresClutch={config.requiresClutch}" +
                    $" | AllowDirectGearSelection={config.allowDirectGearSelection}" +
                    $" | AutoFirst={config.automaticEngageFirstFromNeutral}" +
                    $" | AutoUpRPM={config.automaticUpshiftRPM:F0}" +
                    $" | AutoDownRPM={config.automaticDownshiftRPM:F0}";
            }

            Debug.Log(message);
        }
    }
}
