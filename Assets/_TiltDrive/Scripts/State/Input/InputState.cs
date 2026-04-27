using System;
using UnityEngine;

namespace TiltDrive.State
{
    [Serializable]
    public class InputState
    {
        [Header("Fuente")]
        public InputSourceType sourceType = InputSourceType.None;

        [Header("Ejes Analógicos")]
        [Range(-1f, 1f)] public float steer = 0f;
        [Range(0f, 1f)] public float throttle = 0f;
        [Range(0f, 1f)] public float brake = 0f;
        [Range(0f, 1f)] public float clutch = 0f;

        [Header("Solicitudes de Cambio")]
        public bool gearUpPressed = false;
        public bool gearDownPressed = false;

        /// <summary>
        /// Marcha solicitada directamente por el usuario.
        /// Valores recomendados:
        /// -1 = Reversa
        ///  0 = Neutro / sin solicitud
        ///  1..N = marchas hacia adelante
        /// </summary>
        public int directGearRequest = 0;

        [Header("Motor")]
        public bool engineStartPressed = false;
        public bool engineStartHeld = false;

        [Header("Luces y Señalización")]
        public bool lightsLowPressed = false;
        public bool lightsHighPressed = false;
        public bool hazardPressed = false;
        public bool leftBlinkerPressed = false;
        public bool rightBlinkerPressed = false;

        public void SetSource(InputSourceType newSource)
        {
            sourceType = newSource;
        }

        public void SetAxes(float newSteer, float newThrottle, float newBrake, float newClutch)
        {
            steer = Mathf.Clamp(newSteer, -1f, 1f);
            throttle = Mathf.Clamp01(newThrottle);
            brake = Mathf.Clamp01(newBrake);
            clutch = Mathf.Clamp01(newClutch);
        }

        public void SetSteer(float value)
        {
            steer = Mathf.Clamp(value, -1f, 1f);
        }

        public void SetThrottle(float value)
        {
            throttle = Mathf.Clamp01(value);
        }

        public void SetBrake(float value)
        {
            brake = Mathf.Clamp01(value);
        }

        public void SetClutch(float value)
        {
            clutch = Mathf.Clamp01(value);
        }

        public void RequestGearUp()
        {
            gearUpPressed = true;
        }

        public void RequestGearDown()
        {
            gearDownPressed = true;
        }

        public void RequestDirectGear(int gear)
        {
            directGearRequest = gear;
        }

        public void RequestEngineStart()
        {
            engineStartPressed = true;
            engineStartHeld = true;
        }

        public void SetEngineStartHeld(bool value)
        {
            engineStartHeld = value;
        }

        public void RequestLightsLow()
        {
            lightsLowPressed = true;
        }

        public void RequestLightsHigh()
        {
            lightsHighPressed = true;
        }

        public void RequestHazard()
        {
            hazardPressed = true;
        }

        public void RequestLeftBlinker()
        {
            leftBlinkerPressed = true;
        }

        public void RequestRightBlinker()
        {
            rightBlinkerPressed = true;
        }

        /// <summary>
        /// Limpia solo eventos momentáneos.
        /// No toca ejes analógicos.
        /// </summary>
        public void ClearTransient()
        {
            gearUpPressed = false;
            gearDownPressed = false;
            directGearRequest = 0;

            engineStartPressed = false;

            lightsLowPressed = false;
            lightsHighPressed = false;
            hazardPressed = false;
            leftBlinkerPressed = false;
            rightBlinkerPressed = false;
        }

        /// <summary>
        /// Reinicia todo el estado, incluyendo ejes.
        /// </summary>
        public void ResetAll()
        {
            sourceType = InputSourceType.None;

            steer = 0f;
            throttle = 0f;
            brake = 0f;
            clutch = 0f;
            engineStartHeld = false;

            ClearTransient();
        }
    }
}
