using System;
using UnityEngine;

namespace TiltDrive.LightingSystem
{
    [Serializable]
    public class VehicleLightsConfig
    {
        [Header("Identidad")]
        public string profileName = "Vehicle Lights Base";

        [Header("Direccionales")]
        [Tooltip("Ciclos de parpadeo por segundo.")]
        [Range(0.25f, 4f)] public float turnSignalFrequencyHz = 1.5f;

        [Header("Luces Automaticas")]
        public bool autoBrakeLights = true;
        [Range(0f, 1f)] public float brakeLightInputThreshold = 0.05f;
        public bool autoReverseLights = true;

        [Header("Luces Frontales")]
        [Tooltip("Si esta activo, las luces altas encienden tambien las bajas.")]
        public bool highBeamsRequireLowBeams = true;

        [Tooltip("Si esta activo, apagar bajas apaga tambien altas.")]
        public bool highBeamsTurnOffWithLowBeams = true;

        public void ClampValues()
        {
            turnSignalFrequencyHz = Mathf.Clamp(turnSignalFrequencyHz, 0.25f, 4f);
            brakeLightInputThreshold = Mathf.Clamp01(brakeLightInputThreshold);
        }
    }
}
