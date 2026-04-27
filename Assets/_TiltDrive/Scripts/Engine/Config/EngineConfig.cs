using System;
using UnityEngine;

namespace TiltDrive.EngineSystem
{
    [Serializable]
    public class EngineConfig
    {
        [Header("Identidad")]
        public string engineName = "Motor Base";
        public EngineArchitectureType architectureType = EngineArchitectureType.Inline4;

        [Tooltip("Cilindraje en litros. Ejemplo: 1.6, 2.0, 5.0")]
        [Min(0.1f)] public float displacementLiters = 2.0f;

        [Tooltip("Cantidad de cilindros del motor.")]
        [Min(1)] public int cylinderCount = 4;

        [Header("Límites Operativos")]
        [Tooltip("RPM mínimas estables en ralentí.")]
        [Min(100f)] public float idleRPM = 850f;

        [Tooltip("RPM por debajo de las cuales el motor puede apagarse.")]
        [Min(0f)] public float stallRPM = 500f;

        [Tooltip("RPM máximas operativas normales.")]
        [Min(500f)] public float maxRPM = 5200f;

        [Tooltip("RPM críticas. Superarlas puede generar alerta o fallo futuro.")]
        [Min(500f)] public float criticalRPM = 5600f;

        [Header("Respuesta Dinámica")]
        [Tooltip("Velocidad base de subida de RPM.")]
        [Min(0.01f)] public float rpmRiseSpeed = 1600f;

        [Tooltip("Velocidad base de caída de RPM.")]
        [Min(0.01f)] public float rpmFallSpeed = 1400f;

        [Tooltip("Inercia del motor. Entre más alto, más lento cambia de RPM.")]
        [Min(0.01f)] public float engineInertia = 1.0f;

        [Tooltip("Qué tan sensible es al acelerador.")]
        [Range(0.1f, 5f)] public float throttleResponsiveness = 1.0f;

        [Header("Transiciones de Encendido / Apagado")]
        [Tooltip("Velocidad de subida de RPM durante el arranque.")]
        [Min(0.01f)] public float engineStartRPMSpeed = 1400f;

        [Tooltip("Velocidad de caída de RPM durante el apagado.")]
        [Min(0.01f)] public float engineShutdownRPMSpeed = 2200f;

        [Header("Capacidad Mecánica")]
        [Tooltip("Torque base nominal del motor en Nm.")]
        [Min(1f)] public float baseTorqueNm = 120f;

        [Tooltip("RPM donde el motor entrega su torque más utilizable.")]
        [Min(500f)] public float peakTorqueRPM = 3200f;

        [Tooltip("Fuerza de freno motor.")]
        [Min(0f)] public float engineBrakeStrength = 20f;

        [Header("Sensibilidad a Carga")]
        [Tooltip("Qué tanto afectan las cargas externas al motor.")]
        [Min(0f)] public float loadSensitivity = 1.0f;

        [Tooltip("Qué tanto afectan las pendientes al esfuerzo del motor.")]
        [Min(0f)] public float slopeSensitivity = 1.0f;

        [Tooltip("Qué tanto influye la masa del vehículo en el esfuerzo del motor.")]
        [Min(0f)] public float massInfluence = 1.0f;

        public void ClampValues()
        {
            displacementLiters = Mathf.Max(0.1f, displacementLiters);
            cylinderCount = Mathf.Max(1, cylinderCount);

            idleRPM = Mathf.Max(100f, idleRPM);
            stallRPM = Mathf.Clamp(stallRPM, 0f, idleRPM);
            maxRPM = Mathf.Max(idleRPM + 500f, maxRPM);
            criticalRPM = Mathf.Max(maxRPM, criticalRPM);

            rpmRiseSpeed = Mathf.Max(0.01f, rpmRiseSpeed);
            rpmFallSpeed = Mathf.Max(0.01f, rpmFallSpeed);
            engineInertia = Mathf.Max(0.01f, engineInertia);
            throttleResponsiveness = Mathf.Clamp(throttleResponsiveness, 0.1f, 5f);

            engineStartRPMSpeed = Mathf.Max(0.01f, engineStartRPMSpeed);
            engineShutdownRPMSpeed = Mathf.Max(0.01f, engineShutdownRPMSpeed);

            baseTorqueNm = Mathf.Max(1f, baseTorqueNm);
            peakTorqueRPM = Mathf.Max(500f, peakTorqueRPM);
            engineBrakeStrength = Mathf.Max(0f, engineBrakeStrength);

            loadSensitivity = Mathf.Max(0f, loadSensitivity);
            slopeSensitivity = Mathf.Max(0f, slopeSensitivity);
            massInfluence = Mathf.Max(0f, massInfluence);
        }
    }
}
