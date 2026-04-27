using System;
using UnityEngine;

namespace TiltDrive.LightingSystem
{
    [Serializable]
    public class VehicleLightsState
    {
        [Header("Direccionales")]
        public bool leftTurnSignalRequested = false;
        public bool rightTurnSignalRequested = false;
        public bool leftTurnSignalOn = false;
        public bool rightTurnSignalOn = false;
        public bool hazardLightsOn = false;
        public bool blinkPhaseOn = false;

        [Header("Frontales")]
        public bool lowBeamsOn = false;
        public bool highBeamsOn = false;
        public bool fogLightsOn = false;

        [Header("Posteriores")]
        public bool brakeLightsOn = false;
        public bool reverseLightsOn = false;

        [Header("Auxiliar")]
        public bool otherLightsOn = false;

        [Header("Sistema Electrico")]
        public bool electricalPowerAvailable = true;
        [Range(0f, 1f)] public float brightnessFactor = 1f;

        [Header("Estado y Fallas")]
        [Range(0f, 100f)] public float lightingSystemHealthPercent = 100f;
        public bool leftTurnSignalFunctional = true;
        public bool rightTurnSignalFunctional = true;
        public bool lowBeamsFunctional = true;
        public bool highBeamsFunctional = true;
        public bool fogLightsFunctional = true;
        public bool brakeLightsFunctional = true;
        public bool reverseLightsFunctional = true;
        public bool otherLightsFunctional = true;

        [Header("Trazabilidad")]
        [Min(0)] public int simulationTick = 0;
        [Min(0f)] public float lastUpdateTime = 0f;

        public void CopyFrom(VehicleLightsState source)
        {
            if (source == null)
            {
                ResetRuntime();
                return;
            }

            leftTurnSignalRequested = source.leftTurnSignalRequested;
            rightTurnSignalRequested = source.rightTurnSignalRequested;
            leftTurnSignalOn = source.leftTurnSignalOn;
            rightTurnSignalOn = source.rightTurnSignalOn;
            hazardLightsOn = source.hazardLightsOn;
            blinkPhaseOn = source.blinkPhaseOn;

            lowBeamsOn = source.lowBeamsOn;
            highBeamsOn = source.highBeamsOn;
            fogLightsOn = source.fogLightsOn;

            brakeLightsOn = source.brakeLightsOn;
            reverseLightsOn = source.reverseLightsOn;
            otherLightsOn = source.otherLightsOn;
            electricalPowerAvailable = source.electricalPowerAvailable;
            brightnessFactor = Mathf.Clamp01(source.brightnessFactor);

            lightingSystemHealthPercent = Mathf.Clamp(source.lightingSystemHealthPercent, 0f, 100f);
            leftTurnSignalFunctional = source.leftTurnSignalFunctional;
            rightTurnSignalFunctional = source.rightTurnSignalFunctional;
            lowBeamsFunctional = source.lowBeamsFunctional;
            highBeamsFunctional = source.highBeamsFunctional;
            fogLightsFunctional = source.fogLightsFunctional;
            brakeLightsFunctional = source.brakeLightsFunctional;
            reverseLightsFunctional = source.reverseLightsFunctional;
            otherLightsFunctional = source.otherLightsFunctional;

            simulationTick = Mathf.Max(0, source.simulationTick);
            lastUpdateTime = Mathf.Max(0f, source.lastUpdateTime);
        }

        public void ResetRuntime()
        {
            leftTurnSignalRequested = false;
            rightTurnSignalRequested = false;
            leftTurnSignalOn = false;
            rightTurnSignalOn = false;
            hazardLightsOn = false;
            blinkPhaseOn = false;

            lowBeamsOn = false;
            highBeamsOn = false;
            fogLightsOn = false;

            brakeLightsOn = false;
            reverseLightsOn = false;
            otherLightsOn = false;
            electricalPowerAvailable = true;
            brightnessFactor = 1f;

            lightingSystemHealthPercent = 100f;
            leftTurnSignalFunctional = true;
            rightTurnSignalFunctional = true;
            lowBeamsFunctional = true;
            highBeamsFunctional = true;
            fogLightsFunctional = true;
            brakeLightsFunctional = true;
            reverseLightsFunctional = true;
            otherLightsFunctional = true;

            simulationTick = 0;
            lastUpdateTime = 0f;
        }
    }
}
