using System;
using UnityEngine;

namespace TiltDrive.CoolingSystem
{
    [Serializable]
    public class RadiatorState
    {
        [Header("Estado")]
        [Range(0f, 100f)] public float radiatorHealthPercent = 100f;
        [Min(0f)] public float accumulatedDamagePercent = 0f;
        public CoolantType coolantType = CoolantType.Coolant;
        [Range(0f, 100f)] public float coolantLevelPercent = 100f;
        [Min(0f)] public float coolantTemperatureC = 45f;
        [Min(0f)] public float systemPressureKpa = 95f;

        [Header("Eficiencia")]
        [Range(0f, 1.5f)] public float coolingEfficiency = 1f;
        [Range(0f, 1.5f)] public float airflowEfficiency = 1f;
        public bool radiatorFanActive = false;

        [Header("Daños")]
        public bool hasPerforation = false;
        [Range(0f, 100f)] public float leakRatePercentPerSecond = 0f;
        public bool pressureDamageActive = false;
        public bool lowCoolant = false;
        public bool radiatorOverPressurized = false;

        [Header("Diagnostico")]
        public bool hasRadiatorWarning = false;
        public string lastRadiatorWarningCode = string.Empty;
        public string lastRadiatorWarningMessage = string.Empty;
        [Min(0f)] public float lastRadiatorSeverity = 0f;
        public string lastDamageReason = string.Empty;

        [Header("Trazabilidad")]
        [Min(0)] public int simulationTick = 0;
        [Min(0f)] public float lastUpdateTime = 0f;

        public void InitializeFromConfig(RadiatorConfig config)
        {
            if (config == null)
            {
                ResetRuntime();
                return;
            }

            config.ClampValues();
            radiatorHealthPercent = 100f;
            accumulatedDamagePercent = 0f;
            coolantType = config.coolantType;
            coolantLevelPercent = config.initialCoolantLevelPercent;
            coolantTemperatureC = config.coolantInitialTemperatureC;
            systemPressureKpa = config.basePressureKpa;
            coolingEfficiency = 1f;
            airflowEfficiency = 1f;
            radiatorFanActive = false;
            hasPerforation = false;
            leakRatePercentPerSecond = 0f;
            pressureDamageActive = false;
            lowCoolant = false;
            radiatorOverPressurized = false;
            ClearWarning();
            lastDamageReason = string.Empty;
            simulationTick = 0;
            lastUpdateTime = 0f;
        }

        public void ResetRuntime()
        {
            radiatorHealthPercent = 100f;
            accumulatedDamagePercent = 0f;
            coolantType = CoolantType.Coolant;
            coolantLevelPercent = 100f;
            coolantTemperatureC = 45f;
            systemPressureKpa = 95f;
            coolingEfficiency = 1f;
            airflowEfficiency = 1f;
            radiatorFanActive = false;
            hasPerforation = false;
            leakRatePercentPerSecond = 0f;
            pressureDamageActive = false;
            lowCoolant = false;
            radiatorOverPressurized = false;
            ClearWarning();
            lastDamageReason = string.Empty;
            simulationTick = 0;
            lastUpdateTime = 0f;
        }

        public void CopyFrom(RadiatorState source)
        {
            if (source == null) return;

            radiatorHealthPercent = Mathf.Clamp(source.radiatorHealthPercent, 0f, 100f);
            accumulatedDamagePercent = Mathf.Max(0f, source.accumulatedDamagePercent);
            coolantType = source.coolantType;
            coolantLevelPercent = Mathf.Clamp(source.coolantLevelPercent, 0f, 100f);
            coolantTemperatureC = Mathf.Max(0f, source.coolantTemperatureC);
            systemPressureKpa = Mathf.Max(0f, source.systemPressureKpa);
            coolingEfficiency = Mathf.Clamp(source.coolingEfficiency, 0f, 1.5f);
            airflowEfficiency = Mathf.Clamp(source.airflowEfficiency, 0f, 1.5f);
            radiatorFanActive = source.radiatorFanActive;
            hasPerforation = source.hasPerforation;
            leakRatePercentPerSecond = Mathf.Clamp(source.leakRatePercentPerSecond, 0f, 100f);
            pressureDamageActive = source.pressureDamageActive;
            lowCoolant = source.lowCoolant;
            radiatorOverPressurized = source.radiatorOverPressurized;
            hasRadiatorWarning = source.hasRadiatorWarning;
            lastRadiatorWarningCode = source.lastRadiatorWarningCode;
            lastRadiatorWarningMessage = source.lastRadiatorWarningMessage;
            lastRadiatorSeverity = Mathf.Max(0f, source.lastRadiatorSeverity);
            lastDamageReason = source.lastDamageReason;
            simulationTick = Mathf.Max(0, source.simulationTick);
            lastUpdateTime = Mathf.Max(0f, source.lastUpdateTime);
        }

        public void ClearWarning()
        {
            hasRadiatorWarning = false;
            lastRadiatorWarningCode = string.Empty;
            lastRadiatorWarningMessage = string.Empty;
            lastRadiatorSeverity = 0f;
        }
    }
}
