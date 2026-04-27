using System;
using UnityEngine;

namespace TiltDrive.Simulation
{
    [Serializable]
    public class DriveDamagePenaltyConfig
    {
        [Header("Overrev por Reduccion")]
        [Tooltip("Fraccion de maxRPM usada como ventana para convertir exceso de RPM en severidad 0-1.")]
        [Min(0.1f)] public float overRevSeverityRPMWindowFactor = 0.75f;

        [Tooltip("Danio minimo al motor cuando una reduccion causa overrev.")]
        [Min(0f)] public float downshiftEngineDamageMinPercent = 1.5f;

        [Tooltip("Danio base maximo al motor antes de multiplicadores por salto de marchas.")]
        [Min(0f)] public float downshiftEngineDamageMaxPercent = 18f;

        [Tooltip("Danio minimo a transmision cuando una reduccion causa overrev.")]
        [Min(0f)] public float downshiftTransmissionDamageMinPercent = 1f;

        [Tooltip("Danio base maximo a transmision antes de multiplicadores por salto de marchas.")]
        [Min(0f)] public float downshiftTransmissionDamageMaxPercent = 10f;

        [Tooltip("Multiplicador adicional por cada marcha saltada. 5->3 salta una marcha, 6->1 salta cuatro.")]
        [Min(0f)] public float skippedGearDamageMultiplier = 0.35f;

        [Tooltip("Multiplicador adicional aplicado cuando el exceso de RPM llega a severidad maxima.")]
        [Min(1f)] public float fullSeverityDamageMultiplier = 1.5f;

        [Header("Casos Catastroficos")]
        [Tooltip("Velocidad minima para considerar peligrosa una solicitud de reversa en movimiento.")]
        [Min(0f)] public float reverseMisuseMinSpeedMS = 1f;

        [Tooltip("Danio al motor al engranar reversa mientras el vehiculo avanza.")]
        [Range(0f, 100f)] public float reverseWhileMovingEngineDamagePercent = 100f;

        [Tooltip("Danio a transmision al engranar reversa mientras el vehiculo avanza.")]
        [Range(0f, 100f)] public float reverseWhileMovingTransmissionDamagePercent = 100f;

        [Header("Sobreesfuerzo por Marcha Alta")]
        public bool highGearEngineStrainEnabled = true;

        [Tooltip("Marcha minima desde la cual se considera mala practica exigir demasiado torque a bajas RPM.")]
        [Min(1)] public int highGearStrainMinGear = 3;

        [Tooltip("Acelerador minimo para detectar exigencia fuerte del motor.")]
        [Range(0f, 1f)] public float highGearStrainThrottleThreshold = 0.65f;

        [Tooltip("RPM desde las cuales el motor empieza a considerarse forzado si se acelera fuerte en marcha alta.")]
        [Min(0f)] public float highGearStrainRPMThreshold = 2200f;

        [Tooltip("RPM desde las cuales la condicion se considera severa.")]
        [Min(0f)] public float highGearStrainCriticalRPM = 1500f;

        [Tooltip("Velocidad minima para evitar confundir este diagnostico con el arranque.")]
        [Min(0f)] public float highGearStrainMinSpeedMS = 3f;

        [Tooltip("Acople minimo del clutch para considerar que el motor realmente esta cargando la transmision.")]
        [Range(0f, 1f)] public float highGearStrainMinClutchEngagement = 0.65f;

        [Tooltip("Intervalo minimo entre logs repetidos de sobreesfuerzo por marcha alta.")]
        [Min(0.1f)] public float highGearStrainLogIntervalSeconds = 0.75f;

        [Header("Sobreesfuerzo al Arrancar en Marcha Alta")]
        public bool highGearLaunchStrainEnabled = true;

        [Tooltip("Marcha minima desde la cual intentar arrancar se considera sobreesfuerzo severo.")]
        [Min(1)] public int highGearLaunchStrainMinGear = 3;

