using UnityEditor;
using UnityEngine;

namespace Fire.VersionControlAssistant
{
    [FilePath("ProjectSettings/VersionControlAssistantSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class VersionControlAssistantPreferences : ScriptableSingleton<VersionControlAssistantPreferences>
    {
        [SerializeField] private string _defaultRemote = "origin";
        [SerializeField] private string _defaultPushBranch = "main";
        [SerializeField] private bool _useCustomPushTarget;
        [SerializeField] private string _customPushTarget = string.Empty;

        public static VersionControlAssistantPreferences Instance => instance;

        public string DefaultRemote
        {
            get => string.IsNullOrWhiteSpace(_defaultRemote) ? "origin" : _defaultRemote;
            set => UpdateString(ref _defaultRemote, value, "origin");
        }

        public string DefaultPushBranch
        {
            get => string.IsNullOrWhiteSpace(_defaultPushBranch) ? "main" : _defaultPushBranch;
            set => UpdateString(ref _defaultPushBranch, value, "main");
        }

        public bool UseCustomPushTarget
        {
            get => _useCustomPushTarget;
            set
            {
                if (_useCustomPushTarget == value)
                {
                    return;
                }

                _useCustomPushTarget = value;
                SaveSettings();
            }
        }

        public string CustomPushTarget
        {
            get => _customPushTarget ?? string.Empty;
            set => UpdateString(ref _customPushTarget, value, string.Empty);
        }

        public void ResetToDefaults()
        {
            _defaultRemote = "origin";
            _defaultPushBranch = "main";
            _useCustomPushTarget = false;
            _customPushTarget = string.Empty;
            SaveSettings();
        }

        public void SaveSettings()
        {
            Save(true);
        }

        private void UpdateString(ref string field, string value, string fallback)
        {
            var sanitized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            if (field == sanitized)
            {
                return;
            }

            field = sanitized;
            SaveSettings();
        }
    }
}

