using System;
using UnityEditor;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    internal enum FocusRefreshMode
    {
        Immediate = 0,
        Throttled = 1,
        Manual = 2
    }

    [FilePath("ProjectSettings/FocusOptimizerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class FocusOptimizerSettings : ScriptableSingleton<FocusOptimizerSettings>
    {
        [SerializeField] private bool _enableOptimizer = true;
        [SerializeField] private bool _suspendAutoRefreshWhenInactive = true;
        [SerializeField] private FocusRefreshMode _refreshMode = FocusRefreshMode.Throttled;
        [SerializeField] private double _throttleDelaySeconds = 1.5d;
        [SerializeField] private bool _promptOnManualMode = true;
        [SerializeField] private bool _showConsoleHints = true;
        [SerializeField] private bool _detectLargeExternalChanges = true;
        [SerializeField] private bool _autoBypassLargeChange = true;
        [SerializeField] private int _largeChangeThreshold = 200;
        [SerializeField] private bool _enableExperimentalBackgroundImport;

        [Header("Enter Play Mode 配置")]
        [SerializeField] private bool _enableEnterPlayModeHelper = false;
        [SerializeField] private bool _applyEnterPlayModePreset = false;
        [SerializeField] private EnterPlayModeOptions _enterPlayModeOptions =
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        [SerializeField] private bool _autoRestoreEnterPlayMode = true;

        [Header("统计信息")]
        [SerializeField] private double _lastRefreshDuration;
        [SerializeField] private string _lastRefreshReason = "初始";
        [SerializeField] private double _lastRefreshEditorTime;
        [SerializeField] private int _lastDetectedChangeCount;
        [SerializeField] private bool _lastRefreshSkippedDueToLargeChange;
        [SerializeField] private bool _hasCompletedOnboarding;

        internal bool EnableOptimizer
        {
            get => _enableOptimizer;
            set => SetValue(ref _enableOptimizer, value);
        }

        internal bool SuspendAutoRefreshWhenInactive
        {
            get => _suspendAutoRefreshWhenInactive;
            set => SetValue(ref _suspendAutoRefreshWhenInactive, value);
        }

        internal FocusRefreshMode RefreshMode
        {
            get => _refreshMode;
            set => SetValue(ref _refreshMode, value);
        }

        internal double ThrottleDelaySeconds
        {
            get => _throttleDelaySeconds;
            set => SetValue(ref _throttleDelaySeconds, Math.Max(0.1d, value));
        }

        internal bool PromptOnManualMode
        {
            get => _promptOnManualMode;
            set => SetValue(ref _promptOnManualMode, value);
        }

        internal bool ShowConsoleHints
        {
            get => _showConsoleHints;
            set => SetValue(ref _showConsoleHints, value);
        }

        internal bool DetectLargeExternalChanges
        {
            get => _detectLargeExternalChanges;
            set => SetValue(ref _detectLargeExternalChanges, value);
        }

        internal bool AutoBypassLargeChange
        {
            get => _autoBypassLargeChange;
            set => SetValue(ref _autoBypassLargeChange, value);
        }

        internal int LargeChangeThreshold
        {
            get => _largeChangeThreshold;
            set => SetValue(ref _largeChangeThreshold, Mathf.Clamp(value, 10, 5000));
        }

        internal bool EnableExperimentalBackgroundImport
        {
            get => _enableExperimentalBackgroundImport;
            set => SetValue(ref _enableExperimentalBackgroundImport, value);
        }

        internal bool EnableEnterPlayModeHelper
        {
            get => _enableEnterPlayModeHelper;
            set => SetValue(ref _enableEnterPlayModeHelper, value);
        }

        internal bool ApplyEnterPlayModePreset
        {
            get => _applyEnterPlayModePreset;
            set => SetValue(ref _applyEnterPlayModePreset, value);
        }

        internal EnterPlayModeOptions PlayModeOptions
        {
            get => _enterPlayModeOptions;
            set => SetValue(ref _enterPlayModeOptions, value);
        }

        internal bool AutoRestoreEnterPlayMode
        {
            get => _autoRestoreEnterPlayMode;
            set => SetValue(ref _autoRestoreEnterPlayMode, value);
        }

        internal double LastRefreshDuration
        {
            get => _lastRefreshDuration;
            set => SetValue(ref _lastRefreshDuration, value);
        }

        internal string LastRefreshReason
        {
            get => _lastRefreshReason;
            set => SetValue(ref _lastRefreshReason, value);
        }

        internal double LastRefreshEditorTime
        {
            get => _lastRefreshEditorTime;
            set => SetValue(ref _lastRefreshEditorTime, value);
        }

        internal int LastDetectedChangeCount
        {
            get => _lastDetectedChangeCount;
            set => SetValue(ref _lastDetectedChangeCount, value);
        }

        internal bool LastRefreshSkippedDueToLargeChange
        {
            get => _lastRefreshSkippedDueToLargeChange;
            set => SetValue(ref _lastRefreshSkippedDueToLargeChange, value);
        }

        internal bool HasCompletedOnboarding
        {
            get => _hasCompletedOnboarding;
            set => SetValue(ref _hasCompletedOnboarding, value);
        }

        internal void SaveSettings()
        {
            Save(true);
        }

        private void SetValue<T>(ref T field, T value)
        {
            if (Equals(field, value)) return;
            field = value;
            Save(true);
        }
    }
}

