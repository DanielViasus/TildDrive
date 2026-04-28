using System;
using System.Globalization;
using TiltDrive.ElectricalSystem;
using TiltDrive.EngineSystem;
using TiltDrive.State;
using TiltDrive.TransmissionSystem;
using TiltDrive.VehicleSystem;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TiltDrive.UISystem
{
    public class InstructorTelemetryDashboard : MonoBehaviour
    {
        private enum BackgroundScaleMode
        {
            Stretch = 0,
            Fit = 1,
            Fill = 2
        }

        private enum DashboardLanguage
        {
            Spanish = 0,
            English = 1
        }

        private const float BaseWidth = 1280f;
        private const float BaseHeight = 800f;
        private const string DefaultBackgroundAssetPath = "Assets/_TiltDrive/Art/Sprites/UI/UI_Teacher_BK.png";
        private const string DefaultSteeringBackgroundAssetPath = "Assets/_TiltDrive/Art/Sprites/UI/UI_Sistem_De_Direccion_Bk.png";
        private const string DefaultSteeringSelectorAssetPath = "Assets/_TiltDrive/Art/Sprites/UI/UI_Sistem_De_Direccion_Selector.png";
        private const string DefaultSpeedLimitDeselectedAssetPath = "Assets/_TiltDrive/Art/Sprites/UI/Se\u00f1a_Deseleccionada.png";
        private const string DefaultSpeedLimitSelectedAssetPath = "Assets/_TiltDrive/Art/Sprites/UI/Se\u00f1a_Seleccionada.png";
        private const string DefaultEngineConnectorsAssetPath = "Assets/_TiltDrive/Art/Sprites/UI/UI_Conectores.png";
        private const string DefaultEngineValueBackgroundAssetPath = "Assets/_TiltDrive/Art/Sprites/UI/Barra_info_texto_Velocidad.png";
        private const string DefaultPrimaryFontPath = "Assets/_TiltDrive/Art/Fonts/Orbitron-Regular.ttf";
        private const string DefaultCompanionFontPath = "Assets/_TiltDrive/Art/Fonts/InterVariable.ttf";
        private const float SteeringBaseSize = 180f;
        private const float EngineValueWidth = 105f;
        private const float EngineValueHeight = 26f;
        private const float EngineValueRadius = 7f;
        private const float SpeedLimitButtonSize = 62f;
        private const float SpeedLimitSignSize = 58f;
        private const int SpeedLimitTextureSize = 256;
        private const float SpeedLimitBorderWidth = 5f;

        [Header("Referencias")]
        [SerializeField] private InputStore inputStore;
        [SerializeField] private EngineStore engineStore;
        [SerializeField] private TransmissionStore transmissionStore;
        [SerializeField] private VehicleElectricalStore electricalStore;
        [SerializeField] private VehicleOutputStore vehicleOutputStore;
        [SerializeField] private RuntimeVehicleConfigMenu configMenu;
        [SerializeField] private bool autoFindTelemetryReferences = true;

        [Header("Fondo")]
        [SerializeField] private Texture2D backgroundTexture;
        [SerializeField] private BackgroundScaleMode scaleMode = BackgroundScaleMode.Stretch;
        [SerializeField] private Color fallbackColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        [SerializeField] private bool autoCreateOnPlay = true;

        [Header("Idioma")]
        [SerializeField] private DashboardLanguage language = DashboardLanguage.Spanish;

        [Header("Tipografia")]
        [SerializeField] private Font primaryFont;
        [SerializeField] private Font companionFont;

        [Header("Sistema de direccion")]
        [SerializeField] private bool showDateTime = true;
        [SerializeField] private bool showSteeringSystem = true;
        [SerializeField] private Texture2D steeringBackgroundTexture;
        [SerializeField] private Texture2D steeringSelectorTexture;
        [SerializeField] private Rect steeringRect = new Rect(98f, 165f, 180f, 180f);
        [SerializeField] [Min(0.1f)] private float steeringVisualScale = 1f;
        [SerializeField] private Color textColor = Color.black;
        [SerializeField] [Range(-450f, 450f)] private float fallbackSteeringWheelDegrees = 0f;
        [SerializeField] [Min(1f)] private float totalSteeringWheelDegrees = 900f;

        [Header("Sistema de pedales")]
        [SerializeField] private bool showPedalSystem = true;
        [SerializeField] private Rect pedalsRect = new Rect(52f, 372f, 236f, 192f);
        [SerializeField] private Color pedalActiveColor = Color.black;
        [SerializeField] private Color pedalTextColor = Color.black;
        [SerializeField] [Range(1, 20)] private int pedalSegments = 10;
        [SerializeField] [Min(1f)] private float pedalSegmentHeight = 10f;
        [SerializeField] [Min(0f)] private float pedalSegmentGap = 3f;

        [Header("Sistema de motor")]
        [SerializeField] private bool showEngineSystem = true;
        [SerializeField] private Rect engineRect = new Rect(52f, 610f, 330f, 128f);
        [SerializeField] private Texture2D engineConnectorsTexture;
        [SerializeField] private Texture2D engineValueBackgroundTexture;
        [SerializeField] private Color engineTextColor = Color.black;
        [SerializeField] private Color engineMutedTextColor = new Color(0.38f, 0.38f, 0.38f, 1f);
        [SerializeField] private Color engineButtonColor = Color.black;
        [SerializeField] private Color engineButtonTextColor = Color.white;

        [Header("Velocidad simulada")]
        [SerializeField] private bool showSpeedSystem = true;
        [SerializeField] private Rect speedRect = new Rect(370f, 610f, 345f, 142f);
        [SerializeField] private Texture2D speedLimitDeselectedTexture;
        [SerializeField] private Texture2D speedLimitSelectedTexture;
        [SerializeField] private VehicleSpeedUnit speedDisplayUnit = VehicleSpeedUnit.KilometersPerHour;
        [SerializeField] private Color speedTextColor = Color.black;
        [SerializeField] private Color speedLimitAlertColor = new Color(0.616f, 0f, 0f, 1f);
        [SerializeField] private Color speedLimitCriticalColor = new Color(0.847f, 0f, 0f, 1f);
        [SerializeField] private Color speedLimitSelectedFillColor = new Color(0.851f, 0.851f, 0.851f, 1f);
        [SerializeField] private Color speedLimitDefaultBorderColor = Color.black;
        [SerializeField] private Color speedLimitSelectedBorderColor = new Color(0.616f, 0f, 0f, 1f);
        [SerializeField] [Range(0f, 0.5f)] private float speedLimitTolerance = 0.10f;
        [SerializeField] private int selectedSpeedLimit = 0;

        [Header("Marcha y bateria")]
        [SerializeField] private bool showGearBatterySystem = true;
        [SerializeField] private Rect gearBatteryRect = new Rect(742f, 610f, 245f, 142f);
        [SerializeField] private Color gearBatteryTextColor = Color.black;
        [SerializeField] private Color gearBatteryMutedTextColor = new Color(0.38f, 0.38f, 0.38f, 1f);
        [SerializeField] private Color gearBatteryWarningColor = new Color(0.616f, 0f, 0f, 1f);
        [SerializeField] [Min(0f)] private float looseClutchWarningHoldSeconds = 2f;
        [SerializeField] private bool showBatteryCurrent = false;

        [Header("Estilos telemetria inferior")]
        [SerializeField] [Range(12, 40)] private int telemetryTitleFontSize = 21;
        [SerializeField] private FontStyle telemetryTitleFontStyle = FontStyle.Normal;
        [SerializeField] [Range(20, 72)] private int telemetryValueFontSize = 39;
        [SerializeField] private FontStyle telemetryValueFontStyle = FontStyle.Normal;
        [SerializeField] [Range(8, 32)] private int telemetrySupplementFontSize = 18;
        [SerializeField] [Range(8, 28)] private int speedLimitValueFontSize = 16;
        [SerializeField] [Range(6, 18)] private int speedLimitUnitFontSize = 7;
        [SerializeField] [Range(8, 24)] private int speedLimitNoLimitTopFontSize = 12;
        [SerializeField] [Range(8, 20)] private int speedLimitNoLimitBottomFontSize = 9;
        [SerializeField] private Color telemetryDividerColor = new Color(0f, 0f, 0f, 0.22f);
        [SerializeField] [Min(1f)] private float telemetryDividerWidth = 1f;

        private GUIStyle dateStyle;
        private GUIStyle steeringDegreesStyle;
        private GUIStyle steeringLabelStyle;
        private GUIStyle steeringSubLabelStyle;
        private GUIStyle pedalPercentStyle;
        private GUIStyle pedalLabelStyle;
        private GUIStyle engineTitleStyle;
        private GUIStyle engineLabelStyle;
        private GUIStyle engineUnitStyle;
        private GUIStyle engineValueStyle;
        private GUIStyle engineButtonStyle;
        private GUIStyle speedTitleStyle;
        private GUIStyle speedValueStyle;
        private GUIStyle speedUnitStyle;
        private GUIStyle speedLimitNumberStyle;
        private GUIStyle speedLimitUnitStyle;
        private GUIStyle speedLimitNoLimitTopStyle;
        private GUIStyle speedLimitNoLimitBottomStyle;
        private GUIStyle gearBatteryTitleStyle;
        private GUIStyle gearValueStyle;
        private GUIStyle gearSuffixStyle;
        private GUIStyle batteryValueStyle;
        private GUIStyle batteryUnitStyle;
        private GUIStyle handbrakeStateStyle;
        private GUIStyle invisibleButtonStyle;
        private Texture2D whiteTexture;
        private Texture2D engineButtonTexture;
        private Texture2D engineDotTexture;
        private Texture2D speedLimitDefaultFallbackTexture;
        private Texture2D speedLimitSelectedFallbackTexture;
        private Texture2D transparentTexture;
        private bool wasSpeedLimitExceeded = false;
        private float looseClutchWarningUntil = 0f;
        private string lastTransmissionMisuseSignature = string.Empty;
        private readonly int[] kilometerSpeedLimits = { 0, 30, 50, 90, 120 };
        private readonly int[] mileSpeedLimits = { 0, 25, 35, 55, 70 };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDashboard()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!Application.isPlaying)
            {
                return;
            }

            if (FindFirstObjectByType<InstructorTelemetryDashboard>() != null)
            {
                return;
            }

            GameObject dashboard = new GameObject("Instructor Telemetry Dashboard");
            InstructorTelemetryDashboard component = dashboard.AddComponent<InstructorTelemetryDashboard>();
            if (!component.autoCreateOnPlay)
            {
                Destroy(dashboard);
            }
#endif
        }

        private void Awake()
        {
            EnsureAssetReferences();
            EnsureTelemetryReferences();
        }

        private void OnValidate()
        {
            EnsureAssetReferences();
        }

        private void OnGUI()
        {
            EnsureAssetReferences();
            EnsureTelemetryReferences();

            Font previousFont = GUI.skin.font;
            if (primaryFont != null)
            {
                GUI.skin.font = primaryFont;
            }

            Rect screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
            if (backgroundTexture == null)
            {
                DrawColor(screenRect, fallbackColor);
                GUI.skin.font = previousFont;
                return;
            }

            GUI.DrawTexture(screenRect, backgroundTexture, ResolveScaleMode(), true);
            DrawScaledOverlay();
            GUI.skin.font = previousFont;
        }

        private void DrawScaledOverlay()
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / BaseWidth, Screen.height / BaseHeight);
            float offsetX = (Screen.width - BaseWidth * scale) * 0.5f;
            float offsetY = (Screen.height - BaseHeight * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity,
                Vector3.one * scale);

            EnsureStyles();
            if (showDateTime)
            {
                DrawDateTime();
            }

            if (showSteeringSystem)
            {
                DrawSteeringSystem();
            }

            if (showPedalSystem)
            {
                DrawPedalSystem();
            }

            DrawLowerTelemetryDividers();

            if (showEngineSystem)
            {
                DrawEngineSystem();
            }

            if (showSpeedSystem)
            {
                DrawSpeedSystem();
            }

            if (showGearBatterySystem)
            {
                DrawGearBatterySystem();
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawDateTime()
        {
            DateTime now = DateTime.Now;
            GUI.Label(new Rect(51f, 70f, 180f, 24f), now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), dateStyle);
            GUI.Label(new Rect(51f, 97f, 180f, 24f), now.ToString("HH:mm:ss", CultureInfo.InvariantCulture), dateStyle);
        }

        private void DrawSteeringSystem()
        {
            float steeringDegrees = GetSteeringDegrees();
            float maxWheelDegrees = GetMaxSteeringWheelDegrees();
            float selectorRotation = Mathf.Clamp(steeringDegrees / Mathf.Max(1f, maxWheelDegrees), -1f, 1f) * 450f;

            float steeringVisualSize = SteeringBaseSize * steeringVisualScale;
            Rect textureRect = CenterRect(steeringRect, steeringVisualSize, steeringVisualSize);

            if (steeringBackgroundTexture != null)
            {
                GUI.DrawTexture(textureRect, steeringBackgroundTexture, ScaleMode.StretchToFill, true);
            }

            if (steeringSelectorTexture != null)
            {
                DrawRotatedTexture(textureRect, steeringSelectorTexture, selectorRotation);
            }

            Rect degreesRect = new Rect(textureRect.x + 18f * steeringVisualScale, textureRect.y + 58f * steeringVisualScale, 144f * steeringVisualScale, 46f * steeringVisualScale);
            Rect labelRect = new Rect(textureRect.x + 36f * steeringVisualScale, textureRect.y + 112f * steeringVisualScale, 108f * steeringVisualScale, 17f * steeringVisualScale);
            Rect subLabelRect = new Rect(textureRect.x + 54f * steeringVisualScale, textureRect.y + 128f * steeringVisualScale, 72f * steeringVisualScale, 15f * steeringVisualScale);

            GUI.Label(degreesRect, steeringDegrees.ToString("+0;-0;0", CultureInfo.InvariantCulture) + "\u00B0", steeringDegreesStyle);
            GUI.Label(labelRect, T("direction"), steeringLabelStyle);
            GUI.Label(subLabelRect, T("steering_wheel"), steeringSubLabelStyle);
        }

        private float GetSteeringDegrees()
        {
            if (vehicleOutputStore != null && vehicleOutputStore.Current != null)
            {
                return ConvertInputToSteeringWheelDegrees(vehicleOutputStore.Current.steerInput);
            }

            if (inputStore != null && inputStore.Current != null)
            {
                return ConvertInputToSteeringWheelDegrees(inputStore.Current.steer);
            }

            return Mathf.Clamp(fallbackSteeringWheelDegrees, -GetMaxSteeringWheelDegrees(), GetMaxSteeringWheelDegrees());
        }

        private float ConvertInputToSteeringWheelDegrees(float steeringInput)
        {
            float normalizedInput = Mathf.Clamp(steeringInput, -1f, 1f);
            return normalizedInput * GetMaxSteeringWheelDegrees();
        }

        private float GetMaxSteeringWheelDegrees()
        {
            return Mathf.Max(1f, totalSteeringWheelDegrees) * 0.5f;
        }

        private void DrawRotatedTexture(Rect rect, Texture texture, float angleDegrees)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Vector3 pivot = GUI.matrix.MultiplyPoint(rect.center);
            GUIUtility.RotateAroundPivot(angleDegrees, pivot);
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.matrix = previousMatrix;
        }

        private static Rect CenterRect(Rect container, float width, float height)
        {
            return new Rect(
                container.x + (container.width - width) * 0.5f,
                container.y + (container.height - height) * 0.5f,
                width,
                height);
        }

        private string T(string key)
        {
            bool es = language == DashboardLanguage.Spanish;
            switch (key)
            {
                case "direction": return es ? "Direccion" : "Direction";
                case "steering_wheel": return es ? "Timon" : "Wheel";
                case "clutch": return es ? "Embrague" : "Clutch";
                case "brake": return es ? "Freno" : "Brake";
                case "throttle": return es ? "Acelerador" : "Throttle";
                case "engine": return es ? "Motor" : "Engine";
                case "handbrake": return es ? "Freno de mano" : "Handbrake";
                case "active": return es ? "Activo" : "On";
                case "inactive": return es ? "Inactivo" : "Off";
                case "engine_on": return es ? "Encendido" : "On";
                case "engine_off": return es ? "Apagado" : "Off";
                case "state": return es ? "Estado" : "State";
                case "simulated_speed": return es ? "Velocidad simulada" : "Simulated speed";
                case "no_limit_top": return es ? "SIN" : "NO";
                case "no_limit_bottom": return es ? "LIMITE" : "LIMIT";
                case "current_gear": return es ? "marcha Actual" : "Current gear";
                case "battery_voltage": return es ? "Voltaje de Bateria" : "Battery voltage";
                case "battery_current": return es ? "Consumo de Bateria" : "Battery current";
                default: return key;
            }
        }

        private void DrawPedalSystem()
        {
            float clutch = inputStore != null && inputStore.Current != null ? inputStore.Current.clutch : 0f;
            float brake = inputStore != null && inputStore.Current != null ? inputStore.Current.brake : 0f;
            float throttle = inputStore != null && inputStore.Current != null ? inputStore.Current.throttle : 0f;
            bool handbrakeActive = vehicleOutputStore != null &&
                vehicleOutputStore.Current != null &&
                vehicleOutputStore.Current.handbrakeActive;
            bool clutchWarning = IsLooseClutchShiftWarningActive();

            DrawPedalColumn(new Rect(pedalsRect.x + 8f, pedalsRect.y, 38f, 126f), clutch, T("clutch"), clutchWarning ? gearBatteryWarningColor : pedalActiveColor);
            DrawPedalColumn(new Rect(pedalsRect.x + 82f, pedalsRect.y, 38f, 126f), brake, T("brake"));
            DrawPedalColumn(new Rect(pedalsRect.x + 156f, pedalsRect.y, 38f, 126f), throttle, T("throttle"));
            DrawHandbrakeState(handbrakeActive);
        }

        private void DrawPedalColumn(Rect barRect, float value, string label)
        {
            DrawPedalColumn(barRect, value, label, pedalActiveColor);
        }

        private void DrawPedalColumn(Rect barRect, float value, string label, Color activeColor)
        {
            value = Mathf.Clamp01(value);
            DrawSegmentedPedalBar(barRect, value, activeColor);

            Rect percentRect = new Rect(barRect.x - 8f, pedalsRect.y + 131f, 54f, 18f);
            Rect labelRect = new Rect(barRect.x - 18f, pedalsRect.y + 151f, 74f, 18f);
            GUI.Label(percentRect, Mathf.RoundToInt(value * 100f).ToString(CultureInfo.InvariantCulture) + "%", pedalPercentStyle);
            GUI.Label(labelRect, label, pedalLabelStyle);
        }

        private void DrawHandbrakeState(bool active)
        {
            Color previousTextColor = handbrakeStateStyle.normal.textColor;
            handbrakeStateStyle.normal.textColor = active ? gearBatteryWarningColor : pedalTextColor;

            Rect handbrakeRect = new Rect(pedalsRect.x + 10f, pedalsRect.y + 174f, 202f, 18f);
            string stateText = active ? T("active") : T("inactive");
            string handbrakeLabel = T("handbrake");
            GUI.Label(handbrakeRect, $"{handbrakeLabel}  {stateText}", handbrakeStateStyle);

            handbrakeStateStyle.normal.textColor = previousTextColor;
        }

        private void DrawSegmentedPedalBar(Rect barRect, float value, Color activeColor)
        {
            float clampedValue = Mathf.Clamp01(value);
            int activeSegments = Mathf.RoundToInt(clampedValue * pedalSegments);
            float segmentStep = pedalSegmentHeight + pedalSegmentGap;
            if (activeSegments <= 0)
            {
                DrawColor(
                    new Rect(barRect.x, barRect.yMax - pedalSegmentHeight * 0.5f, barRect.width, pedalSegmentHeight * 0.5f),
                    activeColor);
                return;
            }

            for (int i = 0; i < pedalSegments; i++)
            {
                float y = barRect.yMax - pedalSegmentHeight - i * segmentStep;
                if (y < barRect.y)
                {
                    break;
                }

                if (i < activeSegments)
                {
                    DrawColor(new Rect(barRect.x, y, barRect.width, pedalSegmentHeight), activeColor);
                }
            }
        }

        private void DrawEngineSystem()
        {
            EngineState engine = engineStore != null ? engineStore.Current : null;
            bool isOn = engine != null && engine.engineOn;
            bool isStarting = engine != null && engine.engineStarting;
            float healthPercent = engine != null ? engine.componentHealthPercent : 0f;
            float rpmThousands = engine != null ? engine.currentRPM / 1000f : 0f;

            if (engineConnectorsTexture != null)
            {
                GUI.DrawTexture(new Rect(engineRect.x, engineRect.y, 16f, 118f), engineConnectorsTexture, ScaleMode.StretchToFill, true);
            }
            else
            {
                Vector2 lineTop = new Vector2(engineRect.x + 9f, engineRect.y + 21f);
                Vector2 lineBottom = new Vector2(engineRect.x + 9f, engineRect.y + 119f);
                DrawColor(new Rect(lineTop.x - 1.5f, lineTop.y, 3f, lineBottom.y - lineTop.y), engineTextColor);
                DrawDot(lineTop, 8f, engineTextColor);
                DrawDot(new Vector2(engineRect.x + 22f, engineRect.y + 74f), 5f, engineTextColor);
                DrawDot(new Vector2(engineRect.x + 5f, engineRect.y + 119f), 5f, engineTextColor);
                DrawColor(new Rect(engineRect.x + 9f, engineRect.y + 73f, 14f, 3f), engineTextColor);
                DrawColor(new Rect(engineRect.x + 9f, engineRect.y + 118f, 14f, 3f), engineTextColor);
            }

            Rect titleRect = new Rect(engineRect.x + 30f, engineRect.y + 3f, 145f, 34f);
            GUI.Label(titleRect, T("engine"), engineTitleStyle);
            if (GUI.Button(titleRect, GUIContent.none, invisibleButtonStyle))
            {
                ShowEngineConfiguration();
            }

            string engineButtonText = isOn || isStarting ? T("engine_on") : T("engine_off");
            if (GUI.Button(new Rect(engineRect.x + 160f, engineRect.y + 6f, EngineValueWidth, EngineValueHeight), engineButtonText, engineButtonStyle))
            {
                ToggleEnginePower();
            }

            GUI.Label(new Rect(engineRect.x + 30f, engineRect.y + 56f, 128f, 26f), T("state"), engineLabelStyle);
            GUI.Label(new Rect(engineRect.x + 30f, engineRect.y + 95f, 76f, 26f), "RPM", engineLabelStyle);
            GUI.Label(new Rect(engineRect.x + 92f, engineRect.y + 108f, 58f, 13f), "/X1000", engineUnitStyle);

            DrawEngineValue(new Rect(engineRect.x + 160f, engineRect.y + 55f, EngineValueWidth, EngineValueHeight), Mathf.RoundToInt(healthPercent).ToString(CultureInfo.InvariantCulture) + "%");
            DrawEngineValue(new Rect(engineRect.x + 160f, engineRect.y + 98f, EngineValueWidth, EngineValueHeight), rpmThousands.ToString("0.0", CultureInfo.InvariantCulture));
        }

        private void DrawLowerTelemetryDividers()
        {
            if (telemetryDividerWidth <= 0f)
            {
                return;
            }

            float top = engineRect.y + 4f;
            float height = 116f;
            DrawColor(new Rect(342f, top, telemetryDividerWidth, height), telemetryDividerColor);
            DrawColor(new Rect(714f, top, telemetryDividerWidth, height), telemetryDividerColor);
        }

        private void DrawEngineValue(Rect rect, string value)
        {
            GUI.Label(rect, value, engineValueStyle);
        }

        private void ToggleEnginePower()
        {
            if (engineStore == null || engineStore.Current == null)
            {
                return;
            }

            bool nextState = !(engineStore.Current.engineOn || engineStore.Current.engineStarting);
            engineStore.SetEngineOn(nextState);
            engineStore.SetEngineStarting(false);
            engineStore.SetEngineStalled(false);

            if (nextState)
            {
                float idleRpm = Mathf.Max(0f, engineStore.Current.idleRPM);
                if (engineStore.Current.currentRPM < idleRpm)
                {
                    engineStore.SetCurrentRPM(idleRpm);
                    engineStore.SetTargetRPM(idleRpm);
                }
            }
            else
            {
                engineStore.SetCurrentRPM(0f);
                engineStore.SetTargetRPM(0f);
            }
        }

        private void ShowEngineConfiguration()
        {
            configMenu ??= FindFirstObjectByType<RuntimeVehicleConfigMenu>();
            configMenu?.ShowEngineTab();
        }

        private void DrawSpeedSystem()
        {
            VehicleOutputState vehicle = vehicleOutputStore != null ? vehicleOutputStore.Current : null;
            VehicleSpeedUnit unit = speedDisplayUnit;
            float speed = GetCurrentDisplaySpeed(vehicle, unit);
            string unitLabel = GetSpeedUnitLabel(unit);
            bool limitExceeded = IsSpeedLimitExceeded(speed);
            bool criticalLimitExceeded = IsSpeedLimitCriticalExceeded(speed);
            Color valueColor = ResolveSpeedValueColor(limitExceeded, criticalLimitExceeded);

            if (criticalLimitExceeded && !wasSpeedLimitExceeded)
            {
                Debug.LogWarning(
                    $"[TiltDrive][InstructorTelemetry] Mala practica por limite de velocidad. " +
                    $"Velocidad={speed:F1} {unitLabel}, Limite={selectedSpeedLimit} {unitLabel}, " +
                    $"Tolerancia={(speedLimitTolerance * 100f):F0}%.");
            }

            wasSpeedLimitExceeded = criticalLimitExceeded;

            GUI.Label(new Rect(speedRect.x, speedRect.y, 334f, 32f), T("simulated_speed"), speedTitleStyle);

            speedValueStyle.normal.textColor = valueColor;
            Rect valueRect = new Rect(speedRect.x + 100f, speedRect.y + 39f, 96f, 46f);
            GUI.Label(valueRect, Mathf.RoundToInt(Mathf.Max(0f, speed)).ToString(CultureInfo.InvariantCulture), speedValueStyle);
            Rect unitRect = new Rect(speedRect.x + 212f, speedRect.y + 51f, 82f, 26f);
            GUI.Label(unitRect, unitLabel, speedUnitStyle);
            if (GUI.Button(unitRect, GUIContent.none, invisibleButtonStyle))
            {
                ToggleSpeedDisplayUnit();
            }

            int[] limits = GetSpeedLimitOptions(unit);
            for (int i = 0; i < limits.Length; i++)
            {
                DrawSpeedLimitButton(
                    new Rect(speedRect.x + i * 60f, speedRect.y + 91f, SpeedLimitButtonSize, SpeedLimitButtonSize),
                    limits[i],
                    unitLabel);
            }
        }

        private void DrawGearBatterySystem()
        {
            TransmissionState transmission = transmissionStore != null ? transmissionStore.Current : null;
            VehicleElectricalState electrical = electricalStore != null ? electricalStore.Current : null;
            bool clutchWarning = IsLooseClutchShiftWarningActive();
            Color gearColor = clutchWarning ? gearBatteryWarningColor : gearBatteryTextColor;
            Color batteryColor = IsBatteryWarning(electrical) ? gearBatteryWarningColor : gearBatteryTextColor;

            gearBatteryTitleStyle.normal.textColor = gearBatteryTextColor;
            GUI.Label(new Rect(gearBatteryRect.x, gearBatteryRect.y, 220f, 28f), T("current_gear"), gearBatteryTitleStyle);

            string gearText = FormatGear(transmission != null ? transmission.currentGear : 0);
            gearValueStyle.normal.textColor = gearColor;
            gearSuffixStyle.normal.textColor = gearColor;
            Rect gearValueRect = new Rect(gearBatteryRect.x + 44f, gearBatteryRect.y + 29f, 64f, 42f);
            GUI.Label(gearValueRect, gearText, gearValueStyle);
            if (IsForwardGear(transmission != null ? transmission.currentGear : 0))
            {
                GUI.Label(
                    new Rect(gearBatteryRect.x + 128f, gearBatteryRect.y + 40f, 46f, 22f),
                    GetForwardGearSuffix(transmission != null ? transmission.currentGear : 0),
                    gearSuffixStyle);
            }

            string batteryTitle = showBatteryCurrent ? T("battery_current") : T("battery_voltage");
            gearBatteryTitleStyle.normal.textColor = batteryColor;
            Rect batteryTitleRect = new Rect(gearBatteryRect.x, gearBatteryRect.y + 72f, 236f, 28f);
            GUI.Label(batteryTitleRect, batteryTitle, gearBatteryTitleStyle);
            if (GUI.Button(batteryTitleRect, GUIContent.none, invisibleButtonStyle))
            {
                showBatteryCurrent = !showBatteryCurrent;
            }

            float batteryValue = 0f;
            string batteryUnit = showBatteryCurrent ? "A" : "VDC";
            if (electrical != null)
            {
                batteryValue = showBatteryCurrent ? electrical.loadCurrentAmps : electrical.systemVoltage;
            }

            batteryValueStyle.normal.textColor = batteryColor;
            batteryUnitStyle.normal.textColor = batteryColor;
            Rect batteryValueRect = new Rect(gearBatteryRect.x + 4f, gearBatteryRect.y + 99f, 128f, 42f);
            GUI.Label(batteryValueRect, batteryValue.ToString("0.0", CultureInfo.InvariantCulture), batteryValueStyle);
            if (!showBatteryCurrent && GUI.Button(batteryValueRect, GUIContent.none, invisibleButtonStyle) && IsBatteryDepleted(electrical))
            {
                electricalStore?.SetBatteryChargePercent(100f);
                Debug.Log("[TiltDrive][InstructorTelemetry] Bateria recargada desde la UI del instructor.");
            }

            Rect unitRect = new Rect(gearBatteryRect.x + 146f, gearBatteryRect.y + 116f, 72f, 22f);
            GUI.Label(unitRect, batteryUnit, batteryUnitStyle);
        }

        private void UpdateLooseClutchShiftWarning()
        {
            TransmissionState transmission = transmissionStore != null ? transmissionStore.Current : null;
            bool isLooseClutchShift = transmission != null &&
                transmission.hasMisuseWarning &&
                transmission.lastMisuseCode == "SHIFT_WITH_CLUTCH_RELEASED";
            string signature = isLooseClutchShift
                ? $"{transmission.lastMisuseCode}:{transmission.simulationTick}:{transmission.accumulatedDamagePercent:0.000}:{transmission.lastUpdateTime:0.000}"
                : string.Empty;

            if (isLooseClutchShift && signature != lastTransmissionMisuseSignature)
            {
                looseClutchWarningUntil = Time.time + looseClutchWarningHoldSeconds;
            }

            lastTransmissionMisuseSignature = signature;
        }

        private bool IsLooseClutchShiftWarningActive()
        {
            UpdateLooseClutchShiftWarning();
            return Time.time <= looseClutchWarningUntil;
        }

        private bool IsBatteryWarning(VehicleElectricalState electrical)
        {
            return electrical != null && (electrical.lowVoltageWarning || electrical.criticalVoltageWarning || electrical.systemVoltage <= 11.8f);
        }

        private static bool IsBatteryDepleted(VehicleElectricalState electrical)
        {
            return electrical != null && (electrical.batteryChargePercent <= 0.1f || electrical.systemVoltage <= 0.1f);
        }

        private static bool IsForwardGear(int gear)
        {
            return gear > 0;
        }

        private static string FormatGear(int gear)
        {
            if (gear == 0) return "N";
            if (gear < 0) return "R";
            return gear.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetForwardGearSuffix(int gear)
        {
            switch (gear)
            {
                case 1: return "ra";
                case 2: return "da";
                case 3: return "ra";
                default: return "ta";
            }
        }

        private float GetCurrentDisplaySpeed(VehicleOutputState vehicle, VehicleSpeedUnit unit)
        {
            if (vehicle == null)
            {
                return 0f;
            }

            if (unit == VehicleSpeedUnit.MilesPerHour)
            {
                return Mathf.Abs(vehicle.finalSpeedMPH);
            }

            return Mathf.Abs(vehicle.finalSpeedKMH);
        }

        private bool IsSpeedLimitExceeded(float speed)
        {
            return selectedSpeedLimit > 0 && speed > selectedSpeedLimit;
        }

        private bool IsSpeedLimitCriticalExceeded(float speed)
        {
            if (selectedSpeedLimit <= 0)
            {
                return false;
            }

            return speed > selectedSpeedLimit * (1f + speedLimitTolerance);
        }

        private Color ResolveSpeedValueColor(bool limitExceeded, bool criticalLimitExceeded)
        {
            if (!limitExceeded)
            {
                return speedTextColor;
            }

            if (!criticalLimitExceeded)
            {
                return speedLimitAlertColor;
            }

            Color transparentCritical = speedLimitCriticalColor;
            transparentCritical.a = 0f;
            float phase = Mathf.Repeat(Time.time, 0.8f);
            return phase < 0.3f ? transparentCritical : speedLimitCriticalColor;
        }

        private void DrawSpeedLimitButton(Rect buttonRect, int limit, string unitLabel)
        {
            bool selected = selectedSpeedLimit == limit;
            Rect signRect = CenterRect(buttonRect, SpeedLimitSignSize, SpeedLimitSignSize);
            Texture2D signTexture = selected
                ? (speedLimitSelectedTexture != null ? speedLimitSelectedTexture : speedLimitSelectedFallbackTexture)
                : (speedLimitDeselectedTexture != null ? speedLimitDeselectedTexture : speedLimitDefaultFallbackTexture);
            GUI.DrawTexture(signRect, signTexture, ScaleMode.StretchToFill, true);

            if (limit <= 0)
            {
                GUI.Label(new Rect(signRect.x, signRect.y + 16f, signRect.width, 16f), T("no_limit_top"), speedLimitNoLimitTopStyle);
                GUI.Label(new Rect(signRect.x, signRect.y + 31f, signRect.width, 13f), T("no_limit_bottom"), speedLimitNoLimitBottomStyle);
            }
            else
            {
                GUI.Label(new Rect(signRect.x, signRect.y + 12f, signRect.width, 22f), limit.ToString(CultureInfo.InvariantCulture), speedLimitNumberStyle);
                GUI.Label(new Rect(signRect.x, signRect.y + 34f, signRect.width, 12f), unitLabel, speedLimitUnitStyle);
            }

            if (GUI.Button(buttonRect, GUIContent.none, invisibleButtonStyle))
            {
                selectedSpeedLimit = limit;
                wasSpeedLimitExceeded = false;
            }
        }

        private static string GetSpeedUnitLabel(VehicleSpeedUnit unit)
        {
            return unit == VehicleSpeedUnit.MilesPerHour ? "MPH" : "KM/H";
        }

        private int[] GetSpeedLimitOptions(VehicleSpeedUnit unit)
        {
            return unit == VehicleSpeedUnit.MilesPerHour ? mileSpeedLimits : kilometerSpeedLimits;
        }

        private void ToggleSpeedDisplayUnit()
        {
            speedDisplayUnit = speedDisplayUnit == VehicleSpeedUnit.MilesPerHour
                ? VehicleSpeedUnit.KilometersPerHour
                : VehicleSpeedUnit.MilesPerHour;
            selectedSpeedLimit = 0;
            wasSpeedLimitExceeded = false;
        }

        private ScaleMode ResolveScaleMode()
        {
            switch (scaleMode)
            {
                case BackgroundScaleMode.Fit:
                    return ScaleMode.ScaleToFit;
                case BackgroundScaleMode.Fill:
                    return ScaleMode.ScaleAndCrop;
                default:
                    return ScaleMode.StretchToFill;
            }
        }

        private static void DrawColor(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureAssetReferences()
        {
#if UNITY_EDITOR
            if (backgroundTexture == null)
            {
                backgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultBackgroundAssetPath);
            }

            if (primaryFont == null)
            {
                primaryFont = AssetDatabase.LoadAssetAtPath<Font>(DefaultPrimaryFontPath);
            }

            if (companionFont == null)
            {
                companionFont = AssetDatabase.LoadAssetAtPath<Font>(DefaultCompanionFontPath);
            }

            if (steeringBackgroundTexture == null)
            {
                steeringBackgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultSteeringBackgroundAssetPath);
            }

            if (steeringSelectorTexture == null)
            {
                steeringSelectorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultSteeringSelectorAssetPath);
            }

            if (speedLimitDeselectedTexture == null)
            {
                speedLimitDeselectedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultSpeedLimitDeselectedAssetPath);
            }

            if (speedLimitSelectedTexture == null)
            {
                speedLimitSelectedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultSpeedLimitSelectedAssetPath);
            }

            if (engineConnectorsTexture == null)
            {
                engineConnectorsTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultEngineConnectorsAssetPath);
            }

            if (engineValueBackgroundTexture == null)
            {
                engineValueBackgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultEngineValueBackgroundAssetPath);
            }
