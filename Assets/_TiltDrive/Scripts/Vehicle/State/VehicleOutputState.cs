using System;
using UnityEngine;

namespace TiltDrive.VehicleSystem
{
    [Serializable]
    public class VehicleOutputState
    {
        [Header("Estado Central")]
        public float simulationSpeed = 0f;
        public VehicleSpeedUnit simulationSpeedUnit = VehicleSpeedUnit.KilometersPerHour;
        [Range(0f, 100f)] public float motorSpeedPercent = 0f;
        [Min(0f)] public float engineRPM = 0f;
        public bool engineOn = false;
        public int currentGear = 0;

        [Header("Entrada")]
        [HideInInspector]
        [Min(0f)] public float transmissionOutputTorqueNm = 0f;
        [HideInInspector]
        public float transmissionOutputRPM = 0f;

        [Header("Rueda")]
        [HideInInspector]
        [Min(0f)] public float wheelRadiusMeters = 0.30f;
        [HideInInspector]
        [Min(0f)] public float wheelCircumferenceMeters = 0f;
        [HideInInspector]
        public float wheelAngularVelocityRadS = 0f;

        [Header("Velocidad Final")]
        [HideInInspector]
        public float finalSpeedMS = 0f;
        [HideInInspector]
        public float finalSpeedKMH = 0f;
        [HideInInspector]
        public float finalSpeedMPH = 0f;
        [HideInInspector]
        [Min(0f)] public float absoluteSpeedKMH = 0f;

        [Header("Potencia y Fuerza")]
        [HideInInspector]
        [Range(0f, 100f)] public float driveSpeedPercent = 0f;
        [HideInInspector]
        [Min(0f)] public float outputPowerKW = 0f;
        [HideInInspector]
        [Min(0f)] public float outputPowerHP = 0f;
        [HideInInspector]
        public float tractionForceN = 0f;

        [Header("Dinamica")]
        [HideInInspector]
        public float accelerationMS2 = 0f;
        [HideInInspector]
        public float brakeInput = 0f;
        [HideInInspector]
        public float aerodynamicDragForceN = 0f;
        [HideInInspector]
        public float rollingResistanceForceN = 0f;
        [HideInInspector]
        public float brakeForceN = 0f;
        [HideInInspector]
        public float engineBrakeForceN = 0f;

        [Header("Flags")]
        [HideInInspector]
        public int driveDirection = 0;
        [HideInInspector]
        public bool isMoving = false;

        [Header("Trazabilidad")]
        [HideInInspector]
        [Min(0)] public int simulationTick = 0;
        [HideInInspector]
        [Min(0f)] public float lastUpdateTime = 0f;

        public void ResetRuntime(VehicleOutputConfig config = null)
        {
            simulationSpeed = 0f;
            simulationSpeedUnit = config != null ? config.speedDisplayUnit : VehicleSpeedUnit.KilometersPerHour;
            motorSpeedPercent = 0f;
            engineRPM = 0f;
            engineOn = false;
            currentGear = 0;

            transmissionOutputTorqueNm = 0f;
            transmissionOutputRPM = 0f;

            wheelRadiusMeters = config != null ? Mathf.Max(0.01f, config.wheelRadiusMeters) : 0.30f;
            wheelCircumferenceMeters = 2f * Mathf.PI * wheelRadiusMeters;
            wheelAngularVelocityRadS = 0f;

            finalSpeedMS = 0f;
            finalSpeedKMH = 0f;
            finalSpeedMPH = 0f;
            absoluteSpeedKMH = 0f;

            driveSpeedPercent = 0f;
            outputPowerKW = 0f;
            outputPowerHP = 0f;
            tractionForceN = 0f;

            accelerationMS2 = 0f;
            brakeInput = 0f;
            aerodynamicDragForceN = 0f;
            rollingResistanceForceN = 0f;
            brakeForceN = 0f;
            engineBrakeForceN = 0f;

            driveDirection = 0;
            isMoving = false;

            simulationTick = 0;
            lastUpdateTime = 0f;
        }
    }
}
