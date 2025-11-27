using UnityEditor;

namespace FireTools.FocusOptimizer
{
    internal static class EnterPlayModeOptionHelper
    {
        private static bool _originalCaptured;
        private static bool _originalEnabled;
        private static EnterPlayModeOptions _originalOptions;
        private static bool _presetApplied;

        internal static void TryApplyPreset()
        {
            var settings = FocusOptimizerSettings.instance;

            if (!settings.EnableEnterPlayModeHelper || !settings.ApplyEnterPlayModePreset)
            {
                RestoreIfNeeded(force: false);
                return;
            }

            if (!_originalCaptured)
            {
                _originalEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                _originalOptions = EditorSettings.enterPlayModeOptions;
                _originalCaptured = true;
            }

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = settings.PlayModeOptions;
            _presetApplied = true;
        }

        internal static void RestoreIfNeeded(bool force)
        {
            var settings = FocusOptimizerSettings.instance;
            if (!_presetApplied) return;
            if (!force && !settings.AutoRestoreEnterPlayMode) return;
            if (!_originalCaptured) return;

            EditorSettings.enterPlayModeOptionsEnabled = _originalEnabled;
            EditorSettings.enterPlayModeOptions = _originalOptions;
            _presetApplied = false;
        }
    }
}

