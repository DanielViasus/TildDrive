using System;
using UnityEngine;

namespace TiltDrive.CoolingSystem
{
    public class RadiatorStore : MonoBehaviour
    {
        public static RadiatorStore Instance { get; private set; }

        [Header("Configuracion")]
        [SerializeField] private RadiatorConfig config = new RadiatorConfig();

        [Header("Estado Actual")]
        [SerializeField] private RadiatorState current = new RadiatorState();

        public RadiatorConfig Config => config;
        public RadiatorState Current => current;

        public event Action<RadiatorState> OnStateChanged;
        public event Action<RadiatorConfig> OnConfigChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            config ??= new RadiatorConfig();
            current ??= new RadiatorState();

            config.ClampValues();
            current.InitializeFromConfig(config);
            NotifyStateChanged();
            NotifyConfigChanged();
        }

        public void SetConfig(RadiatorConfig newConfig, bool reinitializeState = true)
        {
            if (newConfig == null)
            {
                Debug.LogWarning("[TiltDrive][RadiatorStore] Se intento asignar un RadiatorConfig nulo.");
                return;
            }

            config = newConfig;
            config.ClampValues();

            if (reinitializeState)
            {
                current.InitializeFromConfig(config);
                NotifyStateChanged();
            }

            NotifyConfigChanged();
        }

        public void ReapplyConfigToState()
        {
            if (config == null || current == null) return;

            config.ClampValues();
            current.coolantType = config.coolantType;
            current.coolantLevelPercent = Mathf.Clamp(current.coolantLevelPercent, 0f, 100f);
            current.coolantTemperatureC = Mathf.Max(config.coolantAmbientTemperatureC, current.coolantTemperatureC);
            current.systemPressureKpa = Mathf.Max(config.basePressureKpa, current.systemPressureKpa);
            NotifyConfigChanged();
            NotifyStateChanged();
        }

        public void ResetRuntime()
        {
            current.InitializeFromConfig(config);
            NotifyStateChanged();
        }

        public void ApplyStateSnapshot(RadiatorState snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[TiltDrive][RadiatorStore] Se intento aplicar un RadiatorState nulo.");
                return;
            }

            current.CopyFrom(snapshot);
            NotifyStateChanged();
        }

        public void Repair()
        {
            if (config != null)
            {
                config.ClampValues();
            }

            current.radiatorHealthPercent = 100f;
            current.accumulatedDamagePercent = 0f;
            current.coolantType = config != null ? config.coolantType : current.coolantType;
            current.coolantLevelPercent = config != null ? config.initialCoolantLevelPercent : 100f;
            current.coolantTemperatureC = config != null ? config.coolantInitialTemperatureC : 45f;
            current.systemPressureKpa = config != null ? config.basePressureKpa : 95f;
            current.coolingEfficiency = 1f;
            current.airflowEfficiency = 1f;
            current.radiatorFanActive = false;
            current.hasPerforation = false;
            current.leakRatePercentPerSecond = 0f;
            current.pressureDamageActive = false;
            current.lowCoolant = false;
            current.radiatorOverPressurized = false;
            current.ClearWarning();
            current.lastDamageReason = string.Empty;
            NotifyStateChanged();
        }

        public void RefillCoolant(float levelPercent = 100f)
        {
            current.coolantLevelPercent = Mathf.Clamp(levelPercent, 0f, 100f);
            current.lowCoolant = current.coolantLevelPercent <= (config != null ? config.lowCoolantWarningPercent : 35f);
            NotifyStateChanged();
        }

        public void ApplyDamagePercent(float damagePercent, string reason, bool causePerforation = false)
        {
            float clampedDamage = Mathf.Max(0f, damagePercent);
            if (clampedDamage <= 0f) return;

            current.radiatorHealthPercent = Mathf.Clamp(current.radiatorHealthPercent - clampedDamage, 0f, 100f);
            current.accumulatedDamagePercent += clampedDamage;
            current.lastDamageReason = reason;

            if (causePerforation)
            {
                current.hasPerforation = true;
                float leak = config != null ? config.perforationLeakRatePercentPerSecond : 3.5f;
                current.leakRatePercentPerSecond = Mathf.Max(current.leakRatePercentPerSecond, leak);
            }

            Debug.LogWarning(
                $"[TiltDrive][RadiatorDamage] Damage={clampedDamage:F2}% | Health={current.radiatorHealthPercent:F1}% | Perforated={current.hasPerforation} | Reason={reason}");

            NotifyStateChanged();
        }

        public void ApplyCollisionDamage(float impactSeverity)
        {
            float severity = Mathf.Clamp01(impactSeverity);
            float damage = Mathf.Lerp(4f, 65f, severity);
            bool perforation = severity >= 0.45f;
            ApplyDamagePercent(damage, "Collision impact", perforation);
        }

        public void ApplyPressureDamage(float pressureSeverity, string reason)
        {
            float severity = Mathf.Clamp01(pressureSeverity);
            float damage = Mathf.Lerp(0.15f, 4f, severity);
            bool perforation = severity >= 0.9f;
            ApplyDamagePercent(damage, reason, perforation);
        }

        public void ForceRefresh()
        {
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
