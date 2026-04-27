using System;
using UnityEngine;

namespace TiltDrive.VehicleSystem
{
    public enum VehicleSpeedUnit
    {
        KilometersPerHour = 0,
        MilesPerHour = 1
    }

    [Serializable]
    public class VehicleOutputConfig
    {
        [Header("Identidad")]
        public string outputName = "Vehicle Output Base";

        [Header("Lectura Principal")]
        public VehicleSpeedUnit speedDisplayUnit = VehicleSpeedUnit.KilometersPerHour;

        [Header("Rueda")]
        [Tooltip("Radio efectivo de la rueda en metros.")]
        [Min(0.01f)] public float wheelRadiusMeters = 0.30f;

        [Header("Dinamica")]
        [Tooltip("Masa simulada del vehiculo. A mayor masa, mas inercia.")]
        [Min(1f)] public float vehicleMassKg = 900f;

        [Tooltip("Escala de la fuerza de traccion que llega desde la transmision.")]
        [Range(0.05f, 2f)] public float tractionForceScale = 0.35f;

        [Tooltip("Resistencia aerodinamica simplificada. Sube con la velocidad al cuadrado.")]
        [Min(0f)] public float aerodynamicDragCoefficient = 0.45f;

        [Tooltip("Resistencia constante de rodadura.")]
        [Min(0f)] public float rollingResistanceCoefficient = 0.025f;

        [Tooltip("Fuerza maxima de freno de servicio.")]
        [Min(0f)] public float brakeForceN = 6000f;

        [Tooltip("Multiplicador del freno motor transmitido por la marcha.")]
        [Min(0f)] public float engineBrakeMultiplier = 1f;

        [Header("Limites Teoricos")]
        [Tooltip("Velocidad teorica maxima en km/h usada para calcular el porcentaje 0-100.")]
        [Min(1f)] public float theoreticalMaxSpeedKmh = 120f;

        [Tooltip("Limita la velocidad reportada a la velocidad teorica maxima.")]
        public bool clampSpeedToTheoreticalMax = true;

        public void ClampValues()
        {
            wheelRadiusMeters = Mathf.Max(0.01f, wheelRadiusMeters);
            vehicleMassKg = Mathf.Max(1f, vehicleMassKg);
            tractionForceScale = Mathf.Clamp(tractionForceScale, 0.05f, 2f);
            aerodynamicDragCoefficient = Mathf.Max(0f, aerodynamicDragCoefficient);
            rollingResistanceCoefficient = Mathf.Max(0f, rollingResistanceCoefficient);
            brakeForceN = Mathf.Max(0f, brakeForceN);
            engineBrakeMultiplier = Mathf.Max(0f, engineBrakeMultiplier);
            theoreticalMaxSpeedKmh = Mathf.Max(1f, theoreticalMaxSpeedKmh);
        }
    }
}
