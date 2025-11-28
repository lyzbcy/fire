using System.Collections.Generic;
using System;
using System.IO;
using PoseDrive.Runtime.Core;
using PoseDrive.Runtime.Input;
using PoseDrive.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace PoseDrive.Editor
{
    /// <summary>
    /// PoseController 主面板，支持多语言 UI。
    /// </summary>
    public class PoseDriveWindow : EditorWindow
    {
        private PoseDetector _detector;
        private PoseDriveManager _manager;
        private ActionClassifier _classifier;
        private PoseInputMapper _mapper;
        private WebcamProvider _webcam;

        private Texture2D _previewTexture;
        private Vector2 _scroll;
        private bool _showSkeleton = true;
        private bool _enableCamera = true;
        private double _lastFpsSample;
        private float _fps;
        private double _nextAutoRefreshTime;
        private const double AutoRefreshInterval = 1.0f;

        [MenuItem("Tools/PoseDrive/主面板", priority = 210)]
        public static void Open()
        {
            PoseDriveWindow window = GetWindow<PoseDriveWindow>();
            window.UpdateTitle();
            window.minSize = new Vector2(420, 520);
        }

        private void OnEnable()
        {
            PoseDriveLocalization.LanguageChanged += HandleLanguageChanged;
            UpdateTitle();
            FindDependencies();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += HandleAutoRefresh;
        }

        private void OnDisable()
        {
            PoseDriveLocalization.LanguageChanged -= HandleLanguageChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= HandleAutoRefresh;
        }

        private void HandleLanguageChanged()
        {
            UpdateTitle();
            Repaint();
        }

        private void UpdateTitle()
        {
            titleContent = new GUIContent(PoseDriveLocalization.Tr("header.title"));
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                FindDependencies();
            }
        }

        private void FindDependencies()
        {
            if (_detector == null)
            {
                _detector = FindObjectOfType<PoseDetector>();
            }
            if (_manager == null)
            {
                _manager = FindObjectOfType<PoseDriveManager>();
            }
            if (_classifier == null)
            {
                _classifier = FindObjectOfType<ActionClassifier>();
            }
            if (_mapper == null)
            {
                _mapper = FindObjectOfType<PoseInputMapper>();
            }

            if (_webcam == null)
            {
                _webcam = FindObjectOfType<WebcamProvider>();
            }
            if (_webcam == null && _detector != null)
            {
                _webcam = _detector.GetComponent<WebcamProvider>();
            }
            if (_webcam != null && _webcam.enabled)
            {
                _webcam.EnsureInitialized();
            }

            _enableCamera = _webcam == null || _webcam.enabled;
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(EditorStylesLibrary.Container))
            {
                EditorStylesLibrary.DrawHeader(
                    PoseDriveLocalization.Tr("header.title"),
                    PoseDriveLocalization.Tr("header.subtitle"),
                    () =>
                    {
                        if (GUILayout.Button(PoseDriveLocalization.Tr("button.help"),
                                EditorStylesLibrary.ToolbarButton, GUILayout.Width(90)))
                        {
                            OpenDocumentation();
                        }

                        if (GUILayout.Button(PoseDriveLocalization.Tr("button.setup"),
                                EditorStylesLibrary.ToolbarButton, GUILayout.Width(110)))
                        {
                            PoseDriveSetupWizard.Open();
                        }

                        if (GUILayout.Button(PoseDriveLocalization.Tr("button.settings"),
                                EditorStylesLibrary.ToolbarButton, GUILayout.Width(90)))
                        {
                            PoseDriveSettingsWindow.Open();
                        }
                    });

                using (var scroll = new GUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scroll.scrollPosition;

                    DrawSystemStatusCard();
                    DrawCameraCard();
                    DrawSkeletonCard();
            DrawActionCard();
                    DrawMovementCard();
                    DrawMappingCard();
                    DrawDebugCard();
                }
            }

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void HandleAutoRefresh()
        {
            if (!this)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextAutoRefreshTime)
            {
                return;
            }

            _nextAutoRefreshTime = EditorApplication.timeSinceStartup + AutoRefreshInterval;
            FindDependencies();

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawSystemStatusCard()
        {
            using (EditorStylesLibrary.CardScope(
                       PoseDriveLocalization.Tr("module.status"),
                       PoseDriveLocalization.Tr("module.status.subtitle")))
            {
                DrawStatusRow("PoseDetector", _detector != null);
                DrawStatusRow("ActionClassifier", _classifier != null);
                DrawStatusRow("PoseInputMapper", _mapper != null);
                DrawStatusRow("WebcamProvider", _webcam != null);

                if (_webcam != null)
                {
                    GUILayout.Label($"状态：{_webcam.Status}", EditorStylesLibrary.Secondary);
                }

                EditorStylesLibrary.DrawDivider();

                if (GUILayout.Button(PoseDriveLocalization.Tr("status.refresh"),
                        EditorStylesLibrary.ToolbarButton, GUILayout.Height(26)))
            {
                FindDependencies();
            }
            }
        }

        private void DrawCameraCard()
        {
            using (EditorStylesLibrary.CardScope(
                       PoseDriveLocalization.Tr("module.camera"),
                       PoseDriveLocalization.Tr("module.camera.subtitle")))
            {
                GUILayout.BeginHorizontal();
                bool newCameraState = EditorGUILayout.ToggleLeft(
                    PoseDriveLocalization.Tr("camera.enable"), _enableCamera, EditorStylesLibrary.Body);
                if (newCameraState != _enableCamera)
                {
                    _enableCamera = newCameraState;
                    if (_webcam != null)
                    {
                        _webcam.enabled = _enableCamera;
                        if (_enableCamera)
                        {
                            _webcam.Initialize();
                        }
                    }
                }

                _showSkeleton = EditorGUILayout.ToggleLeft(
                    PoseDriveLocalization.Tr("camera.skeleton"), _showSkeleton, EditorStylesLibrary.Body);
                GUILayout.EndHorizontal();

                Rect rect = GUILayoutUtility.GetAspectRect(16f / 9f, GUILayout.ExpandWidth(true));

                if (!Application.isPlaying)
                {
                    EditorGUI.DrawRect(rect, new Color(0.07f, 0.07f, 0.07f));
                    EditorGUI.LabelField(rect, PoseDriveLocalization.Tr("camera.noFrame"),
                        EditorStyles.centeredGreyMiniLabel);
                }
                else if (_webcam != null && _enableCamera && _webcam.TryGetFrame(out _previewTexture) && _previewTexture != null)
                {
                    GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit);
                    if (_showSkeleton && _detector != null && _detector.IsPoseValid)
                    {
                        DrawSkeletonOverlay(rect, _previewTexture);
                    }
                }
                else
                {
                    EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.22f));
                    EditorGUI.LabelField(rect, PoseDriveLocalization.Tr("camera.wait"),
                        EditorStyles.centeredGreyMiniLabel);
                }

                GUILayout.Space(6);
                GUILayout.Label(GetCameraStatusLabel(), EditorStylesLibrary.Secondary);
            }
        }

        private void DrawSkeletonCard()
        {
            using (EditorStylesLibrary.CardScope(
                       PoseDriveLocalization.Tr("module.skeleton"),
                       PoseDriveLocalization.Tr("module.skeleton.subtitle")))
            {
                if (_detector?.CurrentPose == null)
                {
                    GUILayout.Label(PoseDriveLocalization.Tr("skeleton.none"), EditorStylesLibrary.Secondary);
                    return;
                }

                PoseData pose = _detector.CurrentPose;
                GUILayout.Label($"{PoseDriveLocalization.Tr("skeleton.count")}：{PoseData.KeypointCount}",
                    EditorStylesLibrary.Body);
                GUILayout.Label($"{PoseDriveLocalization.Tr("skeleton.timestamp")}：{pose.Timestamp:F2}s",
                    EditorStylesLibrary.Secondary);

                EditorStylesLibrary.DrawDivider();
                GUILayout.Label(PoseDriveLocalization.Tr("skeleton.confidence"), EditorStylesLibrary.Body);

                for (int i = 0; i < PoseData.KeypointCount; i += 8)
                {
                    GUILayout.BeginHorizontal();
                    for (int j = i; j < Mathf.Min(i + 8, PoseData.KeypointCount); j++)
                    {
                        float confidence = pose.Keypoints[j].Confidence;
                        Rect rect = GUILayoutUtility.GetRect(16, 24);
                        EditorGUI.DrawRect(rect, new Color(0.22f, 0.24f, 0.28f));
                        Rect fill = new Rect(rect.x, rect.y + rect.height * (1f - confidence), rect.width, rect.height * confidence);
                        EditorGUI.DrawRect(fill, new Color(0.2f, 0.65f, 0.9f));
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawActionCard()
        {
            using (EditorStylesLibrary.CardScope(
                       PoseDriveLocalization.Tr("module.action"),
                       PoseDriveLocalization.Tr("module.action.subtitle")))
            {
                if (_classifier == null)
                {
                    GUILayout.Label(PoseDriveLocalization.Tr("action.none"), EditorStylesLibrary.Secondary);
                    return;
                }

                string currentAction = string.IsNullOrEmpty(_classifier.CurrentLabel)
                    ? PoseDriveLocalization.Tr("action.none")
                    : _classifier.CurrentLabel;
                GUILayout.Label($"{PoseDriveLocalization.Tr("action.current")}：{currentAction}",
                    EditorStylesLibrary.Body);

                Rect bar = GUILayoutUtility.GetRect(10, 18, GUILayout.ExpandWidth(true));
                DrawProgressBar(bar, _classifier.CurrentConfidence, new Color(0.24f, 0.66f, 0.47f));
                GUILayout.Label($"{PoseDriveLocalization.Tr("action.confidence")}：{_classifier.CurrentConfidence:P0}",
                    EditorStylesLibrary.Secondary);
            }
        }

        private void DrawMovementCard()
        {
            using (EditorStylesLibrary.CardScope(
                       PoseDriveLocalization.Tr("module.motion"),
                       PoseDriveLocalization.Tr("module.motion.subtitle")))
            {
                if (_manager == null)
                {
                    GUILayout.Label(PoseDriveLocalization.Tr("motion.managerMissing"), EditorStylesLibrary.Secondary);
                    return;
            }

                GUILayout.BeginHorizontal();
                GUILayout.Label(PoseDriveLocalization.Tr("motion.move"), EditorStylesLibrary.Body, GUILayout.Width(140));
                GUILayout.Label(_manager.MoveVector.ToString("F2"), EditorStylesLibrary.Secondary);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(PoseDriveLocalization.Tr("motion.view"), EditorStylesLibrary.Body, GUILayout.Width(140));
                GUILayout.Label(_manager.ViewDelta.ToString("F2"), EditorStylesLibrary.Secondary);
                GUILayout.EndHorizontal();

                Rect moveRect = GUILayoutUtility.GetRect(10, 60, GUILayout.ExpandWidth(true));
                DrawVectorPreview(moveRect, _manager.MoveVector, new Color(0.18f, 0.55f, 0.96f));

                Rect viewRect = GUILayoutUtility.GetRect(10, 60, GUILayout.ExpandWidth(true));
                DrawVectorPreview(viewRect, _manager.ViewDelta, new Color(0.96f, 0.58f, 0.18f));
            }
        }

        private void DrawMappingCard()
        {
            using (EditorStylesLibrary.CardScope(
                       PoseDriveLocalization.Tr("module.mapping"),
                       PoseDriveLocalization.Tr("module.mapping.subtitle")))
            {
                int count = _mapper?.Bindings?.Count ?? 0;
                GUILayout.Label($"{PoseDriveLocalization.Tr("mapping.count")}：{count}", EditorStylesLibrary.Body);

                if (_mapper?.Bindings == null || _mapper.Bindings.Count == 0)
                {
                    GUILayout.Label(PoseDriveLocalization.Tr("mapping.none"), EditorStylesLibrary.Secondary);
                }
                else
                {
                    foreach (KeyValuePair<string, GestureBinding> pair in _mapper.Bindings)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(pair.Key, EditorStylesLibrary.Body, GUILayout.Width(140));
                        GUILayout.Label(pair.Value.Key.ToString(), EditorStylesLibrary.Secondary);
                        GUILayout.Label(
                            pair.Value.Hold
                                ? PoseDriveLocalization.Tr("mapping.hold")
                                : PoseDriveLocalization.Tr("mapping.once"),
                            EditorStylesLibrary.Secondary, GUILayout.Width(60));
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(6);
                if (GUILayout.Button(PoseDriveLocalization.Tr("mapping.button"),
                        EditorStylesLibrary.ToolbarButton, GUILayout.Height(28)))
                {
                    ActionMappingEditor.Open();
                }
            }
        }

        private void DrawDebugCard()
        {
            using (EditorStylesLibrary.CardScope(
                       PoseDriveLocalization.Tr("module.debug"),
                       PoseDriveLocalization.Tr("module.debug.subtitle")))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(PoseDriveLocalization.Tr("debug.log"), EditorStylesLibrary.Body, GUILayout.Width(100));
                DrawSerializedToggle(_detector, "_debugMode", PoseDriveLocalization.Tr("debug.detector"));
                DrawSerializedToggle(_manager, "_debugMode", PoseDriveLocalization.Tr("debug.manager"));
                GUILayout.EndHorizontal();

                if (Application.isPlaying)
                {
                    if (EditorApplication.timeSinceStartup - _lastFpsSample > 0.5f)
                    {
                        _fps = 1f / Mathf.Max(Time.deltaTime, 1e-5f);
                        _lastFpsSample = EditorApplication.timeSinceStartup;
                    }

                    float latency = _detector?.CurrentPose != null ? Time.time - _detector.CurrentPose.Timestamp : 0f;
                    GUILayout.Label($"FPS：{_fps:F1}    {latency * 1000f:F1}ms", EditorStylesLibrary.Body);
                }
                else
                {
                    GUILayout.Label(PoseDriveLocalization.Tr("debug.fpsHint"), EditorStylesLibrary.Secondary);
                }

                EditorStylesLibrary.DrawDivider();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(PoseDriveLocalization.Tr("debug.panel"),
                        EditorStylesLibrary.ToolbarButton, GUILayout.Height(26)))
                {
                    PoseDriveWindow.Open();
                }
                if (GUILayout.Button(PoseDriveLocalization.Tr("debug.mapping"),
                        EditorStylesLibrary.ToolbarButton, GUILayout.Height(26)))
                {
                    ActionMappingEditor.Open();
                }
                if (GUILayout.Button(PoseDriveLocalization.Tr("debug.wizard"),
                        EditorStylesLibrary.ToolbarButton, GUILayout.Height(26)))
                {
                    PoseDriveSetupWizard.Open();
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawStatusRow(string title, bool ok)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, EditorStylesLibrary.Body);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                ok ? PoseDriveLocalization.Tr("status.ready") : PoseDriveLocalization.Tr("status.missing"),
                EditorStylesLibrary.Secondary);
            GUILayout.EndHorizontal();
        }

        private static void DrawProgressBar(Rect rect, float value, Color color)
        {
            EditorGUI.DrawRect(rect, new Color(0.88f, 0.88f, 0.88f));
            Rect fill = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height);
            EditorGUI.DrawRect(fill, color);
        }

        private static void DrawVectorPreview(Rect rect, Vector2 vector, Color color)
        {
            EditorGUI.DrawRect(rect, new Color(0.93f, 0.93f, 0.93f));
            Vector2 center = new Vector2(rect.x + rect.width / 2f, rect.y + rect.height / 2f);
            Vector2 dir = new Vector2(vector.x, -vector.y) * 40f;
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(3f, center, center + dir);
            Handles.EndGUI();
        }

        private void DrawSerializedToggle(UnityEngine.Object target, string propertyName, string label)
        {
            if (target == null)
            {
                GUILayout.Label(string.Format(PoseDriveLocalization.Tr("toggle.missing"), label),
                    EditorStylesLibrary.Secondary);
                return;
            }

            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                GUILayout.Label(string.Format(PoseDriveLocalization.Tr("toggle.unavailable"), label),
                    EditorStylesLibrary.Secondary);
                return;
            }

            bool value = EditorGUILayout.Toggle(label, prop.boolValue);
            if (value != prop.boolValue)
            {
                prop.boolValue = value;
                so.ApplyModifiedProperties();
            }
        }

        private void DrawSkeletonOverlay(Rect rect, Texture texture)
        {
            if (_detector?.CurrentPose == null)
            {
                return;
            }

            Handles.BeginGUI();
            PoseData pose = _detector.CurrentPose;
            Handles.color = new Color(0.18f, 0.55f, 0.96f);
            foreach ((int a, int b) in SkeletonPairs)
            {
                PoseKeypoint ka = pose.Keypoints[a];
                PoseKeypoint kb = pose.Keypoints[b];
                if (ka.Confidence < 0.1f || kb.Confidence < 0.1f)
                {
                    continue;
                }

                Vector2 pa = ConvertToRect(ka, rect, texture);
                Vector2 pb = ConvertToRect(kb, rect, texture);
                Handles.DrawLine(pa, pb);
            }
            Handles.EndGUI();
        }

        private static Vector2 ConvertToRect(PoseKeypoint kp, Rect rect, Texture texture)
        {
            float aspect = texture.width / (float)texture.height;
            Rect fitRect = rect;
            float targetHeight = rect.width / aspect;
            if (targetHeight > rect.height)
            {
                float scale = rect.height / targetHeight;
                fitRect.width *= scale;
                fitRect.x += (rect.width - fitRect.width) / 2f;
            }
            else
            {
                fitRect.height = targetHeight;
                fitRect.y += (rect.height - fitRect.height) / 2f;
            }

            float x = fitRect.x + kp.X * fitRect.width;
            float y = fitRect.y + (1f - kp.Y) * fitRect.height;
            return new Vector2(x, y);
        }

        private static readonly (int, int)[] SkeletonPairs =
        {
            (PoseJoints.LeftShoulder, PoseJoints.RightShoulder),
            (PoseJoints.LeftShoulder, PoseJoints.LeftElbow),
            (PoseJoints.LeftElbow, PoseJoints.LeftWrist),
            (PoseJoints.RightShoulder, PoseJoints.RightElbow),
            (PoseJoints.RightElbow, PoseJoints.RightWrist),
            (PoseJoints.LeftShoulder, PoseJoints.LeftHip),
            (PoseJoints.RightShoulder, PoseJoints.RightHip),
            (PoseJoints.LeftHip, PoseJoints.RightHip),
            (PoseJoints.LeftHip, PoseJoints.LeftKnee),
            (PoseJoints.LeftKnee, PoseJoints.LeftAnkle),
            (PoseJoints.RightHip, PoseJoints.RightKnee),
            (PoseJoints.RightKnee, PoseJoints.RightAnkle),
        };

        private string GetCameraStatusLabel()
        {
            if (_webcam == null)
            {
                return "WebcamProvider 未找到";
            }

            return _webcam.Status switch
            {
                WebcamStatus.Streaming => PoseDriveLocalization.Tr("camera.ready"),
                WebcamStatus.Initializing => "Camera Initializing...",
                WebcamStatus.NoDevice => "No camera detected",
                WebcamStatus.Error => "Camera error (see console)",
                _ => PoseDriveLocalization.Tr("camera.wait")
            };
        }

        private static void OpenDocumentation()
        {
            const string docPath = "Assets/lyzbcyMade PoseController/Documentation/UserGuide.md";
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(docPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                Application.OpenURL("https://docs.unity3d.com/");
            }
    }
}
}
