using System;
using UnityEngine;

namespace TiltDrive.State
{
    public class InputStore : MonoBehaviour
    {
        public static InputStore Instance { get; private set; }

        [SerializeField] private InputState current = new InputState();

        public InputState Current => current;

        public event Action<InputState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            current.ResetAll();
        }

        // --------------------------------------------------
        // FUENTE
        // --------------------------------------------------

        public void SetSource(InputSourceType source)
        {
            if (current.sourceType == source) return;

            current.SetSource(source);
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // EJES
        // --------------------------------------------------

        public void SetSteer(float value)
        {
            float clamped = Mathf.Clamp(value, -1f, 1f);
            if (Mathf.Approximately(current.steer, clamped)) return;

            current.SetSteer(clamped);
            NotifyStateChanged();
        }

        public void SetThrottle(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(current.throttle, clamped)) return;

            current.SetThrottle(clamped);
            NotifyStateChanged();
        }

        public void SetBrake(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(current.brake, clamped)) return;

            current.SetBrake(clamped);
            NotifyStateChanged();
        }

        public void SetClutch(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(current.clutch, clamped)) return;

            current.SetClutch(clamped);
            NotifyStateChanged();
        }

        public void SetAxes(float steer, float throttle, float brake, float clutch)
        {
            bool changed = false;

            float steerClamped = Mathf.Clamp(steer, -1f, 1f);
            float throttleClamped = Mathf.Clamp01(throttle);
            float brakeClamped = Mathf.Clamp01(brake);
            float clutchClamped = Mathf.Clamp01(clutch);

            if (!Mathf.Approximately(current.steer, steerClamped))
            {
                current.steer = steerClamped;
                changed = true;
            }

            if (!Mathf.Approximately(current.throttle, throttleClamped))
            {
                current.throttle = throttleClamped;
                changed = true;
            }

            if (!Mathf.Approximately(current.brake, brakeClamped))
            {
                current.brake = brakeClamped;
                changed = true;
            }

            if (!Mathf.Approximately(current.clutch, clutchClamped))
            {
                current.clutch = clutchClamped;
                changed = true;
            }

            if (changed)
            {
                NotifyStateChanged();
            }
        }

        // --------------------------------------------------
        // CAMBIOS
        // --------------------------------------------------

        public void RequestGearUp()
        {
            current.RequestGearUp();
            NotifyStateChanged();
        }

        public void RequestGearDown()
        {
            current.RequestGearDown();
            NotifyStateChanged();
        }

        public void RequestDirectGear(int gear)
        {
            current.RequestDirectGear(gear);
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // MOTOR
        // --------------------------------------------------

        public void RequestEngineStart()
        {
            current.RequestEngineStart();
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // LUCES
        // --------------------------------------------------

        public void RequestLightsLow()
        {
            current.RequestLightsLow();
            NotifyStateChanged();
        }

        public void RequestLightsHigh()
        {
            current.RequestLightsHigh();
            NotifyStateChanged();
        }

        public void RequestHazard()
        {
            current.RequestHazard();
            NotifyStateChanged();
        }

        public void RequestLeftBlinker()
        {
            current.RequestLeftBlinker();
            NotifyStateChanged();
        }

        public void RequestRightBlinker()
        {
            current.RequestRightBlinker();
            NotifyStateChanged();
        }

        // --------------------------------------------------
        // CICLO
        // --------------------------------------------------

        public void ClearTransient()
        {
            current.ClearTransient();
        }

        public void ResetAll()
        {
            current.ResetAll();
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke(current);
        }
    }
}