using UnityEngine;
using UnityEngine.InputSystem;
using TiltDrive.Input;
using TiltDrive.State;

namespace TiltDrive.InputSystemRuntime
{
    public class TiltDriveInputReader : MonoBehaviour,
        TiltDriveControls.IWheelActions,
        TiltDriveControls.IGamepadActions
    {
        private TiltDriveControls controls;
        private InputStore store;

        [Header("Config")]
        [SerializeField] private bool enableWheel = true;
        [SerializeField] private bool enableGamepad = true;

        [Header("Pedales")]
        [SerializeField] private PedalAxisMode throttleAxisMode = PedalAxisMode.ZeroToOne;
        [SerializeField] private PedalAxisMode brakeAxisMode = PedalAxisMode.InvertedMinusOneToOne;
        [SerializeField] private PedalAxisMode clutchAxisMode = PedalAxisMode.InvertedMinusOneToOne;
        [SerializeField] private bool autoDetectSignedPedalRanges = true;

        private bool wheelCallbacksRegistered = false;
        private bool gamepadCallbacksRegistered = false;
        private bool throttleUsesSignedRange = false;
        private bool brakeUsesSignedRange = false;
        private bool clutchUsesSignedRange = false;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (controls == null)
            {
                Debug.LogError("[TiltDrive] TiltDriveControls no pudo inicializarse.");
                return;
            }

            if (enableWheel && !wheelCallbacksRegistered)
            {
                controls.Wheel.AddCallbacks(this);
                controls.Wheel.Enable();
                wheelCallbacksRegistered = true;
            }

            if (enableGamepad && !gamepadCallbacksRegistered)
            {
                controls.Gamepad.AddCallbacks(this);
                controls.Gamepad.Enable();
                gamepadCallbacksRegistered = true;
            }
        }

        private void OnDisable()
        {
            if (controls == null) return;

            if (enableWheel && wheelCallbacksRegistered)
            {
                controls.Wheel.RemoveCallbacks(this);
                controls.Wheel.Disable();
                wheelCallbacksRegistered = false;
            }

            if (enableGamepad && gamepadCallbacksRegistered)
            {
                controls.Gamepad.RemoveCallbacks(this);
                controls.Gamepad.Disable();
                gamepadCallbacksRegistered = false;
            }
        }

        private void OnDestroy()
        {
            if (controls != null)
            {
                controls.Dispose();
                controls = null;
            }
        }

        private void LateUpdate()
        {
            if (store != null)
            {
                store.ClearTransient();
            }
        }

        private void EnsureReferences()
        {
            if (controls == null)
            {
                controls = new TiltDriveControls();
            }

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

            if (store == null)
            {
                Debug.LogError("[TiltDrive] No existe InputStore en la escena.");
            }
        }

        // --------------------------------------------------
        // NORMALIZACION
        // --------------------------------------------------

        private float NormalizeWheelSteer(float rawValue)
        {
            return Mathf.Clamp(rawValue, -1f, 1f);
        }

        /// <summary>
        /// Convierte un eje de rango [-1, 1] a [0, 1].
        /// Ejemplo:
        /// -1 = 0
        ///  0 = 0.5
        ///  1 = 1
        /// </summary>
        private float NormalizePedalFromMinusOneToOne(float rawValue)
        {
            return Mathf.Clamp01((rawValue + 1f) * 0.5f);
        }

        /// <summary>
        /// Convierte un eje invertido de rango [1, -1] a [0, 1].
        /// Ejemplo:
        ///  1 = 0
        ///  0 = 0.5
        /// -1 = 1
        /// </summary>
        private float NormalizeInvertedPedalFromMinusOneToOne(float rawValue)
        {
            return Mathf.Clamp01((1f - rawValue) * 0.5f);
        }

        private float NormalizePedal(float rawValue, PedalAxisMode axisMode, ref bool usesSignedRange)
        {
            if (autoDetectSignedPedalRanges && rawValue < -0.05f)
            {
                usesSignedRange = true;
            }

            switch (axisMode)
            {
                case PedalAxisMode.MinusOneToOne:
                    return NormalizePedalFromMinusOneToOne(rawValue);
                case PedalAxisMode.InvertedMinusOneToOne:
                    return NormalizeInvertedPedalFromMinusOneToOne(rawValue);
                case PedalAxisMode.InvertedZeroToOne:
                    if (usesSignedRange)
                    {
                        return NormalizeInvertedPedalFromMinusOneToOne(rawValue);
                    }

                    return Mathf.Clamp01(1f - rawValue);
                default:
                    if (usesSignedRange)
                    {
                        return NormalizePedalFromMinusOneToOne(rawValue);
                    }

                    return Mathf.Clamp01(rawValue);
            }
        }

        // --------------------------------------------------
        // WHEEL
        // --------------------------------------------------

        public void OnSteer(InputAction.CallbackContext context)
        {
            if (store == null) return;

            float raw = context.ReadValue<float>();
            float normalized = NormalizeWheelSteer(raw);

            store.SetSource(InputSourceType.Wheel);
            store.SetSteer(normalized);
        }

