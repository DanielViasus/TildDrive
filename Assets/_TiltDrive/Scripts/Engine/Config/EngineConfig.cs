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
        [Min(100f)] public float idleRPM = 1200f;

        [Tooltip("RPM por debajo de las cuales el motor puede apagarse.")]
        [Min(0f)] public float stallRPM = 500f;

        [Tooltip("RPM máximas operativas normales.")]
        [Min(500f)] public float maxRPM = 10000f;

        [Tooltip("RPM críticas. Superarlas puede generar alerta o fallo futuro.")]
        [Min(500f)] public float criticalRPM = 10500f;

        [Header("Limitador de RPM")]
        [Tooltip("Activa el corte/rebote de RPM al llegar al maxRPM.")]
        public bool enableRevLimiter = true;

        [Tooltip("Caida visual de RPM durante cada corte del limitador.")]
        [Min(10f)] public float revLimiterDropRPM = 650f;

        [Tooltip("Cantidad de rebotes por segundo cuando el limitador esta activo.")]
        [Min(0.5f)] public float revLimiterPulseFrequency = 8f;

        [Tooltip("Multiplicador de torque durante el corte. 0 corta todo, 1 no corta nada.")]
        [Range(0f, 1f)] public float revLimiterTorqueMultiplier = 0.2f;

        [Header("Respuesta Dinámica")]
        [Tooltip("Velocidad base de subida de RPM.")]
        [Min(0.01f)] public float rpmRiseSpeed = 4000f;

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

        [Tooltip("Tiempo minimo que debe mantenerse el boton de encendido para arrancar un motor sano.")]
        [Min(0.05f)] public float minimumStartHoldSeconds = 0.65f;

        [Tooltip("Tiempo adicional requerido cuando el motor esta muy daniado.")]
        [Min(0f)] public float damagedEngineStartExtraSeconds = 2.5f;

        [Tooltip("Tiempo manteniendo el arranque desde el cual se considera abuso del motor de arranque.")]
        [Min(0.1f)] public float starterOveruseWarningSeconds = 4f;

        [Tooltip("Tiempo manteniendo el arranque desde el cual el abuso se considera severo.")]
        [Min(0.1f)] public float starterOveruseCriticalSeconds = 7f;

        [Header("Capacidad Mecánica")]
        [Tooltip("Torque base nominal del motor en Nm.")]
        [Min(1f)] public float baseTorqueNm = 120f;

        [Tooltip("RPM donde el motor entrega su torque más utilizable.")]
        [Min(500f)] public float peakTorqueRPM = 6500f;

        [Tooltip("Fuerza de freno motor.")]
        [Min(0f)] public float engineBrakeStrength = 20f;

        [Header("Temperatura")]
        [Tooltip("Temperatura ambiente hacia la que el motor se enfria.")]
        [Min(0f)] public float engineAmbientTemperatureC = 28f;

        [Tooltip("Temperatura inicial del motor al reiniciar la simulacion.")]
        [Min(0f)] public float engineInitialTemperatureC = 45f;

        [Tooltip("Temperatura normal de operacion a la que tiende un motor sano encendido.")]
        [Min(0f)] public float engineNormalOperatingTemperatureC = 92f;

        [Tooltip("Calentamiento base por segundo cuando el motor esta encendido.")]
        [Min(0f)] public float engineBaseHeatPerSecond = 1.6f;

        [Tooltip("Calentamiento adicional por RPM altas.")]
        [Min(0f)] public float engineRPMHeatPerSecond = 13f;

        [Tooltip("Calentamiento adicional por carga/esfuerzo del motor.")]
        [Min(0f)] public float engineLoadHeatPerSecond = 10f;

        [Tooltip("Calentamiento adicional por sobre revolucionar o entrar al limitador.")]
        [Min(0f)] public float engineOverRevHeatPerSecond = 34f;

        [Tooltip("Enfriamiento base por segundo.")]
        [Min(0f)] public float engineBaseCoolingPerSecond = 2.0f;

        [Tooltip("Enfriamiento adicional por movimiento del vehiculo.")]
        [Min(0f)] public float engineSpeedCoolingPerSecond = 0.14f;

        [Tooltip("Temperatura desde la cual se genera advertencia.")]
        [Min(0f)] public float engineWarningTemperatureC = 108f;

        [Tooltip("Temperatura desde la cual el motor empieza a perder rendimiento.")]
        [Min(0f)] public float engineEfficiencyDropStartTemperatureC = 122f;

        [Tooltip("Temperatura critica del motor.")]
        [Min(0f)] public float engineCriticalTemperatureC = 138f;

        [Tooltip("Temperatura maxima simulada del motor.")]
        [Min(0f)] public float engineMaxTemperatureC = 150f;

        [Tooltip("Eficiencia minima del motor cuando esta criticamente caliente.")]
        [Range(0.05f, 1f)] public float engineMinThermalEfficiency = 0.45f;

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
            revLimiterDropRPM = Mathf.Clamp(revLimiterDropRPM, 10f, maxRPM - idleRPM);
            revLimiterPulseFrequency = Mathf.Max(0.5f, revLimiterPulseFrequency);
            revLimiterTorqueMultiplier = Mathf.Clamp01(revLimiterTorqueMultiplier);

            rpmRiseSpeed = Mathf.Max(0.01f, rpmRiseSpeed);
            rpmFallSpeed = Mathf.Max(0.01f, rpmFallSpeed);
            engineInertia = Mathf.Max(0.01f, engineInertia);
            throttleResponsiveness = Mathf.Clamp(throttleResponsiveness, 0.1f, 5f);

            engineStartRPMSpeed = Mathf.Max(0.01f, engineStartRPMSpeed);
            engineShutdownRPMSpeed = Mathf.Max(0.01f, engineShutdownRPMSpeed);
            minimumStartHoldSeconds = Mathf.Max(0.05f, minimumStartHoldSeconds);
            damagedEngineStartExtraSeconds = Mathf.Max(0f, damagedEngineStartExtraSeconds);
            starterOveruseWarningSeconds = Mathf.Max(0.1f, starterOveruseWarningSeconds);
            starterOveruseCriticalSeconds = Mathf.Max(starterOveruseWarningSeconds + 0.1f, starterOveruseCriticalSeconds);

            baseTorqueNm = Mathf.Max(1f, baseTorqueNm);
            peakTorqueRPM = Mathf.Max(500f, peakTorqueRPM);
            engineBrakeStrength = Mathf.Max(0f, engineBrakeStrength);

            engineAmbientTemperatureC = Mathf.Max(0f, engineAmbientTemperatureC);
            engineInitialTemperatureC = Mathf.Max(engineAmbientTemperatureC, engineInitialTemperatureC);
            engineNormalOperatingTemperatureC = Mathf.Max(engineInitialTemperatureC, engineNormalOperatingTemperatureC);
            engineBaseHeatPerSecond = Mathf.Max(0f, engineBaseHeatPerSecond);
            engineRPMHeatPerSecond = Mathf.Max(0f, engineRPMHeatPerSecond);
            engineLoadHeatPerSecond = Mathf.Max(0f, engineLoadHeatPerSecond);
            engineOverRevHeatPerSecond = Mathf.Max(0f, engineOverRevHeatPerSecond);
            engineBaseCoolingPerSecond = Mathf.Max(0f, engineBaseCoolingPerSecond);
            engineSpeedCoolingPerSecond = Mathf.Max(0f, engineSpeedCoolingPerSecond);
            engineWarningTemperatureC = Mathf.Max(engineNormalOperatingTemperatureC, engineWarningTemperatureC);
            engineEfficiencyDropStartTemperatureC = Mathf.Max(engineWarningTemperatureC, engineEfficiencyDropStartTemperatureC);
            engineCriticalTemperatureC = Mathf.Max(engineEfficiencyDropStartTemperatureC + 1f, engineCriticalTemperatureC);
            engineMaxTemperatureC = Mathf.Max(engineCriticalTemperatureC, engineMaxTemperatureC);
            engineMinThermalEfficiency = Mathf.Clamp(engineMinThermalEfficiency, 0.05f, 1f);

            loadSensitivity = Mathf.Max(0f, loadSensitivity);
            slopeSensitivity = Mathf.Max(0f, slopeSensitivity);
            massInfluence = Mathf.Max(0f, massInfluence);
        }
    }
}
