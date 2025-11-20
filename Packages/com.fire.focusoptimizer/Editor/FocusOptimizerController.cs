using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    [InitializeOnLoad]
    internal static class FocusOptimizerController
    {
        private static bool _lastActiveState;
        private static bool _autoRefreshSuspended;
        private static double _scheduledRefreshAt = -1d;
        private static string _scheduledReason = string.Empty;

        static FocusOptimizerController()
        {
            _lastActiveState = InternalEditorUtility.isApplicationActive;
            EditorApplication.update += OnEditorUpdate;
            EnterPlayModeOptionHelper.TryApplyPreset();
        }

        internal static bool AutoRefreshSuspendedByPlugin => _autoRefreshSuspended;
        internal static double NextScheduledRefreshTime => _scheduledRefreshAt;

        internal static void NotifySettingsChanged()
        {
            var settings = FocusOptimizerSettings.instance;
            if (!settings.EnableOptimizer)
            {
                RestoreAutoRefreshIfNeeded();
                _scheduledRefreshAt = -1d;
            }

            EnterPlayModeOptionHelper.TryApplyPreset();
        }

        internal static void ForceAllowAutoRefresh()
        {
            RestoreAutoRefreshIfNeeded();
        }

        internal static void RequestManualRefresh(string reason)
        {
            PerformRefresh(reason);
        }

        private static void OnEditorUpdate()
        {
            var settings = FocusOptimizerSettings.instance;

            bool currentActive = InternalEditorUtility.isApplicationActive;
            if (currentActive != _lastActiveState)
            {
                if (!currentActive)
                {
                    HandleLostFocus(settings);
                }
                else
                {
                    HandleGainFocus(settings);
                }

                _lastActiveState = currentActive;
            }

            if (_scheduledRefreshAt > 0d && EditorApplication.timeSinceStartup >= _scheduledRefreshAt)
            {
                PerformRefresh(_scheduledReason);
            }
        }

        private static void HandleLostFocus(FocusOptimizerSettings settings)
        {
            if (!settings.EnableOptimizer) return;
            if (!settings.SuspendAutoRefreshWhenInactive) return;
            if (_autoRefreshSuspended) return;

            AssetDatabase.DisallowAutoRefresh();
            _autoRefreshSuspended = true;
            LogInfo("Unity 失去焦点，暂停自动刷新。");
        }

        private static void HandleGainFocus(FocusOptimizerSettings settings)
        {
            if (!settings.EnableOptimizer)
            {
                RestoreAutoRefreshIfNeeded();
                return;
            }

            switch (settings.RefreshMode)
            {
                case FocusRefreshMode.Immediate:
                    PerformRefresh("立即刷新");
                    break;
                case FocusRefreshMode.Throttled:
                    ScheduleThrottledRefresh(settings);
                    break;
                case FocusRefreshMode.Manual:
                    if (settings.PromptOnManualMode)
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (FocusOptimizerWindow.HasOpenInstances)
                            {
                                FocusOptimizerWindow.NotifyManualRefreshNeeded();
                            }
                            else
                            {
                                LogInfo("检测到 Unity 回到前台，等待手动刷新。");
                            }
                        };
                    }
                    break;
            }
        }

        private static void ScheduleThrottledRefresh(FocusOptimizerSettings settings)
        {
            _scheduledRefreshAt = EditorApplication.timeSinceStartup + settings.ThrottleDelaySeconds;
            _scheduledReason = "限流刷新";
            LogInfo($"已计划在 {settings.ThrottleDelaySeconds:0.0}s 后刷新。");
        }

        private static void PerformRefresh(string reason)
        {
            var settings = FocusOptimizerSettings.instance;
            _scheduledRefreshAt = -1d;

            RestoreAutoRefreshIfNeeded();

            double start = EditorApplication.timeSinceStartup;
            AssetDatabase.Refresh();
            double duration = EditorApplication.timeSinceStartup - start;

            settings.LastRefreshDuration = duration;
            settings.LastRefreshReason = string.IsNullOrEmpty(reason) ? "未命名" : reason;
            settings.LastRefreshEditorTime = start;

            LogInfo($"执行刷新（{settings.LastRefreshReason}），耗时 {duration:0.000}s。");
        }

        private static void RestoreAutoRefreshIfNeeded()
        {
            if (!_autoRefreshSuspended) return;
            AssetDatabase.AllowAutoRefresh();
            _autoRefreshSuspended = false;
            LogInfo("恢复自动刷新。");
        }

        private static void LogInfo(string msg)
        {
            if (!FocusOptimizerSettings.instance.ShowConsoleHints) return;
            Debug.Log($"[焦点卡顿优化助手] {msg}");
        }
    }
}