        [Tooltip("Velocidad maxima para considerar que el vehiculo esta intentando arrancar.")]
        [Min(0f)] public float highGearLaunchStrainMaxSpeedMS = 2f;

        [Tooltip("Acelerador minimo para detectar intento real de arrancar en marcha alta.")]
        [Range(0f, 1f)] public float highGearLaunchStrainThrottleThreshold = 0.20f;

        [Tooltip("Acople minimo del clutch donde el motor empieza a cargar la transmision durante el arranque.")]
        [Range(0f, 1f)] public float highGearLaunchStrainMinClutchEngagement = 0.18f;

        [Header("Limites")]
        [Tooltip("Danio maximo al motor que puede aplicar un solo evento.")]
        [Range(0f, 100f)] public float maxSingleEventEngineDamagePercent = 100f;

        [Tooltip("Danio maximo a transmision que puede aplicar un solo evento.")]
        [Range(0f, 100f)] public float maxSingleEventTransmissionDamagePercent = 100f;

        public void ClampValues()
        {
            overRevSeverityRPMWindowFactor = Mathf.Max(0.1f, overRevSeverityRPMWindowFactor);
            downshiftEngineDamageMinPercent = Mathf.Max(0f, downshiftEngineDamageMinPercent);
            downshiftEngineDamageMaxPercent = Mathf.Max(downshiftEngineDamageMinPercent, downshiftEngineDamageMaxPercent);
            downshiftTransmissionDamageMinPercent = Mathf.Max(0f, downshiftTransmissionDamageMinPercent);
            downshiftTransmissionDamageMaxPercent = Mathf.Max(
                downshiftTransmissionDamageMinPercent,
                downshiftTransmissionDamageMaxPercent);
            skippedGearDamageMultiplier = Mathf.Max(0f, skippedGearDamageMultiplier);
            fullSeverityDamageMultiplier = Mathf.Max(1f, fullSeverityDamageMultiplier);
            reverseMisuseMinSpeedMS = Mathf.Max(0f, reverseMisuseMinSpeedMS);
            reverseWhileMovingEngineDamagePercent = Mathf.Clamp(reverseWhileMovingEngineDamagePercent, 0f, 100f);
            reverseWhileMovingTransmissionDamagePercent = Mathf.Clamp(
                reverseWhileMovingTransmissionDamagePercent,
                0f,
                100f);
            highGearStrainMinGear = Mathf.Max(1, highGearStrainMinGear);
            highGearStrainThrottleThreshold = Mathf.Clamp01(highGearStrainThrottleThreshold);
            highGearStrainRPMThreshold = Mathf.Max(0f, highGearStrainRPMThreshold);
            highGearStrainCriticalRPM = Mathf.Clamp(
                highGearStrainCriticalRPM,
                0f,
                Mathf.Max(0f, highGearStrainRPMThreshold));
            highGearStrainMinSpeedMS = Mathf.Max(0f, highGearStrainMinSpeedMS);
            highGearStrainMinClutchEngagement = Mathf.Clamp01(highGearStrainMinClutchEngagement);
            highGearStrainLogIntervalSeconds = Mathf.Max(0.1f, highGearStrainLogIntervalSeconds);
            highGearLaunchStrainMinGear = Mathf.Max(1, highGearLaunchStrainMinGear);
            highGearLaunchStrainMaxSpeedMS = Mathf.Max(0f, highGearLaunchStrainMaxSpeedMS);
            highGearLaunchStrainThrottleThreshold = Mathf.Clamp01(highGearLaunchStrainThrottleThreshold);
            highGearLaunchStrainMinClutchEngagement = Mathf.Clamp01(highGearLaunchStrainMinClutchEngagement);
            maxSingleEventEngineDamagePercent = Mathf.Clamp(maxSingleEventEngineDamagePercent, 0f, 100f);
            maxSingleEventTransmissionDamagePercent = Mathf.Clamp(maxSingleEventTransmissionDamagePercent, 0f, 100f);
        }
    }
}
