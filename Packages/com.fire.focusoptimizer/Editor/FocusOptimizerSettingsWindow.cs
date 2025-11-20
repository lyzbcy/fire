using UnityEditor;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    internal sealed class FocusOptimizerSettingsWindow : EditorWindow
    {
        [MenuItem("Tools/焦点卡顿优化助手/设置", priority = 202)]
        private static void OpenWindow()
        {
            var window = GetWindow<FocusOptimizerSettingsWindow>();
            window.titleContent = new GUIContent(
                FocusOptimizerLocalization.Tr("settings.title"),
                EditorGUIUtility.IconContent("d_Settings").image);
            window.minSize = new Vector2(350, 200);
            window.Show();
        }

        internal static void OpenWindowStatic()
        {
            OpenWindow();
        }

        private void OnEnable()
        {
            FocusOptimizerLocalization.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            FocusOptimizerLocalization.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            titleContent = new GUIContent(
                FocusOptimizerLocalization.Tr("settings.title"),
                EditorGUIUtility.IconContent("d_Settings").image);
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // 语言选择
            DrawLabelWithTooltip(
                FocusOptimizerLocalization.Tr("settings.language"),
                FocusOptimizerLocalization.Tr("settings.language.tooltip"));

            var currentLang = FocusOptimizerLocalization.CurrentLanguage;
            var availableLangs = FocusOptimizerLocalization.AvailableLanguages;

            EditorGUILayout.Space(5);

            foreach (var langInfo in availableLangs)
            {
                EditorGUI.BeginChangeCheck();
                bool selected = EditorGUILayout.ToggleLeft(
                    langInfo.DisplayName,
                    currentLang == langInfo.Language);
                
                if (EditorGUI.EndChangeCheck() && selected)
                {
                    FocusOptimizerLocalization.SetLanguage(langInfo.Language);
                    Repaint();
                }
            }

            EditorGUILayout.Space(20);

            // 关闭按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(FocusOptimizerLocalization.Tr("settings.close"), GUILayout.Width(100)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLabelWithTooltip(string label, string tooltip)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip));
        }
    }
}

