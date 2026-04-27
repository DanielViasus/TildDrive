using UnityEngine;
using TiltDrive.State;

namespace TiltDrive.DebugSystem
{
    public class InputDebugView : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputStore store;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool logTransientButtons = true;
        [SerializeField] private bool logAxes = true;

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
            }
            else
            {
                Debug.LogError("[TiltDrive][InputDebugView] No se encontró InputStore.");
            }
        }

        private void OnDisable()
        {
            if (store != null)
            {
                store.OnStateChanged -= HandleStateChanged;
            }
        }

        private void EnsureReferences()
        {
            if (store == null)
            {
                store = InputStore.Instance;
            }

            if (store == null)
            {
                store = GetComponent<InputStore>();
            }

            if (store == null)
            {
                store = FindFirstObjectByType<InputStore>();
            }
        }

        private void HandleStateChanged(InputState state)
        {
            if (!logStateChanges || state == null) return;

            string source = $"Source={state.sourceType}";

            string axes = logAxes
                ? $" | Steer={state.steer:F2} | Throttle={state.throttle:F2} | Brake={state.brake:F2} | Clutch={state.clutch:F2}"
                : string.Empty;

            string transients = string.Empty;

            if (logTransientButtons)
            {
                transients =
                    $" | GearUp={state.gearUpPressed}" +
                    $" | GearDown={state.gearDownPressed}" +
                    $" | DirectGear={state.directGearRequest}" +
                    $" | EngineStart={state.engineStartPressed}" +
                    $" | EngineStartHeld={state.engineStartHeld}" +
                    $" | Low={state.lightsLowPressed}" +
                    $" | High={state.lightsHighPressed}" +
                    $" | Hazard={state.hazardPressed}" +
                    $" | Left={state.leftBlinkerPressed}" +
                    $" | Right={state.rightBlinkerPressed}";
            }

            Debug.Log($"[TiltDrive][InputState] {source}{axes}{transients}");
        }
    }
}
