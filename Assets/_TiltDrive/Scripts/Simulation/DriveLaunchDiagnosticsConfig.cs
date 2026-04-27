using System;
using UnityEngine;

namespace TiltDrive.Simulation
{
    [Serializable]
    public class DriveLaunchDiagnosticsConfig
    {
        [Header("Rango de Arranque")]
        [Tooltip("Velocidad bajo la cual se considera que el vehiculo esta arrancando desde reposo.")]
        [Min(0f)] public float launchSpeedThresholdMS = 2f;

        [Tooltip("Marcha maxima recomendada para arrancar hacia adelante.")]
        [Min(1)] public int maxRecommendedLaunchGear = 1;

        [Header("Embrague")]
        [Tooltip("Acople minimo donde empieza el punto de contacto.")]
        [Range(0f, 1f)] public float biteEngagementMin = 0.18f;

        [Tooltip("Acople donde el embrague ya esta suficientemente agarrado.")]
        [Range(0f, 1f)] public float biteEngagementMax = 0.55f;

        [Tooltip("Acople considerado como soltar embrague de golpe a baja velocidad.")]
        [Range(0f, 1f)] public float clutchDumpEngagement = 0.85f;

        [Tooltip("Permite arrancar usando solo el ralenti si el embrague se administra dentro del punto de contacto.")]
        public bool allowIdleOnlyLaunch = true;

        [Tooltip("Acelerador maximo para considerar que el conductor esta intentando arrancar solo con ralenti.")]
        [Range(0f, 1f)] public float idleOnlyThrottleThreshold = 0.04f;

        [Tooltip("Margen sobre stallRPM que debe conservar el motor para permitir un arranque solo con ralenti.")]
        [Min(0f)] public float idleOnlyRPMReserve = 180f;

        [Header("Acelerador y Freno")]
        [Tooltip("Acelerador minimo recomendado cuando se esta en punto de contacto.")]
        [Range(0f, 1f)] public float minThrottleAtBite = 0.16f;

        [Tooltip("Acelerador minimo recomendado si el embrague se suelta casi completo desde reposo.")]
        [Range(0f, 1f)] public float minThrottleForClutchDump = 0.35f;

        [Tooltip("Freno desde el cual arrancar se considera una operacion conflictiva.")]
        [Range(0f, 1f)] public float brakeConflictThreshold = 0.25f;

        [Header("Riesgo de Apagado")]
        [Tooltip("Severidad a partir de la cual el motor puede apagarse.")]
        [Range(0f, 1f)] public float stallRiskThreshold = 0.65f;

        [Tooltip("Penalizacion maxima de RPM aplicada por mala tecnica de arranque.")]
        [Min(0f)] public float maxLaunchRPMPenalty = 1600f;

        [Tooltip("Margen sobre stallRPM donde una mala tecnica puede apagar el motor.")]
        [Min(0f)] public float stallRPMMargin = 250f;

        [Header("Carga por Friccion")]
        [Tooltip("Caida maxima de RPM por friccion del clutch durante el arranque.")]
        [Min(0f)] public float maxClutchFrictionRPMDrop = 520f;

        [Tooltip("Factor minimo de torque que el motor puede entregar desde el control de ralenti al soltar suavemente el clutch.")]
        [Range(0f, 1f)] public float idleLaunchTorqueFactor = 0.18f;

        [Tooltip("Acople desde el cual la transmision empieza a forzar RPM por debajo del ralenti si el vehiculo aun no tomo velocidad.")]
        [Range(0f, 1f)] public float idleLockupEngagementStart = 0.60f;

        public void ClampValues()
        {
            launchSpeedThresholdMS = Mathf.Max(0f, launchSpeedThresholdMS);
            maxRecommendedLaunchGear = Mathf.Max(1, maxRecommendedLaunchGear);
            biteEngagementMin = Mathf.Clamp01(biteEngagementMin);
            biteEngagementMax = Mathf.Clamp(biteEngagementMax, biteEngagementMin, 1f);
            clutchDumpEngagement = Mathf.Clamp(clutchDumpEngagement, biteEngagementMax, 1f);
            idleOnlyThrottleThreshold = Mathf.Clamp01(idleOnlyThrottleThreshold);
            idleOnlyRPMReserve = Mathf.Max(0f, idleOnlyRPMReserve);
            minThrottleAtBite = Mathf.Clamp01(minThrottleAtBite);
            minThrottleForClutchDump = Mathf.Clamp01(minThrottleForClutchDump);
            brakeConflictThreshold = Mathf.Clamp01(brakeConflictThreshold);
            stallRiskThreshold = Mathf.Clamp01(stallRiskThreshold);
            maxLaunchRPMPenalty = Mathf.Max(0f, maxLaunchRPMPenalty);
            stallRPMMargin = Mathf.Max(0f, stallRPMMargin);
            maxClutchFrictionRPMDrop = Mathf.Max(0f, maxClutchFrictionRPMDrop);
            idleLaunchTorqueFactor = Mathf.Clamp01(idleLaunchTorqueFactor);
            idleLockupEngagementStart = Mathf.Clamp(idleLockupEngagementStart, biteEngagementMax, clutchDumpEngagement);
        }
    }
}
