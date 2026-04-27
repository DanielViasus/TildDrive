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
        [Min(0f)] public float activeGearSpeedLimitKMH = 0f;
        [HideInInspector]
        public bool gearSpeedLimited = false;
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
        public float requestedBrakeForceN = 0f;
        [HideInInspector]
        public float aerodynamicDragForceN = 0f;
        [HideInInspector]
        public float rollingResistanceForceN = 0f;
        [HideInInspector]
        public float brakeForceN = 0f;
        [HideInInspector]
        public float engineBrakeForceN = 0f;

        [Header("Frenos")]
        [HideInInspector]
        public bool handbrakeActive = false;
        [HideInInspector]
        public bool absActive = false;
        [HideInInspector]
        public bool wheelsLocked = false;
        [HideInInspector]
        public bool hasBrakeWarning = false;
        [HideInInspector]
        [Range(0f, 1f)] public float brakeSeverity = 0f;
        [HideInInspector]
        [Min(0f)] public float brakeDemandDecelerationMS2 = 0f;
        [HideInInspector]
        [Min(0f)] public float brakeTemperatureC = 0f;
        [HideInInspector]
        [Range(0f, 1f)] public float brakeThermalEfficiency = 1f;
        [HideInInspector]
        public bool brakeOverheated = false;
        [HideInInspector]
        public bool brakeFadeActive = false;
        [HideInInspector]
        public string lastBrakeWarningCode = string.Empty;
        [HideInInspector]
        public string lastBrakeWarningMessage = string.Empty;

        [Header("Direccion")]
        [HideInInspector]
        [Range(-1f, 1f)] public float steerInput = 0f;
        [HideInInspector]
        [Range(-1f, 1f)] public float effectiveSteerInput = 0f;
        [HideInInspector]
        public float steeringAngleDegrees = 0f;
        [HideInInspector]
        public float yawRateDegreesPerSecond = 0f;
        [HideInInspector]
        public float headingDegrees = 0f;
        [HideInInspector]
        [Min(0f)] public float turnRadiusMeters = 0f;
        [HideInInspector]
        public bool steeringControlLost = false;
        [HideInInspector]
        public bool hasSteeringWarning = false;
        [HideInInspector]
        [Range(0f, 1f)] public float steeringSeverity = 0f;
        [HideInInspector]
        [Min(0f)] public float steeringInputDelta = 0f;
        [HideInInspector]
        public string lastSteeringWarningCode = string.Empty;
        [HideInInspector]
        public string lastSteeringWarningMessage = string.Empty;

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
            activeGearSpeedLimitKMH = 0f;
            gearSpeedLimited = false;
            outputPowerKW = 0f;
            outputPowerHP = 0f;
            tractionForceN = 0f;

            accelerationMS2 = 0f;
            brakeInput = 0f;
            requestedBrakeForceN = 0f;
            aerodynamicDragForceN = 0f;
            rollingResistanceForceN = 0f;
            brakeForceN = 0f;
            engineBrakeForceN = 0f;

            handbrakeActive = false;
            absActive = false;
            wheelsLocked = false;
            hasBrakeWarning = false;
            brakeSeverity = 0f;
            brakeDemandDecelerationMS2 = 0f;
            brakeTemperatureC = config != null ? config.brakeInitialTemperatureC : 0f;
            brakeThermalEfficiency = 1f;
            brakeOverheated = false;
            brakeFadeActive = false;
            lastBrakeWarningCode = string.Empty;
            lastBrakeWarningMessage = string.Empty;

            steerInput = 0f;
            effectiveSteerInput = 0f;
            steeringAngleDegrees = 0f;
            yawRateDegreesPerSecond = 0f;
            headingDegrees = 0f;
            turnRadiusMeters = 0f;
            steeringControlLost = false;
            hasSteeringWarning = false;
            steeringSeverity = 0f;
            steeringInputDelta = 0f;
            lastSteeringWarningCode = string.Empty;
            lastSteeringWarningMessage = string.Empty;

            driveDirection = 0;
            isMoving = false;

            simulationTick = 0;
            lastUpdateTime = 0f;
        }
    }
}
