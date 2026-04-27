using System;
using UnityEngine;

namespace TiltDrive.VehicleSystem
{
    public class VehicleOutputStore : MonoBehaviour
    {
        public static VehicleOutputStore Instance { get; private set; }

        [Header("Configuracion")]
        [SerializeField] private VehicleOutputConfig config = new VehicleOutputConfig();

        [Header("Estado Actual")]
        [SerializeField] private VehicleOutputState current = new VehicleOutputState();

        public VehicleOutputConfig Config => config;
        public VehicleOutputState Current => current;

        public event Action<VehicleOutputState> OnStateChanged;
        public event Action<VehicleOutputConfig> OnConfigChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            config ??= new VehicleOutputConfig();
            current ??= new VehicleOutputState();

            config.ClampValues();
            current.ResetRuntime(config);

            NotifyConfigChanged();
            NotifyStateChanged();
        }

        public void SetConfig(VehicleOutputConfig newConfig, bool resetRuntime = true)
        {
            if (newConfig == null)
            {
                Debug.LogWarning("[TiltDrive][VehicleOutputStore] Se intento asignar un VehicleOutputConfig nulo.");
                return;
            }

            config = newConfig;
            config.ClampValues();

            if (resetRuntime)
            {
                current.ResetRuntime(config);
                NotifyStateChanged();
            }

            NotifyConfigChanged();
        }

        public void ResetRuntime()
        {
            current.ResetRuntime(config);
            NotifyStateChanged();
        }

        public void ApplyStateSnapshot(VehicleOutputState snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[TiltDrive][VehicleOutputStore] Se intento aplicar un VehicleOutputState nulo.");
                return;
            }

            current.simulationSpeed = snapshot.simulationSpeed;
            current.simulationSpeedUnit = snapshot.simulationSpeedUnit;
            current.motorSpeedPercent = Mathf.Clamp(snapshot.motorSpeedPercent, 0f, 100f);
            current.engineRPM = Mathf.Max(0f, snapshot.engineRPM);
            current.engineOn = snapshot.engineOn;
            current.currentGear = snapshot.currentGear;

            current.transmissionOutputTorqueNm = Mathf.Max(0f, snapshot.transmissionOutputTorqueNm);
            current.transmissionOutputRPM = snapshot.transmissionOutputRPM;

            current.wheelRadiusMeters = Mathf.Max(0.01f, snapshot.wheelRadiusMeters);
            current.wheelCircumferenceMeters = Mathf.Max(0f, snapshot.wheelCircumferenceMeters);
            current.wheelAngularVelocityRadS = snapshot.wheelAngularVelocityRadS;

            current.finalSpeedMS = snapshot.finalSpeedMS;
            current.finalSpeedKMH = snapshot.finalSpeedKMH;
            current.finalSpeedMPH = snapshot.finalSpeedMPH;
            current.absoluteSpeedKMH = Mathf.Max(0f, snapshot.absoluteSpeedKMH);

            current.driveSpeedPercent = Mathf.Clamp(snapshot.driveSpeedPercent, 0f, 100f);
            current.outputPowerKW = Mathf.Max(0f, snapshot.outputPowerKW);
            current.outputPowerHP = Mathf.Max(0f, snapshot.outputPowerHP);
            current.tractionForceN = snapshot.tractionForceN;

            current.accelerationMS2 = snapshot.accelerationMS2;
            current.brakeInput = Mathf.Clamp01(snapshot.brakeInput);
            current.aerodynamicDragForceN = Mathf.Max(0f, snapshot.aerodynamicDragForceN);
            current.rollingResistanceForceN = Mathf.Max(0f, snapshot.rollingResistanceForceN);
            current.brakeForceN = Mathf.Max(0f, snapshot.brakeForceN);
            current.engineBrakeForceN = Mathf.Max(0f, snapshot.engineBrakeForceN);

            current.driveDirection = snapshot.driveDirection;
            current.isMoving = snapshot.isMoving;

            current.simulationTick = Mathf.Max(0, snapshot.simulationTick);
            current.lastUpdateTime = Mathf.Max(0f, snapshot.lastUpdateTime);

            NotifyStateChanged();
        }

        public void ForceRefresh()
        {
            config.ClampValues();
            NotifyConfigChanged();
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke(current);
        }

        private void NotifyConfigChanged()
        {
            OnConfigChanged?.Invoke(config);
        }
    }
}
