using UnityEditor;
using UnityEngine;

namespace PoseDrive.Editor
{
    /// <summary>
    /// PoseDrive 设置窗口，用于语言切换。
    /// </summary>
    public class PoseDriveSettingsWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Tools/PoseDrive/设置", priority = 220)]
        public static void Open()
        {
            PoseDriveSettingsWindow window = GetWindow<PoseDriveSettingsWindow>();
            window.UpdateTitle();
            window.minSize = new Vector2(360, 240);
        }

        private void OnEnable()
        {
            PoseDriveLocalization.LanguageChanged += HandleLanguageChanged;
            UpdateTitle();
        }

        private void OnDisable()
        {
            PoseDriveLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged()
        {
            UpdateTitle();
            Repaint();
        }

        private void UpdateTitle()
        {
            titleContent = new GUIContent(PoseDriveLocalization.Tr("settings.title"));
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(EditorStylesLibrary.Container))
            {
                EditorStylesLibrary.DrawHeader(
                    PoseDriveLocalization.Tr("settings.title"),
                    PoseDriveLocalization.Tr("settings.subtitle"));

                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                using (EditorStylesLibrary.CardScope(
                           PoseDriveLocalization.Tr("settings.language")))
                {
                    foreach (PoseLanguageInfo info in PoseDriveLocalization.AvailableLanguages)
                    {
                        bool selected = PoseDriveLocalization.CurrentLanguage == info.Language;
                        bool newSelected = EditorGUILayout.ToggleLeft(info.DisplayName, selected);
                        if (newSelected && !selected)
                        {
                            PoseDriveLocalization.CurrentLanguage = info.Language;
                            EditorUtility.DisplayDialog(
                                PoseDriveLocalization.Tr("settings.title"),
                                PoseDriveLocalization.Tr("settings.subtitle"),
                                PoseDriveLocalization.Tr("settings.apply"));
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }
}

