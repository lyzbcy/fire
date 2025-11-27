using UnityEditor;
using UnityEngine;

namespace Fire.VersionControlAssistant
{
    /// <summary>
    /// 版本控制助手偏好设置管理器
    /// </summary>
    internal sealed class VersionControlAssistantPreferences
    {
        private const string PrefsKeyPrefix = "Fire.VersionControlAssistant.";
        private const string KeyDefaultRemote = PrefsKeyPrefix + "DefaultRemote";
        private const string KeyDefaultPushBranch = PrefsKeyPrefix + "DefaultPushBranch";
        private const string KeyUseCustomPushTarget = PrefsKeyPrefix + "UseCustomPushTarget";
        private const string KeyCustomPushTarget = PrefsKeyPrefix + "CustomPushTarget";

        private static VersionControlAssistantPreferences _instance;

        /// <summary>
        /// 获取偏好设置单例实例
        /// </summary>
        public static VersionControlAssistantPreferences Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VersionControlAssistantPreferences();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 默认远程仓库名称
        /// </summary>
        public string DefaultRemote
        {
            get => EditorPrefs.GetString(KeyDefaultRemote, "origin");
            set => EditorPrefs.SetString(KeyDefaultRemote, value);
        }

        /// <summary>
        /// 默认推送分支名称
        /// </summary>
        public string DefaultPushBranch
        {
            get => EditorPrefs.GetString(KeyDefaultPushBranch, "main");
            set => EditorPrefs.SetString(KeyDefaultPushBranch, value);
        }

        /// <summary>
        /// 是否使用自定义推送目标
        /// </summary>
        public bool UseCustomPushTarget
        {
            get => EditorPrefs.GetBool(KeyUseCustomPushTarget, false);
            set => EditorPrefs.SetBool(KeyUseCustomPushTarget, value);
        }

        /// <summary>
        /// 自定义推送目标
        /// </summary>
        public string CustomPushTarget
        {
            get => EditorPrefs.GetString(KeyCustomPushTarget, string.Empty);
            set => EditorPrefs.SetString(KeyCustomPushTarget, value);
        }

        private VersionControlAssistantPreferences()
        {
        }
    }
}










