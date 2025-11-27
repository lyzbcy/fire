using UnityEditor;
using UnityEngine;

namespace PoseController.Editor
{
    /// <summary>
    /// PoseController 设置窗口，用于语言切换。
    /// </summary>
    public class PoseControllerSettingsWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Tools/动捕控制器/设置", priority = 220)]
        public static void Open()
        {
            PoseControllerSettingsWindow window = GetWindow<PoseControllerSettingsWindow>();
            window.UpdateTitle();
            window.minSize = new Vector2(360, 240);
        }

        private void OnEnable()
        {
            PoseControllerLocalization.LanguageChanged += HandleLanguageChanged;
            UpdateTitle();
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
            titleContent = new GUIContent(PoseControllerLocalization.Tr("settings.title"));
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(EditorStylesLibrary.Container))
            {
                EditorStylesLibrary.DrawHeader(
                    PoseControllerLocalization.Tr("settings.title"),
                    PoseControllerLocalization.Tr("settings.subtitle"));

                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                using (EditorStylesLibrary.CardScope(
                           PoseControllerLocalization.Tr("settings.language")))
                {
                    foreach (PoseLanguageInfo info in PoseControllerLocalization.AvailableLanguages)
                    {
                        bool selected = PoseControllerLocalization.CurrentLanguage == info.Language;
                        bool newSelected = EditorGUILayout.ToggleLeft(info.DisplayName, selected);
                        if (newSelected && !selected)
                        {
                            PoseControllerLocalization.CurrentLanguage = info.Language;
                            EditorUtility.DisplayDialog(
                                PoseControllerLocalization.Tr("settings.title"),
                                PoseControllerLocalization.Tr("settings.subtitle"),
                                PoseControllerLocalization.Tr("settings.apply"));
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }
}

