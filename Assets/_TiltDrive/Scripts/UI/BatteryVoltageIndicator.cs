using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TiltDrive.ElectricalSystem;

namespace TiltDrive.UISystem
{
    public class BatteryVoltageIndicator : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private VehicleElectricalStore electricalStore;
        [SerializeField] private Text voltageText;
        [SerializeField] private TMP_Text voltageTMP;
        [SerializeField] private Slider chargeSlider;
        [SerializeField] private Image chargeFill;

        [Header("Colores")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.85f, 0.35f);
        [SerializeField] private Color lowColor = new Color(1f, 0.72f, 0.15f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.1f);

        [Header("Opciones")]
        [SerializeField] private bool autoFindReferences = true;
        [SerializeField] private bool showAlternatorState = true;
        [SerializeField] private bool showVoltageSag = true;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (electricalStore != null)
            {
                electricalStore.OnStateChanged += HandleElectricalStateChanged;
                HandleElectricalStateChanged(electricalStore.Current);
            }
        }

        private void OnDisable()
        {
            if (electricalStore != null)
            {
                electricalStore.OnStateChanged -= HandleElectricalStateChanged;
            }
        }

        private void EnsureReferences()
        {
            if (!autoFindReferences) return;

            if (electricalStore == null)
            {
                electricalStore = VehicleElectricalStore.Instance;

                if (electricalStore == null)
                {
                    electricalStore = FindFirstObjectByType<VehicleElectricalStore>();
                }
            }
        }

        private void HandleElectricalStateChanged(VehicleElectricalState state)
        {
            if (state == null) return;

            string alternator = showAlternatorState
                ? $" | ALT={(state.alternatorActive ? "ON" : "OFF")}"
                : string.Empty;
            string sag = showVoltageSag
                ? $" | Drop={state.voltageSag:F2}V"
                : string.Empty;
            string text =
                $"Battery {state.systemVoltage:F2}V | {state.batteryChargePercent:F0}%{sag}{alternator}";

            if (voltageText != null)
            {
                voltageText.text = text;
                voltageText.color = GetStateColor(state);
            }

            if (voltageTMP != null)
            {
                voltageTMP.text = text;
                voltageTMP.color = GetStateColor(state);
            }

            if (chargeSlider != null)
            {
                chargeSlider.value = Mathf.Clamp01(state.batteryChargePercent / 100f);
            }

            if (chargeFill != null)
            {
                chargeFill.color = GetStateColor(state);
                chargeFill.fillAmount = Mathf.Clamp01(state.batteryChargePercent / 100f);
            }
        }

        private Color GetStateColor(VehicleElectricalState state)
        {
            if (state.criticalVoltageWarning)
            {
                return criticalColor;
            }

            if (state.lowVoltageWarning)
            {
                return lowColor;
            }

            return normalColor;
        }
    }
}
