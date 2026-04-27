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

        [Header("Frenos")]
        [Tooltip("Fuerza maxima de freno de servicio.")]
        [Min(0f)] public float brakeForceN = 6000f;

        [Header("Temperatura de Frenos")]
        [Tooltip("Temperatura ambiente hacia la que se enfria el sistema de frenos.")]
        [Min(0f)] public float brakeAmbientTemperatureC = 28f;

        [Tooltip("Temperatura inicial de los frenos al reiniciar la simulacion.")]
        [Min(0f)] public float brakeInitialTemperatureC = 32f;

        [Tooltip("Multiplicador de calentamiento por frenar con velocidad. A mayor valor, mas rapido sube la temperatura.")]
        [Min(0f)] public float brakeHeatGainPerSecond = 34f;

        [Tooltip("Enfriamiento base por segundo aun con el vehiculo quieto.")]
        [Min(0f)] public float brakeBaseCoolingPerSecond = 5f;

        [Tooltip("Enfriamiento adicional por velocidad del vehiculo.")]
        [Min(0f)] public float brakeSpeedCoolingPerSecond = 0.22f;

        [Tooltip("Temperatura desde la cual se advierte calentamiento de frenos.")]
        [Min(0f)] public float brakeWarningTemperatureC = 280f;

        [Tooltip("Temperatura desde la cual empieza a perder fuerza el freno.")]
        [Min(0f)] public float brakeFadeStartTemperatureC = 360f;

        [Tooltip("Temperatura critica donde el freno queda muy fatigado.")]
        [Min(0f)] public float brakeCriticalTemperatureC = 520f;

        [Tooltip("Temperatura maxima simulada.")]
        [Min(0f)] public float brakeMaxTemperatureC = 650f;

        [Tooltip("Fuerza minima disponible cuando los frenos estan criticamente calientes.")]
        [Range(0.05f, 1f)] public float brakeMinThermalEfficiency = 0.42f;

        [Tooltip("Activa el ABS. Si esta apagado, un frenado excesivo puede bloquear ruedas.")]
        public bool enableABS = true;

        [Tooltip("Entrada de freno desde la cual el frenado se considera agresivo.")]
        [Range(0f, 1f)] public float aggressiveBrakeInputThreshold = 0.50f;

        [Tooltip("Desaceleracion demandada desde la cual se genera advertencia de frenado agresivo.")]
        [Min(0f)] public float aggressiveBrakeDecelerationMS2 = 5.5f;

        [Tooltip("Entrada de freno desde la cual puede existir bloqueo de ruedas sin ABS.")]
        [Range(0f, 1f)] public float wheelLockBrakeInputThreshold = 0.60f;

        [Tooltip("Velocidad minima para evaluar bloqueo de ruedas.")]
        [Min(0f)] public float wheelLockMinSpeedMS = 2.0f;

        [Tooltip("Multiplicador de freno cuando las ruedas estan bloqueadas. Simula perdida de adherencia.")]
        [Range(0.1f, 1f)] public float lockedWheelBrakeMultiplier = 0.58f;

        [Tooltip("Multiplicador de direccion cuando las ruedas estan bloqueadas.")]
        [Range(0f, 1f)] public float lockedWheelSteeringMultiplier = 0.15f;

        [Tooltip("Fuerza la direccion a un rango pequeno cercano a cero cuando las ruedas estan bloqueadas.")]
        public bool forceSteeringLossOnWheelLock = true;

        [Tooltip("Angulo maximo de direccion permitido mientras las ruedas estan bloqueadas.")]
        [Range(0f, 10f)] public float lockedWheelMaxSteeringAngleDegrees = 2f;

        [Tooltip("Frecuencia de pulsos del ABS cuando entra a modular el freno.")]
        [Min(0.5f)] public float absPulseFrequency = 12f;

        [Tooltip("Multiplicador minimo de fuerza durante un pulso de ABS.")]
        [Range(0.1f, 1f)] public float absMinBrakeMultiplier = 0.68f;

        [Tooltip("Velocidad residual bajo la cual el vehiculo se considera totalmente detenido.")]
        [Min(0f)] public float fullStopSpeedThresholdMS = 0.08f;

        [Tooltip("Entrada minima de freno que permite fijar la velocidad final en cero a baja velocidad.")]
        [Range(0f, 1f)] public float fullStopBrakeInputThreshold = 0.05f;

        [Tooltip("Fuerza de traccion maxima que se considera despreciable para permitir reposo total sin freno.")]
        [Min(0f)] public float fullStopTractionForceThresholdN = 20f;

        [Header("Direccion")]
        [Tooltip("Distancia entre ejes usada para calcular el giro cinemático.")]
        [Min(0.1f)] public float wheelBaseMeters = 2.6f;

        [Tooltip("Angulo maximo de direccion de las ruedas delanteras.")]
        [Range(1f, 60f)] public float maxSteeringAngleDegrees = 32f;

        [Tooltip("Factor que reduce la direccion disponible a alta velocidad.")]
        [Min(0f)] public float steeringSpeedSensitivity = 0.18f;

        [Tooltip("Velocidad minima para acumular cambio de rumbo.")]
        [Min(0f)] public float minSpeedForSteeringMS = 0.05f;

        [Tooltip("Velocidad minima para detectar maniobras bruscas de direccion.")]
        [Min(0f)] public float aggressiveSteeringMinSpeedMS = 18f;

        [Tooltip("Input absoluto de direccion desde el cual el giro puede considerarse brusco.")]
        [Range(0f, 1f)] public float aggressiveSteeringInputThreshold = 0.75f;

        [Tooltip("Cambio minimo del input de direccion en un tick para considerarlo volantazo.")]
        [Range(0f, 2f)] public float aggressiveSteeringDeltaThreshold = 0.35f;

        [Tooltip("Multiplicador del freno motor transmitido por la marcha.")]
        [Min(0f)] public float engineBrakeMultiplier = 1f;

        [Tooltip("Desaceleracion maxima cuando una marcha queda por debajo de la velocidad actual y fuerza el drivetrain.")]
        [Min(1f)] public float overRevSpeedCorrectionDecelerationMS2 = 35f;

        [Header("Limites Teoricos")]
        [Tooltip("Velocidad teorica maxima en km/h usada para calcular el porcentaje 0-100.")]
        [Min(1f)] public float theoreticalMaxSpeedKmh = 120f;

        [Tooltip("Limita la velocidad reportada a la velocidad teorica maxima.")]
        public bool clampSpeedToTheoreticalMax = true;

        [Tooltip("Limita la velocidad por marcha usando las RPM maximas del motor, la relacion total y el radio de rueda.")]
        public bool clampSpeedToActiveGearRPM = true;

        public void ClampValues()
        {
            wheelRadiusMeters = Mathf.Max(0.01f, wheelRadiusMeters);
            vehicleMassKg = Mathf.Max(1f, vehicleMassKg);
            tractionForceScale = Mathf.Clamp(tractionForceScale, 0.05f, 2f);
            aerodynamicDragCoefficient = Mathf.Max(0f, aerodynamicDragCoefficient);
            rollingResistanceCoefficient = Mathf.Max(0f, rollingResistanceCoefficient);
            brakeForceN = Mathf.Max(0f, brakeForceN);
            brakeAmbientTemperatureC = Mathf.Max(0f, brakeAmbientTemperatureC);
            brakeInitialTemperatureC = Mathf.Max(brakeAmbientTemperatureC, brakeInitialTemperatureC);
            brakeHeatGainPerSecond = Mathf.Max(0f, brakeHeatGainPerSecond);
            brakeBaseCoolingPerSecond = Mathf.Max(0f, brakeBaseCoolingPerSecond);
            brakeSpeedCoolingPerSecond = Mathf.Max(0f, brakeSpeedCoolingPerSecond);
            brakeWarningTemperatureC = Mathf.Max(brakeAmbientTemperatureC, brakeWarningTemperatureC);
            brakeFadeStartTemperatureC = Mathf.Max(brakeWarningTemperatureC, brakeFadeStartTemperatureC);
            brakeCriticalTemperatureC = Mathf.Max(brakeFadeStartTemperatureC + 1f, brakeCriticalTemperatureC);
            brakeMaxTemperatureC = Mathf.Max(brakeCriticalTemperatureC, brakeMaxTemperatureC);
            brakeMinThermalEfficiency = Mathf.Clamp(brakeMinThermalEfficiency, 0.05f, 1f);
            aggressiveBrakeInputThreshold = Mathf.Clamp01(aggressiveBrakeInputThreshold);
            aggressiveBrakeDecelerationMS2 = Mathf.Max(0f, aggressiveBrakeDecelerationMS2);
            wheelLockBrakeInputThreshold = Mathf.Clamp(wheelLockBrakeInputThreshold, aggressiveBrakeInputThreshold, 1f);
            wheelLockMinSpeedMS = Mathf.Max(0f, wheelLockMinSpeedMS);
            lockedWheelBrakeMultiplier = Mathf.Clamp(lockedWheelBrakeMultiplier, 0.1f, 1f);
            lockedWheelSteeringMultiplier = Mathf.Clamp01(lockedWheelSteeringMultiplier);
            lockedWheelMaxSteeringAngleDegrees = Mathf.Clamp(lockedWheelMaxSteeringAngleDegrees, 0f, 10f);
            absPulseFrequency = Mathf.Max(0.5f, absPulseFrequency);
            absMinBrakeMultiplier = Mathf.Clamp(absMinBrakeMultiplier, 0.1f, 1f);
            fullStopSpeedThresholdMS = Mathf.Max(0f, fullStopSpeedThresholdMS);
            fullStopBrakeInputThreshold = Mathf.Clamp01(fullStopBrakeInputThreshold);
            fullStopTractionForceThresholdN = Mathf.Max(0f, fullStopTractionForceThresholdN);
            wheelBaseMeters = Mathf.Max(0.1f, wheelBaseMeters);
            maxSteeringAngleDegrees = Mathf.Clamp(maxSteeringAngleDegrees, 1f, 60f);
            steeringSpeedSensitivity = Mathf.Max(0f, steeringSpeedSensitivity);
            minSpeedForSteeringMS = Mathf.Max(0f, minSpeedForSteeringMS);
            aggressiveSteeringMinSpeedMS = Mathf.Max(0f, aggressiveSteeringMinSpeedMS);
            aggressiveSteeringInputThreshold = Mathf.Clamp01(aggressiveSteeringInputThreshold);
            aggressiveSteeringDeltaThreshold = Mathf.Clamp(aggressiveSteeringDeltaThreshold, 0f, 2f);
            engineBrakeMultiplier = Mathf.Max(0f, engineBrakeMultiplier);
            overRevSpeedCorrectionDecelerationMS2 = Mathf.Max(1f, overRevSpeedCorrectionDecelerationMS2);
            theoreticalMaxSpeedKmh = Mathf.Max(1f, theoreticalMaxSpeedKmh);
        }
    }
}
