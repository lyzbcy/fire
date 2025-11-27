using System;
using System.Reflection;
using PoseController.Runtime.Core;
using PoseController.Runtime.Input;
using PoseController.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace PoseController.Editor
{
    /// <summary>
    /// 一键搭建 PoseController 的配置向导。
    /// </summary>
    public class PoseControllerSetupWizard : EditorWindow
    {
        private const string RootName = "PoseControllerSystem";

        private PoseDetector _detector;
        private ActionClassifier _classifier;
        private PoseControllerManager _manager;
        private PoseInputMapper _mapper;

#if UNITY_BARRACUDA
        private Unity.Barracuda.NNModel _moveNetModel;
        private Unity.Barracuda.NNModel _actionModel;
#else
        private UnityEngine.Object _moveNetModel;
        private UnityEngine.Object _actionModel;
#endif

        private ActionMappingAsset _mappingAsset;
        private Vector2 _scroll;

        [MenuItem("Tools/动捕控制器/配置向导", priority = 220)]
        public static void Open()
        {
            PoseControllerSetupWizard window =
                GetWindow<PoseControllerSetupWizard>(PoseControllerLocalization.Tr("wizard.title"));
            window.UpdateTitle();
        }

        private void OnEnable()
        {
            PoseControllerLocalization.LanguageChanged += HandleLanguageChanged;
            UpdateTitle();
            RefreshReferences();
        }

        private void OnDisable()
        {
            PoseControllerLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged()
        {
            UpdateTitle();
            Repaint();
        }

        private void UpdateTitle()
        {
            titleContent = new GUIContent(PoseControllerLocalization.Tr("wizard.title"));
        }

        private void RefreshReferences()
        {
            _detector = FindObjectOfType<PoseDetector>();
            _classifier = FindObjectOfType<ActionClassifier>();
            _manager = FindObjectOfType<PoseControllerManager>();
            _mapper = FindObjectOfType<PoseInputMapper>();
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(EditorStylesLibrary.Container))
            {
                EditorStylesLibrary.DrawHeader(
                    PoseControllerLocalization.Tr("wizard.title"),
                    PoseControllerLocalization.Tr("wizard.subtitle"));

                _scroll = EditorGUILayout.BeginScrollView(_scroll, true, true);

                DrawStep(PoseControllerLocalization.Tr("wizard.step1"), PoseControllerLocalization.Tr("wizard.step1.desc"), DrawDependencies);
                DrawStep(PoseControllerLocalization.Tr("wizard.step2"), null, DrawModelSelectors);
                DrawStep(PoseControllerLocalization.Tr("wizard.step3"), null, DrawMappingSection);
                DrawStep(PoseControllerLocalization.Tr("wizard.step4"), null, DrawRuntimeSection);
                DrawStep(PoseControllerLocalization.Tr("wizard.step5"), null, DrawShortcutsSection);

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
            bool hasBarracuda = Type.GetType("Unity.Barracuda.Tensor, Unity.Barracuda") != null;
            DrawStatus(
                PoseControllerLocalization.Tr("wizard.dependency.barracuda.name"),
                hasBarracuda,
                PoseControllerLocalization.Tr("wizard.dependency.barracuda.tip"),
                PoseControllerLocalization.Tr("wizard.dependency.barracuda.action"),
                OpenBarracudaPackagePage);

            bool hasCamera = WebCamTexture.devices is { Length: > 0 };
            DrawStatus(
                PoseControllerLocalization.Tr("wizard.dependency.camera.name"),
                hasCamera,
                PoseControllerLocalization.Tr("wizard.dependency.camera.tip"),
                hasCamera ? null : PoseControllerLocalization.Tr("wizard.dependency.camera.action"),
                () => Application.OpenURL("https://support.unity.com/hc/zh-cn/articles/4412149980051"));
        }

        private void DrawModelSelectors()
        {
#if UNITY_BARRACUDA
            _moveNetModel = (Unity.Barracuda.NNModel)EditorGUILayout.ObjectField(
                PoseControllerLocalization.Tr("wizard.model.movenet"),
                _moveNetModel,
                typeof(Unity.Barracuda.NNModel),
                false);
            _actionModel = (Unity.Barracuda.NNModel)EditorGUILayout.ObjectField(
                PoseControllerLocalization.Tr("wizard.model.action"),
                _actionModel,
                typeof(Unity.Barracuda.NNModel),
                false);
            if (_detector != null && GUILayout.Button(PoseControllerLocalization.Tr("wizard.model.pullDetector")))
            {
                SerializedObject so = new SerializedObject(_detector);
                _moveNetModel = so.FindProperty("_compiledModel")?.objectReferenceValue as Unity.Barracuda.NNModel;
            }
            if (_classifier != null && GUILayout.Button(PoseControllerLocalization.Tr("wizard.model.pullClassifier")))
            {
                SerializedObject so = new SerializedObject(_classifier);
                _actionModel = so.FindProperty("_compiledModel")?.objectReferenceValue as Unity.Barracuda.NNModel;
            }
#else
            EditorGUILayout.HelpBox(PoseControllerLocalization.Tr("wizard.model.noBarracuda"), MessageType.Warning);
#endif
        }

        private void DrawMappingSection()
        {
            _mappingAsset = (ActionMappingAsset)EditorGUILayout.ObjectField(
                PoseControllerLocalization.Tr("wizard.binding.asset"),
                _mappingAsset,
                typeof(ActionMappingAsset),
                false);

            if (_mapper != null && GUILayout.Button(PoseControllerLocalization.Tr("wizard.mapping.useExisting")))
            {
                SerializedObject so = new SerializedObject(_mapper);
                _mappingAsset = so.FindProperty("_mappingAsset")?.objectReferenceValue as ActionMappingAsset;
            }

            if (GUILayout.Button(PoseControllerLocalization.Tr("wizard.binding.default")))
            {
                CreateDefaultMappingAsset();
            }
        }

        private void DrawRuntimeSection()
        {
            if (GUILayout.Button(PoseControllerLocalization.Tr("wizard.runtime.button"), GUILayout.Height(32)))
            {
                EnsureRuntimeExists();
                ConfigureRuntime();
                RefreshReferences();
            }

            if (_manager != null)
            {
                EditorGUILayout.HelpBox(
                    string.Format(PoseControllerLocalization.Tr("wizard.runtime.detected"), _manager.name),
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(PoseControllerLocalization.Tr("wizard.runtime.missing"), MessageType.Info);
            }
        }

        private void DrawShortcutsSection()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(PoseControllerLocalization.Tr("wizard.open.window")))
            {
                PoseControllerWindow.Open();
            }

            if (GUILayout.Button(PoseControllerLocalization.Tr("wizard.open.mapping")))
            {
                ActionMappingEditor.Open();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(PoseControllerLocalization.Tr("wizard.open.scene.tp")))
            {
                OpenScene("Assets/lyzbcyMade PoseController/Samples~/ThirdPersonDemo/ThirdPersonDemo.unity");
            }

            if (GUILayout.Button(PoseControllerLocalization.Tr("wizard.open.scene.fp")))
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
                ok ? PoseControllerLocalization.Tr("status.ready") : PoseControllerLocalization.Tr("status.missing"),
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
            _manager = root.AddComponent<PoseControllerManager>();
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

#if UNITY_BARRACUDA
            if (_detector != null && _moveNetModel != null)
            {
                SerializedObject so = new SerializedObject(_detector);
                SerializedProperty prop = so.FindProperty("_compiledModel");
                if (prop != null)
                {
                    prop.objectReferenceValue = _moveNetModel;
                    so.ApplyModifiedProperties();
                }
            }

            if (_classifier != null && _actionModel != null)
            {
                SerializedObject so = new SerializedObject(_classifier);
                SerializedProperty prop = so.FindProperty("_compiledModel");
                if (prop != null)
                {
                    prop.objectReferenceValue = _actionModel;
                    so.ApplyModifiedProperties();
                }
            }
#endif

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
                PoseControllerLocalization.Tr("wizard.binding.createTitle"),
                "PoseDefaultMappings",
                "asset",
                PoseControllerLocalization.Tr("wizard.binding.createDesc"));
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
            if (!System.IO.File.Exists(path))
            {
                EditorUtility.DisplayDialog(
                    PoseControllerLocalization.Tr("dialog.sceneMissing.title"),
                    string.Format(PoseControllerLocalization.Tr("dialog.sceneMissing.message"), path),
                    PoseControllerLocalization.Tr("dialog.sceneMissing.ok"));
                return;
            }

            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            }
        }

        private static void OpenBarracudaPackagePage()
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
                    openMethod.Invoke(null, new object[] { "com.unity.barracuda" });
                    return;
                }
            }

            Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.barracuda@latest");
        }
    }
}

