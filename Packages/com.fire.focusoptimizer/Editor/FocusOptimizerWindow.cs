using UnityEditor;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    internal sealed class FocusOptimizerWindow : EditorWindow
    {
        private static int _openCount;
        private Vector2 _scrollPosition;
        private Texture2D _cardBackgroundTexture;
        private Texture2D _headerGradientTexture;

        internal static bool HasOpenInstances => _openCount > 0;

        [MenuItem("Tools/焦点卡顿优化助手", priority = 201)]
        private static void OpenWindow()
        {
            var window = GetWindow<FocusOptimizerWindow>();
            window.titleContent = new GUIContent(
                FocusOptimizerLocalization.Tr("window.title"),
                EditorGUIUtility.IconContent("d_Settings").image);
            window.Show();
        }

        private void OnEnable()
        {
            _openCount++;
            FocusOptimizerLocalization.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            _openCount = Mathf.Max(0, _openCount - 1);
            FocusOptimizerLocalization.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            titleContent = new GUIContent(
                FocusOptimizerLocalization.Tr("window.title"),
                EditorGUIUtility.IconContent("d_Settings").image);
            Repaint();
        }

        internal static void NotifyManualRefreshNeeded()
        {
            System.Func<string, string> loc = FocusOptimizerLocalization.Tr;
            foreach (var window in Resources.FindObjectsOfTypeAll<FocusOptimizerWindow>())
            {
                window.ShowNotification(new GUIContent(loc("notify.manualRefresh")));
            }
        }

        private void OnGUI()
        {
            InitializeResources();
            
            var settings = FocusOptimizerSettings.instance;

            // 绘制标题栏
            DrawHeader(
                FocusOptimizerLocalization.Tr("window.title"),
                FocusOptimizerLocalization.Tr("window.subtitle"));

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);

            // 刷新策略卡片
            DrawRefreshCard(settings);

            EditorGUILayout.Space(10);

            // Enter Play Mode 优化卡片
            DrawEnterPlayModeCard(settings);

            EditorGUILayout.Space(10);

            // 统计信息卡片
            DrawStatsCard(settings);

            EditorGUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        private void InitializeResources()
        {
            if (_cardBackgroundTexture == null)
            {
                _cardBackgroundTexture = CreateCardBackground();
            }

            if (_headerGradientTexture == null)
            {
                _headerGradientTexture = CreateHeaderGradient();
            }
        }

        private void DrawHeader(string title, string subtitle)
        {
            var headerRect = EditorGUILayout.GetControlRect(false, 60);
            
            // 绘制渐变背景
            if (_headerGradientTexture != null)
            {
                GUI.DrawTexture(headerRect, _headerGradientTexture);
            }

            // 标题和副标题
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };

            var subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f) }
            };

            var titleRect = new Rect(headerRect.x + 15, headerRect.y + 8, headerRect.width - 50, 24);
            var subtitleRect = new Rect(headerRect.x + 15, headerRect.y + 32, headerRect.width - 50, 16);

            GUI.Label(titleRect, title, titleStyle);
            GUI.Label(subtitleRect, subtitle, subtitleStyle);

            // 右上角按钮（帮助和设置）
            float buttonWidth = 80f;
            float buttonHeight = 30f;
            float rightMargin = 10f;
            float buttonSpacing = 5f;
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 4, 4)
            };

            // 帮助按钮
            var helpButtonRect = new Rect(
                headerRect.x + headerRect.width - (buttonWidth * 2) - buttonSpacing - rightMargin, 
                headerRect.y + 15, 
                buttonWidth, 
                buttonHeight);

            if (GUI.Button(helpButtonRect, 
                new GUIContent(
                    FocusOptimizerLocalization.Tr("toolbar.help"),
                    FocusOptimizerLocalization.Tr("toolbar.help.tooltip")), 
                buttonStyle))
            {
                Application.OpenURL("https://lyzbcy.github.io/posts/Unity%E6%8F%92%E4%BB%B6-%E7%84%A6%E7%82%B9%E5%8D%A1%E9%A1%BF%E4%BC%98%E5%8C%96%E5%8A%A9%E6%89%8B/");
            }

            // 设置按钮
            var settingsButtonRect = new Rect(
                headerRect.x + headerRect.width - buttonWidth - rightMargin, 
                headerRect.y + 15, 
                buttonWidth, 
                buttonHeight);

            if (GUI.Button(settingsButtonRect, 
                new GUIContent(
                    FocusOptimizerLocalization.Tr("toolbar.settings"),
                    FocusOptimizerLocalization.Tr("toolbar.settings.tooltip")), 
                buttonStyle))
            {
                FocusOptimizerSettingsWindow.OpenWindowStatic();
            }

            // 分隔线
            EditorGUILayout.Space(5);
            DrawDivider();
        }

        private void DrawRefreshCard(FocusOptimizerSettings settings)
        {
            DrawCard(() =>
            {
                // 卡片标题
                DrawSectionHeader(
                    FocusOptimizerLocalization.Tr("refresh.header"),
                    FocusOptimizerLocalization.Tr("refresh.header.tooltip"),
                    EditorGUIUtility.IconContent("d_Refresh").image);

                EditorGUILayout.Space(8);

                EditorGUI.BeginChangeCheck();

                // 主开关
                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.enable"),
                    FocusOptimizerLocalization.Tr("refresh.enable.tooltip"),
                    settings.EnableOptimizer,
                    value => settings.EnableOptimizer = value);

                EditorGUI.BeginDisabledGroup(!settings.EnableOptimizer);

                EditorGUILayout.Space(5);

                // 失焦暂停
                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.suspend"),
                    FocusOptimizerLocalization.Tr("refresh.suspend.tooltip"),
                    settings.SuspendAutoRefreshWhenInactive,
                    value => settings.SuspendAutoRefreshWhenInactive = value);

                EditorGUILayout.Space(5);

                // 控制台提示
                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.console"),
                    FocusOptimizerLocalization.Tr("refresh.console.tooltip"),
                    settings.ShowConsoleHints,
                    value => settings.ShowConsoleHints = value);

                EditorGUILayout.Space(10);
                DrawDivider();
                EditorGUILayout.Space(10);

                // 刷新策略
                DrawLabelWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.mode"),
                    FocusOptimizerLocalization.Tr("refresh.mode.tooltip"));
                var mode = (FocusRefreshMode)EditorGUILayout.EnumPopup(settings.RefreshMode);
                
                double throttleDelay = settings.ThrottleDelaySeconds;
                if (mode == FocusRefreshMode.Throttled)
                {
                    EditorGUILayout.Space(5);
                    DrawLabelWithTooltip(
                        FocusOptimizerLocalization.Tr("refresh.throttle"),
                        FocusOptimizerLocalization.Tr("refresh.throttle.tooltip"));
                    float slider = EditorGUILayout.Slider((float)throttleDelay, 0.2f, 10f);
                    throttleDelay = slider;
                }

                bool promptManual = settings.PromptOnManualMode;
                if (mode == FocusRefreshMode.Manual)
                {
                    EditorGUILayout.Space(5);
                    DrawToggleWithTooltip(
                        FocusOptimizerLocalization.Tr("refresh.prompt"),
                        FocusOptimizerLocalization.Tr("refresh.prompt.tooltip"),
                        promptManual,
                        value => promptManual = value);
                }

                EditorGUILayout.Space(15);

                // 操作按钮
                EditorGUILayout.BeginHorizontal();
                if (DrawActionButton(
                    FocusOptimizerLocalization.Tr("button.refresh"),
                    FocusOptimizerLocalization.Tr("button.refresh.tooltip"),
                    new Color(0.2f, 0.6f, 0.9f)))
                {
                    FocusOptimizerController.RequestManualRefresh(FocusOptimizerLocalization.Tr("button.refresh"));
                }

                if (DrawActionButton(
                    FocusOptimizerLocalization.Tr("button.restore"),
                    FocusOptimizerLocalization.Tr("button.restore.tooltip"),
                    new Color(0.3f, 0.7f, 0.3f)))
                {
                    FocusOptimizerController.ForceAllowAutoRefresh();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);

                // 状态信息
                DrawStatusInfo();

                EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                {
                    settings.RefreshMode = mode;
                    settings.ThrottleDelaySeconds = throttleDelay;
                    settings.PromptOnManualMode = promptManual;
                    FocusOptimizerController.NotifySettingsChanged();
                }
            });
        }

        private void DrawEnterPlayModeCard(FocusOptimizerSettings settings)
        {
            DrawCard(() =>
            {
                DrawSectionHeader(
                    FocusOptimizerLocalization.Tr("playmode.header"),
                    FocusOptimizerLocalization.Tr("playmode.header.tooltip"),
                    EditorGUIUtility.IconContent("d_PlayButton").image);

                EditorGUILayout.Space(8);

                EditorGUI.BeginChangeCheck();

                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("playmode.enable"),
                    FocusOptimizerLocalization.Tr("playmode.enable.tooltip"),
                    settings.EnableEnterPlayModeHelper,
                    value => settings.EnableEnterPlayModeHelper = value);

                EditorGUI.BeginDisabledGroup(!settings.EnableEnterPlayModeHelper);

                EditorGUILayout.Space(5);

                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("playmode.apply"),
                    FocusOptimizerLocalization.Tr("playmode.apply.tooltip"),
                    settings.ApplyEnterPlayModePreset,
                    value => settings.ApplyEnterPlayModePreset = value);

                EditorGUILayout.Space(5);

                DrawLabelWithTooltip(
                    FocusOptimizerLocalization.Tr("playmode.options"),
                    FocusOptimizerLocalization.Tr("playmode.options.tooltip"));
                var options = (EnterPlayModeOptions)EditorGUILayout.EnumFlagsField(settings.PlayModeOptions);

                EditorGUILayout.Space(5);

                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("playmode.restore"),
                    FocusOptimizerLocalization.Tr("playmode.restore.tooltip"),
                    settings.AutoRestoreEnterPlayMode,
                    value => settings.AutoRestoreEnterPlayMode = value);

                EditorGUILayout.Space(10);

                if (DrawActionButton(
                    FocusOptimizerLocalization.Tr("playmode.button"),
                    FocusOptimizerLocalization.Tr("playmode.button.tooltip"),
                    new Color(0.8f, 0.6f, 0.2f)))
                {
                    EnterPlayModeOptionHelper.RestoreIfNeeded(force: true);
                }

                EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                {
                    settings.PlayModeOptions = options;
                    FocusOptimizerController.NotifySettingsChanged();
                }
            });
        }

        private void DrawStatsCard(FocusOptimizerSettings settings)
        {
            DrawCard(() =>
            {
                DrawSectionHeader(
                    FocusOptimizerLocalization.Tr("stats.header"),
                    FocusOptimizerLocalization.Tr("stats.header.tooltip"),
                    EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);

                EditorGUILayout.Space(10);

                // 统计信息使用更醒目的样式
                var statLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f) }
                };

                var statValueStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    new GUIContent(
                        FocusOptimizerLocalization.Tr("stats.reason"),
                        FocusOptimizerLocalization.Tr("stats.reason.tooltip")),
                    statLabelStyle,
                    GUILayout.Width(120));
                EditorGUILayout.LabelField(settings.LastRefreshReason, statValueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    new GUIContent(
                        FocusOptimizerLocalization.Tr("stats.duration"),
                        FocusOptimizerLocalization.Tr("stats.duration.tooltip")),
                    statLabelStyle,
                    GUILayout.Width(120));
                
                var duration = settings.LastRefreshDuration;
                var durationColor = duration < 0.5f ? new Color(0.3f, 0.8f, 0.3f) :
                                   duration < 1.0f ? new Color(0.8f, 0.7f, 0.2f) :
                                   new Color(0.8f, 0.3f, 0.3f);
                
                var originalColor = GUI.color;
                GUI.color = durationColor;
                EditorGUILayout.LabelField($"{duration:0.000}s", statValueStyle);
                GUI.color = originalColor;
                EditorGUILayout.EndHorizontal();
            });
        }


        private void DrawCard(System.Action content)
        {
            var cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 15, 15),
                margin = new RectOffset(5, 5, 5, 5)
            };

            var backgroundColor = EditorGUIUtility.isProSkin 
                ? new Color(0.22f, 0.22f, 0.22f, 1f)
                : new Color(0.95f, 0.95f, 0.95f, 1f);

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;

            EditorGUILayout.BeginVertical(cardStyle);
            content?.Invoke();
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = originalColor;
        }

        private void DrawSectionHeader(string title, string tooltip, Texture icon)
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };

            var iconContent = icon != null ? new GUIContent(icon) : GUIContent.none;
            var titleContent = new GUIContent($"  {title}", tooltip);

            EditorGUILayout.BeginHorizontal();
            if (icon != null)
            {
                GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(20));
            }
            EditorGUILayout.LabelField(titleContent, headerStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToggleWithTooltip(string label, string tooltip, bool value, System.Action<bool> setValue)
        {
            var content = new GUIContent(label, tooltip);
            bool newValue = EditorGUILayout.ToggleLeft(content, value);
            if (newValue != value)
            {
                setValue(newValue);
            }
        }

        private void DrawLabelWithTooltip(string label, string tooltip)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip));
        }

        private bool DrawActionButton(string label, string tooltip, Color color)
        {
            var originalColor = GUI.color;
            GUI.color = color;
            var content = new GUIContent(label, tooltip);
            bool clicked = GUILayout.Button(content, GUILayout.Height(25));
            GUI.color = originalColor;
            return clicked;
        }

        private void DrawStatusInfo()
        {
            var statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                fontSize = 11
            };

            string stateText = FocusOptimizerController.AutoRefreshSuspendedByPlugin 
                ? FocusOptimizerLocalization.Tr("status.suspended") 
                : FocusOptimizerLocalization.Tr("status.normal");

            double remaining = FocusOptimizerController.NextScheduledRefreshTime - EditorApplication.timeSinceStartup;
            string statusMessage;
            if (remaining > 0)
            {
                statusMessage = FocusOptimizerLocalization.Tr("status.scheduled", stateText, remaining);
            }
            else
            {
                statusMessage = FocusOptimizerLocalization.Tr("status.current", stateText);
            }

            var statusColor = FocusOptimizerController.AutoRefreshSuspendedByPlugin 
                ? new Color(0.8f, 0.6f, 0.2f) 
                : new Color(0.3f, 0.7f, 0.3f);

            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(new GUIContent(statusMessage), statusStyle);
            GUI.color = originalColor;
        }

        private void DrawDivider()
        {
            var lineStyle = new GUIStyle(GUI.skin.box);
            lineStyle.fixedHeight = 1;
            lineStyle.normal.background = CreateColorTexture(
                EditorGUIUtility.isProSkin 
                    ? new Color(0.3f, 0.3f, 0.3f, 0.5f)
                    : new Color(0.7f, 0.7f, 0.7f, 0.5f));
            GUILayout.Box(GUIContent.none, lineStyle, GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        private Texture2D CreateCardBackground()
        {
            var texture = new Texture2D(1, 1);
            var color = EditorGUIUtility.isProSkin 
                ? new Color(0.22f, 0.22f, 0.22f, 1f)
                : new Color(0.95f, 0.95f, 0.95f, 1f);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private Texture2D CreateHeaderGradient()
        {
            const int height = 60;
            var texture = new Texture2D(1, height);
            var topColor = EditorGUIUtility.isProSkin 
                ? new Color(0.25f, 0.25f, 0.25f, 1f)
                : new Color(0.98f, 0.98f, 0.98f, 1f);
            var bottomColor = EditorGUIUtility.isProSkin 
                ? new Color(0.20f, 0.20f, 0.20f, 1f)
                : new Color(0.95f, 0.95f, 0.95f, 1f);

            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                texture.SetPixel(0, y, Color.Lerp(topColor, bottomColor, t));
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
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
    }
}