        public void OnThrottle(InputAction.CallbackContext context)
        {
            if (store == null) return;

            float raw = context.ReadValue<float>();
            float normalized = NormalizePedal(raw, throttleAxisMode, ref throttleUsesSignedRange);

            store.SetSource(InputSourceType.Wheel);
            store.SetThrottle(normalized);
        }

        public void OnBrake(InputAction.CallbackContext context)
        {
            if (store == null) return;

            float raw = context.ReadValue<float>();
            float normalized = NormalizePedal(raw, brakeAxisMode, ref brakeUsesSignedRange);

            store.SetSource(InputSourceType.Wheel);
            store.SetBrake(normalized);
        }

        public void OnClutch(InputAction.CallbackContext context)
        {
            if (store == null) return;

            float raw = context.ReadValue<float>();
            float normalized = NormalizePedal(raw, clutchAxisMode, ref clutchUsesSignedRange);

            store.SetSource(InputSourceType.Wheel);
            store.SetClutch(normalized);
        }

        public void OnGearUp(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestGearUp();
        }

        public void OnGearDown(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestGearDown();
        }

        public void OnGear1(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestDirectGear(1);
        }

        public void OnGear2(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestDirectGear(2);
        }

        public void OnGear3(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestDirectGear(3);
        }

        public void OnGear4(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestDirectGear(4);
        }

        public void OnGear5(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestDirectGear(5);
        }

        public void OnGear6(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestDirectGear(6);
        }

        public void OnReverse(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestDirectGear(-1);
        }

        public void OnLightsLow(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestLightsLow();
        }

        public void OnLightsHigh(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestLightsHigh();
        }

        public void OnHazard(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestHazard();
        }

        public void OnLeftBlinker(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestLeftBlinker();
        }

        public void OnRightBlinker(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Wheel);
            store.RequestRightBlinker();
        }

        public void OnEngineStart(InputAction.CallbackContext context)
        {
            if (store == null) return;

            store.SetSource(InputSourceType.Wheel);

            if (context.started || context.performed)
            {
                store.RequestEngineStart();
            }
            else if (context.canceled)
            {
                store.SetEngineStartHeld(false);
            }
        }

        // --------------------------------------------------
        // GAMEPAD
        // --------------------------------------------------

        void TiltDriveControls.IGamepadActions.OnSteer(InputAction.CallbackContext context)
        {
            if (store == null) return;

            float raw = context.ReadValue<float>();

            store.SetSource(InputSourceType.Gamepad);
            store.SetSteer(Mathf.Clamp(raw, -1f, 1f));
        }

        void TiltDriveControls.IGamepadActions.OnThrottle(InputAction.CallbackContext context)
        {
            if (store == null) return;

            float raw = context.ReadValue<float>();

            store.SetSource(InputSourceType.Gamepad);
            store.SetThrottle(Mathf.Clamp01(raw));
        }

        void TiltDriveControls.IGamepadActions.OnBrake(InputAction.CallbackContext context)
        {
            if (store == null) return;

            float raw = context.ReadValue<float>();

            store.SetSource(InputSourceType.Gamepad);
            store.SetBrake(Mathf.Clamp01(raw));
        }

        void TiltDriveControls.IGamepadActions.OnClutch(InputAction.CallbackContext context)
        {
            if (store == null) return;

            float rawButtonValue = context.ReadValue<float>();
            float normalized = rawButtonValue > 0f ? 1f : 0f;

            store.SetSource(InputSourceType.Gamepad);
            store.SetClutch(normalized);
        }

        void TiltDriveControls.IGamepadActions.OnGearUp(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Gamepad);
            store.RequestGearUp();
        }

        void TiltDriveControls.IGamepadActions.OnGearDown(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Gamepad);
            store.RequestGearDown();
        }

        void TiltDriveControls.IGamepadActions.OnLightsLow(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Gamepad);
            store.RequestLightsLow();
        }

        void TiltDriveControls.IGamepadActions.OnLightsHigh(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Gamepad);
            store.RequestLightsHigh();
        }

        void TiltDriveControls.IGamepadActions.OnHazard(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Gamepad);
            store.RequestHazard();
        }

        void TiltDriveControls.IGamepadActions.OnLeftBlinker(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Gamepad);
            store.RequestLeftBlinker();
        }

        void TiltDriveControls.IGamepadActions.OnRightBlinker(InputAction.CallbackContext context)
        {
            if (store == null || !context.performed) return;

            store.SetSource(InputSourceType.Gamepad);
            store.RequestRightBlinker();
        }

        void TiltDriveControls.IGamepadActions.OnEngineStart(InputAction.CallbackContext context)
        {
            if (store == null) return;

            store.SetSource(InputSourceType.Gamepad);

            if (context.started || context.performed)
            {
                store.RequestEngineStart();
            }
            else if (context.canceled)
            {
                store.SetEngineStartHeld(false);
            }
        }
    }
}
