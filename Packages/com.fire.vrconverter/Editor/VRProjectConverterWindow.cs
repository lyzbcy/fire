using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            "com.unity.xr.interaction.toolkit"
        };

        private static readonly BuildTargetGroup[] TargetGroups =
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android
        };

        private const string GeneratedSettingsFolder = "Assets/VRConverterGenerated/XR";
        private const string GeneratedGeneralSettingsAsset = GeneratedSettingsFolder + "/XRGeneralSettings.asset";

        private const string OpenXrLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string XrOriginTypeName = "Unity.XR.CoreUtils.XROrigin";
        private const string ActionBasedControllerTypeName = "UnityEngine.XR.Interaction.Toolkit.ActionBasedController";
        private const string XrInteractionManagerTypeName = "UnityEngine.XR.Interaction.Toolkit.XRInteractionManager";
        private const string InputActionManagerTypeName = "UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager";
        private const string TrackedPoseDriverTypeName = "UnityEngine.InputSystem.XR.TrackedPoseDriver";
        private const string XrRayInteractorTypeName = "UnityEngine.XR.Interaction.Toolkit.XRRayInteractor";

        private Vector2 _scroll;
        private string _log = "";
        private readonly List<AddRequest> _pendingAddRequests = new List<AddRequest>();
        private bool _isMonitoringAddRequests;

        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            var window = GetWindow<VRProjectConverterWindow>("一键VR转换");
            window.minSize = new Vector2(420, 320);
            window.Log("打开一键 VR 转换工具。");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("一键 VR 转换", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "此工具会尝试：\n" +
                "1. 在 manifest.json 中确保 XR Management / OpenXR / XR Interaction Toolkit。\n" +
                "2. 自动配置 XR Plug-in Management：为 Standalone/Android 启用 OpenXR Loader。\n" +
                "3. 在当前场景中创建 XR Interaction Toolkit 的 XR Origin（若包已导入），否则回退到通用 VR Rig。\n\n" +
                "提示：首次添加 XR 包后 Unity 需要重新导入，等编译完成后再次点击“配置 + 场景转换”即可。",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("一键执行所有步骤（推荐）"))
            {
                RunAllSteps();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("第1步：只检查并添加 XR 依赖包"))
            {
                EnsureXrPackages();
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                if (GUILayout.Button("第2步：配置 XR 设置 + 创建/更新场景 VR Rig"))
                {
                    ConfigureXrProjectSettings();
                    ConvertCurrentSceneToVr();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("执行日志：", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Log(string msg)
        {
            _log += $"[{System.DateTime.Now:HH:mm:ss}] {msg}\n";
            Repaint();
        }

        private void RunAllSteps()
        {
            EnsureXrPackages();
            if (EditorApplication.isCompiling)
            {
                Log("Unity 正在导入/编译 XR 包，请等待完成后再执行“第2步”。");
                return;
            }

            ConfigureXrProjectSettings();
            ConvertCurrentSceneToVr();
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
        private void ConfigureXrProjectSettings()
        {
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

            foreach (var targetGroup in TargetGroups)
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

        /// <summary>
        /// 根据项目当前安装的包尝试创建 XR Origin；若 XR Interaction Toolkit 不可用，则回退到基础 VR Rig。
        /// </summary>
        private void ConvertCurrentSceneToVr()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Log("当前没有打开的场景，无法转换。");
                return;
            }

            DisableLegacyMainCamera();

            if (TryCreateOrUpdateXrOriginRig())
            {
                Log("XR Origin (XR Interaction Toolkit) 已创建/更新。");
            }
            else
            {
                Log("XR Interaction Toolkit 或 XR Core Utils 不可用，使用基础 VRRig。");
                CreateFallbackVrRig();
            }
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

        private bool TryCreateOrUpdateXrOriginRig()
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
                return true;
            }

            var originGo = new GameObject("XR Origin (Action Based)");
            Undo.RegisterCreatedObjectUndo(originGo, "Create XR Origin");
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
            TryEnsureSingletonComponent(InputActionManagerTypeName, "XR Input Action Manager");
        }

        private void TryEnsureSingletonComponent(string typeName, string defaultObjectName)
        {
            var type = FindType(typeName);
            if (type == null)
            {
                return;
            }

            if (FindComponentInScene(type) != null)
            {
                return;
            }

            var go = new GameObject(defaultObjectName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {defaultObjectName}");
            go.AddComponent(type);
            Log($"已创建 {defaultObjectName}。");
        }

        private void TrySetupActionController(GameObject controllerGo, bool isRightHand = false)
        {
            if (controllerGo == null) return;

            var controller = TryAddComponent(controllerGo, ActionBasedControllerTypeName);
            TryAddComponent(controllerGo, XrRayInteractorTypeName);

            if (controllerGo.GetComponent<LineRenderer>() == null)
            {
                var lr = controllerGo.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = false;
                lr.widthMultiplier = 0.005f;
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

        private void CreateFallbackVrRig()
        {
            var existingRig = GameObject.Find("VRRig");
            if (existingRig == null)
            {
                existingRig = new GameObject("VRRig");
                Undo.RegisterCreatedObjectUndo(existingRig, "Create VRRig");
                Log("已创建 VRRig 根对象。");
            }
            else
            {
                Log("场景中已存在 VRRig，对其进行复用/更新。");
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

        private void DisableLegacyMainCamera()
        {
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                Log("未找到标签为 MainCamera 的原主相机。");
                return;
            }

            mainCam.gameObject.SetActive(false);
            Log($"已禁用原主相机：{mainCam.gameObject.name}");
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

        private static Type FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;

            var type = Type.GetType(fullName);
            if (type != null) return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }

        #endregion
    }
}


