using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TiltDrive.EngineSystem;
using TiltDrive.TransmissionSystem;
using TiltDrive.VehicleSystem;
using TiltDrive.LightingSystem;
using TiltDrive.ElectricalSystem;
using TiltDrive.CoolingSystem;
using TiltDrive.Simulation;

namespace TiltDrive.UISystem
{
    public class RuntimeVehicleConfigMenu : MonoBehaviour
    {
        private enum MenuLanguage
        {
            Spanish = 0,
            English = 1
        }

        [Header("Referencias")]
        [SerializeField] private EngineStore engineStore;
        [SerializeField] private EngineSimulationRunner engineRunner;
        [SerializeField] private TransmissionStore transmissionStore;
        [SerializeField] private TransmissionSimulationRunner transmissionRunner;
        [SerializeField] private VehicleOutputStore vehicleOutputStore;
        [SerializeField] private VehicleLightsStore lightsStore;
        [SerializeField] private VehicleElectricalStore electricalStore;
        [SerializeField] private RadiatorStore radiatorStore;

        [Header("Menu")]
        [SerializeField] private bool autoFindReferences = true;
        [SerializeField] private bool showMenu = false;
        [SerializeField] private Key toggleKey = Key.M;
        [SerializeField] private Rect windowRect = new Rect(24f, 24f, 760f, 720f);
        [SerializeField] private MenuLanguage language = MenuLanguage.Spanish;
        [SerializeField] private bool fitWindowToScreen = true;
        [SerializeField] [Range(0.45f, 0.95f)] private float windowWidthPercent = 0.70f;
        [SerializeField] [Range(0.70f, 0.98f)] private float windowHeightPercent = 0.92f;
        [SerializeField] [Min(0f)] private float screenMargin = 18f;

        [Header("Estilo")]
        [SerializeField] [Min(520f)] private float minimumWindowWidth = 720f;
        [SerializeField] [Min(420f)] private float minimumWindowHeight = 620f;
        [SerializeField] [Min(16)] private int titleFontSize = 22;
        [SerializeField] [Min(10)] private int bodyFontSize = 15;
        [SerializeField] [Min(20f)] private float fieldHeight = 30f;
        [SerializeField] private Color windowColor = new Color(0.012f, 0.016f, 0.020f, 1f);
        [SerializeField] private Color panelColor = new Color(0.026f, 0.032f, 0.040f, 1f);
        [SerializeField] private Color accentColor = new Color(0.18f, 0.72f, 1f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.075f, 0.105f, 0.135f, 1f);
        [SerializeField] private Color fieldColor = new Color(0.006f, 0.009f, 0.012f, 1f);
        [SerializeField] private Color textColor = new Color(0.96f, 0.98f, 1f, 1f);
        [SerializeField] private Sprite headerSprite;
        [SerializeField] private Texture2D headerTexture;

        private readonly Dictionary<string, string> textCache = new Dictionary<string, string>();
        private readonly List<ConfigTab> tabs = new List<ConfigTab>();
        private Vector2 scroll;
        private int selectedTab = 0;
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle sectionStyle;
        private GUIStyle contentStyle;
        private GUIStyle textFieldStyle;
        private GUIStyle buttonStyle;
        private GUIStyle toolbarStyle;
        private GUIStyle toggleStyle;
        private GUIStyle scrollStyle;
        private Texture2D windowBackground;
        private Texture2D panelBackground;
        private Texture2D buttonBackground;
        private Texture2D fieldBackground;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        private struct ConfigTab
        {
            public string key;
            public string label;
            public object config;
            public Action refresh;
        }

        private void Awake()
        {
            EnsureReferences();
            RebuildTabs();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                showMenu = !showMenu;
                if (showMenu)
                {
                    EnsureReferences();
                    RebuildTabs();
                }
            }
        }

