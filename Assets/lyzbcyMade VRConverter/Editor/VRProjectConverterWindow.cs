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

        private enum ProjectType
        {
            Unknown = 0,
            Standard3D = 1,
            VR = 2
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
            public bool IsReverseConversion; // 是否为反向转换（VR -> 3D）

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
            
            // 检查 Unity 版本兼容性
            EditorApplication.delayCall += () =>
            {
                if (!UnityVersionChecker.CheckCompatibility(showWarning: false))
                {
                    // 用户选择不继续，关闭窗口
                    Close();
                }
            };
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
            window.Log(Localization.Get("Log.OpenTool"));
            
            // 记录版本信息
            var versionInfo = UnityVersionChecker.GetCompatibilityInfo();
            Logger.LogInfo($"打开窗口 - Unity 版本: {versionInfo.CurrentVersion}, 兼容性: {versionInfo.Status}");
        }

        private void OnGUI()
        {
            // 性能优化：只在必要时初始化样式
            if (_headerTitleStyle == null)
            {
                InitStyles();
            }

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
            Logger.LogInfo(msg); // 同时写入日志文件
            // 性能优化：只在必要时重绘，避免频繁刷新
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                Repaint();
            }
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
                // 语言切换和诊断报告按钮
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    
                    // 语言切换
                    var currentLang = Localization.GetCurrentLanguage();
                    var langDisplay = Localization.GetLanguageDisplayName(currentLang);
                    if (GUILayout.Button(langDisplay, GUILayout.Width(80)))
                    {
                        var nextLang = currentLang switch
                        {
                            Localization.Language.Chinese => Localization.Language.English,
                            Localization.Language.English => Localization.Language.Japanese,
                            Localization.Language.Japanese => Localization.Language.Chinese,
                            _ => Localization.Language.Chinese
                        };
                        Localization.SetLanguage(nextLang);
                        Repaint();
                    }
                    
                    GUILayout.Space(8);
                    
                    // 诊断报告按钮
                    if (GUILayout.Button("诊断报告", GUILayout.Width(100)))
                    {
                        GenerateDiagnosticReport();
                    }
                }
                
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField(Localization.Get("Window.Header.Title"), _headerTitleStyle);
                EditorGUILayout.Space(6);

                EditorGUILayout.LabelField(
                    Localization.Get("Window.Header.Description"),
                    _headerSubTitleStyle);

                EditorGUILayout.Space(12);

                // 显示项目类型
                var projectType = DetectProjectType();
                var projectTypeText = projectType switch
                {
                    ProjectType.VR => Localization.Get("ProjectType.VR", "当前项目类型：VR 项目"),
                    ProjectType.Standard3D => Localization.Get("ProjectType.Standard3D", "当前项目类型：3D 项目"),
                    _ => Localization.Get("ProjectType.Unknown", "当前项目类型：未知")
                };
                EditorGUILayout.LabelField(projectTypeText, EditorStyles.boldLabel);

                EditorGUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var compiling = EditorApplication.isCompiling;
                    var icon = EditorGUIUtility.IconContent(compiling ? "console.warnicon" : "TestPassed");
                    var msg = compiling
                        ? Localization.Get("Status.Compiling")
                        : Localization.Get("Status.Ready");

                    EditorGUILayout.LabelField(icon, GUILayout.Width(24), GUILayout.Height(24));
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.Space(6);
            }
            EditorGUILayout.EndVertical();
        }
        
        private void GenerateDiagnosticReport()
        {
            try
            {
                var report = DiagnosticReporter.GenerateReport();
                var defaultPath = Path.Combine(Application.dataPath, "..", $"VRConverter_Diagnostic_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var exportPath = EditorUtility.SaveFilePanel("导出诊断报告", "", Path.GetFileName(defaultPath), "txt");
                
                if (!string.IsNullOrEmpty(exportPath))
                {
                    if (DiagnosticReporter.ExportReport(exportPath, out var error))
                    {
                        EditorUtility.DisplayDialog(
                            Localization.Get("Success.LogExported", ""),
                            $"诊断报告已导出到:\n{exportPath}",
                            Localization.Get("Button.OK"));
                        EditorUtility.RevealInFinder(exportPath);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            Localization.Get("Error.LogExportFailed"),
                            $"导出失败:\n{error}",
                            Localization.Get("Button.OK"));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "生成诊断报告");
            }
        }

        private void DrawModeSwitcher()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField(Localization.Get("Mode.Title"), _stepTitleStyle);
            EditorGUILayout.Space(10);

            var contents = new[]
            {
                new GUIContent(Localization.Get("Mode.Guided")),
                new GUIContent(Localization.Get("Mode.Professional"))
            };

            int selected = GUILayout.Toolbar((int)_uiMode, contents, GUILayout.Height(32));
            if (selected != (int)_uiMode)
            {
                _uiMode = (ConverterMode)selected;
                SaveModePreference();
            }

            EditorGUILayout.Space(8);
            var desc = _uiMode == ConverterMode.Guided
                ? Localization.Get("Mode.Guided.Description", "保持\"一键执行\"体验，适合第一次接触 VR 项目的同学。")
                : Localization.Get("Mode.Professional.Description", "自定义执行步骤、目标平台与场景策略，满足不同团队流程。");
            EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawCompatibilityInsights()
        {
            var diagnostics = GetProjectDiagnostics();
            var versionInfo = UnityVersionChecker.GetCompatibilityInfo();
            
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField(Localization.Get("Compatibility.Title"), _stepTitleStyle);
            EditorGUILayout.Space(10);

            Action gitAssistantAction = diagnostics.HasGitAssistant
                ? null
                : () => Application.OpenURL(GitAssistantAssetStoreUrl);

            // 版本兼容性状态
            var versionStatus = versionInfo.Status == CompatibilityStatus.Supported;
            var versionValue = $"{versionInfo.CurrentVersion} ({GetVersionStatusText(versionInfo.Status)})";
            var versionHint = versionInfo.Recommendation;

            var rows = new[]
            {
                new DiagnosticRow("Unity 版本", versionValue, versionStatus, versionHint),
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

        private string GetVersionStatusText(CompatibilityStatus status)
        {
            return status switch
            {
                CompatibilityStatus.Supported => "兼容",
                CompatibilityStatus.Warning => "警告",
                CompatibilityStatus.Unsupported => "不兼容",
                _ => "未知"
            };
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
            EditorGUILayout.LabelField(Localization.Get("Pro.Plan.Title"), _stepTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(Localization.Get("Pro.Plan.Description"), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(12);

            _proIncludePackages = EditorGUILayout.ToggleLeft(Localization.Get("Pro.Plan.CheckPackages"), _proIncludePackages);
            _proIncludeProjectSettings = EditorGUILayout.ToggleLeft(Localization.Get("Pro.Plan.CheckProjectSettings"), _proIncludeProjectSettings);
            _proIncludeSceneConversion = EditorGUILayout.ToggleLeft(Localization.Get("Pro.Plan.ConvertScene"), _proIncludeSceneConversion);
            _proIncludeDeviceSimulator = EditorGUILayout.ToggleLeft(Localization.Get("Pro.Plan.DeviceSimulator"), _proIncludeDeviceSimulator);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(Localization.Get("Pro.TargetPlatforms"), _stepTitleStyle);
            EditorGUILayout.Space(4);
            foreach (var group in TargetGroups)
            {
                bool current = _proTargetGroupToggles.TryGetValue(group, out var enabled) ? enabled : true;
                bool next = EditorGUILayout.ToggleLeft(Localization.Get("Pro.TargetPlatform.For", group), current);
                _proTargetGroupToggles[group] = next;
            }
            if (GetProfessionalTargetGroups().Length == 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(Localization.Get("Pro.TargetPlatform.NoSelection"), MessageType.Info);
            }

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(!_proIncludeSceneConversion))
            {
                EditorGUILayout.LabelField(Localization.Get("Pro.SceneStrategy"), _stepTitleStyle);
                _proRigStrategy = (RigStrategy)EditorGUILayout.EnumPopup(new GUIContent(Localization.Get("Pro.RigStrategy"), Localization.Get("Pro.RigStrategy.Tooltip")), _proRigStrategy);
                _proPreserveLegacyMainCamera = EditorGUILayout.ToggleLeft(Localization.Get("Pro.PreserveLegacyCamera"), _proPreserveLegacyMainCamera);
            }

            EditorGUILayout.Space(12);
            if (GUILayout.Button(Localization.Get("Pro.ExecutePlan"), _primaryButtonStyle))
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
            EditorGUILayout.LabelField(Localization.Get("QuickStart.Title"), _stepTitleStyle);
            EditorGUILayout.Space(10);

            var projectType = DetectProjectType();
            bool isVRProject = projectType == ProjectType.VR;

            if (isVRProject)
            {
                EditorGUILayout.LabelField(
                    Localization.Get("Reverse.Description"),
                    EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(12);

                var content = new GUIContent(
                    Localization.Get("Reverse.RunAll.Title"),
                    Localization.Get("Reverse.RunAll.Tooltip"));

                if (GUILayout.Button(content, _primaryButtonStyle))
                {
                    RunReverseConversion();
                }
            }
            else
            {
            EditorGUILayout.LabelField(
                Localization.Get("QuickStart.Description"),
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(12);

            var content = new GUIContent(
                Localization.Get("Guided.RunAll.Title"),
                Localization.Get("QuickStart.RunAll.Tooltip"));

            if (GUILayout.Button(content, _primaryButtonStyle))
            {
                RunAllSteps();
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(Localization.Get("QuickStart.Hint"), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 执行反向转换（VR -> 3D）
        /// </summary>
        private void RunReverseConversion()
        {
            var plan = new ConversionPlan
            {
                EnsurePackages = true,
                ConfigureProjectSettings = true,
                ConvertScene = true,
                ConfigureDeviceSimulator = false,
                TargetGroups = TargetGroups,
                RigStrategy = RigStrategy.Auto,
                DisableLegacyCamera = false,
                IsReverseConversion = true
            };

            ExecuteConversionPlan(plan, "“一键转换为 3D 项目”");
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
            EditorGUILayout.LabelField(Localization.Get("Step1.Title"), _stepTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                Localization.Get("Step1.Description"),
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(12);

            var btnStep1 = new GUIContent(
                Localization.Get("Step1.Button"),
                Localization.Get("Step1.Tooltip"));
            if (GUILayout.Button(btnStep1, _secondaryButtonStyle))
            {
                EnsureXrPackages();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStep2Card()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField(Localization.Get("Step2.Title"), _stepTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                Localization.Get("Step2.Description"),
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                var btnStep2 = new GUIContent(
                    Localization.Get("Step2.Button"),
                    EditorApplication.isCompiling
                        ? Localization.Get("Step2.Tooltip.Compiling")
                        : Localization.Get("Step2.Tooltip.Ready"));

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
            EditorGUILayout.LabelField(Localization.Get("Git.Title"), _stepTitleStyle);
            EditorGUILayout.Space(10);

            var gitAvailable = IsGitAssistantInstalled();
            var description = gitAvailable
                ? Localization.Get("Git.Description.Installed")
                : Localization.Get("Git.Description.NotInstalled");
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(12);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!gitAvailable))
                {
                    if (GUILayout.Button(Localization.Get("Git.OpenAssistant"), _secondaryButtonStyle))
                    {
                        if (!OpenGitAssistantWindow())
                        {
                            EditorUtility.DisplayDialog(Localization.Get("Dialog.Info"), Localization.Get("Dialog.GitAssistantNotFound"), Localization.Get("Button.Good"));
                        }
                    }

                    GUILayout.Space(8);
                    if (GUILayout.Button(Localization.Get("Git.QuickBackup"), _secondaryButtonStyle))
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
                    if (GUILayout.Button(Localization.Get("Git.Rollback"), _secondaryButtonStyle))
                    {
                        AttemptRollbackToBaseline();
                    }
                }

                GUILayout.Space(8);
                var baselineLabel = canRollback
                    ? Localization.Get("Git.Baseline.Recorded", GetShortHash(_lastBaselineCommitHash))
                    : Localization.Get("Git.Baseline.NotRecorded");
                EditorGUILayout.LabelField(baselineLabel, EditorStyles.wordWrappedMiniLabel);
            }

            if (!gitAvailable)
            {
                EditorGUILayout.HelpBox(Localization.Get("Git.Info"), MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 日志区域：滚动文本 + 简短说明，方便新手理解每一步发生了什么。
        /// </summary>
        private void DrawLogArea()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField(Localization.Get("Log.Title"), _stepTitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                Localization.Get("Log.Description"),
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(8);

            // 日志操作按钮
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Localization.Get("Log.ViewFile"), GUILayout.Width(120)))
                {
                    var logFile = Logger.GetCurrentLogFilePath();
                    if (!string.IsNullOrEmpty(logFile) && File.Exists(logFile))
                    {
                        EditorUtility.RevealInFinder(logFile);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(Localization.Get("Dialog.Info"), Localization.Get("Dialog.LogFileNotFound"), Localization.Get("Button.OK"));
                    }
                }

                if (GUILayout.Button(Localization.Get("Log.Export"), GUILayout.Width(100)))
                {
                    var defaultPath = Path.Combine(Application.dataPath, "..", $"VRConverter_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    var exportPath = EditorUtility.SaveFilePanel(Localization.Get("Log.Export"), "", Path.GetFileName(defaultPath), "txt");
                    if (!string.IsNullOrEmpty(exportPath))
                    {
                        if (Logger.ExportLog(exportPath, out var error))
                        {
                            EditorUtility.DisplayDialog(Localization.Get("Dialog.Success"), Localization.Get("Dialog.LogExported", exportPath), Localization.Get("Button.OK"));
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(Localization.Get("Dialog.Failed"), Localization.Get("Dialog.LogExportFailed", error), Localization.Get("Button.OK"));
                        }
                    }
                }

                GUILayout.FlexibleSpace();
            }

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
                Log(Localization.Get("Log.NoStepsSelected"));
                return;
            }

            if (!EnsureBackupReady(actionName))
            {
                Log(Localization.Get("Log.UserCancelled", actionName));
                return;
            }

            // 创建操作备份
            try
            {
                var filesToBackup = new List<string> { "Packages/manifest.json" };
                if (plan.ConfigureProjectSettings)
                {
                    filesToBackup.Add("ProjectSettings/ProjectSettings.asset");
                }
                if (plan.ConvertScene && !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
                {
                    filesToBackup.Add(EditorSceneManager.GetActiveScene().path);
                }

                var backupId = OperationBackup.CreateBackup(actionName, filesToBackup);
                if (!string.IsNullOrEmpty(backupId))
                {
                    Log(Localization.Get("Log.BackupCreated", backupId));
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "创建操作备份", showDialog: false);
                // 备份失败不阻止操作继续，但记录警告
                Logger.LogWarning("操作备份创建失败，但操作将继续执行");
            }

            _lastConversionSucceeded = false;

            // 反向转换（VR -> 3D）
            if (plan.IsReverseConversion)
            {
                if (plan.EnsurePackages)
                {
                    RemoveXrPackages();
                }

                if ((plan.ConfigureProjectSettings || plan.ConvertScene) && EditorApplication.isCompiling)
                {
                    Log(Localization.Get("Log.Compiling"));
                    return;
                }

                // 清理 VRConverterGenerated 目录（包含 DeviceSimulator、StarterAssets 等）
                CleanupGeneratedVrAssets();

                if (plan.ConfigureProjectSettings)
                {
                    Restore3DProjectSettings(plan.TargetGroups);
                }

                if (plan.ConvertScene)
                {
                    bool converted = ConvertCurrentSceneTo3D();
                    if (converted)
                    {
                        HandleConversionCompleted(isReverseConversion: true);
                    }
                }
                else
                {
                    // 即使没有转换场景，如果其他操作成功，也显示完成提示
                    HandleConversionCompleted(isReverseConversion: true);
                }
            }
            else
            {
                // 正向转换（3D -> VR）
            if (plan.EnsurePackages)
            {
                EnsureXrPackages();
            }

            if ((plan.ConfigureProjectSettings || plan.ConvertScene) && EditorApplication.isCompiling)
            {
                Log(Localization.Get("Log.Compiling"));
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
                EditorUtility.DisplayDialog(Localization.Get("Dialog.Info"), Localization.Get("Dialog.NoStepsSelected"), Localization.Get("Button.Good"));
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
            using (var progress = new ProgressReporter("安装 XR 包", "正在检查包依赖..."))
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    Log(Localization.Get("Log.ManifestNotFound"));
                    return;
                }

                progress.UpdateProgress(0.1f, "创建备份...");
                // 创建备份
                try
                {
                    OperationBackup.CreateQuickBackup(manifestPath);
                    Log(Localization.Get("Log.ManifestBackupCreated"));
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleException(ex, "创建 manifest.json 备份", showDialog: false);
                }

            string text;
            try
            {
                text = File.ReadAllText(manifestPath);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "读取 manifest.json", showDialog: true);
                return;
            }

                if (RemoveInvalidLatestVersionEntries(ref text))
                {
                    File.WriteAllText(manifestPath, text);
                    AssetDatabase.Refresh();
                    Log(Localization.Get("Log.LatestRemoved"));
                }

                progress.UpdateProgress(0.3f, "检查包依赖...");
                bool requestedInstall = false;
                int packageIndex = 0;
                foreach (var pkg in RequiredPackages)
                {
                    packageIndex++;
                    progress.UpdateProgress(0.3f + (packageIndex / (float)RequiredPackages.Length) * 0.5f, $"检查 {pkg}...");
                    
                    if (TryGetManifestPackageVersion(text, pkg, out var version))
                    {
                        if (IsValidManifestVersion(version))
                        {
                            Log(Localization.Get("Log.PackageExists", pkg, version));
                            continue;
                        }

                        Log(Localization.Get("Log.PackageInvalidVersion", pkg, version));
                    }
                    else
                    {
                        Log(Localization.Get("Log.PackageNotFound", pkg));
                    }

                    QueuePackageInstall(pkg);
                    requestedInstall = true;
                }

                progress.UpdateProgress(0.9f, requestedInstall ? "等待包安装..." : "完成");
                if (!requestedInstall)
                {
                    Log(Localization.Get("Log.AllPackagesInstalled"));
                }
            }
        }

        /// <summary>
        /// 通过 XR Management API 自动为 Standalone/Android 启用 OpenXR Loader。
        /// </summary>
        private void ConfigureXrProjectSettings(IEnumerable<BuildTargetGroup> targetGroups = null)
        {
            using (var progress = new ProgressReporter("配置项目设置", "正在初始化..."))
            {
                var desiredGroups = (targetGroups ?? TargetGroups)?.Distinct().ToArray() ?? Array.Empty<BuildTargetGroup>();
                if (desiredGroups.Length == 0)
                {
                    desiredGroups = TargetGroups;
                }

                progress.UpdateProgress(0.1f, "检查 XR Management 程序集...");
                var perBuildType = FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
                var generalType = FindType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management");
                var managerType = FindType("UnityEngine.XR.Management.XRManagerSettings, Unity.XR.Management");
                var metadataStoreType = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore, Unity.XR.Management.Editor");

                if (perBuildType == null || generalType == null || managerType == null)
                {
                    Log(Localization.Get("Log.XrManagementNotFound"));
                    return;
                }

                if (FindType(OpenXrLoaderTypeName) == null)
                {
                    Log(Localization.Get("Log.OpenXrLoaderNotFound"));
                    return;
                }

                progress.UpdateProgress(0.3f, "创建 XR General Settings...");
                var perBuildAsset = GetOrCreateGeneralSettingsAsset(perBuildType);
                RegisterXrSettingsConfig(perBuildAsset, perBuildType);

                var getMethod = perBuildType.GetMethod("SettingsForBuildTarget", new[] { typeof(BuildTargetGroup) });
                var setMethod = perBuildType.GetMethod("SetSettingsForBuildTarget", new[] { typeof(BuildTargetGroup), generalType });

                if (getMethod == null || setMethod == null)
                {
                    Log(Localization.Get("Log.XrApiChanged"));
                    return;
                }

                progress.UpdateProgress(0.5f, "配置目标平台...");
                int groupIndex = 0;
                foreach (var targetGroup in desiredGroups)
                {
                    groupIndex++;
                    progress.UpdateProgress(0.5f + (groupIndex / (float)desiredGroups.Length) * 0.4f, $"配置 {targetGroup}...");
                    
                    var generalSettings = GetOrCreateGeneralSettings(perBuildAsset, targetGroup, generalType, getMethod, setMethod);
                    if (generalSettings == null)
                    {
                        Log(Localization.Get("Log.CannotCreateGeneralSettings", targetGroup));
                        continue;
                    }

                    EnsureGeneralSettingsDefaults(generalSettings);
                    var managerSettings = GetOrCreateManagerSettings(generalSettings, managerType);
                    if (managerSettings == null)
                    {
                        Log(Localization.Get("Log.CannotCreateManagerSettings", targetGroup));
                        continue;
                    }

                    if (AssignOpenXrLoader(managerSettings, managerType, metadataStoreType, targetGroup))
                    {
                        Log(Localization.Get("Log.OpenXrEnabled", targetGroup));
                    }
                    else
                    {
                        Log(Localization.Get("Log.OpenXrBindFailed", targetGroup));
                    }
                }

                progress.UpdateProgress(0.95f, "保存资源...");
                AssetDatabase.SaveAssets();
            }
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
                Log(Localization.Get("Log.SimulatorNotWritable"));
                return;
            }

            var prefab = FindOrCreateDeviceSimulatorPrefab();
            if (prefab == null)
            {
                Log(Localization.Get("Log.SimulatorPrefabNotFound"));
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
                Log(Localization.Get("Log.SimulatorConfigured"));
            }
            else
            {
                Log(Localization.Get("Log.SimulatorReady"));
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
                    Log(Localization.Get("Log.SimulatorVersionIncompatible"));
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
                            var errorMsg = ErrorHandler.HandleException(ex, "初始化 XR Device Simulator 设置", showDialog: false);
                            Log(errorMsg);
                        }
                    }
                }
            }

            if (settings == null)
            {
                if (logOnFailure)
                {
                    Log(Localization.Get("Log.SimulatorSettingsFailed"));
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
                Log(Localization.Get("Log.SimulatorSampleImported"));
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
                Log(Localization.Get("Log.StarterAssetsCopied"));
                return true;
            }

            return false;
        }

        private bool TryCopyPackageSampleFolder(string packageAssetPath, string sampleRelativePath, string destinationFolder, string sampleDisplayName)
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageAssetPath);
            if (packageInfo == null)
            {
                Log(Localization.Get("Log.SamplePackageNotFound", sampleDisplayName, packageAssetPath));
                return false;
            }

            var sourcePath = Path.Combine(packageInfo.resolvedPath, sampleRelativePath);
            if (!Directory.Exists(sourcePath))
            {
                Log(Localization.Get("Log.SampleNotFound", packageInfo.resolvedPath, sampleDisplayName, sampleRelativePath));
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
            using (var progress = new ProgressReporter("转换场景", "正在初始化..."))
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (!scene.IsValid())
                {
                    Log(Localization.Get("Log.NoSceneOpen"));
                    return false;
                }

                if (strategy == RigStrategy.SkipSceneChanges)
                {
                    Log(Localization.Get("Log.SkipSceneChanges"));
                    return false;
                }

                progress.UpdateProgress(0.2f, "处理旧相机...");
                LegacyCameraBindingInfo bindingInfo = LegacyCameraBindingInfo.Empty;
                if (disableLegacyMainCamera)
                {
                    bindingInfo = DisableLegacyMainCamera();
                }
                else
                {
                    Log(Localization.Get("Log.PreserveLegacyCamera"));
                }

                progress.UpdateProgress(0.4f, "创建 XR Origin...");
                switch (strategy)
                {
                    case RigStrategy.OnlyUpdateExistingXrOrigin:
                        if (TryCreateOrUpdateXrOriginRig(createIfMissing: false, bindingInfo: bindingInfo))
                        {
                            Log(Localization.Get("Log.XrOriginUpdated"));
                            return true;
                        }

                        Log(Localization.Get("Log.XrOriginNotFound"));
                        return false;
                    case RigStrategy.ForceFallbackRig:
                        progress.UpdateProgress(0.6f, "创建备用 VR Rig...");
                        CreateFallbackVrRig(bindingInfo);
                        return true;
                    default:
                        break;
                }

                progress.UpdateProgress(0.5f, "创建或更新 XR Origin...");
                if (TryCreateOrUpdateXrOriginRig(bindingInfo: bindingInfo))
                {
                    progress.UpdateProgress(0.9f, "保存场景...");
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Log(Localization.Get("Log.XrOriginCreated"));
                    return true;
                }

                progress.UpdateProgress(0.7f, "回退到备用 VR Rig...");
                Log(Localization.Get("Log.FallbackRigUsed"));
                CreateFallbackVrRig(bindingInfo);
                progress.UpdateProgress(0.9f, "保存场景...");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return true;
            }
        }

        private void HandleConversionCompleted(bool isReverseConversion = false)
        {
            _lastConversionSucceeded = true;
            
            if (isReverseConversion)
            {
                Log(Localization.Get("Log.ReverseConversionComplete"));
            }
            else
            {
                Log(Localization.Get("Log.ConversionComplete"));
            }
            
            if (!string.IsNullOrEmpty(_lastBaselineCommitHash))
            {
                Log(Localization.Get("Log.BaselineRecorded", _lastBaselineCommitHash));
            }
            
            // 显示用户友好的成功提示对话框
            string successMessage;
            string title;
            
            if (isReverseConversion)
            {
                title = Localization.Get("Dialog.ReverseConversionSuccess");
                successMessage = Localization.Get("Dialog.ReverseConversionSuccess.Message");
            }
            else
            {
                title = Localization.Get("Dialog.ConversionSuccess");
                successMessage = Localization.Get("Dialog.ConversionSuccess.Message");
            }
            
            if (!string.IsNullOrEmpty(_lastBaselineCommitHash))
            {
                successMessage += Localization.Get("Dialog.ConversionSuccess.Baseline", _lastBaselineCommitHash);
            }
            
            successMessage += Localization.Get("Dialog.ConversionSuccess.Hint");
            
            EditorUtility.DisplayDialog(title, successMessage, Localization.Get("Button.IGotIt"));
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
            Log(Localization.Get("Log.XrGeneralSettingsAssetCreated", GeneratedGeneralSettingsAsset));
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
                Log(Localization.Get("Log.XrGeneralSettingsRegistered", key));
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
            Log(Localization.Get("Log.XrGeneralSettingsCreated", targetGroup));
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
            Log(Localization.Get("Log.XrManagerSettingsCreated", generalSettings.name));
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
                        var errorMsg = ErrorHandler.HandleException(ex, "AssignLoader 调用", showDialog: false);
                        Log(errorMsg);
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
            Log(Localization.Get("Log.DefaultObjectCreated", defaultObjectName));
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
                    Log(Localization.Get("Log.HandTrackingSyncAdded", controllerGo.name));
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
                
                Log(Localization.Get("Log.VrRigRootCreated"));
            }
            else
            {
                Log(Localization.Get("Log.VrRigExists"));
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

            Log(Localization.Get("Log.VrRigComplete"));
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
            
            Log(Localization.Get("Log.CameraBinding", targetGo.name, bindingInfo.Parent.name));
        }

        /// <summary>
        /// 禁用原 Main Camera 并返回其绑定信息，用于后续将 XR Origin 绑定到原父对象
        /// </summary>
        private LegacyCameraBindingInfo DisableLegacyMainCamera()
        {
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                Log(Localization.Get("Log.MainCameraNotFound"));
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
            Log(Localization.Get("Log.MainCameraDisabled", mainCam.gameObject.name));
            
            if (bindingInfo.Parent != null)
            {
                Log(Localization.Get("Log.CameraBindingDetected", bindingInfo.Parent.name));
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
                EditorUtility.DisplayDialog(Localization.Get("Dialog.Tip"), Localization.Get("Dialog.SampleAutoCopied"), Localization.Get("Button.Good"));
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
                        EditorUtility.DisplayDialog(Localization.Get("Dialog.ImportComplete"), Localization.Get("Dialog.SampleImported", sampleDisplayName), Localization.Get("Button.Good"));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(Localization.Get("Dialog.AutoImportFailed"), Localization.Get("Dialog.SampleImportFailed", sampleDisplayName), Localization.Get("Button.Good"));
                        TryOpenPackageManagerSamples(packageName, sampleDisplayName);
                    }
                    break;
                case 1:
                    Log(Localization.Get("Log.UserDeferredSample", sampleDisplayName));
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
                    Log(Localization.Get("Log.SampleAutoImported", sampleDisplayName));
                    return true;
                }
                catch (Exception ex)
                {
                    var errorMsg = ErrorHandler.HandleException(ex, $"自动导入 {sampleDisplayName}", showDialog: false);
                    Log(errorMsg);
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
                EditorUtility.DisplayDialog(Localization.Get("Dialog.PackageManagerOpened"), msg, Localization.Get("Button.Good"));
                return true;
            }

            if (EditorApplication.ExecuteMenuItem("Window/Package Manager"))
            {
                var fallbackMsg = Localization.Get("Log.PackageManagerOpened", packageName, sampleDisplayName);
                Log(fallbackMsg);
                EditorUtility.DisplayDialog(Localization.Get("Dialog.ManualImport"), fallbackMsg, Localization.Get("Button.Good"));
                return true;
            }

            Log(Localization.Get("Log.PackageManagerOpened", packageName, sampleDisplayName));
            EditorUtility.DisplayDialog(Localization.Get("Dialog.CannotOpenPackageManager"), Localization.Get("Dialog.PackageManagerManual"), Localization.Get("Button.Good"));
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
                        var errorMsg = ErrorHandler.HandleException(ex, "打开 Package Manager", showDialog: false);
                        Log(errorMsg);
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
            Log(Localization.Get("Log.InputActionAssetBound"));
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
                Log(Localization.Get("Log.InputActionBound", isRightHand ? Localization.Get("Log.Hand.Right") : Localization.Get("Log.Hand.Left")));
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
            Log(Localization.Get("Log.InputActionReferenceCreated", assetPath));
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
                    : Localization.Get("Log.ActionMapNotSpecified");
                var actionLabel = string.Join(" / ", actionNames);
                Log(Localization.Get("Log.InputActionReferenceFailed", candidatesLabel, actionLabel));
                return false;
            }

            try
            {
                reference.Set(action);
            }
            catch (Exception ex)
            {
                var mapName = action.actionMap != null ? action.actionMap.name : "<未知 Action Map>";
                var errorMsg = ErrorHandler.HandleException(ex, $"创建输入动作引用（{mapName}/{action.name}）", showDialog: false);
                Log(errorMsg);
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
                    Log(Localization.Get("Log.InputActionsNotFound"));
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
                EditorUtility.DisplayDialog(Localization.Get("Dialog.Tip"), Localization.Get("Dialog.VersionControlNotInstalled"), Localization.Get("Button.Good"));
                return;
            }

            if (TryAutoBackupWithGitAssistant(out var message))
            {
                Log(message);
                EditorUtility.DisplayDialog(Localization.Get("Dialog.BackupComplete"), message, Localization.Get("Button.Good"));
                MarkBackupConfirmed();
            }
            else
            {
                EditorUtility.DisplayDialog(Localization.Get("Dialog.BackupFailed"), message, Localization.Get("Button.Good"));
            }
        }

        private void AttemptRollbackToBaseline()
        {
            if (!IsGitAssistantInstalled())
            {
                EditorUtility.DisplayDialog(Localization.Get("Dialog.CannotRollback"), Localization.Get("Dialog.RollbackVersionControlRequired"), Localization.Get("Button.Good"));
                return;
            }

            if (string.IsNullOrEmpty(_lastBaselineCommitHash))
            {
                EditorUtility.DisplayDialog(Localization.Get("Dialog.CannotRollback"), Localization.Get("Dialog.RollbackNoBaseline"), Localization.Get("Button.Good"));
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    Localization.Get("Dialog.ConfirmRollback"),
                    Localization.Get("Dialog.RollbackWarning", _lastBaselineCommitHash),
                    Localization.Get("Button.ConfirmRollback"),
                    Localization.Get("Button.Cancel")))
            {
                return;
            }

            if (TryRunGitAssistantCommand($"reset --hard {_lastBaselineCommitHash}", out var output, out var error))
            {
                AssetDatabase.Refresh();
                _lastConversionSucceeded = false;
                EditorUtility.DisplayDialog(Localization.Get("Dialog.RollbackComplete"), Localization.Get("Dialog.RollbackRestored"), Localization.Get("Button.Good"));
                Log(Localization.Get("Log.RollbackComplete", output));
            }
            else
            {
                var reason = string.IsNullOrWhiteSpace(error) ? Localization.Get("Log.GitCommandFailed") : error;
                EditorUtility.DisplayDialog(Localization.Get("Dialog.RollbackFailed"), reason, Localization.Get("Button.Good"));
                Log(Localization.Get("Log.RollbackFailed", reason));
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
                    Localization.Get("Dialog.BackupBeforeAction"),
                    Localization.Get("Dialog.BackupBeforeActionMessage", actionName),
                    Localization.Get("Button.BackupConfirmed"),
                    Localization.Get("Button.Cancel"));
                if (confirmed)
                {
                    MarkBackupConfirmed();
                }
                return confirmed;
            }

            while (true)
            {
                int option = EditorUtility.DisplayDialogComplex(
                    Localization.Get("Dialog.BackupBeforeActionTitle"),
                    Localization.Get("Dialog.BackupBeforeActionMessage2", actionName),
                    Localization.Get("Button.BackupConfirmed"),
                    Localization.Get("Button.Cancel"),
                    Localization.Get("Button.UseVersionControlBackup"));

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
                            EditorUtility.DisplayDialog(Localization.Get("Dialog.BackupComplete"), message, Localization.Get("Button.Good"));
                            MarkBackupConfirmed();
                            return true;
                        }

                        if (!EditorUtility.DisplayDialog(Localization.Get("Dialog.BackupFailed"), $"{message}\n\n{Localization.Get("Dialog.BackupRetry")}", Localization.Get("Button.Retry"), Localization.Get("Button.Cancel")))
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
                Log(Localization.Get("Log.GitCommitRecorded", hash));
            }
            else
            {
                _lastBaselineCommitHash = string.Empty;
                Log(Localization.Get("Log.GitCommitNotRecorded"));
            }
        }

        private bool TryGetCurrentGitHead(out string hash)
        {
            hash = string.Empty;
            if (!TryRunGitAssistantCommand("rev-parse HEAD", out var output, out var error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Log(Localization.Get("Log.GitCommitFetchFailed", error));
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
                error = ErrorHandler.HandleException(ex, "执行 Git 命令", showDialog: false);
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

        #region Project Type Detection & Reverse Conversion

        /// <summary>
        /// 检测当前项目类型（3D 或 VR）
        /// </summary>
        private ProjectType DetectProjectType()
        {
            // 检查是否安装了 XR 相关包
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
            
            if (!File.Exists(manifestPath))
            {
                return ProjectType.Unknown;
            }

            try
            {
                var text = File.ReadAllText(manifestPath);
                bool hasXrManagement = text.Contains("com.unity.xr.management");
                bool hasOpenXr = text.Contains("com.unity.xr.openxr");
                bool hasXrInteraction = text.Contains("com.unity.xr.interaction.toolkit");
                
                // 如果安装了主要的 XR 包，认为是 VR 项目
                if (hasXrManagement && (hasOpenXr || hasXrInteraction))
                {
                    // 进一步检查场景中是否有 XR Origin
                    var xrOriginType = FindType(XrOriginTypeName);
                    if (xrOriginType != null)
                    {
                        var existingOrigin = FindComponentInScene(xrOriginType);
                        if (existingOrigin != null)
                        {
                            return ProjectType.VR;
                        }
                    }
                    
                    // 检查是否有 VRRig
                    var vrRig = GameObject.Find("VRRig");
                    if (vrRig != null)
                    {
                        return ProjectType.VR;
                    }
                    
                    // 即使场景中没有，如果包已安装，也认为是 VR 项目
                    return ProjectType.VR;
                }
                
                return ProjectType.Standard3D;
            }
            catch
            {
                return ProjectType.Unknown;
            }
        }

        /// <summary>
        /// 移除 XR 相关包
        /// </summary>
        private void RemoveXrPackages()
        {
            using (var progress = new ProgressReporter("移除 XR 包", "正在检查包依赖..."))
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    Log(Localization.Get("Log.ManifestNotFound"));
                    return;
                }

                progress.UpdateProgress(0.1f, "创建备份...");
                try
                {
                    OperationBackup.CreateQuickBackup(manifestPath);
                    Log(Localization.Get("Log.ManifestBackupCreated"));
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleException(ex, "创建 manifest.json 备份", showDialog: false);
                }

                string text;
                try
                {
                    text = File.ReadAllText(manifestPath);
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleException(ex, "读取 manifest.json", showDialog: true);
                    return;
                }

                bool modified = false;
                progress.UpdateProgress(0.3f, "移除 XR 包依赖...");
                
                // 使用逐行处理的方法，更可靠
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None).ToList();
                var newLines = new List<string>();
                bool inDependencies = false;
                
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var trimmedLine = line.Trim();
                    
                    // 检测 dependencies 块开始
                    if (trimmedLine.Contains("\"dependencies\""))
                    {
                        inDependencies = true;
                        newLines.Add(line);
                        continue;
                    }
                    
                    // 检测 dependencies 块结束
                    if (inDependencies && trimmedLine == "}")
                    {
                        inDependencies = false;
                        newLines.Add(line);
                        continue;
                    }
                    
                    // 在 dependencies 块内处理
                    if (inDependencies)
                    {
                        bool shouldRemove = false;
                        foreach (var pkg in RequiredPackages)
                        {
                            if (trimmedLine.Contains($"\"{pkg}\""))
                            {
                                shouldRemove = true;
                                modified = true;
                                Log(Localization.Get("Log.PackageRemoved", pkg));
                                break;
                            }
                        }
                        
                        if (!shouldRemove)
                        {
                            newLines.Add(line);
                        }
                        // 如果移除了这一行，检查下一行是否是最后一个条目（没有逗号）
                        // 如果是，需要给前一行添加逗号
                        else if (newLines.Count > 0)
                        {
                            // 检查下一行是否是最后一个条目
                            bool isLastEntry = true;
                            for (int j = i + 1; j < lines.Count; j++)
                            {
                                var nextLine = lines[j].Trim();
                                if (nextLine == "}" || nextLine == "},")
                                {
                                    break;
                                }
                                if (!string.IsNullOrEmpty(nextLine) && !nextLine.StartsWith("//"))
                                {
                                    isLastEntry = false;
                                    break;
                                }
                            }
                            
                            // 如果前一行有逗号，需要移除（因为现在它是最后一行了）
                            if (isLastEntry && newLines.Count > 0)
                            {
                                var lastLine = newLines[newLines.Count - 1];
                                if (lastLine.TrimEnd().EndsWith(","))
                                {
                                    newLines[newLines.Count - 1] = lastLine.TrimEnd().TrimEnd(',');
                                }
                            }
                        }
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }
                
                text = string.Join("\n", newLines);
                
                // 最后清理：移除连续的逗号、行尾逗号等
                text = System.Text.RegularExpressions.Regex.Replace(text, @",\s*,+", ",");
                text = System.Text.RegularExpressions.Regex.Replace(text, @",(\s*\n\s*})", "$1");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n\s*\n+", "\n");

                if (modified)
                {
                    File.WriteAllText(manifestPath, text);
                    AssetDatabase.Refresh();
                    Log("已移除 XR 包依赖，请等待 Unity 刷新");
                }
                else
                {
                    Log(Localization.Get("Log.NoPackagesToRemove"));
                }
            }
        }

        /// <summary>
        /// 恢复项目设置为 3D 模式
        /// </summary>
        private void Restore3DProjectSettings(IEnumerable<BuildTargetGroup> targetGroups = null)
        {
            using (var progress = new ProgressReporter("恢复项目设置", "正在初始化..."))
            {
                var desiredGroups = (targetGroups ?? TargetGroups)?.Distinct().ToArray() ?? Array.Empty<BuildTargetGroup>();
                if (desiredGroups.Length == 0)
                {
                    desiredGroups = TargetGroups;
                }

                progress.UpdateProgress(0.1f, "检查 XR Management 程序集...");
                var perBuildType = FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
                var generalType = FindType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management");
                var managerType = FindType("UnityEngine.XR.Management.XRManagerSettings, Unity.XR.Management");

                if (perBuildType == null || generalType == null || managerType == null)
                {
                    Log("未检测到 XR Management，可能已移除，跳过项目设置恢复");
                    return;
                }

                progress.UpdateProgress(0.3f, "禁用 XR 设置...");
                var perBuildAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GeneratedGeneralSettingsAsset);
                if (perBuildAsset == null)
                {
                    Log("未找到 XR General Settings 资产，跳过恢复");
                    return;
                }

                var getMethod = perBuildType.GetMethod("SettingsForBuildTarget", new[] { typeof(BuildTargetGroup) });
                if (getMethod == null)
                {
                    Log("无法获取 XR 设置，跳过恢复");
                    return;
                }

                progress.UpdateProgress(0.5f, "禁用目标平台的 XR...");
                int groupIndex = 0;
                foreach (var targetGroup in desiredGroups)
                {
                    groupIndex++;
                    progress.UpdateProgress(0.5f + (groupIndex / (float)desiredGroups.Length) * 0.4f, $"禁用 {targetGroup} 的 XR...");
                    
                    var generalSettings = getMethod.Invoke(perBuildAsset, new object[] { targetGroup }) as ScriptableObject;
                    if (generalSettings == null)
                    {
                        continue;
                    }

                    var so = new SerializedObject(generalSettings);
                    var initProp = so.FindProperty("m_InitManagerOnStart");
                    if (initProp != null)
                    {
                        initProp.boolValue = false;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    var managerProp = so.FindProperty("m_LoaderManagerInstance");
                    if (managerProp != null)
                    {
                        var managerSettings = managerProp.objectReferenceValue as ScriptableObject;
                        if (managerSettings != null)
                        {
                            var managerSO = new SerializedObject(managerSettings);
                            var loadersProp = managerSO.FindProperty("m_Loaders");
                            if (loadersProp != null)
                            {
                                loadersProp.arraySize = 0;
                                managerSO.ApplyModifiedPropertiesWithoutUndo();
                            }
                        }
                    }

                    Log(Localization.Get("Log.XrSettingsDisabled", targetGroup));
                }

                progress.UpdateProgress(0.95f, "保存资源...");
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// 将 VR 场景转换回 3D 场景
        /// </summary>
        private bool ConvertCurrentSceneTo3D()
        {
            using (var progress = new ProgressReporter("转换场景为 3D", "正在初始化..."))
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (!scene.IsValid())
                {
                    Log(Localization.Get("Log.NoSceneOpen"));
                    return false;
                }

                progress.UpdateProgress(0.2f, "查找 XR Origin...");
                
                // 查找并移除 XR Origin
                var xrOriginType = FindType(XrOriginTypeName);
                Component existingOrigin = null;
                if (xrOriginType != null)
                {
                    existingOrigin = FindComponentInScene(xrOriginType);
                }

                // 查找 VRRig
                var vrRig = GameObject.Find("VRRig");
                
                GameObject mainCameraToRestore = null;
                Transform cameraParent = null;
                Vector3 cameraLocalPos = Vector3.zero;
                Quaternion cameraLocalRot = Quaternion.identity;

                if (existingOrigin != null)
                {
                    progress.UpdateProgress(0.4f, "处理 XR Origin...");
                    var originGo = existingOrigin.gameObject;
                    
                    // 尝试从 XR Origin 中提取相机信息
                    var cameraOffset = originGo.transform.Find("Camera Offset");
                    if (cameraOffset != null)
                    {
                        var cameraGo = cameraOffset.Find("Main Camera");
                        if (cameraGo != null)
                        {
                            mainCameraToRestore = cameraGo.gameObject;
                            cameraParent = originGo.transform.parent;
                            cameraLocalPos = originGo.transform.localPosition;
                            cameraLocalRot = originGo.transform.localRotation;
                        }
                    }
                    
                    // 移除 XR Origin
                    Undo.DestroyObjectImmediate(originGo);
                    Log(Localization.Get("Log.XrOriginRemoved"));
                }
                else if (vrRig != null)
                {
                    progress.UpdateProgress(0.4f, "处理 VRRig...");
                    var cameraOffset = vrRig.transform.Find("CameraOffset");
                    if (cameraOffset != null)
                    {
                        var cameraGo = cameraOffset.Find("Main Camera");
                        if (cameraGo != null)
                        {
                            mainCameraToRestore = cameraGo.gameObject;
                            cameraParent = vrRig.transform.parent;
                            cameraLocalPos = vrRig.transform.localPosition;
                            cameraLocalRot = vrRig.transform.localRotation;
                        }
                    }
                    
                    // 移除 VRRig
                    Undo.DestroyObjectImmediate(vrRig);
                    Log(Localization.Get("Log.VrRigRemoved"));
                }

                // 恢复或创建 Main Camera
                progress.UpdateProgress(0.6f, "恢复 Main Camera...");
                Camera mainCamera = null;
                
                if (mainCameraToRestore != null)
                {
                    // 从 XR Origin/VRRig 中提取的相机
                    mainCameraToRestore.transform.SetParent(cameraParent, false);
                    mainCameraToRestore.transform.localPosition = cameraLocalPos;
                    mainCameraToRestore.transform.localRotation = cameraLocalRot;
                    mainCamera = mainCameraToRestore.GetComponent<Camera>();
                    if (mainCamera == null)
                    {
                        mainCamera = mainCameraToRestore.AddComponent<Camera>();
                    }
                    mainCameraToRestore.tag = "MainCamera";
                    mainCameraToRestore.SetActive(true);
                    Log(Localization.Get("Log.MainCameraRestored"));
                }
                else
                {
                    // 查找场景中是否有被禁用的 Main Camera
                    var allCameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
                    foreach (var cam in allCameras)
                    {
                        if (cam.CompareTag("MainCamera") && !cam.gameObject.activeSelf)
                        {
                        cam.gameObject.SetActive(true);
                        mainCamera = cam;
                        Log(Localization.Get("Log.MainCameraEnabled"));
                        break;
                        }
                    }
                    
                    // 如果没有找到，创建一个新的 Main Camera
                    if (mainCamera == null)
                    {
                        var cameraGo = new GameObject("Main Camera");
                        Undo.RegisterCreatedObjectUndo(cameraGo, "Create Main Camera");
                        mainCamera = cameraGo.AddComponent<Camera>();
                        cameraGo.AddComponent<AudioListener>();
                        cameraGo.tag = "MainCamera";
                        cameraGo.transform.position = new Vector3(0, 1.6f, -10);
                        Log(Localization.Get("Log.MainCameraCreated"));
                    }
                }

                // 移除 XR Interaction Manager 和 Input Action Manager
                progress.UpdateProgress(0.8f, "清理 XR 管理器...");
                var interactionManagerType = FindType(XrInteractionManagerTypeName);
                if (interactionManagerType != null)
                {
                    var interactionManager = FindComponentInScene(interactionManagerType);
                    if (interactionManager != null)
                    {
                        Undo.DestroyObjectImmediate(interactionManager.gameObject);
                        Log(Localization.Get("Log.InteractionManagerRemoved"));
                    }
                }

                var inputActionManagerType = FindType(InputActionManagerTypeName);
                if (inputActionManagerType != null)
                {
                    var inputActionManager = FindComponentInScene(inputActionManagerType);
                    if (inputActionManager != null)
                    {
                        Undo.DestroyObjectImmediate(inputActionManager.gameObject);
                        Log(Localization.Get("Log.InputActionManagerRemoved"));
                    }
                }

                // 移除 VRHandTrackingSync 组件（如果存在）
                progress.UpdateProgress(0.85f, "清理 VR 相关组件...");
                var vrHandTrackingSyncType = Type.GetType("VRHandTrackingSync, Assembly-CSharp");
                if (vrHandTrackingSyncType != null)
                {
                    var allSyncComponents = UnityEngine.Object.FindObjectsOfType(vrHandTrackingSyncType, true);
                    if (allSyncComponents != null && allSyncComponents.Length > 0)
                    {
                        foreach (var component in allSyncComponents)
                        {
                            if (component is Component comp)
                            {
                                Undo.DestroyObjectImmediate(comp);
                            }
                        }
                        Log(Localization.Get("Log.VrHandTrackingSyncRemoved", allSyncComponents.Length));
                    }
                }

                progress.UpdateProgress(0.9f, "保存场景...");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Log(Localization.Get("Log.SceneConvertedTo3D"));
                return true;
            }
        }

        /// <summary>
        /// 清理 VRConverterGenerated 目录，移除所有生成的 VR 相关资源
        /// </summary>
        private void CleanupGeneratedVrAssets()
        {
            try
            {
                if (Directory.Exists(GeneratedRootFolder))
                {
                    Log($"正在清理生成的 VR 资源目录: {GeneratedRootFolder}");
                    
                    // 删除整个 VRConverterGenerated 目录
                    FileUtil.DeleteFileOrDirectory(GeneratedRootFolder);
                    
                    // 删除对应的 .meta 文件
                    var metaPath = GeneratedRootFolder + ".meta";
                    if (File.Exists(metaPath))
                    {
                        FileUtil.DeleteFileOrDirectory(metaPath);
                    }
                    
                    AssetDatabase.Refresh();
                    Log(Localization.Get("Log.GeneratedVrAssetsCleaned"));
                }
            }
            catch (Exception ex)
            {
                var errorMsg = ErrorHandler.HandleException(ex, "清理生成的 VR 资源", showDialog: false);
                Log($"清理生成的 VR 资源时出错: {errorMsg}");
            }
        }

        #endregion

        #region Utility helpers

        private void QueuePackageInstall(string packageName)
        {
            try
            {
                var request = Client.Add(packageName);
                _pendingAddRequests.Add(request);
                Log(Localization.Get("Log.PackageInstallRequested", packageName));
                StartMonitoringPackageInstalls();
            }
            catch (Exception ex)
            {
                var errorMsg = ErrorHandler.HandleException(ex, $"安装 {packageName}", showDialog: false);
                Log(errorMsg);
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
                    Log(Localization.Get("Log.PackageInstallSuccess", request.Result.packageId));
                }
                else if (request.Status == StatusCode.Failure)
                {
                    Log(Localization.Get("Log.PackageInstallFailed", request.Error?.message));
                }
                else
                {
                    Log(Localization.Get("Log.PackageInstallStatus", request.Status));
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
                    Log(Localization.Get("Log.ManifestVersionRemoved", pkg));
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