#endif
        }

        private void EnsureTelemetryReferences()
        {
            if (!autoFindTelemetryReferences)
            {
                return;
            }

            inputStore ??= InputStore.Instance != null ? InputStore.Instance : FindFirstObjectByType<InputStore>();
            engineStore ??= EngineStore.Instance != null ? EngineStore.Instance : FindFirstObjectByType<EngineStore>();
            transmissionStore ??= TransmissionStore.Instance != null ? TransmissionStore.Instance : FindFirstObjectByType<TransmissionStore>();
            electricalStore ??= VehicleElectricalStore.Instance != null ? VehicleElectricalStore.Instance : FindFirstObjectByType<VehicleElectricalStore>();
            vehicleOutputStore ??= VehicleOutputStore.Instance != null ? VehicleOutputStore.Instance : FindFirstObjectByType<VehicleOutputStore>();
            configMenu ??= FindFirstObjectByType<RuntimeVehicleConfigMenu>();
        }

        private void EnsureStyles()
        {
            whiteTexture ??= Texture2D.whiteTexture;
            engineButtonTexture ??= engineValueBackgroundTexture != null
                ? engineValueBackgroundTexture
                : MakeRoundedTexture(
                    engineButtonColor,
                    Mathf.RoundToInt(EngineValueWidth),
                    Mathf.RoundToInt(EngineValueHeight),
                    EngineValueRadius);
            engineDotTexture ??= MakeCircleTexture(32);
            speedLimitDefaultFallbackTexture ??= MakeRingTexture(
                SpeedLimitTextureSize,
                SpeedLimitTextureSize * (SpeedLimitBorderWidth / SpeedLimitSignSize),
                speedLimitDefaultBorderColor,
                Color.clear);
            speedLimitSelectedFallbackTexture ??= MakeRingTexture(
                SpeedLimitTextureSize,
                SpeedLimitTextureSize * (SpeedLimitBorderWidth / SpeedLimitSignSize),
                speedLimitSelectedBorderColor,
                speedLimitSelectedFillColor);
            transparentTexture ??= MakeTexture(new Color(0f, 0f, 0f, 0f));

            dateStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 20,
                normal = { textColor = textColor }
            };

            steeringDegreesStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 31,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = textColor }
            };

            steeringLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 13,
                normal = { textColor = textColor }
            };

            steeringSubLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 10,
                normal = { textColor = textColor }
            };

            pedalPercentStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 17,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = pedalTextColor }
            };

            pedalLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 12,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = pedalTextColor }
            };

            engineTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 25,
                normal = { textColor = engineTextColor }
            };

            engineLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 25,
                normal = { textColor = engineMutedTextColor }
            };

            engineUnitStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 10,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = engineMutedTextColor }
            };

            engineValueStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 13,
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(0, 0, 0, 1),
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { background = engineButtonTexture, textColor = engineButtonTextColor }
            };

            engineButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 13,
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(0, 0, 0, 1),
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { background = engineButtonTexture, textColor = engineButtonTextColor },
                hover = { background = engineButtonTexture, textColor = engineButtonTextColor },
                active = { background = engineButtonTexture, textColor = engineButtonTextColor },
                focused = { background = engineButtonTexture, textColor = engineButtonTextColor }
            };

            speedTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 31,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = speedTextColor }
            };

            speedValueStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                font = primaryFont,
                fontSize = 50,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = speedTextColor }
            };

            speedUnitStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 30,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = engineMutedTextColor }
            };

            speedLimitNumberStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 20,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = speedTextColor }
            };

            speedLimitUnitStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 10,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = speedTextColor }
            };

            speedLimitNoLimitTopStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 15,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = speedTextColor }
            };

            speedLimitNoLimitBottomStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 12,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = speedTextColor }
            };

            gearBatteryTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 22,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = gearBatteryTextColor }
            };

            gearValueStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                font = primaryFont,
                fontSize = 46,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = gearBatteryTextColor }
            };

            gearSuffixStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 20,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = gearBatteryMutedTextColor }
            };

            batteryValueStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                font = primaryFont,
                fontSize = 46,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = gearBatteryTextColor }
            };

            batteryUnitStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                font = primaryFont,
                fontSize = 24,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = gearBatteryMutedTextColor }
            };

            handbrakeStateStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = primaryFont,
                fontSize = 14,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = pedalTextColor }
            };

            invisibleButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                normal = { background = transparentTexture, textColor = Color.clear },
                hover = { background = transparentTexture, textColor = Color.clear },
                active = { background = transparentTexture, textColor = Color.clear }
            };

            ApplyTelemetryStyleSizing();
        }

        private void ApplyTelemetryStyleSizing()
        {
            if (engineTitleStyle != null)
            {
                engineTitleStyle.fontSize = telemetryTitleFontSize;
                engineTitleStyle.fontStyle = telemetryTitleFontStyle;
            }

            if (speedTitleStyle != null)
            {
                speedTitleStyle.fontSize = telemetryTitleFontSize;
                speedTitleStyle.fontStyle = telemetryTitleFontStyle;
            }

            if (gearBatteryTitleStyle != null)
            {
                gearBatteryTitleStyle.fontSize = telemetryTitleFontSize;
                gearBatteryTitleStyle.fontStyle = telemetryTitleFontStyle;
            }

            if (speedValueStyle != null)
            {
                speedValueStyle.fontSize = telemetryValueFontSize;
                speedValueStyle.fontStyle = telemetryValueFontStyle;
            }

            if (gearValueStyle != null)
            {
                gearValueStyle.fontSize = telemetryValueFontSize;
                gearValueStyle.fontStyle = telemetryValueFontStyle;
            }

            if (batteryValueStyle != null)
            {
                batteryValueStyle.fontSize = telemetryValueFontSize;
                batteryValueStyle.fontStyle = telemetryValueFontStyle;
            }

            if (engineLabelStyle != null)
            {
                engineLabelStyle.fontSize = telemetrySupplementFontSize;
            }

            if (speedUnitStyle != null)
            {
                speedUnitStyle.fontSize = telemetrySupplementFontSize;
            }

            if (gearSuffixStyle != null)
            {
                gearSuffixStyle.fontSize = telemetrySupplementFontSize;
            }

            if (batteryUnitStyle != null)
            {
                batteryUnitStyle.fontSize = telemetrySupplementFontSize;
            }

            if (speedLimitNumberStyle != null)
            {
                speedLimitNumberStyle.fontSize = speedLimitValueFontSize;
            }

            if (speedLimitUnitStyle != null)
            {
                speedLimitUnitStyle.fontSize = speedLimitUnitFontSize;
            }

            if (speedLimitNoLimitTopStyle != null)
            {
                speedLimitNoLimitTopStyle.fontSize = speedLimitNoLimitTopFontSize;
            }

            if (speedLimitNoLimitBottomStyle != null)
            {
                speedLimitNoLimitBottomStyle.fontSize = speedLimitNoLimitBottomFontSize;
            }

            if (handbrakeStateStyle != null)
            {
                handbrakeStateStyle.fontSize = Mathf.Max(8, telemetrySupplementFontSize - 8);
            }
        }

        private void DrawDot(Vector2 center, float radius, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), engineDotTexture);
            GUI.color = previous;
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeRoundedTexture(Color color, int width, int height, float radius)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            Color clear = new Color(color.r, color.g, color.b, 0f);
            float maxX = width - 1f;
            float maxY = height - 1f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cornerX = x < radius ? radius : x > maxX - radius ? maxX - radius : x;
                    float cornerY = y < radius ? radius : y > maxY - radius ? maxY - radius : y;
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));
                    texture.SetPixel(x, y, distance <= radius ? color : clear);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D MakeCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            float radius = (size - 1f) * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = Vector2.Distance(new Vector2(x, y), center) <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D MakeRingTexture(int size, float borderWidth, Color borderColor, Color fillColor)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            float radius = (size - 1f) * 0.5f;
            float innerRadius = Mathf.Max(0f, radius - borderWidth);
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float outerAlpha = Mathf.Clamp01(radius + 1f - distance);
                    float borderAlpha = Mathf.Clamp01(distance - innerRadius + 1f);

                    if (distance > radius + 1f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                    else if (distance >= innerRadius - 1f)
                    {
                        Color mixedColor = Color.Lerp(fillColor, borderColor, borderAlpha);
                        mixedColor.a *= outerAlpha;
                        texture.SetPixel(x, y, mixedColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, fillColor);
                    }
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
