using UnityEditor;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    internal sealed class FocusOptimizerWindow : EditorWindow
    {
        private static readonly GUIContent RefreshHeader = new("刷新策略", "控制 Unity 回到前台后的刷新方式。");
        private static readonly GUIContent PlayModeHeader = new("Enter Play Mode 优化", "可选地自动切换 Enter Play Mode Options。");
        private static int _openCount;

        internal static bool HasOpenInstances => _openCount > 0;

        [MenuItem("Tools/Focus Optimizer", priority = 201)]
        private static void OpenWindow()
        {
            GetWindow<FocusOptimizerWindow>("Focus Optimizer");
        }

        private void OnEnable()
        {
            _openCount++;
        }

        private void OnDisable()
        {
            _openCount = Mathf.Max(0, _openCount - 1);
        }

        internal static void NotifyManualRefreshNeeded()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<FocusOptimizerWindow>())
            {
                window.ShowNotification(new GUIContent("检测到脚本更新，请手动刷新"));
            }
        }

        private void OnGUI()
        {
            var settings = FocusOptimizerSettings.instance;
            EditorGUILayout.Space(6);
            DrawRefreshSection(settings);
            EditorGUILayout.Space(8);
            DrawEnterPlayModeSection(settings);
            EditorGUILayout.Space(8);
            DrawStatsSection(settings);
        }

        private void DrawRefreshSection(FocusOptimizerSettings settings)
        {
            EditorGUILayout.LabelField(RefreshHeader, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool enableOptimizer = EditorGUILayout.ToggleLeft("启用 Focus Optimizer", settings.EnableOptimizer);
            bool suspendAutoRefresh = EditorGUILayout.ToggleLeft("失焦时暂停自动刷新", settings.SuspendAutoRefreshWhenInactive);
            bool showConsole = EditorGUILayout.ToggleLeft("在 Console 输出状态提示", settings.ShowConsoleHints);
            var mode = (FocusRefreshMode)EditorGUILayout.EnumPopup("焦点回归策略", settings.RefreshMode);
            double throttleDelay = settings.ThrottleDelaySeconds;
            if (mode == FocusRefreshMode.Throttled)
            {
                float slider = EditorGUILayout.Slider("延迟秒数", (float)throttleDelay, 0.2f, 10f);
                throttleDelay = slider;
            }
            bool promptManual = settings.PromptOnManualMode;
            if (mode == FocusRefreshMode.Manual)
            {
                promptManual = EditorGUILayout.Toggle("手动模式提示", promptManual);
            }

            if (EditorGUI.EndChangeCheck())
            {
                settings.EnableOptimizer = enableOptimizer;
                settings.SuspendAutoRefreshWhenInactive = suspendAutoRefresh;
                settings.ShowConsoleHints = showConsole;
                settings.RefreshMode = mode;
                settings.ThrottleDelaySeconds = throttleDelay;
                settings.PromptOnManualMode = promptManual;
                FocusOptimizerController.NotifySettingsChanged();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("立即刷新"))
            {
                FocusOptimizerController.RequestManualRefresh("窗口手动刷新");
            }

            if (GUILayout.Button("重新允许自动刷新"))
            {
                FocusOptimizerController.ForceAllowAutoRefresh();
            }
            EditorGUILayout.EndHorizontal();

            string autoRefreshState = FocusOptimizerController.AutoRefreshSuspendedByPlugin ? "已暂停" : "正常";
            double remaining = FocusOptimizerController.NextScheduledRefreshTime - EditorApplication.timeSinceStartup;
            if (remaining > 0)
            {
                EditorGUILayout.HelpBox($"自动刷新状态：{autoRefreshState}，预计 {remaining:0.0}s 后刷新。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"自动刷新状态：{autoRefreshState}", MessageType.Info);
            }
        }

        private void DrawEnterPlayModeSection(FocusOptimizerSettings settings)
        {
            EditorGUILayout.LabelField(PlayModeHeader, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool helperEnabled = EditorGUILayout.ToggleLeft("启用 Enter Play Mode 辅助", settings.EnableEnterPlayModeHelper);
            EditorGUI.BeginDisabledGroup(!helperEnabled);
            bool applyPreset = EditorGUILayout.ToggleLeft("进入 Play Mode 时应用预设", settings.ApplyEnterPlayModePreset);
            var options = (EnterPlayModeOptions)EditorGUILayout.EnumFlagsField("预设选项", settings.PlayModeOptions);
            bool autoRestore = EditorGUILayout.ToggleLeft("停用时自动恢复原设置", settings.AutoRestoreEnterPlayMode);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                settings.EnableEnterPlayModeHelper = helperEnabled;
                settings.ApplyEnterPlayModePreset = applyPreset;
                settings.PlayModeOptions = options;
                settings.AutoRestoreEnterPlayMode = autoRestore;
                FocusOptimizerController.NotifySettingsChanged();
            }

            if (GUILayout.Button("立即恢复原 Play Mode 设置"))
            {
                EnterPlayModeOptionHelper.RestoreIfNeeded(force: true);
            }
        }

        private void DrawStatsSection(FocusOptimizerSettings settings)
        {
            EditorGUILayout.LabelField("刷新统计", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("最近刷新原因", settings.LastRefreshReason);
            EditorGUILayout.LabelField("最近耗时", $"{settings.LastRefreshDuration:0.000}s");
        }
    }
}

