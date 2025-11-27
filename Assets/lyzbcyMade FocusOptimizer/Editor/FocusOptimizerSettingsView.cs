using UnityEditor;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    /// <summary>
    /// 统一管理焦点卡顿优化助手的 IMGUI 绘制逻辑，供窗口和 Project Settings 复用。
    /// </summary>
    internal sealed class FocusOptimizerSettingsView
    {
        private Vector2 _scrollPosition;
        private Texture2D _cardBackgroundTexture;
        private Texture2D _headerGradientTexture;
        private Texture2D _heroGradientTexture;
        private bool _statusDetailFoldout = true;

        /// <summary>
        /// 清理资源，在窗口关闭时调用
        /// </summary>
        internal void CleanupResources()
        {
            if (_cardBackgroundTexture != null)
            {
                Object.DestroyImmediate(_cardBackgroundTexture);
                _cardBackgroundTexture = null;
            }
            if (_headerGradientTexture != null)
            {
                Object.DestroyImmediate(_headerGradientTexture);
                _headerGradientTexture = null;
            }
            if (_heroGradientTexture != null)
            {
                Object.DestroyImmediate(_heroGradientTexture);
                _heroGradientTexture = null;
            }
        }

        internal void OnGUI()
        {
            InitializeResources();

            var settings = FocusOptimizerSettings.instance;

            DrawHeader(
                FocusOptimizerLocalization.Tr("window.title"),
                FocusOptimizerLocalization.Tr("window.subtitle"));

            DrawHeroBanner(settings);

            DrawQuickActionsToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);

            DrawOnboardingCard(settings);

            if (!settings.HasCompletedOnboarding)
            {
                EditorGUILayout.Space(10);
            }

            DrawRefreshCard(settings);

            EditorGUILayout.Space(10);

            DrawEnterPlayModeCard(settings);

            EditorGUILayout.Space(10);

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

            if (_heroGradientTexture == null)
            {
                _heroGradientTexture = CreateHeroGradient();
            }
        }

        private void DrawHeader(string title, string subtitle)
        {
            var headerRect = EditorGUILayout.GetControlRect(false, 60);

            if (_headerGradientTexture != null)
            {
                GUI.DrawTexture(headerRect, _headerGradientTexture);
            }

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

            float buttonWidth = 80f;
            float buttonHeight = 30f;
            float rightMargin = 10f;
            float buttonSpacing = 5f;
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 4, 4)
            };

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

            EditorGUILayout.Space(5);
            DrawDivider();
        }

        private void DrawQuickActionsToolbar()
        {
            var containerStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(0, 0, 6, 6)
            };

            EditorGUILayout.BeginHorizontal(containerStyle);
            GUILayout.FlexibleSpace();
            var toolbarStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                padding = new RectOffset(10, 10, 4, 4),
                margin = new RectOffset(2, 2, 2, 2),
                fixedHeight = 30,
                imagePosition = ImagePosition.ImageLeft
            };

            var refreshContent = EditorGUIUtility.IconContent("d_Refresh");
            refreshContent.text = FocusOptimizerLocalization.Tr("toolbar.action.refresh");
            refreshContent.tooltip = FocusOptimizerLocalization.Tr("toolbar.action.refresh.tooltip");
            if (GUILayout.Button(
                    refreshContent,
                    toolbarStyle,
                    GUILayout.Width(150)))
            {
                FocusOptimizerController.RequestManualRefresh(FocusOptimizerLocalization.Tr("toolbar.action.refresh"));
            }

            var restoreContent = EditorGUIUtility.IconContent("d_UnityEditor.HierarchyWindow");
            restoreContent.text = FocusOptimizerLocalization.Tr("toolbar.action.restore");
            restoreContent.tooltip = FocusOptimizerLocalization.Tr("toolbar.action.restore.tooltip");
            if (GUILayout.Button(
                    restoreContent,
                    toolbarStyle,
                    GUILayout.Width(150)))
            {
                FocusOptimizerController.ForceAllowAutoRefresh();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawOnboardingCard(FocusOptimizerSettings settings)
        {
            if (settings.HasCompletedOnboarding)
            {
                return;
            }

            DrawCard(() =>
            {
                DrawSectionHeader(
                    FocusOptimizerLocalization.Tr("onboarding.header"),
                    FocusOptimizerLocalization.Tr("onboarding.header.tooltip"),
                    EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(
                    FocusOptimizerLocalization.Tr("onboarding.description"),
                    EditorStyles.wordWrappedLabel);

                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    FocusOptimizerLocalization.Tr("onboarding.recommendation"),
                    MessageType.Info);

                EditorGUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(
                        FocusOptimizerLocalization.Tr("onboarding.button.apply"),
                        GUILayout.Height(28)))
                {
                    ApplyRecommendedPreset(settings);
                    FocusOptimizerController.NotifySettingsChanged();
                }

                if (GUILayout.Button(
                        FocusOptimizerLocalization.Tr("onboarding.button.dismiss"),
                        GUILayout.Height(28)))
                {
                    settings.HasCompletedOnboarding = true;
                }

                EditorGUILayout.EndHorizontal();
            });
        }

        private void ApplyRecommendedPreset(FocusOptimizerSettings settings)
        {
            settings.EnableOptimizer = true;
            settings.SuspendAutoRefreshWhenInactive = true;
            settings.RefreshMode = FocusRefreshMode.Throttled;
            settings.ThrottleDelaySeconds = 1.5d;
            settings.DetectLargeExternalChanges = true;
            settings.AutoBypassLargeChange = true;
            settings.ShowConsoleHints = true;
            settings.HasCompletedOnboarding = true;
        }

        private void DrawRefreshCard(FocusOptimizerSettings settings)
        {
            DrawCard(() =>
            {
                DrawSectionHeader(
                    FocusOptimizerLocalization.Tr("refresh.header"),
                    FocusOptimizerLocalization.Tr("refresh.header.tooltip"),
                    EditorGUIUtility.IconContent("d_Refresh").image);

                EditorGUILayout.Space(8);

                EditorGUI.BeginChangeCheck();

                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.enable"),
                    FocusOptimizerLocalization.Tr("refresh.enable.tooltip"),
                    settings.EnableOptimizer,
                    value => settings.EnableOptimizer = value);

                EditorGUI.BeginDisabledGroup(!settings.EnableOptimizer);

                EditorGUILayout.Space(5);

                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.suspend"),
                    FocusOptimizerLocalization.Tr("refresh.suspend.tooltip"),
                    settings.SuspendAutoRefreshWhenInactive,
                    value => settings.SuspendAutoRefreshWhenInactive = value);

                EditorGUILayout.Space(5);

                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.console"),
                    FocusOptimizerLocalization.Tr("refresh.console.tooltip"),
                    settings.ShowConsoleHints,
                    value => settings.ShowConsoleHints = value);

                EditorGUILayout.Space(10);
                DrawDivider();
                EditorGUILayout.Space(10);

                EditorGUI.BeginDisabledGroup(!settings.SuspendAutoRefreshWhenInactive);

                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.massDetect"),
                    FocusOptimizerLocalization.Tr("refresh.massDetect.tooltip"),
                    settings.DetectLargeExternalChanges,
                    value => settings.DetectLargeExternalChanges = value);

                EditorGUI.BeginDisabledGroup(!settings.DetectLargeExternalChanges);

                EditorGUILayout.Space(5);
                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.massBypass"),
                    FocusOptimizerLocalization.Tr("refresh.massBypass.tooltip"),
                    settings.AutoBypassLargeChange,
                    value => settings.AutoBypassLargeChange = value);

                EditorGUILayout.Space(5);
                DrawLabelWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.massThreshold"),
                    FocusOptimizerLocalization.Tr("refresh.massThreshold.tooltip"));
                int threshold = EditorGUILayout.IntSlider(settings.LargeChangeThreshold, 10, 2000);
                if (threshold != settings.LargeChangeThreshold)
                {
                    settings.LargeChangeThreshold = threshold;
                }

                EditorGUILayout.Space(5);
                EditorGUI.BeginDisabledGroup(!settings.SuspendAutoRefreshWhenInactive);
                DrawToggleWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.experimentalBackgroundImport"),
                    FocusOptimizerLocalization.Tr("refresh.experimentalBackgroundImport.tooltip"),
                    settings.EnableExperimentalBackgroundImport,
                    value => settings.EnableExperimentalBackgroundImport = value);
                EditorGUI.EndDisabledGroup();

                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(10);
                DrawDivider();
                EditorGUILayout.Space(10);

                DrawLabelWithTooltip(
                    FocusOptimizerLocalization.Tr("refresh.mode"),
                    FocusOptimizerLocalization.Tr("refresh.mode.tooltip"));
                var mode = DrawModeSelector(settings.RefreshMode);

                double throttleDelay = settings.ThrottleDelaySeconds;
                if (mode == FocusRefreshMode.Throttled)
                {
                    EditorGUILayout.Space(5);
                    DrawLabelWithTooltip(
                        FocusOptimizerLocalization.Tr("refresh.throttle"),
                        FocusOptimizerLocalization.Tr("refresh.throttle.tooltip"));
                    float slider = EditorGUILayout.Slider((float)throttleDelay, 0.2f, 10f);
                    throttleDelay = slider;
                    DrawProgressBar(
                        FocusOptimizerLocalization.Tr("refresh.throttle"),
                        $"{throttleDelay:0.0}s",
                        Mathf.InverseLerp(0.2f, 10f, (float)throttleDelay),
                        new Color(0.2f, 0.6f, 0.9f));
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

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    new GUIContent(
                        FocusOptimizerLocalization.Tr("stats.massChange"),
                        FocusOptimizerLocalization.Tr("stats.massChange.tooltip")),
                    statLabelStyle,
                    GUILayout.Width(120));
                EditorGUILayout.LabelField(
                    settings.LastDetectedChangeCount.ToString(),
                    statValueStyle);
                EditorGUILayout.EndHorizontal();

                DrawMassChangeHint(settings.LastDetectedChangeCount, settings.LargeChangeThreshold);

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    new GUIContent(
                        FocusOptimizerLocalization.Tr("stats.lastTime"),
                        FocusOptimizerLocalization.Tr("stats.lastTime.tooltip")),
                    statLabelStyle,
                    GUILayout.Width(120));
                double secondsSince = Mathf.Max(0f, (float)(EditorApplication.timeSinceStartup - settings.LastRefreshEditorTime));
                EditorGUILayout.LabelField(
                    FocusOptimizerLocalization.Tr("stats.lastTime.value", secondsSince),
                    statValueStyle);
                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawHeroBanner(FocusOptimizerSettings settings)
        {
            var heroStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(18, 18, 16, 16),
                margin = new RectOffset(0, 0, 8, 8)
            };
            heroStyle.normal.background = _heroGradientTexture;

            EditorGUILayout.BeginVertical(heroStyle);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.15f, 0.15f, 0.15f) }
            };
            var subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.25f, 0.25f, 0.25f) }
            };

            EditorGUILayout.BeginHorizontal();
            var heroIcon = EditorGUIUtility.IconContent("d_UnityEditor.SceneView");
            if (heroIcon?.image != null)
            {
                GUILayout.Label(heroIcon.image, GUILayout.Width(36), GUILayout.Height(36));
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(FocusOptimizerLocalization.Tr("status.header"), titleStyle);
            EditorGUILayout.LabelField(FocusOptimizerLocalization.Tr("window.subtitle"), subtitleStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            string stateText = FocusOptimizerController.AutoRefreshSuspendedByPlugin
                ? FocusOptimizerLocalization.Tr("status.suspended")
                : FocusOptimizerLocalization.Tr("status.normal");
            DrawStatusChip(
                $"{FocusOptimizerLocalization.Tr("status.summary")}：{stateText}",
                FocusOptimizerController.AutoRefreshSuspendedByPlugin
                    ? new Color(0.95f, 0.55f, 0.2f)
                    : new Color(0.3f, 0.8f, 0.4f),
                heroIcon?.image,
                FocusOptimizerLocalization.Tr("status.detail.help"));

            int changeCount = settings.LastDetectedChangeCount;
            DrawStatusChip(
                $"{FocusOptimizerLocalization.Tr("stats.massChange")}：{changeCount}",
                changeCount >= settings.LargeChangeThreshold
                    ? new Color(0.9f, 0.4f, 0.4f)
                    : new Color(0.35f, 0.65f, 0.95f),
                EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow")?.image,
                FocusOptimizerLocalization.Tr("stats.massChange.tooltip"));

            var modeLabel = settings.RefreshMode switch
            {
                FocusRefreshMode.Immediate => FocusOptimizerLocalization.Tr("refresh.mode.immediate"),
                FocusRefreshMode.Throttled => FocusOptimizerLocalization.Tr("refresh.mode.throttled"),
                _ => FocusOptimizerLocalization.Tr("refresh.mode.manual")
            };
            DrawStatusChip(
                $"{FocusOptimizerLocalization.Tr("refresh.mode")}：{modeLabel}",
                new Color(0.45f, 0.55f, 0.95f),
                EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow")?.image,
                FocusOptimizerLocalization.Tr("refresh.mode.tooltip"));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            DrawMetricBadge(
                FocusOptimizerLocalization.Tr("stats.duration"),
                $"{Mathf.Max(0f, (float)settings.LastRefreshDuration):0.000}s",
                FocusOptimizerLocalization.Tr("stats.duration.tooltip"),
                EditorGUIUtility.IconContent("d_Profiler.Record")?.image,
                DetermineDurationColor(settings.LastRefreshDuration));
            DrawMetricBadge(
                FocusOptimizerLocalization.Tr("stats.massChange"),
                settings.LastDetectedChangeCount.ToString(),
                FocusOptimizerLocalization.Tr("stats.massChange.tooltip"),
                EditorGUIUtility.IconContent("d_Project")?.image,
                settings.LastDetectedChangeCount >= settings.LargeChangeThreshold
                    ? new Color(0.9f, 0.4f, 0.4f)
                    : new Color(0.3f, 0.7f, 0.9f));
            double secondsSince = Mathf.Max(0f, (float)(EditorApplication.timeSinceStartup - settings.LastRefreshEditorTime));
            DrawMetricBadge(
                FocusOptimizerLocalization.Tr("stats.lastTime"),
                FocusOptimizerLocalization.Tr("stats.lastTime.value", secondsSince),
                FocusOptimizerLocalization.Tr("stats.lastTime.tooltip"),
                EditorGUIUtility.IconContent("d_UnityEditor.GameView")?.image,
                new Color(0.6f, 0.5f, 0.9f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private Color DetermineDurationColor(double duration)
        {
            if (duration < 0.5f)
            {
                return new Color(0.3f, 0.8f, 0.3f);
            }

            return duration < 1.0f
                ? new Color(0.9f, 0.75f, 0.3f)
                : new Color(0.9f, 0.45f, 0.3f);
        }

        private void DrawMetricBadge(string label, string value, string tooltip, Texture icon, Color accent)
        {
            var badgeStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(0, 8, 0, 0)
            };
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.05f)
                : new Color(0f, 0f, 0f, 0.05f);

            EditorGUILayout.BeginVertical(badgeStyle, GUILayout.MinHeight(64));

            GUI.backgroundColor = originalColor;

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.25f, 0.25f, 0.25f) }
            };
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), labelStyle);

            EditorGUILayout.BeginHorizontal();
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));
            }

            var valueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = accent }
            };

            EditorGUILayout.LabelField(value, valueStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusChip(string text, Color color, Texture icon, string tooltip)
        {
            var content = new GUIContent(text, tooltip);
            var chipStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.15f, 0.15f, 0.15f) }
            };
            float width = chipStyle.CalcSize(content).x + (icon != null ? 40f : 28f);
            var rect = GUILayoutUtility.GetRect(width, 26f, GUILayout.ExpandWidth(false));
            var background = new Color(color.r, color.g, color.b, EditorGUIUtility.isProSkin ? 0.45f : 0.25f);
            EditorGUI.DrawRect(rect, background);

            var innerRect = new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, rect.height - 8f);
            if (icon != null)
            {
                var iconRect = new Rect(innerRect.x, innerRect.y, 16f, 16f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                innerRect.x += 20f;
                innerRect.width -= 20f;
            }

            GUI.Label(innerRect, content, chipStyle);
        }

        private FocusRefreshMode DrawModeSelector(FocusRefreshMode current)
        {
            EditorGUILayout.BeginHorizontal();
            current = DrawModeChip(
                FocusRefreshMode.Immediate,
                current,
                FocusOptimizerLocalization.Tr("refresh.mode.immediate"),
                EditorGUIUtility.IconContent("d_PlayButton")?.image,
                new Color(0.35f, 0.75f, 0.95f));
            current = DrawModeChip(
                FocusRefreshMode.Throttled,
                current,
                FocusOptimizerLocalization.Tr("refresh.mode.throttled"),
                EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow")?.image,
                new Color(0.5f, 0.6f, 0.95f));
            current = DrawModeChip(
                FocusRefreshMode.Manual,
                current,
                FocusOptimizerLocalization.Tr("refresh.mode.manual"),
                EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow")?.image,
                new Color(0.9f, 0.65f, 0.35f));
            EditorGUILayout.EndHorizontal();

            return current;
        }

        private FocusRefreshMode DrawModeChip(
            FocusRefreshMode target,
            FocusRefreshMode current,
            string label,
            Texture icon,
            Color accent)
        {
            bool selected = current == target;
            var style = new GUIStyle("Button")
            {
                fontSize = 12,
                fixedHeight = 32,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 4, 4)
            };
            style.imagePosition = icon != null ? ImagePosition.ImageAbove : ImagePosition.TextOnly;

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = selected
                ? accent
                : new Color(accent.r, accent.g, accent.b, EditorGUIUtility.isProSkin ? 0.35f : 0.2f);

            style.normal.textColor = selected ? Color.white : (EditorGUIUtility.isProSkin ? Color.white : new Color(0.2f, 0.2f, 0.2f));

            var content = new GUIContent(label) { image = icon };
            bool pressed = GUILayout.Toggle(selected, content, style, GUILayout.MinWidth(90));

            GUI.backgroundColor = previousColor;

            if (pressed)
            {
                current = target;
            }

            return current;
        }

        private void DrawProgressBar(string label, string value, float normalized, Color fillColor)
        {
            var infoStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.2f, 0.2f, 0.2f) }
            };
            EditorGUILayout.LabelField($"{label} · {value}", infoStyle);

            var rect = GUILayoutUtility.GetRect(10, 18, GUILayout.ExpandWidth(true));
            var background = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.15f)
                : new Color(0f, 0f, 0f, 0.15f);
            EditorGUI.DrawRect(rect, background);

            var fillRect = rect;
            fillRect.width *= Mathf.Clamp01(normalized);
            EditorGUI.DrawRect(fillRect, fillColor);

            var overlayStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.2f, 0.2f, 0.2f) },
                fontStyle = FontStyle.Bold
            };
            GUI.Label(rect, value, overlayStyle);
        }

        private void DrawCard(System.Action content)
        {
            if (_cardBackgroundTexture == null)
            {
                _cardBackgroundTexture = CreateCardBackground();
            }

            var cardStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(16, 16, 14, 14),
                margin = new RectOffset(0, 0, 4, 4)
            };
            cardStyle.normal.background = _cardBackgroundTexture;

            EditorGUILayout.BeginVertical(cardStyle);
            content?.Invoke();
            EditorGUILayout.EndVertical();
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
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
                padding = new RectOffset(14, 14, 5, 5),
                normal = { textColor = Color.white }
            };

            var normalTex = CreateColorTexture(color);
            var hoverTex = CreateColorTexture(new Color(
                Mathf.Min(1f, color.r * 1.15f),
                Mathf.Min(1f, color.g * 1.15f),
                Mathf.Min(1f, color.b * 1.15f)));
            var activeTex = CreateColorTexture(new Color(
                Mathf.Max(0f, color.r * 0.85f),
                Mathf.Max(0f, color.g * 0.85f),
                Mathf.Max(0f, color.b * 0.85f)));

            buttonStyle.normal.background = normalTex;
            buttonStyle.hover.background = hoverTex;
            buttonStyle.active.background = activeTex;

            var content = new GUIContent(label, tooltip);
            return GUILayout.Button(content, buttonStyle);
        }

        private void DrawStatusInfo()
        {
            string stateText = FocusOptimizerController.AutoRefreshSuspendedByPlugin
                ? FocusOptimizerLocalization.Tr("status.suspended")
                : FocusOptimizerLocalization.Tr("status.normal");

            double remaining = FocusOptimizerController.NextScheduledRefreshTime - EditorApplication.timeSinceStartup;
            string statusMessage = remaining > 0
                ? FocusOptimizerLocalization.Tr("status.scheduled", stateText, remaining)
                : FocusOptimizerLocalization.Tr("status.current", stateText);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                FocusOptimizerLocalization.Tr("status.summary"),
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                statusMessage,
                EditorStyles.wordWrappedLabel);

            _statusDetailFoldout = EditorGUILayout.Foldout(
                _statusDetailFoldout,
                FocusOptimizerLocalization.Tr("status.detail"),
                true);
            if (_statusDetailFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(
                    FocusOptimizerLocalization.Tr("status.detail.help"),
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            var settings = FocusOptimizerSettings.instance;
            if (settings.LastRefreshSkippedDueToLargeChange)
            {
                EditorGUILayout.HelpBox(
                    FocusOptimizerLocalization.Tr(
                        "status.massiveChange",
                        settings.LastDetectedChangeCount,
                        settings.LargeChangeThreshold),
                    MessageType.Warning);
            }
            else if (settings.LastDetectedChangeCount > 0 && settings.DetectLargeExternalChanges)
            {
                EditorGUILayout.HelpBox(
                    FocusOptimizerLocalization.Tr(
                        "status.massiveChangeInfo",
                        settings.LastDetectedChangeCount),
                    MessageType.Info);
            }
        }

        private void DrawMassChangeHint(int changeCount, int threshold)
        {
            if (changeCount <= 0)
            {
                return;
            }

            string key = changeCount >= threshold
                ? "stats.massChange.levelHigh"
                : changeCount >= Mathf.Max(10, threshold / 2)
                    ? "stats.massChange.levelMid"
                    : "stats.massChange.levelLow";

            EditorGUILayout.HelpBox(
                FocusOptimizerLocalization.Tr(key, changeCount, threshold),
                MessageType.None);
        }

        private void DrawDivider()
        {
            var lineStyle = new GUIStyle(GUI.skin.box)
            {
                fixedHeight = 1
            };
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
                ? new Color(0.22f, 0.24f, 0.28f, 1f)
                : new Color(0.96f, 0.96f, 0.97f, 1f);
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

        private Texture2D CreateHeroGradient()
        {
            const int height = 120;
            var texture = new Texture2D(1, height);
            var topColor = EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.21f, 0.32f, 1f)
                : new Color(0.91f, 0.95f, 1f, 1f);
            var bottomColor = EditorGUIUtility.isProSkin
                ? new Color(0.1f, 0.12f, 0.2f, 1f)
                : new Color(0.82f, 0.9f, 1f, 1f);

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










