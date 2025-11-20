using UnityEditor;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    internal sealed class FocusOptimizerWindow : EditorWindow
    {
        private static int _openCount;
        private FocusOptimizerSettingsView _view;

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
            _view ??= new FocusOptimizerSettingsView();
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
            (_view ??= new FocusOptimizerSettingsView()).OnGUI();
        }
    }
}
