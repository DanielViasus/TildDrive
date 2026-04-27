using System;
using UnityEngine;

namespace TiltDrive.CoolingSystem
{
    [Serializable]
    public class RadiatorConfig
    {
        [Header("Identidad")]
        public string profileName = "Radiador Base";

        [Header("Liquido refrigerante")]
        public CoolantType coolantType = CoolantType.Coolant;
        [Range(0f, 100f)] public float initialCoolantLevelPercent = 100f;
        [Min(0f)] public float coolantAmbientTemperatureC = 28f;
        [Min(0f)] public float coolantInitialTemperatureC = 45f;

        [Header("Eficiencia")]
        [Tooltip("Eficiencia base del radiador con salud y nivel correctos.")]
        [Range(0f, 2f)] public float baseCoolingEfficiency = 1f;

        [Tooltip("Cuanto ayuda el flujo de aire por velocidad del vehiculo.")]
        [Range(0f, 2f)] public float airflowCoolingEfficiency = 1f;

        [Tooltip("Ayuda de ventilador cuando el vehiculo no se mueve rapido.")]
        [Range(0f, 2f)] public float fanCoolingEfficiency = 0.35f;

        [Tooltip("Temperatura desde la que el ventilador se considera activo.")]
        [Min(0f)] public float fanActivationTemperatureC = 96f;

        [Tooltip("Eficiencia minima aun con radiador dañado, para evitar saltos imposibles.")]
        [Range(0f, 1f)] public float minimumCoolingEfficiency = 0.12f;

        [Header("Presion y daño")]
        [Min(0f)] public float basePressureKpa = 95f;
        [Min(0f)] public float warningPressureKpa = 145f;
        [Min(0f)] public float pressureDamageStartKpa = 165f;
        [Min(0f)] public float criticalPressureKpa = 190f;
        [Min(0f)] public float pressureDamagePerSecond = 1.25f;

        [Tooltip("Daño adicional por segundo cuando el motor esta en temperatura critica.")]
        [Min(0f)] public float overheatDamagePerSecond = 0.8f;

        [Header("Fugas")]
        [Range(0f, 100f)] public float perforationLeakRatePercentPerSecond = 3.5f;
        [Range(0f, 100f)] public float lowCoolantWarningPercent = 35f;
        [Range(0f, 100f)] public float criticalCoolantPercent = 12f;

        public void ClampValues()
        {
            initialCoolantLevelPercent = Mathf.Clamp(initialCoolantLevelPercent, 0f, 100f);
            coolantAmbientTemperatureC = Mathf.Max(0f, coolantAmbientTemperatureC);
            coolantInitialTemperatureC = Mathf.Max(coolantAmbientTemperatureC, coolantInitialTemperatureC);
            baseCoolingEfficiency = Mathf.Clamp(baseCoolingEfficiency, 0f, 2f);
            airflowCoolingEfficiency = Mathf.Clamp(airflowCoolingEfficiency, 0f, 2f);
            fanCoolingEfficiency = Mathf.Clamp(fanCoolingEfficiency, 0f, 2f);
            fanActivationTemperatureC = Mathf.Max(coolantAmbientTemperatureC, fanActivationTemperatureC);
            minimumCoolingEfficiency = Mathf.Clamp01(minimumCoolingEfficiency);

            basePressureKpa = Mathf.Max(0f, basePressureKpa);
            warningPressureKpa = Mathf.Max(basePressureKpa, warningPressureKpa);
            pressureDamageStartKpa = Mathf.Max(warningPressureKpa, pressureDamageStartKpa);
            criticalPressureKpa = Mathf.Max(pressureDamageStartKpa, criticalPressureKpa);
            pressureDamagePerSecond = Mathf.Max(0f, pressureDamagePerSecond);
            overheatDamagePerSecond = Mathf.Max(0f, overheatDamagePerSecond);

            perforationLeakRatePercentPerSecond = Mathf.Clamp(perforationLeakRatePercentPerSecond, 0f, 100f);
            lowCoolantWarningPercent = Mathf.Clamp(lowCoolantWarningPercent, 0f, 100f);
            criticalCoolantPercent = Mathf.Clamp(criticalCoolantPercent, 0f, lowCoolantWarningPercent);
        }

        public float GetCoolantEfficiencyMultiplier()
        {
            switch (coolantType)
            {
                case CoolantType.Water:
                    return 0.82f;
                case CoolantType.DeionizedWater:
                    return 0.92f;
                case CoolantType.PerformanceCoolant:
                    return 1.12f;
                default:
                    return 1f;
            }
        }

        public float GetCoolantPressureToleranceMultiplier()
        {
            switch (coolantType)
            {
                case CoolantType.Water:
                    return 0.88f;
                case CoolantType.DeionizedWater:
                    return 0.94f;
                case CoolantType.PerformanceCoolant:
                    return 1.08f;
                default:
                    return 1f;
            }
        }
    }
}
