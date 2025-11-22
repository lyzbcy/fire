using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace OneClick.VRConverter.Editor
{
    /// <summary>
    /// 一键将当前项目配置为基础 VR 项目的工具窗口。
    /// 菜单：Tools/VR Converter/一键转换当前项目为VR...
    /// </summary>
    public class VRProjectConverterWindow : EditorWindow
    {
        private const string MenuPath = "Tools/VR Converter/一键转换当前项目为VR...";

        // 需要添加的 XR 相关包名
        private static readonly string[] RequiredPackages =
        {
            "com.unity.xr.management",
            "com.unity.xr.openxr",
            "com.unity.xr.interaction.toolkit",
            "com.unity.inputsystem"
        };

        private static readonly BuildTargetGroup[] TargetGroups =
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android
        };

        private const string GeneratedRootFolder = "Assets/VRConverterGenerated";
        private const string GeneratedSettingsFolder = GeneratedRootFolder + "/XR";
        private const string GeneratedGeneralSettingsAsset = GeneratedSettingsFolder + "/XRGeneralSettings.asset";
        private const string GeneratedInputActionsFolder = GeneratedRootFolder + "/InputActions";
        private const string DefaultXriInputActionsGuid = "c348712bda248c246b8c49b3db54643f";
        private const string DeviceSimulatorSettingsTypeName = "UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings";
        private const string ScriptableSettingsBaseTypeName = "Unity.XR.CoreUtils.ScriptableSettingsBase, Unity.XR.CoreUtils";
        private const string DeviceSimulatorPackageId = "Packages/com.unity.xr.interaction.toolkit";
        private const string StarterAssetsSampleRelativePath = "Samples~/Starter Assets";
        private const string StarterAssetsSampleDisplayName = "Starter Assets";
        private const string GeneratedStarterAssetsFolder = GeneratedRootFolder + "/StarterAssets";
        private const string DeviceSimulatorSampleRelativePath = "Samples~/XR Device Simulator";
        private const string DeviceSimulatorPrefabName = "XR Device Simulator.prefab";
        private const string GeneratedSimulatorFolder = "Assets/VRConverterGenerated/DeviceSimulator";
        private const string GeneratedSimulatorPrefabPath = GeneratedSimulatorFolder + "/" + DeviceSimulatorPrefabName;

        private const string OpenXrLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string XrOriginTypeName = "Unity.XR.CoreUtils.XROrigin";
        private const string ActionBasedControllerTypeName = "UnityEngine.XR.Interaction.Toolkit.ActionBasedController";
        private const string XrInteractionManagerTypeName = "UnityEngine.XR.Interaction.Toolkit.XRInteractionManager";
        private const string InputActionManagerTypeName = "UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager";
        private const string TrackedPoseDriverTypeName = "UnityEngine.InputSystem.XR.TrackedPoseDriver";
        private const string XrRayInteractorTypeName = "UnityEngine.XR.Interaction.Toolkit.XRRayInteractor";
        private const string ModePrefKey = "OneClick.VRConverter.Mode";
        private const double DiagnosticsRefreshSeconds = 1.5d;
        private const float WideLayoutThreshold = 760f;

        private enum ConverterMode
        {
            Guided = 0,
            Professional = 1
        }

        private enum RigStrategy
        {
            Auto = 0,
            OnlyUpdateExistingXrOrigin = 1,
            ForceFallbackRig = 2,
            SkipSceneChanges = 3
        }

        private struct ConversionPlan
        {
            public bool EnsurePackages;
            public bool ConfigureProjectSettings;
            public bool ConvertScene;
            public bool ConfigureDeviceSimulator;
            public BuildTargetGroup[] TargetGroups;
            public RigStrategy RigStrategy;
            public bool DisableLegacyCamera;

            public bool HasAnyOperation =>
                EnsurePackages || ConfigureProjectSettings || ConvertScene || ConfigureDeviceSimulator;
        }

        /// <summary>
        /// 存储原 Main Camera 的绑定信息，用于将新的 XR Origin 绑定到原父对象
        /// </summary>
        private struct LegacyCameraBindingInfo
        {
            public Transform Parent;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public bool IsValid;

            public static LegacyCameraBindingInfo Empty => new LegacyCameraBindingInfo
            {
                IsValid = false
            };
        }

        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private Vector2 _windowScroll;
        private Vector2 _logScroll;
        private const string GitAssistantMenuPath = "Tools/Version Control Assistant";
        private const string GitAssistantAssetStoreUrl = "https://assetstore.unity.com/packages/slug/345864";
        private static readonly string[] GitAssistantWindowTypeNames =
        {
            // 旧版命名空间
            "Fire.VersionControlAssistant.VersionControlAssistantWindow",
            // 兼容最新 GitAssistant（类型名变为 GitAssistantWindow）
            "Fire.GitAssistant.GitAssistantWindow",
            // 历史版本可能仍沿用 VersionControlAssistantWindow 命名
            "Fire.GitAssistant.VersionControlAssistantWindow"
        };
        private static readonly string[] GitAssistantUtilityTypeNames =
        {
            "Fire.VersionControlAssistant.GitProcessUtility",
            "Fire.GitAssistant.GitProcessUtility"
        };
        private const double BackupConfirmationValidSeconds = 300d;

        private string _log = "";
        private readonly List<AddRequest> _pendingAddRequests = new List<AddRequest>();
        private bool _isMonitoringAddRequests;

        // UI 样式
        private GUIStyle _headerTitleStyle;
        private GUIStyle _headerSubTitleStyle;
        private GUIStyle _stepTitleStyle;
        private GUIStyle _logTextStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _secondaryButtonStyle;
        private GUIStyle _heroCardStyle;
        private GUIStyle _diagnosticCardStyle;
        private Texture2D _cardBackgroundTexture;
        private Texture2D _heroGradientTexture;
        private Texture2D _diagnosticPositiveTexture;
        private Texture2D _diagnosticNegativeTexture;

        private InputActionAsset _cachedDefaultInputActions;
        private readonly Dictionary<string, InputActionReference> _actionReferenceCache = new Dictionary<string, InputActionReference>();
        private DateTime? _lastBackupConfirmationTimeUtc;
        private string _lastBaselineCommitHash;
        private bool _lastConversionSucceeded;
        private ConverterMode _uiMode = ConverterMode.Guided;
        private bool _proIncludePackages = true;
        private bool _proIncludeProjectSettings = true;
        private bool _proIncludeSceneConversion = true;
        private bool _proIncludeDeviceSimulator = true;
        private bool _proPreserveLegacyMainCamera;
        private RigStrategy _proRigStrategy = RigStrategy.Auto;
        private readonly Dictionary<BuildTargetGroup, bool> _proTargetGroupToggles = new Dictionary<BuildTargetGroup, bool>();
        private ProjectDiagnostics _cachedDiagnostics;
        private double _lastDiagnosticsSampleTime;
        private bool _promptedStarterAssets;
        private bool _promptedDeviceSimulatorSample;

        private void OnEnable()
        {
            // Unity 恢复布局时 EditorStyles 资源可能尚未就绪，改为延迟初始化
            EditorApplication.delayCall += Repaint;
            LoadModePreference();
            InitProTargetGroupToggles();
        }

        private void OnDisable()
        {
            SaveModePreference();
        }

        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            var window = GetWindow<VRProjectConverterWindow>("一键VR转换");
            window.minSize = new Vector2(420, 320);
            window.Log("打开一键 VR 转换工具。");
        }

        private void OnGUI()
        {
            InitStyles();

            float scrollViewHeight = Mathf.Max(0f, position.height - 16f);
            _windowScroll = EditorGUILayout.BeginScrollView(
                _windowScroll,
                false,
                false,
                GUILayout.Height(scrollViewHeight));
            {
                DrawHeader();
                EditorGUILayout.Space(8);

                DrawModeSwitcher();
                EditorGUILayout.Space(8);

                DrawCompatibilityInsights();
                EditorGUILayout.Space(10);

                if (_uiMode == ConverterMode.Guided)
                {
                    DrawGuidedMode();
                }
                else
                {
                    DrawProfessionalMode();
                }

                EditorGUILayout.Space(10);
                DrawLogArea();
            }
            EditorGUILayout.EndScrollView();
        }

        private void Log(string msg)
        {
            _log += $"[{System.DateTime.Now:HH:mm:ss}] {msg}\n";
            Repaint();
        }

        /// <summary>
        /// 初始化窗口内使用到的 GUIStyle，保证整体更有设计感。
        /// </summary>
        private void InitStyles()
        {
            if (_headerTitleStyle != null) return;

            // Unity 启动或重新加载布局时，EditorStyles 可能仍未初始化
            if (EditorStyles.boldLabel == null ||
                EditorStyles.label == null ||
                EditorStyles.textArea == null)
            {
                // 下一帧再次尝试，避免 NullReferenceException
                EditorApplication.delayCall += Repaint;
                return;
            }

            // 创建纹理资源
            _cardBackgroundTexture ??= CreateCardBackgroundTexture();
            _heroGradientTexture ??= CreateHeroGradientTexture();
            _diagnosticPositiveTexture ??= CreateDiagnosticTexture(new Color(0.2f, 0.7f, 0.4f, 0.15f));
            _diagnosticNegativeTexture ??= CreateDiagnosticTexture(new Color(0.9f, 0.4f, 0.2f, 0.15f));

            // 标题样式 - 苹果风格：更大的字体，更清晰的层次
            _headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.1f, 0.1f, 0.1f) },
                margin = new RectOffset(0, 0, 0, 0)
            };

            _headerSubTitleStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 13,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.82f, 0.88f) : new Color(0.45f, 0.45f, 0.45f) },
                margin = new RectOffset(0, 0, 0, 0)
            };

            _stepTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.95f, 0.95f, 1f) : new Color(0.15f, 0.15f, 0.15f) },
                margin = new RectOffset(0, 0, 0, 0)
            };

            _logTextStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 11,
                padding = new RectOffset(8, 8, 6, 6)
            };

            // 卡片样式 - 苹果风格：更大的内边距，更舒适的间距
            _cardStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(20, 20, 18, 18),
                margin = new RectOffset(0, 0, 6, 6)
            };
            _cardStyle.normal.background = _cardBackgroundTexture;

            // 主按钮样式 - 苹果风格：更优雅的蓝色，更好的状态反馈
            _primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 36,
                padding = new RectOffset(24, 24, 8, 8),
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };
            // 苹果系统蓝色：RGB(0, 122, 255) -> Unity Color
            var primaryNormalTex = CreateColorTexture(new Color(0f, 0.478f, 1f));
            var primaryHoverTex = CreateColorTexture(new Color(0.1f, 0.55f, 1f));
            var primaryActiveTex = CreateColorTexture(new Color(0f, 0.4f, 0.9f));
            _primaryButtonStyle.normal.background = primaryNormalTex;
            _primaryButtonStyle.hover.background = primaryHoverTex;
            _primaryButtonStyle.active.background = primaryActiveTex;

            // 次按钮样式 - 苹果风格：更柔和的灰色，更好的对比度
            _secondaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fixedHeight = 32,
                padding = new RectOffset(16, 16, 6, 6),
                alignment = TextAnchor.MiddleCenter
            };
            var secondaryNormalTex = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.3f) : new Color(0.75f, 0.75f, 0.8f));
            var secondaryHoverTex = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.35f) : new Color(0.8f, 0.8f, 0.85f));
            var secondaryActiveTex = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.25f) : new Color(0.7f, 0.7f, 0.75f));
            _secondaryButtonStyle.normal.background = secondaryNormalTex;
            _secondaryButtonStyle.hover.background = secondaryHoverTex;
            _secondaryButtonStyle.active.background = secondaryActiveTex;
            _secondaryButtonStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.95f) : new Color(0.2f, 0.2f, 0.2f);

            // Hero 卡片样式（头部）- 苹果风格：更大的内边距，更优雅的渐变
            _heroCardStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(24, 24, 20, 20),
                margin = new RectOffset(0, 0, 0, 0)
            };
            _heroCardStyle.normal.background = _heroGradientTexture;

            // 诊断卡片样式 - 苹果风格：更舒适的内边距
            _diagnosticCardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(14, 14, 12, 12),
                margin = new RectOffset(0, 0, 3, 3)
            };
        }

        private Texture2D CreateCardBackgroundTexture()
        {
            var texture = new Texture2D(1, 1);
            // 苹果风格：更柔和的背景色，更好的对比度
            var color = EditorGUIUtility.isProSkin
                ? new Color(0.20f, 0.22f, 0.26f, 1f)
                : new Color(0.98f, 0.98f, 0.99f, 1f);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private Texture2D CreateHeroGradientTexture()
        {
            const int height = 40;
            var texture = new Texture2D(1, height);
            // 苹果风格：更优雅的渐变，更柔和的过渡
            var topColor = EditorGUIUtility.isProSkin
                ? new Color(0.28f, 0.30f, 0.36f, 1f)
                : new Color(0.99f, 0.99f, 1f, 1f);
            var bottomColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.24f, 0.30f, 1f)
                : new Color(0.97f, 0.97f, 0.98f, 1f);

            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                // 使用平滑的插值曲线
                float smoothT = t * t * (3f - 2f * t);
                texture.SetPixel(0, y, Color.Lerp(topColor, bottomColor, smoothT));
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private Texture2D CreateDiagnosticTexture(Color tint)
        {
            var texture = new Texture2D(1, 1);
            // 苹果风格：更柔和的背景色，更自然的色调融合
            var baseColor = EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.18f, 0.22f, 1f)
                : new Color(0.96f, 0.96f, 0.97f, 1f);
            var finalColor = Color.Lerp(baseColor, tint, 0.25f);
            texture.SetPixel(0, 0, finalColor);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private void LoadModePreference()
        {
            if (EditorPrefs.HasKey(ModePrefKey))
            {
                _uiMode = (ConverterMode)EditorPrefs.GetInt(ModePrefKey, (int)ConverterMode.Guided);
            }
        }

        private void SaveModePreference()
        {
            EditorPrefs.SetInt(ModePrefKey, (int)_uiMode);
        }

        private void InitProTargetGroupToggles()
        {
            foreach (var group in TargetGroups)
            {
                if (!_proTargetGroupToggles.ContainsKey(group))
                {
                    _proTargetGroupToggles[group] = true;
                }
            }
        }

        /// <summary>
        /// 顶部头部区域：标题 + 简短说明 + 状态提示。
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_heroCardStyle);
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("一键 VR 项目转换", _headerTitleStyle);
                EditorGUILayout.Space(6);

                EditorGUILayout.LabelField(
                    "面向新手的引导式工具：帮助你将当前项目快速配置为基础 VR 项目，" +
                    "自动处理 XR 包依赖、XR Plug-in Management 配置以及场景中的 XR Rig。",
                    _headerSubTitleStyle);

                EditorGUILayout.Space(12);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var compiling = EditorApplication.isCompiling;
                    var icon = EditorGUIUtility.IconContent(compiling ? "console.warnicon" : "TestPassed");
                    var msg = compiling
                        ? "Unity 正在导入或编译脚本，请等待完成后再执行\"第 2 步\"或\"一键执行\"操作。"
                        : "当前状态良好，可以直接执行\"一键执行所有步骤（推荐）\"。";

                    EditorGUILayout.LabelField(icon, GUILayout.Width(24), GUILayout.Height(24));
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.Space(6);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawModeSwitcher()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("模式选择", _stepTitleStyle);
            EditorGUILayout.Space(10);

            var contents = new[]
            {
                new GUIContent("傻瓜式一键"),
                new GUIContent("专业模式")
            };

            int selected = GUILayout.Toolbar((int)_uiMode, contents, GUILayout.Height(32));
            if (selected != (int)_uiMode)
            {
                _uiMode = (ConverterMode)selected;
                SaveModePreference();
            }

            EditorGUILayout.Space(8);
            var desc = _uiMode == ConverterMode.Guided
                ? "保持\"一键执行\"体验，适合第一次接触 VR 项目的同学。"
                : "自定义执行步骤、目标平台与场景策略，满足不同团队流程。";
            EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawCompatibilityInsights()
        {
            var diagnostics = GetProjectDiagnostics();
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("项目体检 & 兼容性建议", _stepTitleStyle);
            EditorGUILayout.Space(10);

            Action gitAssistantAction = diagnostics.HasGitAssistant
                ? null
                : () => Application.OpenURL(GitAssistantAssetStoreUrl);

            var rows = new[]
            {
                new DiagnosticRow("渲染管线", diagnostics.RenderPipelineLabel, true, diagnostics.RenderPipelineHint),
                new DiagnosticRow("XR Management", diagnostics.HasXrManagement ? "已检测到" : "尚未安装", diagnostics.HasXrManagement,
                    diagnostics.HasXrManagement ? "可直接配置 XRGeneralSettings。" : "建议先通过第 1 步或 Package Manager 导入 XR Management。"),
                new DiagnosticRow("OpenXR Loader", diagnostics.HasOpenXr ? "已就绪" : "未检测到", diagnostics.HasOpenXr,
                    diagnostics.HasOpenXr ? "可直接为 Standalone / Android 启用。" : "请确认 com.unity.xr.openxr 已导入。"),
                new DiagnosticRow("XR Interaction Toolkit", diagnostics.HasXri ? "已导入" : "未导入", diagnostics.HasXri,
                    diagnostics.HasXri ? "将优先创建 XR Origin（Action Based）。" : "缺少时将退回基础 VRRig。"),
                new DiagnosticRow("输入系统", diagnostics.HasNewInputSystem ? "新输入系统已启用" : "建议启用新输入系统", diagnostics.HasNewInputSystem,
                    diagnostics.HasNewInputSystem
                        ? "可自动绑定 XRI Default Input Actions。"
                        : "未检测到 Unity Input System 类型，可能需要在 Player Settings 中切换或安装该包。"),
                new DiagnosticRow("VR 模拟设备", diagnostics.HasVrSimulator ? "已配置" : "未配置", diagnostics.HasVrSimulator,
                    diagnostics.HasVrSimulator
                        ? "进入 Play 模式后自动实例化 XR Device Simulator。"
                        : "将在傻瓜式模式下一键配置，或在专业模式中勾选“配置 VR 模拟设备”。"),
                new DiagnosticRow("版本控制助手", diagnostics.HasGitAssistant ? "已安装" : "未安装", diagnostics.HasGitAssistant,
                    diagnostics.HasGitAssistant ? "可直接使用快速备份 / 回滚。" : "建议先导入 com.fire.gitassistant，以提升备份体验。",
                    gitAssistantAction)
            };

            int columns = position.width >= WideLayoutThreshold ? 2 : 1;
            for (int i = 0; i < rows.Length; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    int index = i + col;
                    if (index >= rows.Length)
                        break;

                    DrawDiagnosticRow(rows[index].Title, rows[index].Value, rows[index].Positive, rows[index].Hint, rows[index].OnClick);
                    if (columns > 1 && col == 0)
                    {
                        GUILayout.Space(8);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private readonly struct DiagnosticRow
        {
            public readonly string Title;
            public readonly string Value;
            public readonly bool Positive;
            public readonly string Hint;
            public readonly Action OnClick;

            public DiagnosticRow(string title, string value, bool positive, string hint, Action onClick = null)
            {
                Title = title;
                Value = value;
                Positive = positive;
                Hint = hint;
                OnClick = onClick;
            }
        }

        private void DrawDiagnosticRow(string title, string value, bool positive, string hint, Action onClick)
        {
            var cardBg = positive ? _diagnosticPositiveTexture : _diagnosticNegativeTexture;
            var originalBg = _diagnosticCardStyle.normal.background;
            _diagnosticCardStyle.normal.background = cardBg;

            using (new EditorGUILayout.VerticalScope(_diagnosticCardStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(140));
                    var prevColor = GUI.contentColor;
                    // 苹果风格：更柔和的成功/警告颜色
                    GUI.contentColor = positive ? new Color(0.15f, 0.7f, 0.4f) : new Color(0.9f, 0.5f, 0.3f);
                    EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
                    GUI.contentColor = prevColor;
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(hint, EditorStyles.wordWrappedMiniLabel);
            }

            _diagnosticCardStyle.normal.background = originalBg;

            if (onClick != null)
            {
                var rowRect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);
                var currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 &&
                    rowRect.Contains(currentEvent.mousePosition))
                {
                    onClick.Invoke();
                    currentEvent.Use();
                }
            }
        }

        private void DrawGuidedMode()
        {
            bool wideLayout = position.width >= WideLayoutThreshold;
            if (wideLayout)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        DrawQuickActions();
                        EditorGUILayout.Space(10);
                        DrawStepCards();
                    }

                    GUILayout.Space(10);

                    using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(320)))
                    {
                        DrawGitAssistantSupportCard();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawQuickActions();
                EditorGUILayout.Space(10);
                DrawStepCards();
                EditorGUILayout.Space(10);
                DrawGitAssistantSupportCard();
            }
        }

        private void DrawProfessionalMode()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("专业模式计划", _stepTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("自定义执行步骤与目标平台，适配已有项目结构。", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(12);

            _proIncludePackages = EditorGUILayout.ToggleLeft("XR 依赖检查 / 安装", _proIncludePackages);
            _proIncludeProjectSettings = EditorGUILayout.ToggleLeft("Project Settings：XR Plug-in 配置", _proIncludeProjectSettings);
            _proIncludeSceneConversion = EditorGUILayout.ToggleLeft("场景转换（XR Rig / VRRig）", _proIncludeSceneConversion);
            _proIncludeDeviceSimulator = EditorGUILayout.ToggleLeft("配置 VR 模拟设备（XR Device Simulator）", _proIncludeDeviceSimulator);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("目标平台", _stepTitleStyle);
            EditorGUILayout.Space(4);
            foreach (var group in TargetGroups)
            {
                bool current = _proTargetGroupToggles.TryGetValue(group, out var enabled) ? enabled : true;
                bool next = EditorGUILayout.ToggleLeft($"为 {group} 配置 XR", current);
                _proTargetGroupToggles[group] = next;
            }
            if (GetProfessionalTargetGroups().Length == 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("未选择目标平台时将回退到默认（Standalone + Android）。", MessageType.Info);
            }

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(!_proIncludeSceneConversion))
            {
                EditorGUILayout.LabelField("场景转换策略", _stepTitleStyle);
                _proRigStrategy = (RigStrategy)EditorGUILayout.EnumPopup(new GUIContent("Rig 策略", "选择如何处理 XR Origin / VRRig。"), _proRigStrategy);
                _proPreserveLegacyMainCamera = EditorGUILayout.ToggleLeft("保留现有 Main Camera（不强制禁用）", _proPreserveLegacyMainCamera);
            }

            EditorGUILayout.Space(12);
            if (GUILayout.Button("执行专业模式计划", _primaryButtonStyle))
            {
                RunProfessionalPlan();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            DrawGitAssistantSupportCard();
        }

        /// <summary>
        /// 快捷操作区域：一键执行主按钮。
        /// </summary>
        private void DrawQuickActions()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("快速开始（推荐）", _stepTitleStyle);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                "适合第一次接触 VR 项目的同学：点击一次即可按顺序执行所有必要步骤。" +
                "如果中途需要重新导入包，可以稍后再单独执行\"第 2 步\"。",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(12);

            var content = new GUIContent(
                "一键执行所有步骤（推荐）",
                "依次执行：\n" +
                "1. 检查并安装 XR Management / OpenXR / XR Interaction Toolkit；\n" +
                "2. 自动配置 XR Plug-in Management（Standalone + Android 启用 OpenXR）；\n" +
                "3. 将当前场景转换为 VR 场景并创建/更新 XR Rig。");

            if (GUILayout.Button(content, _primaryButtonStyle))
            {
                RunAllSteps();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("如果你不熟悉 XR 配置，推荐优先使用上面的\"一键执行\"按钮。", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 分步操作卡片：第 1 步（包依赖）+ 第 2 步（配置 & 场景）。
        /// </summary>
        private void DrawStepCards()
        {
            bool stackCards = position.width < WideLayoutThreshold;

            if (!stackCards)
            {
                EditorGUILayout.BeginHorizontal();
            }

            DrawStep1Card();

            if (stackCards)
            {
                EditorGUILayout.Space(8);
            }
            else
            {
                GUILayout.Space(8);
            }

            DrawStep2Card();

            if (!stackCards)
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawStep1Card()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("第 1 步：准备 XR 依赖包", _stepTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                "在 Packages/manifest.json 中检查并安装如下 XR 相关包：\n" +
                "- XR Management\n- OpenXR\n- XR Interaction Toolkit\n\n" +
                "适合刚将普通项目升级为 VR 项目时使用。",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(12);

            var btnStep1 = new GUIContent(
                "执行第 1 步",
                "仅执行 XR 依赖检查和安装，不会修改 XR 设置或场景。" +
                "\n建议在看到 Unity 编译完成后再继续执行第 2 步。");
            if (GUILayout.Button(btnStep1, _secondaryButtonStyle))
            {
                EnsureXrPackages();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStep2Card()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("第 2 步：配置项目 & 场景", _stepTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                "为 Standalone / Android 自动启用 OpenXR Loader，" +
                "并在当前场景内创建或更新 XR Origin（如可用）或基础 VRRig。\n\n" +
                "若你已经手动导入好 XR 包，可直接从第 2 步开始。",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                var btnStep2 = new GUIContent(
                    "执行第 2 步",
                    EditorApplication.isCompiling
                        ? "当前 Unity 正在编译，暂不可执行。请等待编译完成后再点击。"
                        : "配置 XRGeneralSettings / XRManagerSettings，并在当前场景中创建或更新 VR Rig。");

                if (GUILayout.Button(btnStep2, _secondaryButtonStyle))
                {
                    var plan = new ConversionPlan
                    {
                        EnsurePackages = false,
                        ConfigureProjectSettings = true,
                        ConvertScene = true,
                        ConfigureDeviceSimulator = true,
                        TargetGroups = TargetGroups,
                        RigStrategy = RigStrategy.Auto,
                        DisableLegacyCamera = true
                    };
                    ExecuteConversionPlan(plan, "\"第 2 步：配置项目 & 场景\"");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private ProjectDiagnostics GetProjectDiagnostics()
        {
            double now = EditorApplication.timeSinceStartup;
            if (_cachedDiagnostics != null && now - _lastDiagnosticsSampleTime < DiagnosticsRefreshSeconds)
            {
                return _cachedDiagnostics;
            }

            var diagnostics = new ProjectDiagnostics();

            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (pipelineAsset == null)
            {
                diagnostics.RenderPipelineLabel = "内置渲染管线";
                diagnostics.RenderPipelineHint = "使用 Built-in Render Pipeline，可直接使用模板配置。";
            }
            else
            {
                diagnostics.RenderPipelineLabel = pipelineAsset.GetType().Name.Replace("PipelineAsset", "");
                diagnostics.RenderPipelineHint = $"检测到 {pipelineAsset.name}，如使用 URP/HDRP，请确认对应 XR Renderer 已启用。";
            }

            diagnostics.HasXrManagement = FindType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management") != null;
            diagnostics.HasOpenXr = FindType(OpenXrLoaderTypeName) != null;
            diagnostics.HasXri = FindType(XrOriginTypeName) != null;
            diagnostics.HasNewInputSystem = FindType("UnityEngine.InputSystem.PlayerInput") != null;
            diagnostics.HasGitAssistant = IsGitAssistantInstalled();
            diagnostics.HasVrSimulator = IsVrSimulatorConfigured();

            _cachedDiagnostics = diagnostics;
            _lastDiagnosticsSampleTime = now;
            return diagnostics;
        }

        private void DrawGitAssistantSupportCard()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("版本控制助手联动（备份 & 回滚）", _stepTitleStyle);
            EditorGUILayout.Space(10);

            var gitAvailable = IsGitAssistantInstalled();
            var description = gitAvailable
                ? "检测到已安装版本控制助手：执行 VR 转换前请完成一次提交/标签备份，转换成功后可利用下方按钮快速回滚。"
                : "尚未检测到版本控制助手。建议先在 Package Manager 中导入 com.fire.gitassistant，以便执行自动备份与回滚。";
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(12);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!gitAvailable))
                {
                    if (GUILayout.Button("打开版本控制助手", _secondaryButtonStyle))
                    {
                        if (!OpenGitAssistantWindow())
                        {
                            EditorUtility.DisplayDialog("提示", "未能打开版本控制助手，请确认已正确安装。", "好的");
                        }
                    }

                    GUILayout.Space(8);
                    if (GUILayout.Button("使用版本控制助手快速备份", _secondaryButtonStyle))
                    {
                        TriggerQuickBackupFlow();
                    }
                }
            }

            EditorGUILayout.Space(10);

            bool canRollback = gitAvailable && _lastConversionSucceeded && !string.IsNullOrEmpty(_lastBaselineCommitHash);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!canRollback))
                {
                    if (GUILayout.Button("回滚到转换前版本", _secondaryButtonStyle))
                    {
                        AttemptRollbackToBaseline();
                    }
                }

                GUILayout.Space(8);
                var baselineLabel = canRollback
                    ? $"记录的提交：{GetShortHash(_lastBaselineCommitHash)}"
                    : "尚未记录可回滚的提交";
                EditorGUILayout.LabelField(baselineLabel, EditorStyles.wordWrappedMiniLabel);
            }

            if (!gitAvailable)
            {
                EditorGUILayout.HelpBox("安装版本控制助手后，可在此窗口中获得自动备份与回滚按钮。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 日志区域：滚动文本 + 简短说明，方便新手理解每一步发生了什么。
        /// </summary>
        private void DrawLogArea()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("执行日志（可帮助排查问题）", _stepTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                "这里会实时显示每一步执行情况，例如：\n" +
                "- 是否成功安装 XR 相关包；\n" +
                "- 是否成功启用 OpenXR Loader；\n" +
                "- 场景中是否成功创建 XR Origin / VRRig 等。\n" +
                "当你遇到问题时，可以先查看此处日志再处理。",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(8);

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.MinHeight(140));
            EditorGUILayout.TextArea(_log, _logTextStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void RunAllSteps()
        {
            var plan = new ConversionPlan
            {
                EnsurePackages = true,
                ConfigureProjectSettings = true,
                ConvertScene = true,
                ConfigureDeviceSimulator = true,
                TargetGroups = TargetGroups,
                RigStrategy = RigStrategy.Auto,
                DisableLegacyCamera = true
            };

            ExecuteConversionPlan(plan, "“一键执行所有步骤（推荐）”");
        }

        private void ExecuteConversionPlan(ConversionPlan plan, string actionName)
        {
            if (!plan.HasAnyOperation)
            {
                Log("未选择需要执行的步骤。");
                return;
            }

            if (!EnsureBackupReady(actionName))
            {
                Log($"用户取消执行 {actionName}，原因：尚未完成备份确认。");
                return;
            }

            _lastConversionSucceeded = false;

            if (plan.EnsurePackages)
            {
                EnsureXrPackages();
            }

            if ((plan.ConfigureProjectSettings || plan.ConvertScene) && EditorApplication.isCompiling)
            {
                Log("Unity 正在导入或编译脚本，请等待完成后再执行后续步骤。");
                return;
            }

            if (plan.ConfigureProjectSettings)
            {
                ConfigureXrProjectSettings(plan.TargetGroups);
            }

            if (plan.ConvertScene)
            {
                bool converted = ConvertCurrentSceneToVr(plan.RigStrategy, plan.DisableLegacyCamera);
                if (converted)
                {
                    HandleConversionCompleted();
                }
            }

            if (plan.ConfigureDeviceSimulator)
            {
                EnsureDeviceSimulatorConfigured();
            }
        }

        private void RunProfessionalPlan()
        {
            var targetGroups = GetProfessionalTargetGroups();
            bool shouldConvertScene = _proIncludeSceneConversion && _proRigStrategy != RigStrategy.SkipSceneChanges;

            var plan = new ConversionPlan
            {
                EnsurePackages = _proIncludePackages,
                ConfigureProjectSettings = _proIncludeProjectSettings,
                ConvertScene = shouldConvertScene,
                ConfigureDeviceSimulator = _proIncludeDeviceSimulator,
                TargetGroups = targetGroups.Length > 0 ? targetGroups : TargetGroups,
                RigStrategy = _proRigStrategy,
                DisableLegacyCamera = !_proPreserveLegacyMainCamera
            };

            if (!plan.HasAnyOperation)
            {
                EditorUtility.DisplayDialog("提示", "请至少勾选一个需要执行的步骤。", "好的");
                return;
            }

            ExecuteConversionPlan(plan, "“专业模式计划”");
        }

        private BuildTargetGroup[] GetProfessionalTargetGroups()
        {
            return _proTargetGroupToggles
                .Where(pair => pair.Value)
                .Select(pair => pair.Key)
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// 修改 Packages/manifest.json，确保 XR 相关依赖存在。
        /// </summary>
        private void EnsureXrPackages()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");

            if (!File.Exists(manifestPath))
            {
                Log("未找到 Packages/manifest.json，无法自动添加 XR 依赖。");
                return;
            }

            string text = File.ReadAllText(manifestPath);

            if (RemoveInvalidLatestVersionEntries(ref text))
            {
                File.WriteAllText(manifestPath, text);
                AssetDatabase.Refresh();
                Log("已移除 manifest.json 中的 \"latest\" 占位符，请等待 Unity 刷新。");
            }

            bool requestedInstall = false;
            foreach (var pkg in RequiredPackages)
            {
                if (TryGetManifestPackageVersion(text, pkg, out var version))
                {
                    if (IsValidManifestVersion(version))
                    {
                        Log($"已存在依赖：{pkg} ({version})");
                        continue;
                    }

                    Log($"检测到 {pkg} 使用无效版本 \"{version}\"，将重新安装。");
                }
                else
                {
                    Log($"manifest.json 中未找到 {pkg}，准备安装。");
                }

                QueuePackageInstall(pkg);
                requestedInstall = true;
            }

            if (!requestedInstall)
            {
                Log("所有必需 XR 包均已安装，无需额外操作。");
            }
        }

        /// <summary>
        /// 通过 XR Management API 自动为 Standalone/Android 启用 OpenXR Loader。
        /// </summary>
        private void ConfigureXrProjectSettings(IEnumerable<BuildTargetGroup> targetGroups = null)
        {
            var desiredGroups = (targetGroups ?? TargetGroups)?.Distinct().ToArray() ?? Array.Empty<BuildTargetGroup>();
            if (desiredGroups.Length == 0)
            {
                desiredGroups = TargetGroups;
            }

            var perBuildType = FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
            var generalType = FindType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management");
            var managerType = FindType("UnityEngine.XR.Management.XRManagerSettings, Unity.XR.Management");
            var metadataStoreType = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore, Unity.XR.Management.Editor");

            if (perBuildType == null || generalType == null || managerType == null)
            {
                Log("未检测到 XR Management 程序集，可能仍在导入。跳过自动配置。");
                return;
            }

            if (FindType(OpenXrLoaderTypeName) == null)
            {
                Log("未检测到 OpenXR Loader 类型，请确认 com.unity.xr.openxr 包已经导入。");
                return;
            }

            var perBuildAsset = GetOrCreateGeneralSettingsAsset(perBuildType);
            RegisterXrSettingsConfig(perBuildAsset, perBuildType);

            var getMethod = perBuildType.GetMethod("SettingsForBuildTarget", new[] { typeof(BuildTargetGroup) });
            var setMethod = perBuildType.GetMethod("SetSettingsForBuildTarget", new[] { typeof(BuildTargetGroup), generalType });

            if (getMethod == null || setMethod == null)
            {
                Log("XRGeneralSettingsPerBuildTarget API 发生变化，无法自动设置。");
                return;
            }

            foreach (var targetGroup in desiredGroups)
            {
                var generalSettings = GetOrCreateGeneralSettings(perBuildAsset, targetGroup, generalType, getMethod, setMethod);
                if (generalSettings == null)
                {
                    Log($"无法为 {targetGroup} 创建 XR General Settings。");
                    continue;
                }

                EnsureGeneralSettingsDefaults(generalSettings);
                var managerSettings = GetOrCreateManagerSettings(generalSettings, managerType);
                if (managerSettings == null)
                {
                    Log($"无法为 {targetGroup} 创建 XR Manager Settings。");
                    continue;
                }

                if (AssignOpenXrLoader(managerSettings, managerType, metadataStoreType, targetGroup))
                {
                    Log($"已为 {targetGroup} 启用 OpenXR Loader。");
                }
                else
                {
                    Log($"未能为 {targetGroup} 自动绑定 OpenXR Loader，请手动在 Project Settings 中确认。");
                }
            }

            AssetDatabase.SaveAssets();
        }

        private void EnsureDeviceSimulatorConfigured()
        {
            if (!TryGetDeviceSimulatorSettings(true, out var settings, out var settingsType))
            {
                return;
            }

            var autoProp = settingsType.GetProperty("automaticallyInstantiateSimulatorPrefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var prefabProp = settingsType.GetProperty("simulatorPrefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var editorOnlyProp = settingsType.GetProperty("automaticallyInstantiateInEditorOnly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (autoProp == null || prefabProp == null)
            {
                Log("XR Device Simulator 设置不可写，已跳过自动配置。");
                return;
            }

            var prefab = FindOrCreateDeviceSimulatorPrefab();
            if (prefab == null)
            {
                Log("未能找到 XR Device Simulator 预制体，请在 Package Manager 中重新导入 XR Device Simulator Sample。");
                PromptDeviceSimulatorSampleImport();
                return;
            }

            bool updated = false;
            bool autoInstantiate = Convert.ToBoolean(autoProp.GetValue(settings));
            if (!autoInstantiate)
            {
                autoProp.SetValue(settings, true);
                updated = true;
            }

            if (editorOnlyProp != null && !(bool)editorOnlyProp.GetValue(settings))
            {
                editorOnlyProp.SetValue(settings, true);
                updated = true;
            }

            var currentPrefab = prefabProp.GetValue(settings) as GameObject;
            if (currentPrefab == null || currentPrefab != prefab)
            {
                prefabProp.SetValue(settings, prefab);
                updated = true;
            }

            if (updated)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Log("已配置 XR Device Simulator，进入 Play 模式即可使用键鼠模拟 VR 设备。");
            }
            else
            {
                Log("XR Device Simulator 已处于可用状态。");
            }
        }

        private bool TryGetDeviceSimulatorSettings(bool logOnFailure, out ScriptableObject settings, out Type settingsType)
        {
            settings = null;
            settingsType = FindType(DeviceSimulatorSettingsTypeName);
            if (settingsType == null)
            {
                if (logOnFailure)
                {
                    Log("当前 XR Interaction Toolkit 版本缺少 XR Device Simulator 设置，已跳过模拟设备配置。");
                }
                return false;
            }

            var instanceProp = settingsType.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instanceProp != null)
            {
                settings = instanceProp.GetValue(null) as ScriptableObject;
            }

            if (settings == null)
            {
                var scriptableSettingsBaseType = FindType(ScriptableSettingsBaseTypeName);
                var getInstanceMethod = scriptableSettingsBaseType?.GetMethod(
                    "GetInstanceByType",
                    BindingFlags.Public | BindingFlags.Static);
                if (getInstanceMethod != null)
                {
                    try
                    {
                        settings = getInstanceMethod.Invoke(null, new object[] { settingsType }) as ScriptableObject;
                    }
                    catch (Exception ex)
                    {
                        if (logOnFailure)
                        {
                            Log($"无法初始化 XR Device Simulator 设置：{ex.Message}");
                        }
                    }
                }
            }

            if (settings == null)
            {
                if (logOnFailure)
                {
                    Log("未能创建 XR Device Simulator 设置实例。");
                }
                return false;
            }

            return true;
        }

        private bool IsVrSimulatorConfigured()
        {
            if (!TryGetDeviceSimulatorSettings(false, out var settings, out var settingsType))
            {
                return false;
            }

            var autoProp = settingsType.GetProperty("automaticallyInstantiateSimulatorPrefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var prefabProp = settingsType.GetProperty("simulatorPrefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (autoProp == null || prefabProp == null)
            {
                return false;
            }

            bool autoInstantiate;
            GameObject prefab;
            try
            {
                autoInstantiate = Convert.ToBoolean(autoProp.GetValue(settings));
            }
            catch
            {
                autoInstantiate = false;
            }

            try
            {
                prefab = prefabProp.GetValue(settings) as GameObject;
            }
            catch
            {
                prefab = null;
            }

            return autoInstantiate && prefab != null;
        }

        private GameObject FindOrCreateDeviceSimulatorPrefab()
        {
            foreach (var guid in AssetDatabase.FindAssets("XR Device Simulator t:Prefab"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(DeviceSimulatorPrefabName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    return prefab;
                }
            }

            var generatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedSimulatorPrefabPath);
            if (generatedPrefab != null)
            {
                return generatedPrefab;
            }

            if (!TryCopyDeviceSimulatorSample())
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedSimulatorPrefabPath);
        }

        private bool TryCopyDeviceSimulatorSample()
        {
            if (TryCopyPackageSampleFolder(DeviceSimulatorPackageId, DeviceSimulatorSampleRelativePath, GeneratedSimulatorFolder, "XR Device Simulator"))
            {
                Log("已自动导入 XR Device Simulator Sample，位于 Assets/VRConverterGenerated/DeviceSimulator。");
                return true;
            }

            return false;
        }

        private bool TryEnsureStarterAssetsSampleAvailable()
        {
            var existing = AssetDatabase.FindAssets("\"XRI Default Input Actions\" t:InputActionAsset");
            if (existing != null && existing.Length > 0)
            {
                return true;
            }

            if (TryCopyPackageSampleFolder(DeviceSimulatorPackageId, StarterAssetsSampleRelativePath, GeneratedStarterAssetsFolder, StarterAssetsSampleDisplayName))
            {
                Log("已复制 Starter Assets Sample，位于 Assets/VRConverterGenerated/StarterAssets。");
                return true;
            }

            return false;
        }

        private bool TryCopyPackageSampleFolder(string packageAssetPath, string sampleRelativePath, string destinationFolder, string sampleDisplayName)
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageAssetPath);
            if (packageInfo == null)
            {
                Log($"未能定位 {sampleDisplayName} 所在的包（{packageAssetPath}）。");
                return false;
            }

            var sourcePath = Path.Combine(packageInfo.resolvedPath, sampleRelativePath);
            if (!Directory.Exists(sourcePath))
            {
                Log($"在 {packageInfo.resolvedPath} 中未找到 {sampleDisplayName} Sample（路径：{sampleRelativePath}）。");
                return false;
            }

            EnsureDirectoryExists(GeneratedRootFolder);

            var destinationPath = Path.GetFullPath(destinationFolder);
            if (Directory.Exists(destinationPath))
            {
                FileUtil.DeleteFileOrDirectory(destinationPath);
                var meta = destinationPath + ".meta";
                if (File.Exists(meta))
                {
                    FileUtil.DeleteFileOrDirectory(meta);
                }
            }

            FileUtil.CopyFileOrDirectory(sourcePath, destinationPath);
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// 根据项目当前安装的包尝试创建 XR Origin；若 XR Interaction Toolkit 不可用，则回退到基础 VR Rig。
        /// </summary>
        private bool ConvertCurrentSceneToVr(RigStrategy strategy = RigStrategy.Auto, bool disableLegacyMainCamera = true)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Log("当前没有打开的场景，无法转换。");
                return false;
            }

            if (strategy == RigStrategy.SkipSceneChanges)
            {
                Log("已根据专业模式设置，跳过场景转换步骤。");
                return false;
            }

            LegacyCameraBindingInfo bindingInfo = LegacyCameraBindingInfo.Empty;
            if (disableLegacyMainCamera)
            {
                bindingInfo = DisableLegacyMainCamera();
            }
            else
            {
                Log("专业模式：保留原 Main Camera，不自动禁用。");
            }

            switch (strategy)
            {
                case RigStrategy.OnlyUpdateExistingXrOrigin:
                    if (TryCreateOrUpdateXrOriginRig(createIfMissing: false, bindingInfo: bindingInfo))
                    {
                        Log("已更新场景中的 XR Origin。");
                        return true;
                    }

                    Log("未找到现有 XR Origin，且策略为\"仅更新\"，未做额外改动。");
                    return false;
                case RigStrategy.ForceFallbackRig:
                    CreateFallbackVrRig(bindingInfo);
                    return true;
                default:
                    break;
            }

            if (TryCreateOrUpdateXrOriginRig(bindingInfo: bindingInfo))
            {
                Log("XR Origin (XR Interaction Toolkit) 已创建/更新。");
                return true;
            }

            Log("XR Interaction Toolkit 或 XR Core Utils 不可用，使用基础 VRRig。");
            CreateFallbackVrRig(bindingInfo);
            return true;
        }

        private void HandleConversionCompleted()
        {
            _lastConversionSucceeded = true;
            Log("VR 场景转换完成，可以使用下方 Git 按钮创建备份或回滚。");
            if (!string.IsNullOrEmpty(_lastBaselineCommitHash))
            {
                Log($"已记录转换前的 Git 提交：{_lastBaselineCommitHash}");
            }
            
            // 显示用户友好的成功提示对话框
            string successMessage = "🎉 VR 转换成功！\n\n" +
                "您的项目已成功转换为 VR 项目。\n\n" +
                "主要变更：\n" +
                "• XR Origin 已添加到场景\n" +
                "• 左右手控制器已配置\n" +
                "• 项目设置已更新为 VR 模式\n" +
                "• 手部追踪同步已优化\n\n";
            
            if (!string.IsNullOrEmpty(_lastBaselineCommitHash))
            {
                successMessage += $"已记录转换前的 Git 提交：{_lastBaselineCommitHash}\n\n";
            }
            
            successMessage += "提示：可以使用窗口下方的 Git 按钮创建备份或回滚。";
            
            EditorUtility.DisplayDialog("转换成功", successMessage, "好的，我知道了");
        }

        #region XR Project Settings helpers

        private ScriptableObject GetOrCreateGeneralSettingsAsset(Type perBuildType)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GeneratedGeneralSettingsAsset);
            if (asset != null)
            {
                if (perBuildType.IsInstanceOfType(asset))
                {
                    return asset;
                }
                AssetDatabase.DeleteAsset(GeneratedGeneralSettingsAsset);
            }

            EnsureDirectoryExists(GeneratedSettingsFolder);
            asset = ScriptableObject.CreateInstance(perBuildType);
            asset.name = "XRGeneralSettingsPerBuildTarget";
            AssetDatabase.CreateAsset(asset, GeneratedGeneralSettingsAsset);
            AssetDatabase.SaveAssets();
            Log($"已创建 XRGeneralSettingsPerBuildTarget 资产：{GeneratedGeneralSettingsAsset}");
            return asset;
        }

        private void RegisterXrSettingsConfig(ScriptableObject perBuildAsset, Type perBuildType)
        {
            var keyField = perBuildType.GetField("k_SettingsKey", BindingFlags.Public | BindingFlags.Static);
            var key = keyField?.GetValue(null) as string;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            EditorBuildSettings.TryGetConfigObject(key, out ScriptableObject existing);
            if (existing != perBuildAsset)
            {
                EditorBuildSettings.AddConfigObject(key, perBuildAsset, true);
                Log($"已注册 XR General Settings（{key}）。");
            }
        }

        private ScriptableObject GetOrCreateGeneralSettings(ScriptableObject perBuildAsset, BuildTargetGroup targetGroup, Type generalType, MethodInfo getMethod, MethodInfo setMethod)
        {
            var existing = getMethod.Invoke(perBuildAsset, new object[] { targetGroup }) as ScriptableObject;
            if (existing != null)
            {
                return existing;
            }

            var general = ScriptableObject.CreateInstance(generalType) as ScriptableObject;
            if (general == null)
            {
                return null;
            }

            general.name = $"{targetGroup} XR General Settings";
            AssetDatabase.AddObjectToAsset(general, perBuildAsset);
            setMethod.Invoke(perBuildAsset, new object[] { targetGroup, general });
            EditorUtility.SetDirty(perBuildAsset);
            Log($"已创建 {targetGroup} 的 XR General Settings。");
            return general;
        }

        private void EnsureGeneralSettingsDefaults(ScriptableObject generalSettings)
        {
            if (generalSettings == null) return;

            var so = new SerializedObject(generalSettings);
            var initProp = so.FindProperty("m_InitManagerOnStart");
            if (initProp != null && !initProp.boolValue)
            {
                initProp.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private ScriptableObject GetOrCreateManagerSettings(ScriptableObject generalSettings, Type managerType)
        {
            if (generalSettings == null) return null;
            if (managerType == null) return null;

            var so = new SerializedObject(generalSettings);
            var managerProp = so.FindProperty("m_LoaderManagerInstance");
            if (managerProp == null)
            {
                return null;
            }

            var manager = managerProp.objectReferenceValue as ScriptableObject;
            if (manager != null)
            {
                return manager;
            }

            manager = ScriptableObject.CreateInstance(managerType) as ScriptableObject;
            if (manager == null)
            {
                return null;
            }

            manager.name = $"{generalSettings.name}_XRManagerSettings";
            AssetDatabase.AddObjectToAsset(manager, generalSettings);
            managerProp.objectReferenceValue = manager;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(generalSettings);
            Log($"已创建 {generalSettings.name} 对应的 XR Manager Settings。");
            return manager;
        }

        private bool AssignOpenXrLoader(UnityEngine.Object managerSettings, Type managerType, Type metadataStoreType, BuildTargetGroup targetGroup)
        {
            if (managerSettings == null || managerType == null)
            {
                return false;
            }

            var loaderType = FindType(OpenXrLoaderTypeName);
            if (loaderType == null)
            {
                return false;
            }

            if (IsLoaderAlreadyAssigned(managerSettings, loaderType))
            {
                return true;
            }

            if (metadataStoreType != null)
            {
                var assignMethod = metadataStoreType.GetMethod(
                    "AssignLoader",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { managerType, typeof(string), typeof(BuildTargetGroup) },
                    null);

                if (assignMethod != null)
                {
                    try
                    {
                        var result = assignMethod.Invoke(null, new[] { managerSettings, loaderType.FullName, (object)targetGroup });
                        if (result is bool assigned && assigned)
                        {
                            EditorUtility.SetDirty(managerSettings);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"AssignLoader 调用失败：{ex.Message}");
                    }
                }
            }

            return IsLoaderAlreadyAssigned(managerSettings, loaderType);
        }

        private bool IsLoaderAlreadyAssigned(UnityEngine.Object managerSettings, Type loaderType)
        {
            var managerSO = new SerializedObject(managerSettings);
            var loadersProp = managerSO.FindProperty("m_Loaders");
            if (loadersProp == null)
            {
                return false;
            }

            for (int i = 0; i < loadersProp.arraySize; i++)
            {
                var element = loadersProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null) continue;
                if (element.objectReferenceValue.GetType().FullName == loaderType.FullName)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Scene conversion helpers

        private bool TryCreateOrUpdateXrOriginRig(bool createIfMissing = true, LegacyCameraBindingInfo bindingInfo = default)
        {
            var xrOriginType = FindType(XrOriginTypeName);
            if (xrOriginType == null)
            {
                return false;
            }

            Component existingOrigin = FindComponentInScene(xrOriginType);
            if (existingOrigin != null)
            {
                EnsureOriginStructure(existingOrigin.gameObject);
                // 如果已有 XR Origin 但原 Main Camera 有父对象，尝试绑定
                if (bindingInfo.IsValid && bindingInfo.Parent != null && existingOrigin.transform.parent != bindingInfo.Parent)
                {
                    ApplyBindingInfo(existingOrigin.gameObject, bindingInfo);
                }
                return true;
            }

            if (!createIfMissing)
            {
                return false;
            }

            var originGo = new GameObject("XR Origin (Action Based)");
            Undo.RegisterCreatedObjectUndo(originGo, "Create XR Origin");
            
            // 应用原 Main Camera 的绑定信息
            ApplyBindingInfo(originGo, bindingInfo);
            
            var originComponent = originGo.AddComponent(xrOriginType);

            BuildOriginHierarchy(originGo, originComponent);
            EnsureInteractionManagers();
            return true;
        }

        private void EnsureOriginStructure(GameObject originGo)
        {
            var originComponent = originGo.GetComponent(FindType(XrOriginTypeName));
            BuildOriginHierarchy(originGo, originComponent);
            EnsureInteractionManagers();
        }

        private void BuildOriginHierarchy(GameObject originGo, Component originComponent)
        {
            if (originComponent == null) return;

            var cameraOffset = originGo.transform.Find("Camera Offset")?.gameObject ?? CreateChild(originGo.transform, "Camera Offset");
            var cameraGo = cameraOffset.transform.Find("Main Camera")?.gameObject ?? CreateChild(cameraOffset.transform, "Main Camera");
            cameraGo.tag = "MainCamera";

            if (cameraGo.GetComponent<Camera>() == null)
            {
                cameraGo.AddComponent<Camera>();
            }

            if (cameraGo.GetComponent<AudioListener>() == null)
            {
                cameraGo.AddComponent<AudioListener>();
            }

            TryAddComponent(cameraGo, TrackedPoseDriverTypeName);

            var leftHand = cameraOffset.transform.Find("LeftHand Controller")?.gameObject ?? CreateChild(cameraOffset.transform, "LeftHand Controller");
            var rightHand = cameraOffset.transform.Find("RightHand Controller")?.gameObject ?? CreateChild(cameraOffset.transform, "RightHand Controller");

            TrySetupActionController(leftHand);
            TrySetupActionController(rightHand, isRightHand: true);

            var originSO = new SerializedObject(originComponent);
            var floorProp = originSO.FindProperty("m_CameraFloorOffsetObject");
            if (floorProp != null) floorProp.objectReferenceValue = cameraOffset;
            var camProp = originSO.FindProperty("m_CameraGameObject");
            if (camProp != null) camProp.objectReferenceValue = cameraGo;
            var trackablesProp = originSO.FindProperty("m_TrackablesParent");
            if (trackablesProp != null) trackablesProp.objectReferenceValue = cameraOffset;
            originSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private void EnsureInteractionManagers()
        {
            TryEnsureSingletonComponent(XrInteractionManagerTypeName, "XR Interaction Manager");
            var inputManager = TryEnsureSingletonComponent(InputActionManagerTypeName, "XR Input Action Manager");
            ConfigureInputActionManager(inputManager);
        }

        private Component TryEnsureSingletonComponent(string typeName, string defaultObjectName)
        {
            var type = FindType(typeName);
            if (type == null)
            {
                return null;
            }

            var existing = FindComponentInScene(type);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(defaultObjectName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {defaultObjectName}");
            var component = go.AddComponent(type);
            Log($"已创建 {defaultObjectName}。");
            return component;
        }

        private void TrySetupActionController(GameObject controllerGo, bool isRightHand = false)
        {
            if (controllerGo == null) return;

            var controller = TryAddComponent(controllerGo, ActionBasedControllerTypeName);
            var rayInteractor = TryAddComponent(controllerGo, XrRayInteractorTypeName);
            ConfigureActionBasedController(controller, isRightHand);

            // 优化 LineRenderer 配置，确保与相机同步
            if (controllerGo.GetComponent<LineRenderer>() == null)
            {
                var lr = controllerGo.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = false;
                lr.widthMultiplier = 0.005f;
            }

            // 配置 XR Ray Interactor 的同步参数，确保手部追踪与相机同步
            if (rayInteractor != null)
            {
                var raySo = new SerializedObject(rayInteractor);
                
                // 保持 XR Ray Interactor 的默认设置，不进行修改
                // 让 Unity 的 XR Ray Interactor 自己处理同步逻辑
                
                raySo.ApplyModifiedPropertiesWithoutUndo();
                
                // 尝试添加同步优化脚本（如果存在）
                var syncScriptType = Type.GetType("VRHandTrackingSync, Assembly-CSharp");
                if (syncScriptType != null && controllerGo.GetComponent(syncScriptType) == null)
                {
                    controllerGo.AddComponent(syncScriptType);
                    Log($"已为 {controllerGo.name} 添加手部追踪同步优化脚本。");
                }
            }

            if (controller == null) return;

            var so = new SerializedObject(controller);
            var handedProp = so.FindProperty("m_SelectUsage");
            if (handedProp != null)
            {
                handedProp.intValue = isRightHand ? 1 : 0;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void CreateFallbackVrRig(LegacyCameraBindingInfo bindingInfo = default)
        {
            var existingRig = GameObject.Find("VRRig");
            if (existingRig == null)
            {
                existingRig = new GameObject("VRRig");
                Undo.RegisterCreatedObjectUndo(existingRig, "Create VRRig");
                
                // 应用原 Main Camera 的绑定信息
                ApplyBindingInfo(existingRig, bindingInfo);
                
                Log("已创建 VRRig 根对象。");
            }
            else
            {
                Log("场景中已存在 VRRig，对其进行复用/更新。");
                // 如果原 Main Camera 有父对象且当前 VRRig 没有绑定，尝试绑定
                if (bindingInfo.IsValid && bindingInfo.Parent != null && existingRig.transform.parent != bindingInfo.Parent)
                {
                    ApplyBindingInfo(existingRig, bindingInfo);
                }
            }

            var cameraOffset = existingRig.transform.Find("CameraOffset")?.gameObject ?? CreateChild(existingRig.transform, "CameraOffset");
            var mainCamera = cameraOffset.transform.Find("Main Camera")?.gameObject ?? CreateChild(cameraOffset.transform, "Main Camera");
            mainCamera.tag = "MainCamera";
            if (mainCamera.GetComponent<Camera>() == null)
            {
                mainCamera.AddComponent<Camera>();
            }

            CreateChild(existingRig.transform, "LeftHand Controller");
            CreateChild(existingRig.transform, "RightHand Controller");

            Log("基础 VR Rig 已创建/更新。");
        }

        private GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>
        /// 将新的 VR Rig 绑定到原 Main Camera 的父对象，并保持相对位置和旋转
        /// </summary>
        private void ApplyBindingInfo(GameObject targetGo, LegacyCameraBindingInfo bindingInfo)
        {
            if (!bindingInfo.IsValid || bindingInfo.Parent == null)
            {
                return;
            }

            // 将 XR Origin/VRRig 设置为原 Main Camera 的父对象的子对象
            targetGo.transform.SetParent(bindingInfo.Parent, false);
            
            // 保持原 Main Camera 的本地位置和旋转（相对于父对象）
            // 注意：这里使用原 Main Camera 的本地变换，因为 XR Origin 的 Camera Offset 会处理 VR 相关的偏移
            targetGo.transform.localPosition = bindingInfo.LocalPosition;
            targetGo.transform.localRotation = bindingInfo.LocalRotation;
            
            Log($"已将 {targetGo.name} 绑定到原主相机的父对象：{bindingInfo.Parent.name}，并保持原位置和旋转。");
        }

        /// <summary>
        /// 禁用原 Main Camera 并返回其绑定信息，用于后续将 XR Origin 绑定到原父对象
        /// </summary>
        private LegacyCameraBindingInfo DisableLegacyMainCamera()
        {
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                Log("未找到标签为 MainCamera 的原主相机。");
                return LegacyCameraBindingInfo.Empty;
            }

            var bindingInfo = new LegacyCameraBindingInfo
            {
                Parent = mainCam.transform.parent,
                LocalPosition = mainCam.transform.localPosition,
                LocalRotation = mainCam.transform.localRotation,
                IsValid = true
            };

            mainCam.gameObject.SetActive(false);
            Log($"已禁用原主相机：{mainCam.gameObject.name}");
            
            if (bindingInfo.Parent != null)
            {
                Log($"检测到原主相机绑定在：{bindingInfo.Parent.name}，新的 XR Origin 将继承此绑定关系。");
            }

            return bindingInfo;
        }

        private Component TryAddComponent(GameObject target, string typeName)
        {
            var type = FindType(typeName);
            if (type == null)
            {
                return null;
            }

            if (!typeof(Component).IsAssignableFrom(type))
            {
                return null;
            }

            var existing = target.GetComponent(type);
            if (existing != null)
            {
                return existing;
            }

            return Undo.AddComponent(target, type);
        }

        private Component FindComponentInScene(Type type)
        {
            var obj = UnityEngine.Object.FindObjectOfType(type);
            return obj as Component;
        }

        #endregion

        #region Sample import helpers

        private void PromptStarterAssetsImport()
        {
            if (TryEnsureStarterAssetsSampleAvailable())
            {
                EditorUtility.DisplayDialog("提示", "已自动复制 Starter Assets Sample，稍后可重新执行操作。", "好的");
                return;
            }

            const string packageName = "com.unity.xr.interaction.toolkit";
            const string sampleName = "Starter Assets";
            PromptSampleImport(
                ref _promptedStarterAssets,
                "缺少 XRI Starter Assets",
                "为了绑定默认输入，需要导入 XR Interaction Toolkit 自带的 Starter Assets（其中包含 “XRI Default Input Actions.inputactions”）。\n\n是否现在导入？",
                packageName,
                sampleName);
        }

        private void PromptDeviceSimulatorSampleImport()
        {
            const string packageName = "com.unity.xr.interaction.toolkit";
            const string sampleName = "XR Device Simulator";
            PromptSampleImport(
                ref _promptedDeviceSimulatorSample,
                "缺少 XR Device Simulator Sample",
                "若要启用键鼠模拟 VR 设备，需要导入 XR Device Simulator Sample。\n\n是否现在导入？",
                packageName,
                sampleName);
        }

        private void PromptSampleImport(ref bool promptFlag, string title, string message, string packageName, string sampleDisplayName)
        {
            if (promptFlag)
            {
                return;
            }

            promptFlag = true;
            int option = EditorUtility.DisplayDialogComplex(
                title,
                message,
                "一键导入（推荐）",
                "稍后处理",
                "打开 Package Manager");

            switch (option)
            {
                case 0:
                    if (TryImportPackageSample(packageName, sampleDisplayName))
                    {
                        EditorUtility.DisplayDialog("导入完成", $"{sampleDisplayName} Sample 已导入，工具会在下一次执行时自动继续。", "好的");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("自动导入失败", $"未能自动导入 {sampleDisplayName} Sample，将尝试打开 Package Manager。", "好的");
                        TryOpenPackageManagerSamples(packageName, sampleDisplayName);
                    }
                    break;
                case 1:
                    Log($"用户选择稍后导入 {sampleDisplayName} Sample。");
                    break;
                case 2:
                    TryOpenPackageManagerSamples(packageName, sampleDisplayName);
                    break;
            }
        }

        private bool TryImportPackageSample(string packageName, string sampleDisplayName)
        {
            var sampleType = FindType("UnityEditor.PackageManager.Sample, UnityEditor.PackageManagerUIModule")
                             ?? FindType("UnityEditor.PackageManager.Sample");
            if (sampleType == null)
            {
                return false;
            }

            var findMethods = sampleType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "FindByPackage")
                .ToArray();
            if (findMethods.Length == 0)
            {
                return false;
            }

            object samplesObj = null;
            foreach (var method in findMethods)
            {
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                if (parameters.Length > 0)
                {
                    args[0] = packageName;
                }

                try
                {
                    samplesObj = method.Invoke(null, args);
                    if (samplesObj != null)
                    {
                        break;
                    }
                }
                catch
                {
                    // 忽略并尝试下一个重载
                }
            }

            if (samplesObj == null)
            {
                return false;
            }

            if (!(samplesObj is IEnumerable enumerable))
            {
                return false;
            }

            var displayNameProp = sampleType.GetProperty("displayName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            var importMethod = sampleType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "Import");

            if (displayNameProp == null || importMethod == null)
            {
                return false;
            }

            foreach (var sample in enumerable)
            {
                if (sample == null)
                {
                    continue;
                }

                var displayName = displayNameProp.GetValue(sample) as string;
                if (!string.Equals(displayName, sampleDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var importParams = importMethod.GetParameters();
                var args = importParams.Length == 0 ? null : new object[importParams.Length];
                try
                {
                    importMethod.Invoke(sample, args);
                    AssetDatabase.Refresh();
                    Log($"已自动导入 {sampleDisplayName} Sample。");
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"自动导入 {sampleDisplayName} 失败：{ex.Message}");
                    return false;
                }
            }

            return false;
        }

        private bool TryOpenPackageManagerSamples(string packageName, string sampleDisplayName)
        {
            if (TryInvokePackageManagerOpen(packageName))
            {
                var msg = $"已打开 Package Manager，已尝试定位 {packageName}。请在 Samples 面板中导入 {sampleDisplayName}。";
                Log(msg);
                EditorUtility.DisplayDialog("已打开 Package Manager", msg, "好的");
                return true;
            }

            if (EditorApplication.ExecuteMenuItem("Window/Package Manager"))
            {
                var fallbackMsg = $"已打开 Package Manager。请手动选择 {packageName} 并导入 {sampleDisplayName} Sample。";
                Log(fallbackMsg);
                EditorUtility.DisplayDialog("请手动导入", fallbackMsg, "好的");
                return true;
            }

            Log("未能自动打开 Package Manager，请通过菜单 Window/Package Manager 手动打开。");
            EditorUtility.DisplayDialog("无法打开 Package Manager", "请从菜单 Window/Package Manager 手动打开，然后导入所需 Sample。", "好的");
            return false;
        }

        private bool TryInvokePackageManagerOpen(string packageName)
        {
            var typeNames = new[]
            {
                "UnityEditor.PackageManager.UI.Window, UnityEditor.PackageManagerUIModule",
                "UnityEditor.PackageManager.UI.PackageManagerWindow, UnityEditor.PackageManagerUIModule",
                "UnityEditor.PackageManager.UI.Window"
            };

            foreach (var typeName in typeNames)
            {
                var windowType = FindType(typeName);
                if (windowType == null)
                {
                    continue;
                }

                var openMethods = windowType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "Open")
                    .ToArray();

                foreach (var method in openMethods)
                {
                    var parameters = method.GetParameters();
                    try
                    {
                        if (parameters.Length == 0)
                        {
                            method.Invoke(null, null);
                            return true;
                        }

                        var args = new object[parameters.Length];
                        args[0] = packageName;
                        for (int i = 1; i < args.Length; i++)
                        {
                            args[i] = null;
                        }

                        method.Invoke(null, args);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log($"打开 Package Manager 失败：{ex.Message}");
                    }
                }
            }

            return false;
        }

        #endregion

        #region Input Action helpers

        private struct ControllerActionBinding
        {
            public string PropertyName;
            public IReadOnlyList<string> MapNames;
            public IReadOnlyList<string> ActionNames;

            public ControllerActionBinding(string propertyName, string mapName, string actionName)
                : this(propertyName, new[] { mapName }, new[] { actionName })
            {
            }

            public ControllerActionBinding(string propertyName, IReadOnlyList<string> mapNames, string actionName)
                : this(propertyName, mapNames, new[] { actionName })
            {
            }

            public ControllerActionBinding(string propertyName, IReadOnlyList<string> mapNames, IReadOnlyList<string> actionNames)
            {
                PropertyName = propertyName;
                MapNames = mapNames;
                ActionNames = actionNames;
            }
        }

        private static readonly string[] LeftControllerMapCandidates = { "XRI Left", "XRI LeftHand" };
        private static readonly string[] LeftInteractionMapCandidates = { "XRI Left Interaction", "XRI LeftHand Interaction" };
        private static readonly string[] RightControllerMapCandidates = { "XRI Right", "XRI RightHand" };
        private static readonly string[] RightInteractionMapCandidates = { "XRI Right Interaction", "XRI RightHand Interaction" };

        private static bool IsMatchingActionMapName(string actualName, string candidateName)
        {
            if (string.IsNullOrEmpty(actualName) || string.IsNullOrEmpty(candidateName))
            {
                return false;
            }

            if (string.Equals(actualName, candidateName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(NormalizeActionMapName(actualName), NormalizeActionMapName(candidateName), StringComparison.Ordinal);
        }

        private static string NormalizeActionMapName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(name.Length);
            foreach (var ch in name)
            {
                if (!char.IsWhiteSpace(ch))
                {
                    builder.Append(char.ToLowerInvariant(ch));
                }
            }

            builder.Replace("hand", string.Empty);
            return builder.ToString();
        }

        private static readonly ControllerActionBinding[] LeftControllerBindings =
        {
            new ControllerActionBinding("m_PositionAction", LeftControllerMapCandidates, "Position"),
            new ControllerActionBinding("m_RotationAction", LeftControllerMapCandidates, "Rotation"),
            new ControllerActionBinding("m_IsTrackedAction", LeftControllerMapCandidates, "Is Tracked"),
            new ControllerActionBinding("m_TrackingStateAction", LeftControllerMapCandidates, "Tracking State"),
            new ControllerActionBinding("m_SelectAction", LeftInteractionMapCandidates, "Select"),
            new ControllerActionBinding("m_SelectActionValue", LeftInteractionMapCandidates, "Select Value"),
            new ControllerActionBinding("m_ActivateAction", LeftInteractionMapCandidates, "Activate"),
            new ControllerActionBinding("m_ActivateActionValue", LeftInteractionMapCandidates, "Activate Value"),
            new ControllerActionBinding("m_UIPressAction", LeftInteractionMapCandidates, "UI Press"),
            new ControllerActionBinding("m_UIPressActionValue", LeftInteractionMapCandidates, "UI Press Value"),
            new ControllerActionBinding("m_UIScrollAction", LeftInteractionMapCandidates, "UI Scroll"),
            new ControllerActionBinding("m_HapticDeviceAction", LeftControllerMapCandidates, "Haptic Device"),
            new ControllerActionBinding("m_RotateAnchorAction", LeftInteractionMapCandidates, new[] { "Rotate Manipulation", "Rotate Anchor" }),
            new ControllerActionBinding("m_DirectionalAnchorRotationAction", LeftInteractionMapCandidates, new[] { "Directional Manipulation", "Directional Anchor Rotation" }),
            new ControllerActionBinding("m_TranslateAnchorAction", LeftInteractionMapCandidates, new[] { "Translate Manipulation", "Translate Anchor" }),
            new ControllerActionBinding("m_ScaleToggleAction", LeftInteractionMapCandidates, "Scale Toggle"),
            new ControllerActionBinding("m_ScaleDeltaAction", LeftInteractionMapCandidates, new[] { "Scale Over Time", "Scale Delta" })
        };

        private static readonly ControllerActionBinding[] RightControllerBindings =
        {
            new ControllerActionBinding("m_PositionAction", RightControllerMapCandidates, "Position"),
            new ControllerActionBinding("m_RotationAction", RightControllerMapCandidates, "Rotation"),
            new ControllerActionBinding("m_IsTrackedAction", RightControllerMapCandidates, "Is Tracked"),
            new ControllerActionBinding("m_TrackingStateAction", RightControllerMapCandidates, "Tracking State"),
            new ControllerActionBinding("m_SelectAction", RightInteractionMapCandidates, "Select"),
            new ControllerActionBinding("m_SelectActionValue", RightInteractionMapCandidates, "Select Value"),
            new ControllerActionBinding("m_ActivateAction", RightInteractionMapCandidates, "Activate"),
            new ControllerActionBinding("m_ActivateActionValue", RightInteractionMapCandidates, "Activate Value"),
            new ControllerActionBinding("m_UIPressAction", RightInteractionMapCandidates, "UI Press"),
            new ControllerActionBinding("m_UIPressActionValue", RightInteractionMapCandidates, "UI Press Value"),
            new ControllerActionBinding("m_UIScrollAction", RightInteractionMapCandidates, "UI Scroll"),
            new ControllerActionBinding("m_HapticDeviceAction", RightControllerMapCandidates, "Haptic Device"),
            new ControllerActionBinding("m_RotateAnchorAction", RightInteractionMapCandidates, new[] { "Rotate Manipulation", "Rotate Anchor" }),
            new ControllerActionBinding("m_DirectionalAnchorRotationAction", RightInteractionMapCandidates, new[] { "Directional Manipulation", "Directional Anchor Rotation" }),
            new ControllerActionBinding("m_TranslateAnchorAction", RightInteractionMapCandidates, new[] { "Translate Manipulation", "Translate Anchor" }),
            new ControllerActionBinding("m_ScaleToggleAction", RightInteractionMapCandidates, "Scale Toggle"),
            new ControllerActionBinding("m_ScaleDeltaAction", RightInteractionMapCandidates, new[] { "Scale Over Time", "Scale Delta" })
        };

        private void ConfigureInputActionManager(Component inputActionManager)
        {
            if (inputActionManager == null)
            {
                return;
            }

            var defaultAsset = LoadDefaultXriInputActionsAsset();
            if (defaultAsset == null)
            {
                return;
            }

            var so = new SerializedObject(inputActionManager);
            var actionAssetsProp = so.FindProperty("m_ActionAssets");
            if (actionAssetsProp == null)
            {
                return;
            }

            bool alreadyBound = false;
            for (int i = 0; i < actionAssetsProp.arraySize; i++)
            {
                if (actionAssetsProp.GetArrayElementAtIndex(i).objectReferenceValue == defaultAsset)
                {
                    alreadyBound = true;
                    break;
                }
            }

            if (alreadyBound)
            {
                return;
            }

            actionAssetsProp.InsertArrayElementAtIndex(actionAssetsProp.arraySize);
            actionAssetsProp.GetArrayElementAtIndex(actionAssetsProp.arraySize - 1).objectReferenceValue = defaultAsset;
            so.ApplyModifiedPropertiesWithoutUndo();
            Log("已将“XRI Default Input Actions”绑定到 XR Input Action Manager，以自动启用输入映射。");
        }

        private void ConfigureActionBasedController(Component controller, bool isRightHand)
        {
            if (controller == null)
            {
                return;
            }

            var defaultAsset = LoadDefaultXriInputActionsAsset();
            if (defaultAsset == null)
            {
                return;
            }

            var bindings = isRightHand ? RightControllerBindings : LeftControllerBindings;
            var so = new SerializedObject(controller);
            bool hasChanges = false;

            foreach (var binding in bindings)
            {
                var cacheKey = $"{(isRightHand ? "Right" : "Left")}_{binding.PropertyName}";
                if (AssignControllerAction(so, binding.PropertyName, cacheKey, binding.MapNames, binding.ActionNames))
                {
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                Log($"已为{(isRightHand ? "右手" : "左手")} Action Based Controller 绑定默认输入动作。");
            }
        }

        private bool AssignControllerAction(SerializedObject controllerSo, string propertyName, string cacheKey, IReadOnlyList<string> mapNames, IReadOnlyList<string> actionNames)
        {
            var property = controllerSo.FindProperty(propertyName);
            if (property == null)
            {
                return false;
            }

            var reference = GetOrCreateActionReference(cacheKey, mapNames, actionNames);
            if (reference == null)
            {
                return false;
            }

            bool changed = false;
            var useReferenceProp = property.FindPropertyRelative("m_UseReference");
            if (useReferenceProp != null && !useReferenceProp.boolValue)
            {
                useReferenceProp.boolValue = true;
                changed = true;
            }

            var referenceProp = property.FindPropertyRelative("m_Reference");
            if (referenceProp != null && referenceProp.objectReferenceValue != reference)
            {
                referenceProp.objectReferenceValue = reference;
                changed = true;
            }

            return changed;
        }

        private InputActionReference GetOrCreateActionReference(string cacheKey, IReadOnlyList<string> mapNames, IReadOnlyList<string> actionNames)
        {
            if (_actionReferenceCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var asset = LoadDefaultXriInputActionsAsset();
            if (asset == null)
            {
                return null;
            }

            EnsureDirectoryExists(GeneratedInputActionsFolder);
            var assetPath = $"{GeneratedInputActionsFolder}/{cacheKey}.asset";
            var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(assetPath);
            if (reference == null)
            {
                reference = CreateActionReference(cacheKey, assetPath, asset, mapNames, actionNames);
            }
            else if (!TryConfigureActionReference(reference, asset, mapNames, actionNames))
            {
                AssetDatabase.DeleteAsset(assetPath);
                reference = CreateActionReference(cacheKey, assetPath, asset, mapNames, actionNames);
            }

            if (reference == null)
            {
                return null;
            }

            _actionReferenceCache[cacheKey] = reference;
            return reference;
        }

        private InputActionReference CreateActionReference(string cacheKey, string assetPath, InputActionAsset sourceAsset, IReadOnlyList<string> mapNames, IReadOnlyList<string> actionNames)
        {
            var reference = ScriptableObject.CreateInstance<InputActionReference>();
            if (!TryConfigureActionReference(reference, sourceAsset, mapNames, actionNames))
            {
                UnityEngine.Object.DestroyImmediate(reference);
                return null;
            }

            reference.name = cacheKey;
            AssetDatabase.CreateAsset(reference, assetPath);
            AssetDatabase.SaveAssets();
            Log($"已生成输入动作引用资产：{assetPath}");
            return reference;
        }

        private bool TryConfigureActionReference(InputActionReference reference, InputActionAsset asset, IReadOnlyList<string> mapNames, IReadOnlyList<string> actionNames)
        {
            if (reference == null || asset == null || actionNames == null || actionNames.Count == 0)
            {
                return false;
            }

            var action = FindActionByMapCandidates(asset, mapNames, actionNames);
            if (action == null)
            {
                var candidatesLabel = mapNames != null && mapNames.Count > 0
                    ? string.Join(" / ", mapNames)
                    : "<未指定 Action Map>";
                var actionLabel = string.Join(" / ", actionNames);
                Log($"无法创建输入动作引用（{candidatesLabel}/{actionLabel}）：请确认“XRI Default Input Actions”中包含该 Action Map。");
                return false;
            }

            try
            {
                reference.Set(action);
            }
            catch (Exception ex)
            {
                var mapName = action.actionMap != null ? action.actionMap.name : "<未知 Action Map>";
                Log($"无法创建输入动作引用（{mapName}/{action.name}）：{ex.Message}");
                return false;
            }

            return true;
        }

        private InputAction FindActionByMapCandidates(InputActionAsset asset, IReadOnlyList<string> mapNames, IReadOnlyList<string> actionNames)
        {
            if (actionNames == null || actionNames.Count == 0)
            {
                return null;
            }

            foreach (var actionName in actionNames)
            {
                var action = FindActionByMapCandidates(asset, mapNames, actionName);
                if (action != null)
                {
                    return action;
                }
            }

            return null;
        }

        private InputAction FindActionByMapCandidates(InputActionAsset asset, IReadOnlyList<string> mapNames, string actionName)
        {
            if (asset == null || string.IsNullOrEmpty(actionName))
            {
                return null;
            }

            if (mapNames != null && mapNames.Count > 0)
            {
                foreach (var mapName in mapNames)
                {
                    if (string.IsNullOrEmpty(mapName))
                    {
                        continue;
                    }

                    var map = asset.actionMaps.FirstOrDefault(m =>
                        IsMatchingActionMapName(m.name, mapName));
                    var action = map?.FindAction(actionName, throwIfNotFound: false);
                    if (action != null)
                    {
                        return action;
                    }
                }
            }

            return asset.FindAction(actionName, throwIfNotFound: false);
        }

        private InputActionAsset LoadDefaultXriInputActionsAsset()
        {
            if (_cachedDefaultInputActions != null)
            {
                return _cachedDefaultInputActions;
            }

            InputActionAsset asset = null;
            var guidPath = AssetDatabase.GUIDToAssetPath(DefaultXriInputActionsGuid);
            if (!string.IsNullOrEmpty(guidPath))
            {
                asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(guidPath);
            }

            if (asset == null)
            {
                var guids = AssetDatabase.FindAssets("\"XRI Default Input Actions\" t:InputActionAsset");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                    if (asset != null)
                    {
                        break;
                    }
                }
            }

            if (asset == null)
            {
                if (TryEnsureStarterAssetsSampleAvailable())
                {
                    var guids = AssetDatabase.FindAssets("\"XRI Default Input Actions\" t:InputActionAsset");
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                        if (asset != null)
                        {
                            break;
                        }
                    }
                }

                if (asset == null)
                {
                    Log("未在项目中找到“XRI Default Input Actions.inputactions”。请在 Package Manager 中重新导入 XR Interaction Toolkit 的 Starter Assets。");
                    PromptStarterAssetsImport();
                    return null;
                }
            }

            _cachedDefaultInputActions = asset;
            return _cachedDefaultInputActions;
        }

        #endregion

        #region 版本控制助手集成

        private bool IsGitAssistantInstalled()
        {
            return GetGitAssistantWindowType() != null && GetGitAssistantUtilityType() != null;
        }

        private bool OpenGitAssistantWindow()
        {
            if (!IsGitAssistantInstalled())
            {
                return false;
            }

            return EditorApplication.ExecuteMenuItem(GitAssistantMenuPath);
        }

        private void TriggerQuickBackupFlow()
        {
            if (!IsGitAssistantInstalled())
            {
                EditorUtility.DisplayDialog("提示", "当前项目未安装版本控制助手，无法执行快速备份。", "好的");
                return;
            }

            if (TryAutoBackupWithGitAssistant(out var message))
            {
                Log(message);
                EditorUtility.DisplayDialog("备份完成", message, "好的");
                MarkBackupConfirmed();
            }
            else
            {
                EditorUtility.DisplayDialog("备份失败", message, "好的");
            }
        }

        private void AttemptRollbackToBaseline()
        {
            if (!IsGitAssistantInstalled())
            {
                EditorUtility.DisplayDialog("无法回滚", "请先安装版本控制助手后再尝试回滚。", "好的");
                return;
            }

            if (string.IsNullOrEmpty(_lastBaselineCommitHash))
            {
                EditorUtility.DisplayDialog("无法回滚", "当前会话未记录转换前的 Git 提交，无法执行自动回滚。", "好的");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "确认回滚？",
                    $"将使用 git reset --hard {_lastBaselineCommitHash} 恢复到转换前的版本。\n\n该操作会丢弃当前所有未提交的改动，确定要继续吗？",
                    "确认回滚",
                    "取消"))
            {
                return;
            }

            if (TryRunGitAssistantCommand($"reset --hard {_lastBaselineCommitHash}", out var output, out var error))
            {
                AssetDatabase.Refresh();
                _lastConversionSucceeded = false;
                EditorUtility.DisplayDialog("回滚完成", "已恢复到转换前的 Git 版本。", "好的");
                Log($"Git 回滚完成：{output}");
            }
            else
            {
                var reason = string.IsNullOrWhiteSpace(error) ? "Git 命令执行失败，请查看控制台。" : error;
                EditorUtility.DisplayDialog("回滚失败", reason, "好的");
                Log($"回滚失败：{reason}");
            }
        }

        private bool EnsureBackupReady(string actionName)
        {
            if (!NeedsBackupConfirmation())
            {
                return true;
            }

            if (!IsGitAssistantInstalled())
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "执行前请备份",
                    $"即将执行 {actionName}，该操作会修改 XR 依赖、项目设置以及当前场景。\n\n请确认你已经手动保存场景并完成一次备份或 Git 提交。",
                    "我已完成备份",
                    "取消");
                if (confirmed)
                {
                    MarkBackupConfirmed();
                }
                return confirmed;
            }

            while (true)
            {
                int option = EditorUtility.DisplayDialogComplex(
                    "执行前请先备份",
                    $"执行 {actionName} 会批量修改 manifest、Project Settings 与当前场景。\n\n建议使用版本控制助手创建备份提交，或者确认已完成其他备份手段。",
                    "我已完成备份",
                    "取消",
                    "使用版本控制助手快速备份");

                switch (option)
                {
                    case 0:
                        MarkBackupConfirmed();
                        return true;
                    case 1:
                        return false;
                    case 2:
                        if (TryAutoBackupWithGitAssistant(out var message))
                        {
                            Log(message);
                            EditorUtility.DisplayDialog("备份完成", message, "好的");
                            MarkBackupConfirmed();
                            return true;
                        }

                        if (!EditorUtility.DisplayDialog("备份失败", $"{message}\n\n需要重试吗？", "重试", "取消"))
                        {
                            return false;
                        }
                        break;
                }
            }
        }

        private bool NeedsBackupConfirmation()
        {
            if (!_lastBackupConfirmationTimeUtc.HasValue)
            {
                return true;
            }

            var elapsed = DateTime.UtcNow - _lastBackupConfirmationTimeUtc.Value;
            return elapsed.TotalSeconds > BackupConfirmationValidSeconds;
        }

        private void MarkBackupConfirmed()
        {
            _lastBackupConfirmationTimeUtc = DateTime.UtcNow;
            CacheBaselineCommitHash();
        }

        private void CacheBaselineCommitHash()
        {
            if (!IsGitAssistantInstalled())
            {
                _lastBaselineCommitHash = string.Empty;
                return;
            }

            if (TryGetCurrentGitHead(out var hash))
            {
                _lastBaselineCommitHash = hash;
                Log($"已记录当前 Git 提交 {hash}，可在成功后回滚。");
            }
            else
            {
                _lastBaselineCommitHash = string.Empty;
                Log("未能记录当前 Git 提交，可能尚未初始化仓库。");
            }
        }

        private bool TryGetCurrentGitHead(out string hash)
        {
            hash = string.Empty;
            if (!TryRunGitAssistantCommand("rev-parse HEAD", out var output, out var error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Log($"获取 Git 提交失败：{error}");
                }
                return false;
            }

            hash = output.Trim();
            return !string.IsNullOrEmpty(hash);
        }

        private bool TryAutoBackupWithGitAssistant(out string message)
        {
            message = string.Empty;
            if (!IsGitAssistantInstalled())
            {
                message = "未检测到版本控制助手。";
                return false;
            }

            if (!TryRunGitAssistantCommand("status --porcelain", out var statusOutput, out var statusError))
            {
                message = string.IsNullOrWhiteSpace(statusError)
                    ? "无法读取 Git 状态。"
                    : statusError;
                return false;
            }

            if (string.IsNullOrWhiteSpace(statusOutput))
            {
                message = "当前工作区没有改动，视为已备份。";
                return true;
            }

            if (!TryRunGitAssistantCommand("add -A", out _, out var addError))
            {
                message = string.IsNullOrWhiteSpace(addError)
                    ? "git add -A 执行失败。"
                    : addError;
                return false;
            }

            var commitMessage = $"备份：VR 转换前 {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            if (!TryRunGitAssistantCommand($"commit -m \"{commitMessage}\"", out var commitOutput, out var commitError))
            {
                var combined = $"{commitOutput}\n{commitError}".Trim();
                if (combined.IndexOf("nothing to commit", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    message = "已经是最新提交，视为已备份。";
                    return true;
                }

                message = string.IsNullOrWhiteSpace(combined)
                    ? "git commit 执行失败。"
                    : combined;
                message = $"创建备份提交失败：{message}";
                return false;
            }

            message = $"已创建备份提交：{commitMessage}";
            return true;
        }

        private bool TryRunGitAssistantCommand(string arguments, out string output, out string error)
        {
            output = string.Empty;
            error = string.Empty;

            var utilityType = GetGitAssistantUtilityType();
            if (utilityType == null)
            {
                error = "未安装版本控制助手。";
                return false;
            }

            MethodInfo runMethod = utilityType.GetMethod(
                "Run",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(bool) },
                null);

            object resultObj;
            try
            {
                if (runMethod != null)
                {
                    resultObj = runMethod.Invoke(null, new object[] { arguments, true });
                }
                else
                {
                    runMethod = utilityType.GetMethod(
                        "Run",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(string) },
                        null);
                    if (runMethod == null)
                    {
                        error = "版本控制助手版本过旧，缺少 Run 方法。";
                        return false;
                    }

                    resultObj = runMethod.Invoke(null, new object[] { arguments });
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            if (resultObj == null)
            {
                error = "版本控制助手未返回结果。";
                return false;
            }

            var resultType = resultObj.GetType();
            var successProp = resultType.GetProperty("Success");
            var outputProp = resultType.GetProperty("Output");
            var errorProp = resultType.GetProperty("Error");

            bool success = successProp != null && (bool)successProp.GetValue(resultObj);
            output = outputProp?.GetValue(resultObj) as string ?? string.Empty;
            error = errorProp?.GetValue(resultObj) as string ?? string.Empty;

            return success;
        }

        private string GetShortHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return string.Empty;
            }

            return hash.Length <= 7 ? hash : hash.Substring(0, 7);
        }

        #endregion

        #region Utility helpers

        private void QueuePackageInstall(string packageName)
        {
            try
            {
                var request = Client.Add(packageName);
                _pendingAddRequests.Add(request);
                Log($"已向 Package Manager 提交安装请求：{packageName}");
                StartMonitoringPackageInstalls();
            }
            catch (Exception ex)
            {
                Log($"无法安装 {packageName}：{ex.Message}");
            }
        }

        private void StartMonitoringPackageInstalls()
        {
            if (_isMonitoringAddRequests)
                return;

            _isMonitoringAddRequests = true;
            EditorApplication.update += MonitorPackageInstallRequests;
        }

        private void MonitorPackageInstallRequests()
        {
            for (int i = _pendingAddRequests.Count - 1; i >= 0; i--)
            {
                var request = _pendingAddRequests[i];
                if (!request.IsCompleted)
                    continue;

                if (request.Status == StatusCode.Success)
                {
                    Log($"包安装成功：{request.Result.packageId}");
                }
                else if (request.Status == StatusCode.Failure)
                {
                    Log($"包安装失败：{request.Error?.message}");
                }
                else
                {
                    Log($"包安装状态：{request.Status}");
                }

                _pendingAddRequests.RemoveAt(i);
            }

            if (_pendingAddRequests.Count == 0)
            {
                EditorApplication.update -= MonitorPackageInstallRequests;
                _isMonitoringAddRequests = false;
            }
        }

        private bool RemoveInvalidLatestVersionEntries(ref string manifestText)
        {
            bool removed = false;
            foreach (var pkg in RequiredPackages)
            {
                string pattern = $"\"{pkg}\": \"latest\"";
                int idx;
                while ((idx = manifestText.IndexOf(pattern, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    int lineStart = manifestText.LastIndexOf('\n', idx);
                    if (lineStart < 0) lineStart = 0;
                    int lineEnd = manifestText.IndexOf('\n', idx);
                    if (lineEnd < 0) lineEnd = manifestText.Length;

                    manifestText = manifestText.Remove(lineStart, lineEnd - lineStart);
                    removed = true;
                    Log($"移除了 manifest.json 中 {pkg} 的 \"latest\" 版本约束。");
                }
            }

            return removed;
        }

        private bool TryGetManifestPackageVersion(string manifestText, string packageName, out string version)
        {
            version = null;
            string pattern = $"\"{packageName}\"";
            int idx = manifestText.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0)
                return false;

            int colonIdx = manifestText.IndexOf(':', idx);
            if (colonIdx < 0)
                return false;

            int firstQuote = manifestText.IndexOf('"', colonIdx + 1);
            if (firstQuote < 0)
                return false;

            int secondQuote = manifestText.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
                return false;

            version = manifestText.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();
            return true;
        }

        private bool IsValidManifestVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return false;

            return !version.Equals("latest", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureDirectoryExists(string path)
        {
            var full = Path.GetFullPath(path);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
                AssetDatabase.Refresh();
            }
        }

        private static Type GetGitAssistantWindowType()
        {
            return FindFirstType(GitAssistantWindowTypeNames);
        }

        private static Type GetGitAssistantUtilityType()
        {
            return FindFirstType(GitAssistantUtilityTypeNames);
        }

        private static Type FindFirstType(IEnumerable<string> typeNames)
        {
            if (typeNames == null)
            {
                return null;
            }

            foreach (var fullName in typeNames)
            {
                var type = FindType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private class ProjectDiagnostics
        {
            public string RenderPipelineLabel;
            public string RenderPipelineHint;
            public bool HasXrManagement;
            public bool HasOpenXr;
            public bool HasXri;
            public bool HasNewInputSystem;
            public bool HasGitAssistant;
            public bool HasVrSimulator;
        }

        private static Type FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;

            if (_typeCache.TryGetValue(fullName, out var cached))
            {
                return cached;
            }

            var type = Type.GetType(fullName);
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(fullName);
                    if (type != null)
                    {
                        break;
                    }
                }
            }

            _typeCache[fullName] = type;
            return type;
        }

        #endregion
    }
}


