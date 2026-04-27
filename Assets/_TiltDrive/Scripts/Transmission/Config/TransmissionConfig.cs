using System;
using UnityEngine;

namespace TiltDrive.TransmissionSystem
{
    [Serializable]
    public class TransmissionConfig
    {
        [Header("Identidad")]
        public string transmissionName = "Caja Base";
        public TransmissionType transmissionType = TransmissionType.Manual;

        [Header("Estructura")]
        [Tooltip("Cantidad de marchas hacia adelante.")]
        [Min(1)] public int forwardGearCount = 6;

        [Tooltip("Indica si la caja tiene neutro.")]
        public bool hasNeutral = true;

        [Tooltip("Indica si la caja tiene reversa.")]
        public bool hasReverse = true;

        [Header("Relaciones")]
        [Tooltip("Relación final del diferencial.")]
        [Min(0.01f)] public float finalDriveRatio = 7.50f;

        [Tooltip("Eficiencia general de transmisión.")]
        [Range(0.1f, 1f)] public float transmissionEfficiency = 0.9f;

        [Tooltip("Relación de reversa.")]
        [Min(0.01f)] public float reverseGearRatio = 3.20f;

        [Tooltip("Relaciones de marchas hacia adelante. Gear 1 = index 0.")]
        public float[] forwardGearRatios = new float[]
        {
            3.50f, // 1ra
            2.10f, // 2da
            1.40f, // 3ra
            1.00f, // 4ta
            0.80f, // 5ta
            0.65f  // 6ta
        };

        [Header("Cambio")]
        [Tooltip("Tiempo base de cambio entre marchas.")]
        [Min(0.01f)] public float shiftDuration = 0.25f;

        [Tooltip("Indica si requiere clutch para cambiar.")]
        public bool requiresClutch = true;

        [Tooltip("Permite cambios directos por número de marcha.")]
        public bool allowDirectGearSelection = true;

        [Header("Cambio Automatico")]
        [Tooltip("En automatico, engrana primera desde neutro cuando el motor esta encendido.")]
        public bool automaticEngageFirstFromNeutral = true;

        [Tooltip("RPM del motor para subir marcha en automatico.")]
        [Min(100f)] public float automaticUpshiftRPM = 5200f;

        [Tooltip("RPM del motor para bajar marcha en automatico.")]
        [Min(100f)] public float automaticDownshiftRPM = 1800f;

        public void ClampValues()
        {
            forwardGearCount = Mathf.Max(1, forwardGearCount);
            finalDriveRatio = Mathf.Max(0.01f, finalDriveRatio);
            transmissionEfficiency = Mathf.Clamp(transmissionEfficiency, 0.1f, 1f);
            reverseGearRatio = Mathf.Max(0.01f, reverseGearRatio);
            shiftDuration = Mathf.Max(0.01f, shiftDuration);
            automaticUpshiftRPM = Mathf.Max(100f, automaticUpshiftRPM);
            automaticDownshiftRPM = Mathf.Clamp(automaticDownshiftRPM, 100f, automaticUpshiftRPM - 100f);

            if (forwardGearRatios == null || forwardGearRatios.Length == 0)
            {
                forwardGearRatios = new float[] { 3.50f };
            }

            if (forwardGearRatios.Length != forwardGearCount)
            {
                Array.Resize(ref forwardGearRatios, forwardGearCount);

                for (int i = 0; i < forwardGearRatios.Length; i++)
                {
                    if (forwardGearRatios[i] <= 0f)
                    {
                        forwardGearRatios[i] = 1f;
                    }
                }
            }

            for (int i = 0; i < forwardGearRatios.Length; i++)
            {
                forwardGearRatios[i] = Mathf.Max(0.01f, forwardGearRatios[i]);
            }
        }

        public bool IsForwardGearValid(int gear)
        {
            return gear >= 1 && gear <= forwardGearCount;
        }

        public float GetForwardGearRatio(int gear)
        {
            if (!IsForwardGearValid(gear)) return 0f;

            int index = gear - 1;
            if (forwardGearRatios == null || index < 0 || index >= forwardGearRatios.Length)
            {
                return 0f;
            }

            return Mathf.Max(0.01f, forwardGearRatios[index]);
        }

        public float GetGearRatio(int gear)
        {
            // Convención:
            // -1 = reversa
            //  0 = neutro
            //  1..N = marchas hacia adelante

            if (gear == 0)
            {
                return 0f;
            }

            if (gear == -1)
            {
                return hasReverse ? Mathf.Max(0.01f, reverseGearRatio) : 0f;
            }

            return GetForwardGearRatio(gear);
        }
    }
}
