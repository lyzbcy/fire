using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PoseDrive.Runtime.Core;
using PoseDrive.Runtime.Input;
using PoseDrive.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace PoseDrive.Editor
{
    /// <summary>
    /// 一键搭建 PoseDrive 的配置向导。
    /// </summary>
    public class PoseDriveSetupWizard : EditorWindow
    {
        private const string RootName = "PoseControllerSystem";

        private PoseDetector _detector;
        private ActionClassifier _classifier;
        private PoseDriveManager _manager;
        private PoseInputMapper _mapper;

        // 使用 UnityEngine.Object 以支持运行时检测，避免编译时依赖
        private UnityEngine.Object _moveNetModel;
        private UnityEngine.Object _actionModel;

        private ActionMappingAsset _mappingAsset;
        private Vector2 _scroll;

        [MenuItem("Tools/PoseDrive/配置向导", priority = 220)]
        public static void Open()
        {
            PoseDriveSetupWizard window =
                GetWindow<PoseDriveSetupWizard>(PoseDriveLocalization.Tr("wizard.title"));
            window.UpdateTitle();
        }

        private void OnEnable()
        {
            PoseDriveLocalization.LanguageChanged += HandleLanguageChanged;
            UpdateTitle();
            RefreshReferences();
            PoseDriveLogger.LogInfo("配置向导窗口已打开");
        }

        private void OnDisable()
        {
            PoseDriveLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged()
        {
            UpdateTitle();
            Repaint();
        }

        private void UpdateTitle()
        {
            titleContent = new GUIContent(PoseDriveLocalization.Tr("wizard.title"));
        }

        private void RefreshReferences()
        {
            _detector = FindObjectOfType<PoseDetector>();
            _classifier = FindObjectOfType<ActionClassifier>();
            _manager = FindObjectOfType<PoseDriveManager>();
            _mapper = FindObjectOfType<PoseInputMapper>();
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(EditorStylesLibrary.Container))
            {
                EditorStylesLibrary.DrawHeader(
                    PoseDriveLocalization.Tr("wizard.title"),
                    PoseDriveLocalization.Tr("wizard.subtitle"));

                _scroll = EditorGUILayout.BeginScrollView(_scroll, true, true);

                DrawStep(PoseDriveLocalization.Tr("wizard.step1"), PoseDriveLocalization.Tr("wizard.step1.desc"), DrawDependencies);
                DrawStep(PoseDriveLocalization.Tr("wizard.step2"), null, DrawModelSelectors);
                DrawStep(PoseDriveLocalization.Tr("wizard.step3"), null, DrawMappingSection);
                DrawStep(PoseDriveLocalization.Tr("wizard.step4"), null, DrawRuntimeSection);
                DrawStep(PoseDriveLocalization.Tr("wizard.step5"), null, DrawShortcutsSection);

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawStep(string title, string subtitle, Action drawer)
        {
            using (EditorStylesLibrary.CardScope(title, subtitle))
            {
                drawer?.Invoke();
            }
        }

        private void DrawDependencies()
        {
            // 检查编译时宏
#if UNITY_SENTIS
            bool hasCompileTimeMacro = true;
#else
            bool hasCompileTimeMacro = false;
#endif

            bool hasSentisAssemblies = Type.GetType("Unity.Sentis.TensorFloat, Unity.Sentis") != null;
            bool hasSentisInManifest = IsPackageInManifest("com.unity.sentis");
            
            PoseDriveLogger.LogDebug($"步骤1 - 检查依赖: 编译时宏={hasCompileTimeMacro}, 运行时检测={hasSentisAssemblies}, 在 manifest 中={hasSentisInManifest}");

            // 自动检查并更新宏（静默执行，不弹窗）
            if (hasSentisInManifest && !hasCompileTimeMacro)
            {
                PoseDriveMacroManager.CheckAndUpdateMacro();
            }
            
            // 自定义 Sentis 状态显示，支持两个按钮
            GUILayout.BeginHorizontal();
            GUIContent sentisContent = new GUIContent(
                PoseDriveLocalization.Tr("wizard.dependency.barracuda.name"),
                PoseDriveLocalization.Tr("wizard.dependency.barracuda.tip"));
            GUILayout.Label(sentisContent, EditorStylesLibrary.Body, GUILayout.Width(120));
            Color prev = GUI.color;
            GUI.color = hasSentisAssemblies ? Color.green : Color.red;
            GUILayout.Label(
                hasSentisAssemblies ? PoseDriveLocalization.Tr("status.ready") : PoseDriveLocalization.Tr("status.missing"),
                EditorStylesLibrary.Body, GUILayout.Width(60));
            GUI.color = prev;

            if (!hasSentisAssemblies)
            {
                if (GUILayout.Button(PoseDriveLocalization.Tr("wizard.dependency.barracuda.action"), 
                    EditorStylesLibrary.ToolbarButton, GUILayout.Width(160)))
                {
                    OpenSentisPackagePage();
                }
                
                // 如果不在 manifest 中，显示自动安装按钮
                if (!hasSentisInManifest)
                {
                    if (GUILayout.Button(PoseDriveLocalization.Tr("wizard.dependency.barracuda.install"), 
                        EditorStylesLibrary.ToolbarButton, GUILayout.Width(160)))
                    {
                        InstallSentis();
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (!hasSentisAssemblies)
            {
                GUILayout.Label(PoseDriveLocalization.Tr("wizard.dependency.barracuda.tip"), EditorStylesLibrary.Secondary);
            }

            bool hasCamera = WebCamTexture.devices is { Length: > 0 };
            DrawStatus(
                PoseDriveLocalization.Tr("wizard.dependency.camera.name"),
                hasCamera,
                PoseDriveLocalization.Tr("wizard.dependency.camera.tip"),
                hasCamera ? null : PoseDriveLocalization.Tr("wizard.dependency.camera.action"),
                () => Application.OpenURL("https://support.unity.com/hc/zh-cn/articles/4412149980051"));
        }

        private void DrawModelSelectors()
        {
            Type modelAssetType = Type.GetType("Unity.Sentis.ModelAsset, Unity.Sentis");
            bool hasSentis = modelAssetType != null;

            PoseDriveLogger.LogDebug($"步骤2 - 选择模型: SentisModelAssetType={(hasSentis ? "可用" : "缺失")}");

            if (!hasSentis)
            {
                EditorGUILayout.HelpBox(PoseDriveLocalization.Tr("wizard.model.noBarracuda"), MessageType.Warning);
                PoseDriveLogger.LogWarning("步骤2 - Sentis 未安装或 ModelAsset 类型不可用，无法选择模型");
                return;
            }

            _moveNetModel = EditorGUILayout.ObjectField(
                PoseDriveLocalization.Tr("wizard.model.movenet"),
                _moveNetModel,
                modelAssetType,
                false);

            _actionModel = EditorGUILayout.ObjectField(
                PoseDriveLocalization.Tr("wizard.model.action"),
                _actionModel,
                modelAssetType,
                false);

            if (_detector != null && GUILayout.Button(PoseDriveLocalization.Tr("wizard.model.pullDetector")))
            {
                TryPullModelReference(_detector, "_modelAsset", ref _moveNetModel, "PoseDetector");
            }

            if (_classifier != null && GUILayout.Button(PoseDriveLocalization.Tr("wizard.model.pullClassifier")))
            {
                TryPullModelReference(_classifier, "_modelAsset", ref _actionModel, "ActionClassifier");
            }
        }

        private void TryPullModelReference(Component component, string fieldName, ref UnityEngine.Object targetSlot, string sourceName)
        {
            if (component == null)
            {
                return;
            }

            try
            {
                SerializedObject so = new SerializedObject(component);
                SerializedProperty prop = so.FindProperty(fieldName);
                if (prop != null && prop.objectReferenceValue != null)
                {
                    targetSlot = prop.objectReferenceValue;
                    PoseDriveLogger.LogInfo($"从 {sourceName} 拉取了模型引用");
                    EditorUtility.DisplayDialog("成功", $"已成功从 {sourceName} 拉取模型！", "确定");
                    return;
                }

                var field = component.GetType().GetField(
                    fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var value = field.GetValue(component) as UnityEngine.Object;
                    if (value != null)
                    {
                        targetSlot = value;
                        PoseDriveLogger.LogInfo($"通过反射从 {sourceName} 拉取了模型引用");
                        EditorUtility.DisplayDialog("成功", $"已成功通过反射从 {sourceName} 拉取模型！", "确定");
                        return;
                    }
                }

                EditorUtility.DisplayDialog("提示", $"{sourceName} 中未找到已配置的模型。", "确定");
                PoseDriveLogger.LogWarning($"{sourceName} 中未找到已配置的模型字段 {fieldName}");
            }
            catch (Exception ex)
            {
                PoseDriveLogger.LogException(ex, $"从 {sourceName} 拉取模型时出错");
                EditorUtility.DisplayDialog("错误", $"拉取模型时出错：{ex.Message}", "确定");
            }
        }

        private void DrawMappingSection()
        {
            _mappingAsset = (ActionMappingAsset)EditorGUILayout.ObjectField(
                PoseDriveLocalization.Tr("wizard.binding.asset"),
                _mappingAsset,
                typeof(ActionMappingAsset),
                false);

            if (_mapper != null && GUILayout.Button(PoseDriveLocalization.Tr("wizard.mapping.useExisting")))
            {
                SerializedObject so = new SerializedObject(_mapper);
                _mappingAsset = so.FindProperty("_mappingAsset")?.objectReferenceValue as ActionMappingAsset;
            }

            if (GUILayout.Button(PoseDriveLocalization.Tr("wizard.binding.default")))
            {
                CreateDefaultMappingAsset();
            }
        }

        private void DrawRuntimeSection()
        {
            if (GUILayout.Button(PoseDriveLocalization.Tr("wizard.runtime.button"), GUILayout.Height(32)))
            {
                EnsureRuntimeExists();
                ConfigureRuntime();
                RefreshReferences();
            }

            if (_manager != null)
            {
                EditorGUILayout.HelpBox(
                    string.Format(PoseDriveLocalization.Tr("wizard.runtime.detected"), _manager.name),
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(PoseDriveLocalization.Tr("wizard.runtime.missing"), MessageType.Info);
            }
        }

        private void DrawShortcutsSection()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(PoseDriveLocalization.Tr("wizard.open.window")))
            {
                PoseDriveWindow.Open();
            }

            if (GUILayout.Button(PoseDriveLocalization.Tr("wizard.open.mapping")))
            {
                ActionMappingEditor.Open();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(PoseDriveLocalization.Tr("wizard.open.scene.tp")))
            {
                OpenScene("Assets/lyzbcyMade PoseController/Samples~/ThirdPersonDemo/ThirdPersonDemo.unity");
            }

            if (GUILayout.Button(PoseDriveLocalization.Tr("wizard.open.scene.fp")))
            {
                OpenScene("Assets/lyzbcyMade PoseController/Samples~/FirstPersonDemo/FirstPersonDemo.unity");
            }
        }

        private static void DrawStatus(string label, bool ok, string tip, string actionLabel = null, Action action = null)
        {
            GUILayout.BeginHorizontal();
            GUIContent content = new GUIContent(label, tip);
            GUILayout.Label(content, EditorStylesLibrary.Body, GUILayout.Width(120));
            Color prev = GUI.color;
            GUI.color = ok ? Color.green : Color.red;
            GUILayout.Label(
                ok ? PoseDriveLocalization.Tr("status.ready") : PoseDriveLocalization.Tr("status.missing"),
                EditorStylesLibrary.Body, GUILayout.Width(60));
            GUI.color = prev;

            if (!ok && !string.IsNullOrEmpty(actionLabel) && action != null)
            {
                if (GUILayout.Button(actionLabel, EditorStylesLibrary.ToolbarButton, GUILayout.Width(160)))
                {
                    action();
                }
            }

            GUILayout.EndHorizontal();

            if (!ok)
            {
                GUILayout.Label(tip, EditorStylesLibrary.Secondary);
            }
        }

        private void EnsureRuntimeExists()
        {
            if (_manager != null)
            {
                return;
            }

            GameObject root = new GameObject(RootName);
            _detector = root.AddComponent<PoseDetector>();
            _classifier = root.AddComponent<ActionClassifier>();
            _mapper = root.AddComponent<PoseInputMapper>();
            _manager = root.AddComponent<PoseDriveManager>();
            root.AddComponent<WebcamProvider>();
            MarkSceneDirty(root);
        }

        private void ConfigureRuntime()
        {
            if (_manager == null)
            {
                return;
            }

            if (_detector == null)
            {
                _detector = _manager.GetComponent<PoseDetector>() ?? _manager.gameObject.AddComponent<PoseDetector>();
            }

            if (_classifier == null)
            {
                _classifier = _manager.GetComponent<ActionClassifier>() ?? _manager.gameObject.AddComponent<ActionClassifier>();
            }

            if (_mapper == null)
            {
                _mapper = _manager.GetComponent<PoseInputMapper>() ?? _manager.gameObject.AddComponent<PoseInputMapper>();
            }

            // 配置 Sentis 模型
            Type modelAssetType = Type.GetType("Unity.Sentis.ModelAsset, Unity.Sentis");
            if (modelAssetType == null)
            {
                PoseDriveLogger.LogWarning("Sentis 未安装，跳过模型配置");
            }
            else
            {
                if (_detector != null && _moveNetModel != null)
                {
                    try
                    {
                        SerializedObject so = new SerializedObject(_detector);
                        SerializedProperty prop = so.FindProperty("_modelAsset");
                        if (prop != null)
                        {
                            prop.objectReferenceValue = _moveNetModel;
                            so.ApplyModifiedProperties();
                            PoseDriveLogger.LogInfo("已为 PoseDetector 配置 MoveNet 模型（Sentis ModelAsset）");
                        }
                    }
                    catch (Exception ex)
                    {
                        PoseDriveLogger.LogException(ex, "配置 PoseDetector 模型时出错");
                    }
                }

                if (_classifier != null && _actionModel != null)
                {
                    try
                    {
                        SerializedObject so = new SerializedObject(_classifier);
                        SerializedProperty prop = so.FindProperty("_modelAsset");
                        if (prop != null)
                        {
                            prop.objectReferenceValue = _actionModel;
                            so.ApplyModifiedProperties();
                            PoseDriveLogger.LogInfo("已为 ActionClassifier 配置动作分类模型（Sentis ModelAsset）");
                        }
                    }
                    catch (Exception ex)
                    {
                        PoseDriveLogger.LogException(ex, "配置 ActionClassifier 模型时出错");
                    }
                }
            }

            if (_mapper != null && _mappingAsset != null)
            {
                SerializedObject so = new SerializedObject(_mapper);
                SerializedProperty prop = so.FindProperty("_mappingAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = _mappingAsset;
                    so.ApplyModifiedProperties();
                }
            }

            if (_manager != null)
            {
                SerializedObject so = new SerializedObject(_manager);
                SerializedProperty poseProp = so.FindProperty("_poseDetector");
                if (poseProp != null)
                {
                    poseProp.objectReferenceValue = _detector;
                }

                SerializedProperty classifierProp = so.FindProperty("_actionClassifier");
                if (classifierProp != null)
                {
                    classifierProp.objectReferenceValue = _classifier;
                }

                SerializedProperty mapperProp = so.FindProperty("_inputMapper");
                if (mapperProp != null)
                {
                    mapperProp.objectReferenceValue = _mapper;
                }

                so.ApplyModifiedProperties();
            }

            MarkSceneDirty(_manager.gameObject);
        }

        private void CreateDefaultMappingAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                PoseDriveLocalization.Tr("wizard.binding.createTitle"),
                "PoseDefaultMappings",
                "asset",
                PoseDriveLocalization.Tr("wizard.binding.createDesc"));
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ActionMappingAsset asset = ScriptableObject.CreateInstance<ActionMappingAsset>();
            asset.Bindings.Add(new GestureBinding { GestureName = "Nod", Key = VirtualKey.Keyboard(KeyCode.E) });
            asset.Bindings.Add(new GestureBinding { GestureName = "Shake", Key = VirtualKey.Keyboard(KeyCode.Q) });
            asset.Bindings.Add(new GestureBinding { GestureName = "Grab", Key = VirtualKey.Mouse(0) });
            asset.Bindings.Add(new GestureBinding { GestureName = "HandsUp", Key = VirtualKey.Keyboard(KeyCode.Space) });

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _mappingAsset = asset;
        }

        private static void MarkSceneDirty(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(obj.scene);
        }

        private static void OpenScene(string path)
        {
            PoseDriveLogger.LogInfo($"尝试打开场景: {path}");
            
            // 使用 AssetDatabase 检查资源是否存在（而不是 File.Exists）
            // 因为 Unity 资源可能还没有导入
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                // 如果找不到，尝试刷新资源数据库
                AssetDatabase.Refresh();
                guid = AssetDatabase.AssetPathToGUID(path);
                
                if (string.IsNullOrEmpty(guid))
                {
                    PoseDriveLogger.LogWarning($"场景文件不存在: {path}");
                    EditorUtility.DisplayDialog(
                        PoseDriveLocalization.Tr("dialog.sceneMissing.title"),
                        string.Format(PoseDriveLocalization.Tr("dialog.sceneMissing.message"), path),
                        PoseDriveLocalization.Tr("dialog.sceneMissing.ok"));
                    return;
                }
            }

            try
            {
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                    PoseDriveLogger.LogInfo($"成功打开场景: {path}");
                }
                else
                {
                    PoseDriveLogger.LogInfo("用户取消了场景切换");
                }
            }
            catch (Exception ex)
            {
                PoseDriveLogger.LogException(ex, $"打开场景时出错: {path}");
                EditorUtility.DisplayDialog(
                    "错误",
                    $"无法打开场景: {ex.Message}",
                    "确定");
            }
        }

        private static void OpenSentisPackagePage()
        {
            Type pmWindow =
                Type.GetType("UnityEditor.PackageManager.UI.Window,UnityEditor.PackageManagerUINative") ??
                Type.GetType("UnityEditor.PackageManager.UI.Window,UnityEditor.PackageManagerUI");

            if (pmWindow != null)
            {
                MethodInfo openMethod = pmWindow.GetMethod(
                    "Open",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(string) },
                    null);

                if (openMethod != null)
                {
                    openMethod.Invoke(null, new object[] { "com.unity.sentis" });
                    return;
                }
            }

            Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.sentis@latest");
        }

        /// <summary>
        /// 检查包是否在 manifest.json 中
        /// </summary>
        private static bool IsPackageInManifest(string packageName)
        {
            try
            {
                string manifestPath = "Packages/manifest.json";
                if (!File.Exists(manifestPath))
                {
                    PoseDriveLogger.LogWarning($"manifest.json 文件不存在: {manifestPath}");
                    return false;
                }

                string content = File.ReadAllText(manifestPath, Encoding.UTF8);
                bool exists = content.Contains($"\"{packageName}\"");
                PoseDriveLogger.LogDebug($"检查包 {packageName} 在 manifest 中: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                PoseDriveLogger.LogException(ex, $"检查包 {packageName} 在 manifest 中时出错");
                return false;
            }
        }

        /// <summary>
        /// 安装 Sentis 包到 manifest.json
        /// </summary>
        private static void InstallSentis()
        {
            PoseDriveLogger.LogInfo("开始安装 Sentis 包");
            
            // 先检查是否已经在 manifest 中
            if (IsPackageInManifest("com.unity.sentis"))
            {
                PoseDriveLogger.LogWarning("Sentis 已在 manifest 中，无需重复安装");
                EditorUtility.DisplayDialog(
                    PoseDriveLocalization.Tr("wizard.dependency.barracuda.name"),
                    PoseDriveLocalization.Tr("wizard.dependency.barracuda.install.already"),
                    "确定");
                return;
            }

            // 询问用户是否同意
            bool confirmed = EditorUtility.DisplayDialog(
                PoseDriveLocalization.Tr("wizard.dependency.barracuda.name"),
                PoseDriveLocalization.Tr("wizard.dependency.barracuda.install.confirm"),
                "确定",
                "取消");

            if (!confirmed)
            {
                PoseDriveLogger.LogInfo("用户取消了 Sentis 安装");
                return;
            }
            
            PoseDriveLogger.LogInfo("用户确认安装 Sentis");

            try
            {
                string manifestPath = "Packages/manifest.json";
                if (!File.Exists(manifestPath))
                {
                    EditorUtility.DisplayDialog(
                        "错误",
                        $"找不到文件: {manifestPath}",
                        "确定");
                    return;
                }

                // 读取 manifest.json
                string content = File.ReadAllText(manifestPath, Encoding.UTF8);
                
                // 检查是否已存在
                if (content.Contains("com.unity.sentis"))
                {
                    EditorUtility.DisplayDialog(
                        PoseDriveLocalization.Tr("wizard.dependency.barracuda.name"),
                        PoseDriveLocalization.Tr("wizard.dependency.barracuda.install.already"),
                        "确定");
                    return;
                }

                // 查找 "dependencies": { 的位置
                int dependenciesIndex = content.IndexOf("\"dependencies\"");
                if (dependenciesIndex == -1)
                {
                    EditorUtility.DisplayDialog(
                        "错误",
                        "无法在 manifest.json 中找到 dependencies 部分",
                        "确定");
                    return;
                }

                // 找到 dependencies 后面的开括号
                int braceIndex = content.IndexOf('{', dependenciesIndex);
                if (braceIndex == -1)
                {
                    EditorUtility.DisplayDialog(
                        "错误",
                        "无法在 manifest.json 中找到 dependencies 的开括号",
                        "确定");
                    return;
                }

                // 查找第一个依赖项的起始位置（跳过开括号和可能的空白/换行）
                int insertIndex = braceIndex + 1;
                
                // 跳过空白字符和换行
                while (insertIndex < content.Length && 
                       (char.IsWhiteSpace(content[insertIndex]) || content[insertIndex] == '\n' || content[insertIndex] == '\r'))
                {
                    insertIndex++;
                }

                // 构建要插入的内容
                string sentisEntry = "    \"com.unity.sentis\": \"1.3.0\",";
                
                // 如果 dependencies 不为空，需要添加换行和缩进
                if (insertIndex < content.Length && content[insertIndex] != '}')
                {
                    // 有现有依赖项，在开头插入并添加换行
                    sentisEntry = "\n    " + sentisEntry;
                }
                else
                {
                    // 没有依赖项，直接添加
                    sentisEntry = "\n    " + sentisEntry + "\n";
                }

                // 插入内容
                content = content.Insert(insertIndex, sentisEntry);

                // 写回文件
                File.WriteAllText(manifestPath, content, Encoding.UTF8);
                PoseDriveLogger.LogInfo($"成功将 Sentis 添加到 manifest.json: {manifestPath}");
                
                // 刷新 AssetDatabase
                AssetDatabase.Refresh();
                PoseDriveLogger.LogInfo("已刷新 AssetDatabase");

                EditorUtility.DisplayDialog(
                    PoseDriveLocalization.Tr("wizard.dependency.barracuda.name"),
                    PoseDriveLocalization.Tr("wizard.dependency.barracuda.install.success"),
                    "确定");
                
                PoseDriveLogger.LogInfo("Sentis 安装流程完成");
            }
            catch (Exception ex)
            {
                PoseDriveLogger.LogException(ex, "安装 Sentis 时发生异常");
                EditorUtility.DisplayDialog(
                    "错误",
                    string.Format(PoseDriveLocalization.Tr("wizard.dependency.barracuda.install.error"), ex.Message),
                    "确定");
            }
        }
    }
}

