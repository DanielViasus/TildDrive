using UnityEngine;
using TiltDrive.EngineSystem;

namespace TiltDrive.DebugSystem
{
    public class EngineDebugView : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private EngineStore store;

        [Header("Logs")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logConfigChanges = true;

        [Header("Campos del Estado")]
        [SerializeField] private bool showLogicalState = true;
        [SerializeField] private bool showRPM = true;
        [SerializeField] private bool showLoadAndTorque = true;
        [SerializeField] private bool showWarnings = true;
        [SerializeField] private bool showTrace = false;

        [Header("Campos de Config")]
        [SerializeField] private bool showIdentity = true;
        [SerializeField] private bool showOperatingLimits = true;
        [SerializeField] private bool showDynamicResponse = false;
        [SerializeField] private bool showMechanicalData = false;
        [SerializeField] private bool showLoadSensitivity = false;
        [SerializeField] private bool showStartShutdownData = false;

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
                Debug.LogError("[TiltDrive][EngineDebugView] No se encontró EngineStore.");
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
                store = EngineStore.Instance;
            }

            if (store == null)
            {
                store = GetComponent<EngineStore>();
            }

            if (store == null)
            {
                store = FindFirstObjectByType<EngineStore>();
            }
        }

        private void HandleStateChanged(EngineState state)
        {
            if (!logStateChanges || state == null) return;

            string message = "[TiltDrive][EngineState]";

            if (showLogicalState)
            {
                message +=
                    $" | EngineOn={state.engineOn}" +
                    $" | Starting={state.engineStarting}" +
                    $" | ShuttingDown={state.engineShuttingDown}" +
                    $" | Stalled={state.engineStalled}";
            }

            if (showRPM)
            {
                message +=
                    $" | CurrentRPM={state.currentRPM:F0}" +
                    $" | TargetRPM={state.targetRPM:F0}" +
                    $" | IdleRPM={state.idleRPM:F0}" +
                    $" | StallRPM={state.stallRPM:F0}" +
                    $" | MaxRPM={state.maxRPM:F0}" +
                    $" | CriticalRPM={state.criticalRPM:F0}";
            }

            if (showLoadAndTorque)
            {
                message +=
                    $" | Health={state.componentHealthPercent:F1}" +
                    $" | Damage={state.accumulatedDamagePercent:F1}" +
                    $" | Load={state.engineLoad:F2}" +
                    $" | ClutchLoad={state.clutchFrictionLoad:F2}" +
                    $" | ClutchRPMDrop={state.clutchFrictionRPMDrop:F0}" +
                    $" | IdleAssist={state.idleLaunchAssistFactor:F2}" +
                    $" | TorqueNm={state.engineTorqueNm:F2}" +
                    $" | EngineBrakeTorqueNm={state.engineBrakeTorqueNm:F2}" +
                    $" | RevLimiter={state.revLimiterActive}" +
                    $" | RevTorqueFactor={state.revLimiterTorqueFactor:F2}";
            }

            if (showWarnings)
            {
                message +=
                    $" | BelowIdle={state.isBelowIdle}" +
                    $" | CriticalZone={state.isInCriticalZone}" +
                    $" | OverRevving={state.isOverRevving}" +
                    $" | LaunchWarning={state.hasLaunchWarning}" +
                    $" | LaunchMisuse={state.hasLaunchMisuse}" +
                    $" | StallRisk={state.launchStallRisk}" +
                    $" | LaunchCode={state.lastLaunchWarningCode}" +
                    $" | Warning={state.hasEngineWarning}";
            }

            if (showTrace)
            {
                message +=
                    $" | Tick={state.simulationTick}" +
                    $" | LastUpdateTime={state.lastUpdateTime:F3}";
            }

            Debug.Log(message);
        }

        private void HandleConfigChanged(EngineConfig config)
        {
            if (!logConfigChanges || config == null) return;

            string message = "[TiltDrive][EngineConfig]";

            if (showIdentity)
            {
                message +=
                    $" | Name={config.engineName}" +
                    $" | Architecture={config.architectureType}" +
                    $" | DisplacementL={config.displacementLiters:F2}" +
                    $" | Cylinders={config.cylinderCount}";
            }

            if (showOperatingLimits)
            {
                message +=
                    $" | IdleRPM={config.idleRPM:F0}" +
                    $" | StallRPM={config.stallRPM:F0}" +
                    $" | MaxRPM={config.maxRPM:F0}" +
                    $" | CriticalRPM={config.criticalRPM:F0}" +
                    $" | RevLimiter={config.enableRevLimiter}" +
                    $" | RevDropRPM={config.revLimiterDropRPM:F0}" +
                    $" | RevPulseHz={config.revLimiterPulseFrequency:F1}" +
                    $" | RevTorqueMultiplier={config.revLimiterTorqueMultiplier:F2}";
            }

            if (showDynamicResponse)
            {
                message +=
                    $" | RiseSpeed={config.rpmRiseSpeed:F2}" +
                    $" | FallSpeed={config.rpmFallSpeed:F2}" +
                    $" | Inertia={config.engineInertia:F2}" +
                    $" | ThrottleResponse={config.throttleResponsiveness:F2}";
            }

            if (showMechanicalData)
            {
                message +=
                    $" | BaseTorqueNm={config.baseTorqueNm:F2}" +
                    $" | PeakTorqueRPM={config.peakTorqueRPM:F0}" +
                    $" | EngineBrakeStrength={config.engineBrakeStrength:F2}";
            }

            if (showLoadSensitivity)
            {
                message +=
                    $" | LoadSensitivity={config.loadSensitivity:F2}" +
                    $" | SlopeSensitivity={config.slopeSensitivity:F2}" +
                    $" | MassInfluence={config.massInfluence:F2}";
            }

            if (showStartShutdownData)
            {
                message +=
                    $" | StartRPMSpeed={config.engineStartRPMSpeed:F2}" +
                    $" | ShutdownRPMSpeed={config.engineShutdownRPMSpeed:F2}";
            }

            Debug.Log(message);
        }
    }
}
