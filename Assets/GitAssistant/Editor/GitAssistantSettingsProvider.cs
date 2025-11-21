using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fire.GitAssistant
{
    internal sealed class GitAssistantSettingsProvider : SettingsProvider
    {
        private const string Path = "Project/Version Control Assistant";
        private SerializedObject _serializedSettings;

        private GitAssistantSettingsProvider(string settingsPath, SettingsScope scope)
            : base(settingsPath, scope) { }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            if (GitAssistantPreferences.Instance == null)
            {
                return null;
            }

            return new GitAssistantSettingsProvider(Path, SettingsScope.Project)
            {
                label = "版本控制助手",
                keywords = new System.Collections.Generic.HashSet<string>(new[]
                {
                    "git", "remote", "branch", "push", "pull"
                })
            };
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _serializedSettings = new SerializedObject(GitAssistantPreferences.Instance);
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedSettings == null)
            {
                return;
            }

            _serializedSettings.Update();

            EditorGUILayout.LabelField(GitLocalization.Tr("settings.remoteSection"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(GitLocalization.Tr("settings.remoteHelp"), MessageType.Info);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("_defaultRemote"),
                new GUIContent(GitLocalization.Tr("settings.defaultRemote")));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("_defaultPushBranch"),
                new GUIContent(GitLocalization.Tr("settings.defaultBranch")));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(GitLocalization.Tr("settings.customPushSection"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("_useCustomPushTarget"),
                new GUIContent(GitLocalization.Tr("settings.customPushToggle")));

            using (new EditorGUI.DisabledScope(!_serializedSettings.FindProperty("_useCustomPushTarget").boolValue))
            {
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("_customPushTarget"),
                    new GUIContent(GitLocalization.Tr("settings.customPushTarget")));
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(GitLocalization.Tr("settings.resetDefaults"), GUILayout.Width(180)))
            {
                GitAssistantPreferences.Instance.ResetToDefaults();
                _serializedSettings = new SerializedObject(GitAssistantPreferences.Instance);
            }

            if (_serializedSettings.ApplyModifiedProperties())
            {
                GitAssistantPreferences.Instance.SaveSettings();
            }
        }
    }
}