        private void OnGUI()
        {
            if (!showMenu) return;
            EnsureStyles();
            ApplyResponsiveWindowLayout();
            windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, GUIContent.none, windowStyle);
        }

        private void DrawWindow(int windowId)
        {
            DrawHeader();

            if (tabs.Count == 0)
            {
                GUILayout.Label(T("no_configs"), labelStyle);
                if (GUILayout.Button(T("refresh"), buttonStyle, GUILayout.Height(36f)))
                {
                    EnsureReferences();
                    RebuildTabs();
                }

                GUI.DragWindow();
                return;
            }

            string[] labels = new string[tabs.Count];
            for (int i = 0; i < tabs.Count; i++)
            {
                labels[i] = T(tabs[i].key);
            }

            selectedTab = Mathf.Clamp(
                GUILayout.Toolbar(selectedTab, labels, toolbarStyle, GUILayout.Height(36f)),
                0,
                tabs.Count - 1);

            GUILayout.Space(10f);
            GUILayout.BeginVertical(contentStyle);
            scroll = GUILayout.BeginScrollView(scroll, scrollStyle);
            if (IsWorkshopTabSelected())
            {
                DrawWorkshopTab();
            }
            else
            {
                DrawConfigObject(tabs[selectedTab].config, tabs[selectedTab].key);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            if (!IsWorkshopTabSelected() && GUILayout.Button(T("apply"), buttonStyle, GUILayout.Height(38f))) ApplyCurrentTab();
            if (IsWorkshopTabSelected() && GUILayout.Button(T("workshop_repair_all"), buttonStyle, GUILayout.Height(38f))) RepairAllVitalSystems();
            if (GUILayout.Button(T("refresh_refs"), buttonStyle, GUILayout.Height(38f)))
            {
                EnsureReferences();
                RebuildTabs();
            }
            if (GUILayout.Button(T("close"), buttonStyle, GUILayout.Height(38f))) showMenu = false;
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void EnsureReferences()
        {
            if (!autoFindReferences) return;

            engineStore ??= EngineStore.Instance != null ? EngineStore.Instance : FindFirstObjectByType<EngineStore>();
            engineRunner ??= FindFirstObjectByType<EngineSimulationRunner>();
            transmissionStore ??= TransmissionStore.Instance != null ? TransmissionStore.Instance : FindFirstObjectByType<TransmissionStore>();
            transmissionRunner ??= FindFirstObjectByType<TransmissionSimulationRunner>();
            vehicleOutputStore ??= VehicleOutputStore.Instance != null ? VehicleOutputStore.Instance : FindFirstObjectByType<VehicleOutputStore>();
            lightsStore ??= VehicleLightsStore.Instance != null ? VehicleLightsStore.Instance : FindFirstObjectByType<VehicleLightsStore>();
            electricalStore ??= VehicleElectricalStore.Instance != null ? VehicleElectricalStore.Instance : FindFirstObjectByType<VehicleElectricalStore>();
            radiatorStore ??= RadiatorStore.Instance != null ? RadiatorStore.Instance : FindFirstObjectByType<RadiatorStore>();
        }

        private void RebuildTabs()
        {
            tabs.Clear();

            AddTab("tab_engine", engineStore != null ? engineStore.Config : null, () => engineStore?.ForceRefresh());
            AddTab("tab_radiator", radiatorStore != null ? radiatorStore.Config : null, () => radiatorStore?.ForceRefresh());
            AddTab("tab_launch", engineRunner != null ? engineRunner.LaunchDiagnosticsConfig : null, null);
            AddTab("tab_transmission", transmissionStore != null ? transmissionStore.Config : null, () => transmissionStore?.ForceRefresh());
            AddTab("tab_damage", transmissionRunner != null ? transmissionRunner.DamagePenaltyConfig : null, null);
            AddTab("tab_vehicle", vehicleOutputStore != null ? vehicleOutputStore.Config : null, () => vehicleOutputStore?.ForceRefresh());
            AddTab("tab_lights", lightsStore != null ? lightsStore.Config : null, () => lightsStore?.ForceRefresh());
            AddTab("tab_electrical", electricalStore != null ? electricalStore.Config : null, () => electricalStore?.ForceRefresh());
            AddWorkshopTab();

            selectedTab = Mathf.Clamp(selectedTab, 0, Mathf.Max(0, tabs.Count - 1));
        }

        private void AddTab(string key, object config, Action refresh)
        {
            if (config == null) return;
            tabs.Add(new ConfigTab { key = key, label = T(key), config = config, refresh = refresh });
        }

        private void AddWorkshopTab()
        {
            tabs.Add(new ConfigTab { key = "tab_workshop", label = T("tab_workshop"), config = null, refresh = ForceWorkshopRefresh });
        }

        private bool IsWorkshopTabSelected()
        {
            return tabs.Count > 0 &&
                selectedTab >= 0 &&
                selectedTab < tabs.Count &&
                tabs[selectedTab].key == "tab_workshop";
        }

        private void DrawConfigObject(object config, string tabKey)
        {
            if (config == null)
            {
                GUILayout.Label(T("missing_config"), labelStyle);
                return;
            }

            GUILayout.Label(T(tabKey), sectionStyle, GUILayout.Height(34f));
            FieldInfo[] fields = config.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                DrawField(config, field, $"{tabKey}.{field.Name}");
            }
        }

        private void DrawField(object target, FieldInfo field, string key)
        {
            Type fieldType = field.FieldType;
            object value = field.GetValue(target);
            string label = FieldLabel(field.Name);

            if (fieldType == typeof(float))
            {
                float next = DrawFloatField(label, (float)value, key);
                if (!Mathf.Approximately((float)value, next))
                {
                    field.SetValue(target, next);
                    ApplyCurrentTab();
                }
                return;
            }

            if (fieldType == typeof(int))
            {
                int next = DrawIntField(label, (int)value, key);
                if ((int)value != next)
                {
                    field.SetValue(target, next);
                    ApplyCurrentTab();
                }
                return;
            }

            if (fieldType == typeof(bool))
            {
                bool next = GUILayout.Toggle((bool)value, label, toggleStyle, GUILayout.Height(fieldHeight));
                if ((bool)value != next)
                {
                    field.SetValue(target, next);
                    ApplyCurrentTab();
                }
                return;
            }

            if (fieldType == typeof(string))
            {
                DrawStringField(target, field, key, value as string ?? string.Empty, label);
                return;
            }

            if (fieldType.IsEnum)
            {
                DrawEnumField(target, field, value, label);
                return;
            }

            if (fieldType == typeof(float[]))
            {
                DrawFloatArrayField(target, field, key, value as float[], label);
            }
        }

        private float DrawFloatField(string label, float value, string key)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(300f), GUILayout.Height(fieldHeight));
            string text = GetTextValue(key, value.ToString("0.###", CultureInfo.InvariantCulture));
            GUI.SetNextControlName(key);
            text = GUILayout.TextField(text, textFieldStyle, GUILayout.Height(fieldHeight));
            textCache[key] = text;
            GUILayout.EndHorizontal();

            return TryParseFloat(text, out float parsed) ? parsed : value;
        }

        private int DrawIntField(string label, int value, string key)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(300f), GUILayout.Height(fieldHeight));
            string text = GetTextValue(key, value.ToString(CultureInfo.InvariantCulture));
            GUI.SetNextControlName(key);
            text = GUILayout.TextField(text, textFieldStyle, GUILayout.Height(fieldHeight));
            textCache[key] = text;
            GUILayout.EndHorizontal();

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) return parsed;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed)) return parsed;
            return value;
        }

        private void DrawStringField(object target, FieldInfo field, string key, string value, string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(300f), GUILayout.Height(fieldHeight));
            string text = GetTextValue(key, value);
            GUI.SetNextControlName(key);
            text = GUILayout.TextField(text, textFieldStyle, GUILayout.Height(fieldHeight));
            textCache[key] = text;
            GUILayout.EndHorizontal();

            if (text != value)
            {
                field.SetValue(target, text);
                ApplyCurrentTab();
            }
        }

        private void DrawEnumField(object target, FieldInfo field, object value, string label)
        {
            string[] names = Enum.GetNames(field.FieldType);
            string[] displayNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                displayNames[i] = EnumLabel(names[i]);
            }

            int currentIndex = Mathf.Max(0, Array.IndexOf(names, value.ToString()));

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(300f), GUILayout.Height(fieldHeight));
            int nextIndex = GUILayout.SelectionGrid(
                currentIndex,
                displayNames,
                Mathf.Min(3, names.Length),
                buttonStyle,
                GUILayout.Height(fieldHeight));
            GUILayout.EndHorizontal();

            if (nextIndex != currentIndex && nextIndex >= 0 && nextIndex < names.Length)
            {
                field.SetValue(target, Enum.Parse(field.FieldType, names[nextIndex]));
                ApplyCurrentTab();
            }
        }

        private void DrawFloatArrayField(object target, FieldInfo field, string key, float[] values, string label)
        {
            if (values == null)
            {
                values = Array.Empty<float>();
                field.SetValue(target, values);
            }

            GUILayout.Label(label, sectionStyle, GUILayout.Height(30f));
            for (int i = 0; i < values.Length; i++)
            {
                float next = DrawFloatField(ArrayItemLabel(key, i), values[i], $"{key}.{i}");
                if (!Mathf.Approximately(values[i], next))
                {
                    values[i] = Mathf.Max(0.01f, next);
                    field.SetValue(target, values);
                    ApplyCurrentTab();
                }
            }
        }

        private string GetTextValue(string key, string fallback)
        {
            if (!textCache.TryGetValue(key, out string text))
            {
                text = fallback;
                textCache[key] = text;
            }

            return text;
        }

        private static bool TryParseFloat(string text, out float value)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private void ApplyCurrentTab()
        {
            if (tabs.Count == 0) return;

            object config = tabs[selectedTab].config;
            if (config == null)
            {
                tabs[selectedTab].refresh?.Invoke();
                return;
            }

            MethodInfo clamp = config.GetType().GetMethod("ClampValues", BindingFlags.Instance | BindingFlags.Public);
            clamp?.Invoke(config, null);
            tabs[selectedTab].refresh?.Invoke();
        }

        private void DrawWorkshopTab()
        {
            GUILayout.Label(T("tab_workshop"), sectionStyle, GUILayout.Height(34f));
            GUILayout.Label(T("workshop_hint"), labelStyle, GUILayout.Height(28f));
            GUILayout.Space(8f);

            DrawEngineWorkshop();
            DrawRadiatorWorkshop();
            DrawTransmissionWorkshop();
            DrawBrakeWorkshop();
            DrawElectricalWorkshop();
            DrawLightsWorkshop();
        }

        private void DrawEngineWorkshop()
        {
            GUILayout.Label(T("workshop_engine"), sectionStyle, GUILayout.Height(30f));

            if (engineStore == null || engineStore.Current == null)
            {
                GUILayout.Label(T("workshop_missing_engine"), labelStyle, GUILayout.Height(fieldHeight));
                return;
            }

            EngineState state = engineStore.Current;
            bool changed = false;

            float health = DrawLiveFloatField(T("workshop_health_percent"), state.componentHealthPercent, "workshop.engine.health");
            if (!Mathf.Approximately(health, state.componentHealthPercent))
            {
                state.componentHealthPercent = Mathf.Clamp(health, 0f, 100f);
                changed = true;
            }

            float damage = DrawLiveFloatField(T("workshop_damage_percent"), state.accumulatedDamagePercent, "workshop.engine.damage");
            if (!Mathf.Approximately(damage, state.accumulatedDamagePercent))
            {
                state.accumulatedDamagePercent = Mathf.Max(0f, damage);
                changed = true;
            }

            float rpm = DrawLiveFloatField(T("workshop_engine_rpm"), state.currentRPM, "workshop.engine.rpm");
            if (!Mathf.Approximately(rpm, state.currentRPM))
            {
                state.currentRPM = Mathf.Max(0f, rpm);
                state.targetRPM = Mathf.Max(0f, rpm);
                changed = true;
            }

            float temperature = DrawLiveFloatField(T("workshop_engine_temperature"), state.engineTemperatureC, "workshop.engine.temperature");
            if (!Mathf.Approximately(temperature, state.engineTemperatureC))
            {
                state.engineTemperatureC = Mathf.Max(0f, temperature);
                changed = true;
            }

            float thermalEfficiency = DrawLiveFloatField(T("workshop_engine_thermal_efficiency"), state.thermalEfficiency, "workshop.engine.thermalEfficiency");
            if (!Mathf.Approximately(thermalEfficiency, state.thermalEfficiency))
            {
                state.thermalEfficiency = Mathf.Clamp01(thermalEfficiency);
                changed = true;
            }

            bool engineOn = DrawWorkshopToggle(T("workshop_engine_on"), state.engineOn);
            if (engineOn != state.engineOn)
            {
                state.engineOn = engineOn;
                state.engineStarting = false;
                state.engineShuttingDown = false;
                if (engineOn && state.currentRPM < state.idleRPM)
                {
                    state.currentRPM = state.idleRPM;
                    state.targetRPM = state.idleRPM;
                    textCache.Remove("workshop.engine.rpm");
                }
                changed = true;
            }

            bool stalled = DrawWorkshopToggle(T("workshop_engine_stalled"), state.engineStalled);
            if (stalled != state.engineStalled)
            {
                state.engineStalled = stalled;
                changed = true;
            }

            bool overheated = DrawWorkshopToggle(T("workshop_engine_overheated"), state.engineOverheated);
            if (overheated != state.engineOverheated)
            {
                state.engineOverheated = overheated;
                state.engineTemperatureWarning = overheated || state.engineTemperatureWarning;
                changed = true;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("workshop_repair_engine"), buttonStyle, GUILayout.Height(34f)))
            {
                state.componentHealthPercent = 100f;
                state.accumulatedDamagePercent = 0f;
                state.engineStalled = false;
                state.starterOveruseWarning = false;
                state.hasLaunchWarning = false;
                state.hasLaunchMisuse = false;
                state.launchStallRisk = false;
                state.lastLaunchWarningCode = string.Empty;
                state.lastLaunchWarningMessage = string.Empty;
                state.lastStarterWarningCode = string.Empty;
                state.lastStarterWarningMessage = string.Empty;
                CoolEngineState(state);
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_damage_25"), buttonStyle, GUILayout.Height(34f)))
            {
                ApplyEngineDamage(25f);
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_reset_engine"), buttonStyle, GUILayout.Height(34f)))
            {
                engineStore.ResetRuntime();
                textCache.Clear();
                return;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("workshop_cool_engine"), buttonStyle, GUILayout.Height(34f)))
            {
                CoolEngineState(state);
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_heat_engine"), buttonStyle, GUILayout.Height(34f)))
            {
                HeatEngineState(state);
                changed = true;
                textCache.Clear();
            }
            GUILayout.EndHorizontal();

            if (changed)
            {
                engineStore.ApplyStateSnapshot(state);
            }

            GUILayout.Space(10f);
        }

        private void DrawRadiatorWorkshop()
        {
            GUILayout.Label(T("workshop_radiator"), sectionStyle, GUILayout.Height(30f));

            if (radiatorStore == null || radiatorStore.Current == null)
            {
                GUILayout.Label(T("workshop_missing_radiator"), labelStyle, GUILayout.Height(fieldHeight));
                return;
            }

            RadiatorState state = radiatorStore.Current;
            bool changed = false;

            float health = DrawLiveFloatField(T("workshop_radiator_health"), state.radiatorHealthPercent, "workshop.radiator.health");
            if (!Mathf.Approximately(health, state.radiatorHealthPercent))
            {
                state.radiatorHealthPercent = Mathf.Clamp(health, 0f, 100f);
                changed = true;
            }

            float damage = DrawLiveFloatField(T("workshop_damage_percent"), state.accumulatedDamagePercent, "workshop.radiator.damage");
            if (!Mathf.Approximately(damage, state.accumulatedDamagePercent))
            {
                state.accumulatedDamagePercent = Mathf.Max(0f, damage);
                changed = true;
            }

            float coolantLevel = DrawLiveFloatField(T("workshop_radiator_coolant_level"), state.coolantLevelPercent, "workshop.radiator.coolantLevel");
            if (!Mathf.Approximately(coolantLevel, state.coolantLevelPercent))
            {
                state.coolantLevelPercent = Mathf.Clamp(coolantLevel, 0f, 100f);
                changed = true;
            }

            float coolantTemperature = DrawLiveFloatField(T("workshop_radiator_temperature"), state.coolantTemperatureC, "workshop.radiator.temperature");
            if (!Mathf.Approximately(coolantTemperature, state.coolantTemperatureC))
            {
                state.coolantTemperatureC = Mathf.Max(0f, coolantTemperature);
                changed = true;
            }

            float pressure = DrawLiveFloatField(T("workshop_radiator_pressure"), state.systemPressureKpa, "workshop.radiator.pressure");
            if (!Mathf.Approximately(pressure, state.systemPressureKpa))
            {
                state.systemPressureKpa = Mathf.Max(0f, pressure);
                changed = true;
            }

            float cooling = DrawLiveFloatField(T("workshop_radiator_cooling_efficiency"), state.coolingEfficiency, "workshop.radiator.cooling");
            if (!Mathf.Approximately(cooling, state.coolingEfficiency))
            {
                state.coolingEfficiency = Mathf.Clamp(cooling, 0f, 1.5f);
                changed = true;
            }

            bool perforated = DrawWorkshopToggle(T("workshop_radiator_perforated"), state.hasPerforation);
            if (perforated != state.hasPerforation)
            {
                state.hasPerforation = perforated;
                RadiatorConfig config = radiatorStore != null ? radiatorStore.Config : null;
                state.leakRatePercentPerSecond = perforated
                    ? Mathf.Max(state.leakRatePercentPerSecond, config != null ? config.perforationLeakRatePercentPerSecond : 3.5f)
                    : 0f;
                changed = true;
            }

            bool pressureDamage = DrawWorkshopToggle(T("workshop_radiator_pressure_damage"), state.pressureDamageActive);
            if (pressureDamage != state.pressureDamageActive)
            {
                state.pressureDamageActive = pressureDamage;
                changed = true;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("workshop_repair_radiator"), buttonStyle, GUILayout.Height(34f)))
            {
                radiatorStore.Repair();
                textCache.Clear();
                return;
            }
            if (GUILayout.Button(T("workshop_damage_25"), buttonStyle, GUILayout.Height(34f)))
            {
                radiatorStore.ApplyDamagePercent(25f, "Runtime workshop damage");
                textCache.Clear();
                return;
            }
            if (GUILayout.Button(T("workshop_perforate_radiator"), buttonStyle, GUILayout.Height(34f)))
            {
                radiatorStore.ApplyDamagePercent(35f, "Runtime workshop perforation", true);
                textCache.Clear();
                return;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("workshop_refill_coolant"), buttonStyle, GUILayout.Height(34f)))
            {
                radiatorStore.RefillCoolant(100f);
                textCache.Clear();
                return;
            }
            if (GUILayout.Button(T("workshop_drain_coolant"), buttonStyle, GUILayout.Height(34f)))
            {
                state.coolantLevelPercent = 0f;
                state.lowCoolant = true;
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_reset_radiator"), buttonStyle, GUILayout.Height(34f)))
            {
                radiatorStore.ResetRuntime();
                textCache.Clear();
                return;
            }
            GUILayout.EndHorizontal();

            if (changed)
            {
                radiatorStore.ApplyStateSnapshot(state);
            }

            GUILayout.Space(10f);
        }

        private void DrawTransmissionWorkshop()
        {
            GUILayout.Label(T("workshop_transmission"), sectionStyle, GUILayout.Height(30f));

            if (transmissionStore == null || transmissionStore.Current == null)
            {
                GUILayout.Label(T("workshop_missing_transmission"), labelStyle, GUILayout.Height(fieldHeight));
                return;
            }

            TransmissionState state = transmissionStore.Current;
            bool changed = false;

            float health = DrawLiveFloatField(T("workshop_health_percent"), state.componentHealthPercent, "workshop.transmission.health");
            if (!Mathf.Approximately(health, state.componentHealthPercent))
            {
                state.componentHealthPercent = Mathf.Clamp(health, 0f, 100f);
                changed = true;
            }

            float damage = DrawLiveFloatField(T("workshop_damage_percent"), state.accumulatedDamagePercent, "workshop.transmission.damage");
            if (!Mathf.Approximately(damage, state.accumulatedDamagePercent))
            {
                state.accumulatedDamagePercent = Mathf.Max(0f, damage);
                changed = true;
            }

            int gear = DrawLiveIntField(T("workshop_current_gear"), state.currentGear, "workshop.transmission.gear");
            if (gear != state.currentGear)
            {
                state.currentGear = gear;
                state.requestedGear = gear;
                changed = true;
            }

            float clutch = DrawLiveFloatField(T("workshop_clutch_engagement"), state.clutchEngagement, "workshop.transmission.clutch");
            if (!Mathf.Approximately(clutch, state.clutchEngagement))
            {
                state.clutchEngagement = Mathf.Clamp01(clutch);
                state.clutchInput = state.clutchEngagement;
                state.clutchDisengaged = state.clutchEngagement <= 0.05f;
                changed = true;
            }

            bool shiftAllowed = DrawWorkshopToggle(T("workshop_shift_allowed"), state.shiftAllowed);
            if (shiftAllowed != state.shiftAllowed)
            {
                state.shiftAllowed = shiftAllowed;
                changed = true;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("workshop_repair_transmission"), buttonStyle, GUILayout.Height(34f)))
            {
                state.componentHealthPercent = 100f;
                state.accumulatedDamagePercent = 0f;
                state.hasMisuseWarning = false;
                state.hasTransmissionWarning = false;
                state.lastMisuseCode = string.Empty;
                state.lastMisuseMessage = string.Empty;
                state.lastMisuseSeverity = 0f;
                state.shiftAllowed = true;
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_damage_25"), buttonStyle, GUILayout.Height(34f)))
            {
                state.componentHealthPercent = Mathf.Clamp(state.componentHealthPercent - 25f, 0f, 100f);
                state.accumulatedDamagePercent += 25f;
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_reset_transmission"), buttonStyle, GUILayout.Height(34f)))
            {
                transmissionStore.ResetRuntime();
                textCache.Clear();
                return;
            }
            GUILayout.EndHorizontal();

            if (changed)
            {
                transmissionStore.ApplyStateSnapshot(state);
            }

            GUILayout.Space(10f);
        }

        private void DrawElectricalWorkshop()
        {
            GUILayout.Label(T("workshop_electrical"), sectionStyle, GUILayout.Height(30f));

            if (electricalStore == null || electricalStore.Current == null)
            {
                GUILayout.Label(T("workshop_missing_electrical"), labelStyle, GUILayout.Height(fieldHeight));
                return;
            }

            VehicleElectricalState state = electricalStore.Current;
            bool changed = false;

            float charge = DrawLiveFloatField(T("workshop_battery_charge"), state.batteryChargePercent, "workshop.electrical.charge");
            if (!Mathf.Approximately(charge, state.batteryChargePercent))
            {
                electricalStore.SetBatteryChargePercent(charge);
                textCache.Remove("workshop.electrical.voltage");
            }

            float voltage = DrawLiveFloatField(T("workshop_battery_voltage"), state.batteryVoltage, "workshop.electrical.voltage");
            if (!Mathf.Approximately(voltage, state.batteryVoltage))
            {
                state.batteryVoltage = Mathf.Max(0f, voltage);
                state.systemVoltage = Mathf.Max(0f, voltage);
                changed = true;
            }

            bool electricalAvailable = DrawWorkshopToggle(T("workshop_electrical_available"), state.electricalAvailable);
            if (electricalAvailable != state.electricalAvailable)
            {
                state.electricalAvailable = electricalAvailable;
                state.ignitionAvailable = electricalAvailable;
                state.lightsPowerAvailable = electricalAvailable;
                state.lightsBrightnessFactor = electricalAvailable ? Mathf.Max(0.2f, state.lightsBrightnessFactor) : 0f;
                changed = true;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("workshop_charge_full"), buttonStyle, GUILayout.Height(34f)))
            {
                electricalStore.SetBatteryChargePercent(100f);
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_drain_battery"), buttonStyle, GUILayout.Height(34f)))
            {
                electricalStore.SetBatteryChargePercent(0f);
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_reset_electrical"), buttonStyle, GUILayout.Height(34f)))
            {
                electricalStore.ResetRuntime();
                textCache.Clear();
                return;
            }
            GUILayout.EndHorizontal();

            if (changed)
            {
                electricalStore.ApplyStateSnapshot(state);
            }

            GUILayout.Space(10f);
        }

        private void DrawBrakeWorkshop()
        {
            GUILayout.Label(T("workshop_brakes"), sectionStyle, GUILayout.Height(30f));

            if (vehicleOutputStore == null || vehicleOutputStore.Current == null)
            {
                GUILayout.Label(T("workshop_missing_vehicle"), labelStyle, GUILayout.Height(fieldHeight));
                return;
            }

            VehicleOutputState state = vehicleOutputStore.Current;
            bool changed = false;

            float temperature = DrawLiveFloatField(T("workshop_brake_temperature"), state.brakeTemperatureC, "workshop.brakes.temperature");
            if (!Mathf.Approximately(temperature, state.brakeTemperatureC))
            {
                state.brakeTemperatureC = Mathf.Max(0f, temperature);
                changed = true;
            }

            float efficiency = DrawLiveFloatField(T("workshop_brake_efficiency"), state.brakeThermalEfficiency, "workshop.brakes.efficiency");
            if (!Mathf.Approximately(efficiency, state.brakeThermalEfficiency))
            {
                state.brakeThermalEfficiency = Mathf.Clamp01(efficiency);
                changed = true;
            }

            bool fadeActive = DrawWorkshopToggle(T("workshop_brake_fade"), state.brakeFadeActive);
            if (fadeActive != state.brakeFadeActive)
            {
                state.brakeFadeActive = fadeActive;
                changed = true;
            }

            bool overheated = DrawWorkshopToggle(T("workshop_brake_overheated"), state.brakeOverheated);
            if (overheated != state.brakeOverheated)
            {
                state.brakeOverheated = overheated;
                changed = true;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("workshop_cool_brakes"), buttonStyle, GUILayout.Height(34f)))
            {
                CoolBrakeState(state);
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_heat_brakes"), buttonStyle, GUILayout.Height(34f)))
            {
                HeatBrakeState(state);
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_reset_vehicle"), buttonStyle, GUILayout.Height(34f)))
            {
                vehicleOutputStore.ResetRuntime();
                textCache.Clear();
                return;
            }
            GUILayout.EndHorizontal();

            if (changed)
            {
                vehicleOutputStore.ApplyStateSnapshot(state);
            }

            GUILayout.Space(10f);
        }

        private void DrawLightsWorkshop()
        {
            GUILayout.Label(T("workshop_lights"), sectionStyle, GUILayout.Height(30f));

            if (lightsStore == null || lightsStore.Current == null)
            {
                GUILayout.Label(T("workshop_missing_lights"), labelStyle, GUILayout.Height(fieldHeight));
                return;
            }

            VehicleLightsState state = lightsStore.Current;
            bool changed = false;

            float health = DrawLiveFloatField(T("workshop_lights_health"), state.lightingSystemHealthPercent, "workshop.lights.health");
            if (!Mathf.Approximately(health, state.lightingSystemHealthPercent))
            {
                state.lightingSystemHealthPercent = Mathf.Clamp(health, 0f, 100f);
                changed = true;
            }

            changed |= DrawLightFunctionalToggle(T("workshop_left_signal"), ref state.leftTurnSignalFunctional);
            changed |= DrawLightFunctionalToggle(T("workshop_right_signal"), ref state.rightTurnSignalFunctional);
            changed |= DrawLightFunctionalToggle(T("workshop_low_beams"), ref state.lowBeamsFunctional);
            changed |= DrawLightFunctionalToggle(T("workshop_high_beams"), ref state.highBeamsFunctional);
            changed |= DrawLightFunctionalToggle(T("workshop_fog_lights"), ref state.fogLightsFunctional);
            changed |= DrawLightFunctionalToggle(T("workshop_brake_lights"), ref state.brakeLightsFunctional);
            changed |= DrawLightFunctionalToggle(T("workshop_reverse_lights"), ref state.reverseLightsFunctional);
            changed |= DrawLightFunctionalToggle(T("workshop_other_lights"), ref state.otherLightsFunctional);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(T("workshop_repair_lights"), buttonStyle, GUILayout.Height(34f)))
            {
                RepairLightsState(state);
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_fail_all_lights"), buttonStyle, GUILayout.Height(34f)))
            {
                state.lightingSystemHealthPercent = 0f;
                state.leftTurnSignalFunctional = false;
                state.rightTurnSignalFunctional = false;
                state.lowBeamsFunctional = false;
                state.highBeamsFunctional = false;
                state.fogLightsFunctional = false;
                state.brakeLightsFunctional = false;
                state.reverseLightsFunctional = false;
                state.otherLightsFunctional = false;
                changed = true;
                textCache.Clear();
            }
            if (GUILayout.Button(T("workshop_reset_lights"), buttonStyle, GUILayout.Height(34f)))
            {
                lightsStore.ResetRuntime();
                textCache.Clear();
                return;
            }
            GUILayout.EndHorizontal();

            if (changed)
            {
                lightsStore.ApplyStateSnapshot(state);
            }

            GUILayout.Space(10f);
        }

        private bool DrawWorkshopToggle(string label, bool value)
        {
            return GUILayout.Toggle(value, label, toggleStyle, GUILayout.Height(fieldHeight));
        }

        private float DrawLiveFloatField(string label, float value, string key)
        {
            if (GUI.GetNameOfFocusedControl() != key)
            {
                textCache[key] = value.ToString("0.###", CultureInfo.InvariantCulture);
            }

            return DrawFloatField(label, value, key);
        }

        private int DrawLiveIntField(string label, int value, string key)
        {
            if (GUI.GetNameOfFocusedControl() != key)
            {
                textCache[key] = value.ToString(CultureInfo.InvariantCulture);
            }

            return DrawIntField(label, value, key);
        }

        private bool DrawLightFunctionalToggle(string label, ref bool value)
        {
            bool next = DrawWorkshopToggle(label, value);
            if (next == value)
            {
                return false;
            }

            value = next;
            return true;
        }

        private void ApplyEngineDamage(float damagePercent)
        {
            if (engineStore == null || engineStore.Current == null) return;
            engineStore.ApplyDamagePercent(damagePercent, "Runtime workshop damage");
        }

        private void CoolEngineState(EngineState state)
        {
            EngineConfig config = engineStore != null ? engineStore.Config : null;
            state.engineTemperatureC = config != null ? config.engineInitialTemperatureC : 45f;
            state.thermalEfficiency = 1f;
            state.engineTemperatureWarning = false;
            state.engineOverheated = false;
            state.engineThermalDerateActive = false;
            state.lastTemperatureWarningCode = string.Empty;
            state.lastTemperatureWarningMessage = string.Empty;
            state.lastTemperatureSeverity = 0f;
        }

        private void HeatEngineState(EngineState state)
        {
            EngineConfig config = engineStore != null ? engineStore.Config : null;
            float criticalTemperature = config != null ? config.engineCriticalTemperatureC : 135f;
            float minEfficiency = config != null ? config.engineMinThermalEfficiency : 0.52f;

            state.engineTemperatureC = criticalTemperature;
            state.thermalEfficiency = Mathf.Clamp01(minEfficiency);
            state.engineTemperatureWarning = true;
            state.engineOverheated = true;
            state.engineThermalDerateActive = true;
            state.lastTemperatureWarningCode = "ENGINE_OVERHEAT_CRITICAL";
            state.lastTemperatureWarningMessage = "Engine temperature is critical; torque output is severely reduced.";
            state.lastTemperatureSeverity = 1f;
        }

        private void RepairLightsState(VehicleLightsState state)
        {
            state.lightingSystemHealthPercent = 100f;
            state.leftTurnSignalFunctional = true;
            state.rightTurnSignalFunctional = true;
            state.lowBeamsFunctional = true;
            state.highBeamsFunctional = true;
            state.fogLightsFunctional = true;
            state.brakeLightsFunctional = true;
            state.reverseLightsFunctional = true;
            state.otherLightsFunctional = true;
        }

        private void CoolBrakeState(VehicleOutputState state)
        {
            VehicleOutputConfig config = vehicleOutputStore != null ? vehicleOutputStore.Config : null;
            state.brakeTemperatureC = config != null ? config.brakeInitialTemperatureC : 32f;
            state.brakeThermalEfficiency = 1f;
            state.brakeOverheated = false;
            state.brakeFadeActive = false;
            state.hasBrakeWarning = false;
            state.lastBrakeWarningCode = string.Empty;
            state.lastBrakeWarningMessage = string.Empty;
        }

        private void HeatBrakeState(VehicleOutputState state)
        {
            VehicleOutputConfig config = vehicleOutputStore != null ? vehicleOutputStore.Config : null;
            float criticalTemperature = config != null ? config.brakeCriticalTemperatureC : 520f;
            float minEfficiency = config != null ? config.brakeMinThermalEfficiency : 0.42f;

            state.brakeTemperatureC = criticalTemperature;
            state.brakeThermalEfficiency = Mathf.Clamp01(minEfficiency);
            state.brakeOverheated = true;
            state.brakeFadeActive = true;
            state.hasBrakeWarning = true;
            state.brakeSeverity = 1f;
            state.lastBrakeWarningCode = "BRAKE_OVERHEAT_CRITICAL";
            state.lastBrakeWarningMessage = "Brake temperature is critical; braking force is severely reduced.";
        }

        private void RepairAllVitalSystems()
        {
            if (engineStore != null && engineStore.Current != null)
            {
                EngineState engine = engineStore.Current;
                engine.componentHealthPercent = 100f;
                engine.accumulatedDamagePercent = 0f;
                engine.engineStalled = false;
                engine.starterOveruseWarning = false;
                engine.hasLaunchWarning = false;
                engine.hasLaunchMisuse = false;
                engine.launchStallRisk = false;
                engine.lastLaunchWarningCode = string.Empty;
                engine.lastLaunchWarningMessage = string.Empty;
                engine.lastStarterWarningCode = string.Empty;
                engine.lastStarterWarningMessage = string.Empty;
                CoolEngineState(engine);
                engineStore.ApplyStateSnapshot(engine);
            }

            if (radiatorStore != null)
            {
                radiatorStore.Repair();
            }

            if (transmissionStore != null && transmissionStore.Current != null)
            {
                TransmissionState transmission = transmissionStore.Current;
                transmission.componentHealthPercent = 100f;
                transmission.accumulatedDamagePercent = 0f;
                transmission.hasMisuseWarning = false;
                transmission.hasTransmissionWarning = false;
                transmission.lastMisuseCode = string.Empty;
                transmission.lastMisuseMessage = string.Empty;
                transmission.lastMisuseSeverity = 0f;
                transmission.shiftAllowed = true;
                transmissionStore.ApplyStateSnapshot(transmission);
            }

            if (electricalStore != null)
            {
                electricalStore.SetBatteryChargePercent(100f);
            }

            if (vehicleOutputStore != null && vehicleOutputStore.Current != null)
            {
                VehicleOutputState vehicle = vehicleOutputStore.Current;
                CoolBrakeState(vehicle);
                vehicleOutputStore.ApplyStateSnapshot(vehicle);
            }

            if (lightsStore != null && lightsStore.Current != null)
            {
                VehicleLightsState lights = lightsStore.Current;
                RepairLightsState(lights);
                lightsStore.ApplyStateSnapshot(lights);
            }

            textCache.Clear();
        }

        private void ForceWorkshopRefresh()
        {
            engineStore?.ForceRefresh();
            radiatorStore?.ForceRefresh();
            transmissionStore?.ForceRefresh();
            electricalStore?.ForceRefresh();
            lightsStore?.ForceRefresh();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(headerStyle, GUILayout.Height(72f));

            Texture image = headerTexture != null
                ? headerTexture
                : (headerSprite != null ? headerSprite.texture : null);

            if (image != null)
            {
                Rect imageRect = GUILayoutUtility.GetRect(56f, 56f, GUILayout.Width(72f), GUILayout.Height(56f));
                GUI.DrawTexture(imageRect, image, ScaleMode.ScaleToFit, true);
            }

            GUILayout.BeginVertical();
            GUILayout.Space(4f);
            GUILayout.Label("TiltDrive", titleStyle);
            GUILayout.Label(T("subtitle"), labelStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            DrawLanguageSelector();
            GUILayout.Label("M", sectionStyle, GUILayout.Width(48f), GUILayout.Height(44f));
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
        }

        private void DrawLanguageSelector()
        {
            GUILayout.BeginVertical(GUILayout.Width(190f));
            GUILayout.Label(T("language"), labelStyle, GUILayout.Height(22f));

            string[] options =
            {
                language == MenuLanguage.Spanish ? "Español" : "Spanish",
                language == MenuLanguage.Spanish ? "Inglés" : "English"
            };

            int nextLanguage = GUILayout.SelectionGrid(
                (int)language,
                options,
                2,
                buttonStyle,
                GUILayout.Height(32f));

            if (nextLanguage != (int)language)
            {
                language = (MenuLanguage)nextLanguage;
                RebuildTabs();
            }

            GUILayout.EndVertical();
        }

        private string T(string key)
        {
            bool es = language == MenuLanguage.Spanish;
            switch (key)
            {
                case "subtitle": return es ? "Configuración del vehículo en tiempo real" : "Live vehicle tuning";
                case "language": return es ? "Idioma" : "Language";
                case "no_configs": return es ? "No se encontraron configuraciones en la escena." : "No configs found in the scene.";
                case "missing_config": return es ? "Esta configuración no está disponible." : "This config is not available.";
                case "refresh": return es ? "Buscar de nuevo" : "Refresh";
                case "apply": return es ? "Aplicar y ajustar límites" : "Apply and clamp";
                case "refresh_refs": return es ? "Actualizar referencias" : "Refresh references";
                case "close": return es ? "Cerrar" : "Close";
                case "tab_engine": return es ? "Motor" : "Engine";
                case "tab_radiator": return es ? "Radiador" : "Radiator";
                case "tab_launch": return es ? "Arranque" : "Launch";
                case "tab_transmission": return es ? "Transmisión" : "Transmission";
                case "tab_damage": return es ? "Fallos" : "Misuse";
                case "tab_vehicle": return es ? "Vehículo" : "Vehicle";
                case "tab_lights": return es ? "Luces" : "Lights";
                case "tab_electrical": return es ? "Eléctrico" : "Electrical";
                case "tab_workshop": return es ? "Taller" : "Workshop";
                case "workshop_hint": return es ? "Controles vivos para reparar, dañar o forzar estados durante pruebas." : "Live controls to repair, damage, or force runtime states during tests.";
                case "workshop_engine": return es ? "Motor" : "Engine";
                case "workshop_radiator": return es ? "Radiador y refrigeracion" : "Radiator and cooling";
                case "workshop_transmission": return es ? "Transmisión" : "Transmission";
                case "workshop_brakes": return es ? "Frenos" : "Brakes";
                case "workshop_electrical": return es ? "Batería y sistema eléctrico" : "Battery and electrical system";
                case "workshop_lights": return es ? "Luces y fallas" : "Lights and failures";
                case "workshop_health_percent": return es ? "Salud del componente (%)" : "Component health (%)";
                case "workshop_damage_percent": return es ? "Daño acumulado (%)" : "Accumulated damage (%)";
                case "workshop_engine_rpm": return es ? "RPM actuales del motor" : "Current engine RPM";
                case "workshop_engine_temperature": return es ? "Temperatura del motor (°C)" : "Engine temperature (°C)";
                case "workshop_engine_thermal_efficiency": return es ? "Eficiencia térmica del motor" : "Engine thermal efficiency";
                case "workshop_engine_on": return es ? "Motor encendido" : "Engine on";
                case "workshop_engine_stalled": return es ? "Motor apagado por calado" : "Engine stalled";
                case "workshop_engine_overheated": return es ? "Motor sobrecalentado" : "Engine overheated";
                case "workshop_radiator_health": return es ? "Salud del radiador (%)" : "Radiator health (%)";
                case "workshop_radiator_coolant_level": return es ? "Nivel de refrigerante (%)" : "Coolant level (%)";
                case "workshop_radiator_temperature": return es ? "Temperatura del refrigerante (Â°C)" : "Coolant temperature (Â°C)";
                case "workshop_radiator_pressure": return es ? "Presion del sistema (kPa)" : "System pressure (kPa)";
                case "workshop_radiator_cooling_efficiency": return es ? "Eficiencia de enfriamiento" : "Cooling efficiency";
                case "workshop_radiator_perforated": return es ? "Radiador perforado / con fuga" : "Radiator perforated / leaking";
                case "workshop_radiator_pressure_damage": return es ? "DaÃ±o por sobrepresion activo" : "Pressure damage active";
                case "workshop_current_gear": return es ? "Marcha actual (-1=R, 0=N)" : "Current gear (-1=R, 0=N)";
                case "workshop_clutch_engagement": return es ? "Acople del clutch (0 a 1)" : "Clutch engagement (0 to 1)";
                case "workshop_shift_allowed": return es ? "Cambios permitidos" : "Shifts allowed";
                case "workshop_brake_temperature": return es ? "Temperatura de frenos (°C)" : "Brake temperature (°C)";
                case "workshop_brake_efficiency": return es ? "Eficiencia térmica del freno" : "Brake thermal efficiency";
                case "workshop_brake_fade": return es ? "Fatiga de frenos activa" : "Brake fade active";
                case "workshop_brake_overheated": return es ? "Frenos sobrecalentados" : "Brakes overheated";
                case "workshop_battery_charge": return es ? "Carga de batería (%)" : "Battery charge (%)";
                case "workshop_battery_voltage": return es ? "Voltaje actual de batería" : "Current battery voltage";
                case "workshop_electrical_available": return es ? "Sistema eléctrico disponible" : "Electrical system available";
                case "workshop_lights_health": return es ? "Salud del sistema de luces (%)" : "Lighting system health (%)";
                case "workshop_left_signal": return es ? "Direccional izquierda funcional" : "Left signal functional";
                case "workshop_right_signal": return es ? "Direccional derecha funcional" : "Right signal functional";
                case "workshop_low_beams": return es ? "Luces bajas funcionales" : "Low beams functional";
                case "workshop_high_beams": return es ? "Luces altas funcionales" : "High beams functional";
                case "workshop_fog_lights": return es ? "Exploradoras funcionales" : "Fog lights functional";
                case "workshop_brake_lights": return es ? "Luces de freno funcionales" : "Brake lights functional";
                case "workshop_reverse_lights": return es ? "Luz de reversa funcional" : "Reverse lights functional";
                case "workshop_other_lights": return es ? "Luz auxiliar funcional" : "Auxiliary light functional";
                case "workshop_repair_engine": return es ? "Reparar motor" : "Repair engine";
                case "workshop_repair_radiator": return es ? "Reparar radiador" : "Repair radiator";
                case "workshop_repair_transmission": return es ? "Reparar transmisión" : "Repair transmission";
                case "workshop_repair_lights": return es ? "Reparar luces" : "Repair lights";
                case "workshop_repair_all": return es ? "Reparar todo" : "Repair all";
                case "workshop_damage_25": return es ? "Dañar -25%" : "Damage -25%";
                case "workshop_cool_engine": return es ? "Enfriar motor" : "Cool engine";
                case "workshop_heat_engine": return es ? "Sobrecalentar motor" : "Overheat engine";
                case "workshop_perforate_radiator": return es ? "Perforar radiador" : "Perforate radiator";
                case "workshop_refill_coolant": return es ? "Llenar refrigerante" : "Refill coolant";
                case "workshop_drain_coolant": return es ? "Vaciar refrigerante" : "Drain coolant";
                case "workshop_fail_all_lights": return es ? "Fallar todas las luces" : "Fail all lights";
                case "workshop_cool_brakes": return es ? "Enfriar frenos" : "Cool brakes";
                case "workshop_heat_brakes": return es ? "Sobrecalentar frenos" : "Overheat brakes";
                case "workshop_charge_full": return es ? "Cargar batería" : "Charge battery";
                case "workshop_drain_battery": return es ? "Descargar batería" : "Drain battery";
                case "workshop_reset_engine": return es ? "Reset motor" : "Reset engine";
                case "workshop_reset_radiator": return es ? "Reset radiador" : "Reset radiator";
                case "workshop_reset_transmission": return es ? "Reset transmisión" : "Reset transmission";
                case "workshop_reset_vehicle": return es ? "Reset vehículo" : "Reset vehicle";
                case "workshop_reset_electrical": return es ? "Reset eléctrico" : "Reset electrical";
                case "workshop_reset_lights": return es ? "Reset luces" : "Reset lights";
                case "workshop_missing_engine": return es ? "No se encontró EngineStore en la escena." : "EngineStore was not found in the scene.";
                case "workshop_missing_radiator": return es ? "No se encontró RadiatorStore en la escena." : "RadiatorStore was not found in the scene.";
                case "workshop_missing_transmission": return es ? "No se encontró TransmissionStore en la escena." : "TransmissionStore was not found in the scene.";
                case "workshop_missing_vehicle": return es ? "No se encontró VehicleOutputStore en la escena." : "VehicleOutputStore was not found in the scene.";
                case "workshop_missing_electrical": return es ? "No se encontró VehicleElectricalStore en la escena." : "VehicleElectricalStore was not found in the scene.";
                case "workshop_missing_lights": return es ? "No se encontró VehicleLightsStore en la escena." : "VehicleLightsStore was not found in the scene.";
                default: return key;
            }
        }

        private string FieldLabel(string fieldName)
        {
            bool es = language == MenuLanguage.Spanish;
            switch (fieldName)
            {
                case "engineName": return es ? "Nombre del motor" : "Engine profile";
                case "architectureType": return es ? "Arquitectura del motor" : "Engine layout";
                case "displacementLiters": return es ? "Cilindraje (L)" : "Displacement (L)";
                case "cylinderCount": return es ? "Número de cilindros" : "Cylinder count";
                case "idleRPM": return es ? "RPM de ralentí" : "Idle RPM";
                case "stallRPM": return es ? "RPM de apagado" : "Stall RPM";
                case "maxRPM": return es ? "RPM máximas" : "Max RPM";
                case "criticalRPM": return es ? "Zona crítica de RPM" : "Critical RPM";
                case "enableRevLimiter": return es ? "Limitador de RPM activo" : "Rev limiter enabled";
                case "revLimiterDropRPM": return es ? "Caída del limitador (RPM)" : "Rev limiter RPM drop";
                case "revLimiterPulseFrequency": return es ? "Velocidad del rebote del limitador" : "Limiter bounce speed";
                case "revLimiterTorqueMultiplier": return es ? "Torque durante el corte" : "Torque during limiter cut";
                case "rpmRiseSpeed": return es ? "Respuesta al subir RPM" : "RPM rise response";
                case "rpmFallSpeed": return es ? "Respuesta al bajar RPM" : "RPM fall response";
                case "engineInertia": return es ? "Inercia del motor" : "Engine inertia";
                case "throttleResponsiveness": return es ? "Sensibilidad del acelerador" : "Throttle response curve";
                case "engineStartRPMSpeed": return es ? "Velocidad del motor de arranque" : "Starter RPM speed";
                case "engineShutdownRPMSpeed": return es ? "Caída de RPM al apagar" : "Shutdown RPM fall speed";
                case "minimumStartHoldSeconds": return es ? "Tiempo mínimo sosteniendo encendido" : "Minimum start hold time";
                case "damagedEngineStartExtraSeconds": return es ? "Tiempo extra por motor dañado" : "Extra start time when damaged";
                case "starterOveruseWarningSeconds": return es ? "Aviso por abuso del starter" : "Starter overuse warning time";
                case "starterOveruseCriticalSeconds": return es ? "Abuso severo del starter" : "Severe starter overuse time";
                case "baseTorqueNm": return es ? "Torque base (Nm)" : "Base torque (Nm)";
                case "peakTorqueRPM": return es ? "RPM de mejor torque" : "Best torque RPM";
                case "engineBrakeStrength": return es ? "Freno motor" : "Engine braking";
                case "engineAmbientTemperatureC": return es ? "Temperatura ambiente del motor" : "Engine ambient temperature";
                case "engineInitialTemperatureC": return es ? "Temperatura inicial del motor" : "Initial engine temperature";
                case "engineNormalOperatingTemperatureC": return es ? "Temperatura normal de operación" : "Normal operating temperature";
                case "engineBaseHeatPerSecond": return es ? "Calentamiento base del motor" : "Engine base heat";
                case "engineRPMHeatPerSecond": return es ? "Calentamiento por RPM" : "RPM heat gain";
                case "engineLoadHeatPerSecond": return es ? "Calentamiento por esfuerzo" : "Load heat gain";
                case "engineOverRevHeatPerSecond": return es ? "Calentamiento por sobre RPM" : "Over-rev heat gain";
                case "engineBaseCoolingPerSecond": return es ? "Enfriamiento base del motor" : "Engine base cooling";
                case "engineSpeedCoolingPerSecond": return es ? "Enfriamiento del motor por velocidad" : "Engine speed cooling";
                case "engineWarningTemperatureC": return es ? "Aviso por temperatura del motor" : "Engine temperature warning";
                case "engineEfficiencyDropStartTemperatureC": return es ? "Inicio de pérdida de eficiencia" : "Efficiency drop starts";
                case "engineCriticalTemperatureC": return es ? "Temperatura crítica del motor" : "Critical engine temperature";
                case "engineMaxTemperatureC": return es ? "Temperatura máxima del motor" : "Max engine temperature";
                case "engineMinThermalEfficiency": return es ? "Eficiencia mínima por calor" : "Minimum thermal efficiency";
                case "loadSensitivity": return es ? "Sensibilidad a carga" : "Load sensitivity";
                case "slopeSensitivity": return es ? "Sensibilidad a pendiente" : "Slope sensitivity";
                case "massInfluence": return es ? "Influencia del peso" : "Mass influence";

                case "coolantType": return es ? "Tipo de refrigerante" : "Coolant type";
                case "initialCoolantLevelPercent": return es ? "Nivel inicial de refrigerante" : "Initial coolant level";
                case "coolantAmbientTemperatureC": return es ? "Temperatura ambiente del refrigerante" : "Coolant ambient temperature";
                case "coolantInitialTemperatureC": return es ? "Temperatura inicial del refrigerante" : "Initial coolant temperature";
                case "baseCoolingEfficiency": return es ? "Eficiencia base del radiador" : "Base radiator efficiency";
                case "airflowCoolingEfficiency": return es ? "Eficiencia por flujo de aire" : "Airflow cooling efficiency";
                case "fanCoolingEfficiency": return es ? "Eficiencia del ventilador" : "Fan cooling efficiency";
                case "fanActivationTemperatureC": return es ? "Temperatura para activar ventilador" : "Fan activation temperature";
                case "minimumCoolingEfficiency": return es ? "Eficiencia minima de enfriamiento" : "Minimum cooling efficiency";
                case "basePressureKpa": return es ? "Presion base del sistema" : "Base system pressure";
                case "warningPressureKpa": return es ? "Aviso por presion" : "Pressure warning";
                case "pressureDamageStartKpa": return es ? "Inicio de daño por presion" : "Pressure damage starts";
                case "criticalPressureKpa": return es ? "Presion critica" : "Critical pressure";
                case "pressureDamagePerSecond": return es ? "Daño por presion por segundo" : "Pressure damage per second";
                case "overheatDamagePerSecond": return es ? "Daño por sobrecalentamiento" : "Overheat damage per second";
                case "perforationLeakRatePercentPerSecond": return es ? "Fuga por perforacion" : "Perforation leak rate";
                case "lowCoolantWarningPercent": return es ? "Aviso por poco refrigerante" : "Low coolant warning";
                case "criticalCoolantPercent": return es ? "Refrigerante critico" : "Critical coolant";

                case "launchSpeedThresholdMS": return es ? "Velocidad considerada arranque" : "Launch speed range";
                case "maxRecommendedLaunchGear": return es ? "Marcha recomendada para arrancar" : "Recommended launch gear";
                case "biteEngagementMin": return es ? "Inicio del punto de contacto" : "Clutch bite starts";
                case "biteEngagementMax": return es ? "Fin del punto de contacto" : "Clutch bite ends";
                case "clutchDumpEngagement": return es ? "Embrague soltado de golpe" : "Clutch dump engagement";
                case "allowIdleOnlyLaunch": return es ? "Permitir arranque solo con ralentí" : "Allow idle-only launch";
                case "idleOnlyThrottleThreshold": return es ? "Acelerador máximo para ralentí" : "Idle-only throttle limit";
                case "idleOnlyRPMReserve": return es ? "Reserva de RPM para ralentí" : "Idle launch RPM reserve";
                case "minThrottleAtBite": return es ? "Acelerador mínimo en contacto" : "Minimum throttle at bite";
                case "minThrottleForClutchDump": return es ? "Acelerador mínimo al soltar clutch" : "Minimum throttle for clutch dump";
                case "brakeConflictThreshold": return es ? "Freno conflictivo al arrancar" : "Brake conflict threshold";
                case "stallRiskThreshold": return es ? "Riesgo de apagarse" : "Stall risk threshold";
                case "maxLaunchRPMPenalty": return es ? "Castigo máximo de RPM en arranque" : "Max launch RPM penalty";
                case "stallRPMMargin": return es ? "Margen de apagado por RPM" : "Stall RPM margin";
                case "maxClutchFrictionRPMDrop": return es ? "Caída máxima por fricción del clutch" : "Max clutch-friction RPM drop";
                case "idleLaunchTorqueFactor": return es ? "Ayuda de torque al ralentí" : "Idle launch torque assist";
                case "idleLockupEngagementStart": return es ? "Acople que empieza a forzar RPM" : "Idle lockup starts";

                case "transmissionName": return es ? "Nombre de la caja" : "Transmission profile";
                case "transmissionType": return es ? "Tipo de transmisión" : "Transmission type";
                case "forwardGearCount": return es ? "Número de marchas" : "Forward gears";
                case "hasNeutral": return es ? "Tiene neutro" : "Has neutral";
                case "hasReverse": return es ? "Tiene reversa" : "Has reverse";
                case "finalDriveRatio": return es ? "Relación final" : "Final drive ratio";
                case "transmissionEfficiency": return es ? "Eficiencia de transmisión" : "Transmission efficiency";
                case "reverseGearRatio": return es ? "Relación de reversa" : "Reverse gear ratio";
                case "forwardGearRatios": return es ? "Relaciones por marcha" : "Gear ratios";
                case "shiftDuration": return es ? "Tiempo de cambio" : "Shift duration";
                case "requiresClutch": return es ? "Requiere clutch" : "Requires clutch";
                case "allowDirectGearSelection": return es ? "Permitir selección directa" : "Allow direct gear selection";
                case "automaticEngageFirstFromNeutral": return es ? "Automática engrana 1ra desde neutro" : "Automatic engages 1st from neutral";
                case "automaticUpshiftRPM": return es ? "RPM para subir en automática" : "Automatic upshift RPM";
                case "automaticDownshiftRPM": return es ? "RPM para bajar en automática" : "Automatic downshift RPM";

                case "outputName": return es ? "Nombre del modelo de vehículo" : "Vehicle output profile";
                case "speedDisplayUnit": return es ? "Unidad de velocidad" : "Speed unit";
                case "wheelRadiusMeters": return es ? "Radio de rueda (m)" : "Wheel radius (m)";
                case "vehicleMassKg": return es ? "Peso simulado (kg)" : "Simulated mass (kg)";
                case "tractionForceScale": return es ? "Escala de tracción" : "Traction scale";
                case "aerodynamicDragCoefficient": return es ? "Resistencia aerodinámica" : "Aerodynamic drag";
                case "rollingResistanceCoefficient": return es ? "Resistencia de rodadura" : "Rolling resistance";
                case "brakeForceN": return es ? "Fuerza de freno" : "Brake force";
                case "brakeAmbientTemperatureC": return es ? "Temperatura ambiente de frenos" : "Brake ambient temperature";
                case "brakeInitialTemperatureC": return es ? "Temperatura inicial de frenos" : "Initial brake temperature";
                case "brakeHeatGainPerSecond": return es ? "Calentamiento de frenos" : "Brake heat gain";
                case "brakeBaseCoolingPerSecond": return es ? "Enfriamiento base de frenos" : "Brake base cooling";
                case "brakeSpeedCoolingPerSecond": return es ? "Enfriamiento por velocidad" : "Speed cooling";
                case "brakeWarningTemperatureC": return es ? "Aviso por temperatura de frenos" : "Brake temperature warning";
                case "brakeFadeStartTemperatureC": return es ? "Inicio de fatiga de frenos" : "Brake fade starts";
                case "brakeCriticalTemperatureC": return es ? "Temperatura crítica de frenos" : "Critical brake temperature";
                case "brakeMaxTemperatureC": return es ? "Temperatura máxima de frenos" : "Max brake temperature";
                case "brakeMinThermalEfficiency": return es ? "Eficiencia mínima por calor" : "Minimum thermal efficiency";
                case "enableABS": return es ? "ABS activo" : "ABS enabled";
                case "aggressiveBrakeInputThreshold": return es ? "Freno agresivo desde" : "Aggressive brake input";
                case "aggressiveBrakeDecelerationMS2": return es ? "Desaceleración agresiva" : "Aggressive deceleration";
                case "wheelLockBrakeInputThreshold": return es ? "Bloqueo de ruedas desde" : "Wheel lock brake input";
                case "wheelLockMinSpeedMS": return es ? "Velocidad mínima para bloqueo" : "Wheel lock minimum speed";
                case "lockedWheelBrakeMultiplier": return es ? "Freno con ruedas bloqueadas" : "Locked-wheel brake factor";
                case "lockedWheelSteeringMultiplier": return es ? "Dirección con ruedas bloqueadas" : "Locked-wheel steering factor";
                case "forceSteeringLossOnWheelLock": return es ? "Forzar pérdida de dirección" : "Force steering loss on lock";
                case "lockedWheelMaxSteeringAngleDegrees": return es ? "Ángulo máximo con bloqueo" : "Max steering while locked";
                case "absPulseFrequency": return es ? "Velocidad de pulsos ABS" : "ABS pulse speed";
                case "absMinBrakeMultiplier": return es ? "Freno mínimo en pulso ABS" : "ABS minimum brake factor";
                case "fullStopSpeedThresholdMS": return es ? "Velocidad para reposo total" : "Full-stop speed threshold";
                case "fullStopBrakeInputThreshold": return es ? "Freno mínimo para detener" : "Full-stop brake input";
                case "fullStopTractionForceThresholdN": return es ? "Tracción despreciable para detener" : "Full-stop traction threshold";
                case "wheelBaseMeters": return es ? "Distancia entre ejes" : "Wheelbase";
                case "maxSteeringAngleDegrees": return es ? "Ángulo máximo de dirección" : "Max steering angle";
                case "steeringSpeedSensitivity": return es ? "Dirección sensible a velocidad" : "Speed steering sensitivity";
                case "minSpeedForSteeringMS": return es ? "Velocidad mínima para girar" : "Minimum speed for steering";
                case "aggressiveSteeringMinSpeedMS": return es ? "Velocidad para volantazo" : "Aggressive steering speed";
                case "aggressiveSteeringInputThreshold": return es ? "Input para volantazo" : "Aggressive steering input";
                case "aggressiveSteeringDeltaThreshold": return es ? "Cambio brusco de dirección" : "Aggressive steering delta";
                case "engineBrakeMultiplier": return es ? "Multiplicador de freno motor" : "Engine brake multiplier";
                case "overRevSpeedCorrectionDecelerationMS2": return es ? "Corrección por sobre RPM" : "Over-rev correction decel";
                case "theoreticalMaxSpeedKmh": return es ? "Velocidad máxima teórica" : "Theoretical max speed";
                case "clampSpeedToTheoreticalMax": return es ? "Limitar a velocidad máxima" : "Clamp to max speed";
                case "clampSpeedToActiveGearRPM": return es ? "Limitar por RPM de la marcha" : "Clamp by gear RPM";

                case "profileName": return es ? "Nombre del perfil" : "Profile name";
                case "turnSignalFrequencyHz": return es ? "Velocidad de direccionales" : "Turn signal speed";
                case "autoBrakeLights": return es ? "Luces de freno automáticas" : "Automatic brake lights";
                case "brakeLightInputThreshold": return es ? "Freno para encender luz" : "Brake light threshold";
                case "autoReverseLights": return es ? "Luz de reversa automática" : "Automatic reverse lights";
                case "highBeamsRequireLowBeams": return es ? "Altas también encienden bajas" : "High beams require low beams";
                case "highBeamsTurnOffWithLowBeams": return es ? "Apagar bajas apaga altas" : "Low beams turn off high beams";

                case "batteryCapacityAh": return es ? "Capacidad de batería (Ah)" : "Battery capacity (Ah)";
                case "initialChargePercent": return es ? "Carga inicial de batería" : "Initial battery charge";
                case "nominalBatteryVoltage": return es ? "Voltaje nominal" : "Nominal voltage";
                case "fullChargeVoltage": return es ? "Voltaje con carga completa" : "Full-charge voltage";
                case "lowChargeVoltage": return es ? "Voltaje bajo" : "Low voltage";
                case "criticalVoltage": return es ? "Voltaje crítico" : "Critical voltage";
                case "noPowerVoltage": return es ? "Voltaje sin energía útil" : "No-power voltage";
                case "chargeSimulationScale": return es ? "Escala de carga/descarga" : "Charge simulation scale";
                case "voltageDropPerAmp": return es ? "Caída por amperio" : "Voltage drop per amp";
                case "starterVoltageSagPer100A": return es ? "Caída del starter por 100A" : "Starter sag per 100A";
                case "minimumIgnitionVoltage": return es ? "Voltaje mínimo para encender" : "Minimum ignition voltage";
                case "minimumIgnitionChargePercent": return es ? "Carga mínima para encender" : "Minimum ignition charge";
                case "starterCurrentAmps": return es ? "Consumo del motor de arranque" : "Starter current";
                case "alternatorVoltage": return es ? "Voltaje del alternador" : "Alternator voltage";
                case "alternatorMaxChargeAmps": return es ? "Carga máxima del alternador" : "Alternator max charge";
                case "alternatorActiveMinRPM": return es ? "RPM mínimas del alternador" : "Alternator minimum RPM";
                case "alternatorFullChargeRPM": return es ? "RPM de carga plena" : "Full-charge alternator RPM";
                case "engineElectronicsAmps": return es ? "Consumo electrónico del motor" : "Engine electronics draw";
                case "ignitionStandbyAmps": return es ? "Consumo en espera" : "Standby ignition draw";
                case "lowBeamsAmps": return es ? "Consumo luces bajas" : "Low beams draw";
                case "highBeamsAmps": return es ? "Consumo luces altas" : "High beams draw";
                case "fogLightsAmps": return es ? "Consumo exploradoras" : "Fog lights draw";
                case "brakeLightsAmps": return es ? "Consumo luces de freno" : "Brake lights draw";
                case "reverseLightsAmps": return es ? "Consumo luz de reversa" : "Reverse lights draw";
                case "singleTurnSignalAmps": return es ? "Consumo de una direccional" : "Single turn signal draw";
                case "otherLightsAmps": return es ? "Consumo luz auxiliar" : "Auxiliary light draw";
                case "minimumLightsVoltage": return es ? "Voltaje mínimo para luces" : "Minimum lights voltage";
                case "fullBrightnessVoltage": return es ? "Voltaje para brillo completo" : "Full brightness voltage";

                case "overRevSeverityRPMWindowFactor": return es ? "Ventana de severidad por sobre RPM" : "Over-rev severity window";
                case "downshiftEngineDamageMinPercent": return es ? "Daño mínimo al motor por reducción" : "Min engine damage on bad downshift";
                case "downshiftEngineDamageMaxPercent": return es ? "Daño máximo al motor por reducción" : "Max engine damage on bad downshift";
                case "downshiftTransmissionDamageMinPercent": return es ? "Daño mínimo a caja por reducción" : "Min transmission damage on bad downshift";
                case "downshiftTransmissionDamageMaxPercent": return es ? "Daño máximo a caja por reducción" : "Max transmission damage on bad downshift";
                case "skippedGearDamageMultiplier": return es ? "Multiplicador por saltar marchas" : "Skipped-gear damage multiplier";
                case "fullSeverityDamageMultiplier": return es ? "Multiplicador con severidad máxima" : "Full-severity damage multiplier";
                case "reverseMisuseMinSpeedMS": return es ? "Velocidad mínima para mal uso de reversa" : "Reverse misuse minimum speed";
                case "reverseWhileMovingEngineDamagePercent": return es ? "Daño al motor por reversa en movimiento" : "Engine damage reversing while moving";
                case "reverseWhileMovingTransmissionDamagePercent": return es ? "Daño a caja por reversa en movimiento" : "Transmission damage reversing while moving";
                case "highGearEngineStrainEnabled": return es ? "Detectar motor ahogado en marcha alta" : "Detect high-gear engine strain";
                case "highGearStrainMinGear": return es ? "Marcha mínima para sobreesfuerzo" : "Minimum gear for strain";
                case "highGearStrainThrottleThreshold": return es ? "Acelerador para sobreesfuerzo" : "Throttle for strain";
                case "highGearStrainRPMThreshold": return es ? "RPM bajas para sobreesfuerzo" : "Low RPM strain threshold";
                case "highGearStrainCriticalRPM": return es ? "RPM críticas de sobreesfuerzo" : "Critical strain RPM";
                case "highGearStrainMinSpeedMS": return es ? "Velocidad mínima de sobreesfuerzo" : "Minimum strain speed";
                case "highGearStrainMinClutchEngagement": return es ? "Acople mínimo para sobreesfuerzo" : "Minimum clutch engagement for strain";
                case "highGearStrainLogIntervalSeconds": return es ? "Intervalo de avisos de sobreesfuerzo" : "Strain warning interval";
                case "highGearLaunchStrainEnabled": return es ? "Detectar arranque en marcha alta" : "Detect high-gear launch strain";
                case "highGearLaunchStrainMinGear": return es ? "Marcha mínima para mal arranque" : "Minimum gear for bad launch";
                case "highGearLaunchStrainMaxSpeedMS": return es ? "Velocidad máxima de arranque" : "Maximum launch speed";
                case "highGearLaunchStrainThrottleThreshold": return es ? "Acelerador para mal arranque" : "Throttle for bad launch";
                case "highGearLaunchStrainMinClutchEngagement": return es ? "Clutch mínimo para mal arranque" : "Minimum clutch for bad launch";
                case "maxSingleEventEngineDamagePercent": return es ? "Daño máximo al motor por evento" : "Max engine damage per event";
                case "maxSingleEventTransmissionDamagePercent": return es ? "Daño máximo a caja por evento" : "Max transmission damage per event";
                default: return Nicify(fieldName);
            }
        }

        private string EnumLabel(string enumName)
        {
            bool es = language == MenuLanguage.Spanish;
            switch (enumName)
            {
                case "KilometersPerHour": return es ? "km/h" : "km/h";
                case "MilesPerHour": return es ? "mph" : "mph";
                case "Manual": return es ? "Manual" : "Manual";
                case "Automatic": return es ? "Automática" : "Automatic";
                case "Inline4": return es ? "4 cilindros en línea" : "Inline 4";
                case "Inline6": return es ? "6 cilindros en línea" : "Inline 6";
                case "V6": return "V6";
                case "V8": return "V8";
                case "V10": return "V10";
                case "V12": return "V12";
                case "Electric": return es ? "Eléctrico" : "Electric";
                case "Water": return es ? "Agua" : "Water";
                case "DeionizedWater": return es ? "Agua desionizada" : "Deionized water";
                case "Coolant": return es ? "Refrigerante" : "Coolant";
                case "PerformanceCoolant": return es ? "Refrigerante de alto rendimiento" : "Performance coolant";
                default: return Nicify(enumName);
            }
        }

        private string ArrayItemLabel(string key, int index)
        {
            bool es = language == MenuLanguage.Spanish;
            if (key.Contains("forwardGearRatios"))
            {
                return es ? $"Marcha {index + 1}" : $"Gear {index + 1}";
            }

            return $"[{index + 1}]";
        }

        private void ApplyResponsiveWindowLayout()
        {
            if (!fitWindowToScreen)
            {
                windowRect.width = Mathf.Max(minimumWindowWidth, windowRect.width);
                windowRect.height = Mathf.Max(minimumWindowHeight, windowRect.height);
                return;
            }

            float availableWidth = Mathf.Max(minimumWindowWidth, Screen.width - screenMargin * 2f);
            float availableHeight = Mathf.Max(minimumWindowHeight, Screen.height - screenMargin * 2f);
            float targetWidth = Mathf.Clamp(Screen.width * windowWidthPercent, minimumWindowWidth, availableWidth);
            float targetHeight = Mathf.Clamp(Screen.height * windowHeightPercent, minimumWindowHeight, availableHeight);
            bool screenChanged = Screen.width != lastScreenWidth || Screen.height != lastScreenHeight;

            windowRect.width = targetWidth;
            windowRect.height = targetHeight;

            if (screenChanged)
            {
                windowRect.x = Mathf.Max(screenMargin, (Screen.width - targetWidth) * 0.5f);
                windowRect.y = Mathf.Max(screenMargin, (Screen.height - targetHeight) * 0.5f);
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
            }

            windowRect.x = Mathf.Clamp(windowRect.x, screenMargin, Mathf.Max(screenMargin, Screen.width - windowRect.width - screenMargin));
            windowRect.y = Mathf.Clamp(windowRect.y, screenMargin, Mathf.Max(screenMargin, Screen.height - windowRect.height - screenMargin));
        }

        private void EnsureStyles()
        {
            windowBackground ??= MakeTexture(ForceOpaque(Darken(windowColor, 0.80f)));
            panelBackground ??= MakeTexture(ForceOpaque(Darken(panelColor, 0.90f)));
            buttonBackground ??= MakeTexture(ForceOpaque(buttonColor));
            fieldBackground ??= MakeTexture(ForceOpaque(fieldColor));

            windowStyle ??= new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(18, 18, 18, 16),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = windowBackground, textColor = textColor }
            };

            headerStyle ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 8, 8),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = panelBackground, textColor = textColor }
            };

            titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = titleFontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = accentColor }
            };

            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = bodyFontSize,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = textColor }
            };

            sectionStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = bodyFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 6, 6),
                normal = { background = panelBackground, textColor = accentColor }
            };

            contentStyle ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 10, 8, 8),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = panelBackground, textColor = textColor }
            };

            textFieldStyle ??= new GUIStyle(GUI.skin.textField)
            {
                fontSize = bodyFontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 5, 5),
                normal = { background = fieldBackground, textColor = textColor },
                focused = { background = fieldBackground, textColor = Color.white }
            };

            buttonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = bodyFontSize,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 6, 6),
                normal = { background = buttonBackground, textColor = textColor },
                hover = { background = MakeTexture(Color.Lerp(buttonColor, accentColor, 0.25f)), textColor = Color.white },
                active = { background = MakeTexture(accentColor), textColor = Color.white }
            };

            toolbarStyle ??= new GUIStyle(buttonStyle)
            {
                fixedHeight = 0f,
                margin = new RectOffset(2, 2, 2, 2)
            };

            toggleStyle ??= new GUIStyle(GUI.skin.toggle)
            {
                fontSize = bodyFontSize,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = textColor },
                onNormal = { textColor = accentColor },
                hover = { textColor = Color.white },
                onHover = { textColor = Color.white }
            };

            scrollStyle ??= new GUIStyle(GUI.skin.scrollView)
            {
                padding = new RectOffset(4, 8, 4, 4),
                normal = { background = panelBackground }
            };
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

        private static Color ForceOpaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        private static Color Darken(Color color, float factor)
        {
            color.r *= factor;
            color.g *= factor;
            color.b *= factor;
            return color;
        }

        private static string Nicify(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length + 8);
            builder.Append(char.ToUpperInvariant(value[0]));

            for (int i = 1; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsUpper(c) && value[i - 1] != ' ')
                {
                    builder.Append(' ');
                }
                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
