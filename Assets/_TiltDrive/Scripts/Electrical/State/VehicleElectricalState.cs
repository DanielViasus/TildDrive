using System;
using UnityEngine;

namespace TiltDrive.ElectricalSystem
{
    [Serializable]
    public class VehicleElectricalState
    {
        [Header("Bateria")]
        [Min(0f)] public float batteryChargeAh = 0f;
        [Range(0f, 100f)] public float batteryChargePercent = 100f;
        [Min(0f)] public float batteryVoltage = 12.6f;
        [Min(0f)] public float systemVoltage = 12.6f;
        [Min(0f)] public float voltageSag = 0f;

        [Header("Carga y Consumo")]
        [Min(0f)] public float loadCurrentAmps = 0f;
        [Min(0f)] public float alternatorChargeAmps = 0f;
        public float netCurrentAmps = 0f;
        public bool alternatorActive = false;
        public bool starterActive = false;
        [Min(0f)] public float starterHoldSeconds = 0f;

        [Header("Disponibilidad")]
        public bool electricalAvailable = true;
        public bool ignitionAvailable = true;
        public bool lightsPowerAvailable = true;
        [Range(0f, 1f)] public float lightsBrightnessFactor = 1f;

        [Header("Diagnostico")]
        public bool hasElectricalWarning = false;
        public bool lowVoltageWarning = false;
        public bool criticalVoltageWarning = false;
        public string lastWarningCode = string.Empty;
        public string lastWarningMessage = string.Empty;

        [Header("Trazabilidad")]
        [Min(0)] public int simulationTick = 0;
        [Min(0f)] public float lastUpdateTime = 0f;

        public void InitializeFromConfig(VehicleElectricalConfig config)
        {
            if (config == null)
            {
                ResetRuntime();
                return;
            }

            config.ClampValues();

            batteryChargeAh = config.batteryCapacityAh * Mathf.Clamp01(config.initialChargePercent / 100f);
            batteryChargePercent = config.initialChargePercent;
            batteryVoltage = CalculateOpenCircuitVoltage(config, batteryChargePercent);
            systemVoltage = batteryVoltage;
            ResetRuntimeFlags();
        }

        public void CopyFrom(VehicleElectricalState source)
        {
            if (source == null)
            {
                ResetRuntime();
                return;
            }

            batteryChargeAh = Mathf.Max(0f, source.batteryChargeAh);
            batteryChargePercent = Mathf.Clamp(source.batteryChargePercent, 0f, 100f);
            batteryVoltage = Mathf.Max(0f, source.batteryVoltage);
            systemVoltage = Mathf.Max(0f, source.systemVoltage);
            voltageSag = Mathf.Max(0f, source.voltageSag);

            loadCurrentAmps = Mathf.Max(0f, source.loadCurrentAmps);
            alternatorChargeAmps = Mathf.Max(0f, source.alternatorChargeAmps);
            netCurrentAmps = source.netCurrentAmps;
            alternatorActive = source.alternatorActive;
            starterActive = source.starterActive;
            starterHoldSeconds = Mathf.Max(0f, source.starterHoldSeconds);

            electricalAvailable = source.electricalAvailable;
            ignitionAvailable = source.ignitionAvailable;
            lightsPowerAvailable = source.lightsPowerAvailable;
            lightsBrightnessFactor = Mathf.Clamp01(source.lightsBrightnessFactor);

            hasElectricalWarning = source.hasElectricalWarning;
            lowVoltageWarning = source.lowVoltageWarning;
            criticalVoltageWarning = source.criticalVoltageWarning;
            lastWarningCode = source.lastWarningCode;
            lastWarningMessage = source.lastWarningMessage;

            simulationTick = Mathf.Max(0, source.simulationTick);
            lastUpdateTime = Mathf.Max(0f, source.lastUpdateTime);
        }

        public void ResetRuntime()
        {
            batteryChargeAh = 0f;
            batteryChargePercent = 0f;
            batteryVoltage = 0f;
            systemVoltage = 0f;
            voltageSag = 0f;
            ResetRuntimeFlags();
        }

        private void ResetRuntimeFlags()
        {
            loadCurrentAmps = 0f;
            alternatorChargeAmps = 0f;
            netCurrentAmps = 0f;
            alternatorActive = false;
            starterActive = false;
            starterHoldSeconds = 0f;
            electricalAvailable = batteryVoltage > 0f;
            ignitionAvailable = electricalAvailable;
            lightsPowerAvailable = electricalAvailable;
            lightsBrightnessFactor = electricalAvailable ? 1f : 0f;
            hasElectricalWarning = false;
            lowVoltageWarning = false;
            criticalVoltageWarning = false;
            lastWarningCode = string.Empty;
            lastWarningMessage = string.Empty;
            simulationTick = 0;
            lastUpdateTime = 0f;
        }

        public static float CalculateOpenCircuitVoltage(VehicleElectricalConfig config, float chargePercent)
        {
            if (config == null)
            {
                return 0f;
            }

            float normalizedCharge = Mathf.Clamp01(chargePercent / 100f);
            return Mathf.Lerp(config.noPowerVoltage, config.fullChargeVoltage, normalizedCharge);
        }
    }
}
