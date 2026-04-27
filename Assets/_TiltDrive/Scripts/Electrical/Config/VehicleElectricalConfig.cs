using System;
using UnityEngine;

namespace TiltDrive.ElectricalSystem
{
    [Serializable]
    public class VehicleElectricalConfig
    {
        [Header("Bateria")]
        public string profileName = "Vehicle Electrical Base";
        [Min(1f)] public float batteryCapacityAh = 7f;
        [Range(0f, 100f)] public float initialChargePercent = 100f;
        [Min(0f)] public float nominalBatteryVoltage = 12.6f;
        [Min(0f)] public float fullChargeVoltage = 12.8f;
        [Min(0f)] public float lowChargeVoltage = 11.8f;
        [Min(0f)] public float criticalVoltage = 10.5f;
        [Min(0f)] public float noPowerVoltage = 9.5f;

        [Header("Comportamiento de Simulacion")]
        [Tooltip("Escala de tiempo para que la descarga/carga sea visible en simulacion.")]
        [Min(0.1f)] public float chargeSimulationScale = 12f;

        [Tooltip("Caida de voltaje instantanea por cada amperio consumido.")]
        [Min(0f)] public float voltageDropPerAmp = 0.012f;

        [Tooltip("Caida adicional aproximada de voltaje por cada 100A consumidos por el starter.")]
        [Min(0f)] public float starterVoltageSagPer100A = 1.8f;

        [Header("Encendido")]
        [Min(0f)] public float minimumIgnitionVoltage = 11.2f;
        [Range(0f, 100f)] public float minimumIgnitionChargePercent = 8f;
        [Min(0f)] public float starterCurrentAmps = 95f;

        [Header("Alternador")]
        [Min(0f)] public float alternatorVoltage = 14.2f;
        [Min(0f)] public float alternatorMaxChargeAmps = 35f;
        [Min(0f)] public float alternatorActiveMinRPM = 900f;
        [Min(0f)] public float alternatorFullChargeRPM = 3500f;

        [Header("Consumos Base")]
        [Min(0f)] public float engineElectronicsAmps = 2.5f;
        [Min(0f)] public float ignitionStandbyAmps = 0.25f;

        [Header("Consumos de Luces")]
        [Min(0f)] public float lowBeamsAmps = 4.0f;
        [Min(0f)] public float highBeamsAmps = 5.0f;
        [Min(0f)] public float fogLightsAmps = 3.0f;
        [Min(0f)] public float brakeLightsAmps = 1.5f;
        [Min(0f)] public float reverseLightsAmps = 1.2f;
        [Min(0f)] public float singleTurnSignalAmps = 0.8f;
        [Min(0f)] public float otherLightsAmps = 1.0f;

        [Header("Salida a Sistemas")]
        [Min(0f)] public float minimumLightsVoltage = 10.2f;
        [Min(0f)] public float fullBrightnessVoltage = 12.0f;

        public void ClampValues()
        {
            batteryCapacityAh = Mathf.Max(1f, batteryCapacityAh);
            initialChargePercent = Mathf.Clamp(initialChargePercent, 0f, 100f);
            nominalBatteryVoltage = Mathf.Max(0f, nominalBatteryVoltage);
            fullChargeVoltage = Mathf.Max(nominalBatteryVoltage, fullChargeVoltage);
            lowChargeVoltage = Mathf.Clamp(lowChargeVoltage, 0f, fullChargeVoltage);
            criticalVoltage = Mathf.Clamp(criticalVoltage, 0f, lowChargeVoltage);
            noPowerVoltage = Mathf.Clamp(noPowerVoltage, 0f, criticalVoltage);
            chargeSimulationScale = Mathf.Max(0.1f, chargeSimulationScale);
            voltageDropPerAmp = Mathf.Max(0f, voltageDropPerAmp);
            starterVoltageSagPer100A = Mathf.Max(0f, starterVoltageSagPer100A);

            minimumIgnitionVoltage = Mathf.Max(0f, minimumIgnitionVoltage);
            minimumIgnitionChargePercent = Mathf.Clamp(minimumIgnitionChargePercent, 0f, 100f);
            starterCurrentAmps = Mathf.Max(0f, starterCurrentAmps);

            alternatorVoltage = Mathf.Max(fullChargeVoltage, alternatorVoltage);
            alternatorMaxChargeAmps = Mathf.Max(0f, alternatorMaxChargeAmps);
            alternatorActiveMinRPM = Mathf.Max(0f, alternatorActiveMinRPM);
            alternatorFullChargeRPM = Mathf.Max(alternatorActiveMinRPM + 1f, alternatorFullChargeRPM);

            engineElectronicsAmps = Mathf.Max(0f, engineElectronicsAmps);
            ignitionStandbyAmps = Mathf.Max(0f, ignitionStandbyAmps);
            lowBeamsAmps = Mathf.Max(0f, lowBeamsAmps);
            highBeamsAmps = Mathf.Max(0f, highBeamsAmps);
            fogLightsAmps = Mathf.Max(0f, fogLightsAmps);
            brakeLightsAmps = Mathf.Max(0f, brakeLightsAmps);
            reverseLightsAmps = Mathf.Max(0f, reverseLightsAmps);
            singleTurnSignalAmps = Mathf.Max(0f, singleTurnSignalAmps);
            otherLightsAmps = Mathf.Max(0f, otherLightsAmps);

            minimumLightsVoltage = Mathf.Max(0f, minimumLightsVoltage);
            fullBrightnessVoltage = Mathf.Max(minimumLightsVoltage + 0.01f, fullBrightnessVoltage);
        }
    }
}
