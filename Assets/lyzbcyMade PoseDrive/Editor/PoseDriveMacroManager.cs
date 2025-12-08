using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PoseDrive.Editor
{
    /// <summary>
    /// 自动管理 UNITY_SENTIS 宏定义
    /// </summary>
    [InitializeOnLoad]
    public static class PoseDriveMacroManager
    {
        private const string SentisPackageName = "com.unity.sentis";
        private const string MacroName = "UNITY_SENTIS";

        static PoseDriveMacroManager()
        {
            // Unity 启动时自动检查
            EditorApplication.delayCall += CheckAndUpdateMacro;
        }

        /// <summary>
        /// 检查并更新宏定义
        /// </summary>
        [MenuItem("Tools/PoseDrive/检查并更新 Sentis 宏", priority = 300)]
        public static void CheckAndUpdateMacro()
        {
            bool isSentisInstalled = IsSentisInstalled();
            bool hasMacro = HasMacro(MacroName);

            PoseDriveLogger.LogInfo($"Sentis 包检测: 已安装={isSentisInstalled}, 宏定义={hasMacro}");

            if (isSentisInstalled && !hasMacro)
            {
                AddMacro(MacroName);
                PoseDriveLogger.LogInfo($"已自动添加 {MacroName} 宏定义");
                EditorUtility.DisplayDialog("成功", $"已自动添加 {MacroName} 宏定义。\n\nUnity 将重新编译脚本，请稍候。", "确定");
            }
            else if (!isSentisInstalled && hasMacro)
            {
                // 可选：如果包被移除，是否移除宏
                // RemoveMacro(MacroName);
                PoseDriveLogger.LogWarning($"Sentis 包未安装，但 {MacroName} 宏仍存在");
            }
            else if (isSentisInstalled && hasMacro)
            {
                PoseDriveLogger.LogInfo($"{MacroName} 宏已正确配置");
            }
            else
            {
                PoseDriveLogger.LogWarning($"Sentis 包未安装，{MacroName} 宏未定义");
            }
        }

        /// <summary>
        /// 检查 Sentis 包是否已安装
        /// </summary>
        private static bool IsSentisInstalled()
        {
            try
            {
                // 方法1: 检查包是否在 manifest.json 中
                string manifestPath = "Packages/manifest.json";
                if (System.IO.File.Exists(manifestPath))
                {
                    string content = System.IO.File.ReadAllText(manifestPath);
                    if (content.Contains(SentisPackageName))
                    {
                        return true;
                    }
                }

                // 方法2: 尝试通过 PackageManager API 检查
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{SentisPackageName}");
                if (packageInfo != null)
                {
                    return true;
                }

                // 方法3: 尝试加载 Sentis 类型
                var sentisType = System.Type.GetType("Unity.Sentis.TensorFloat, Unity.Sentis");
                if (sentisType != null)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查是否已定义指定宏
        /// </summary>
        private static bool HasMacro(string macro)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            return defines.Split(';').Contains(macro);
        }

        /// <summary>
        /// 添加宏定义
        /// </summary>
        private static void AddMacro(string macro)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            
            if (!defines.Split(';').Contains(macro))
            {
                if (string.IsNullOrEmpty(defines))
                {
                    defines = macro;
                }
                else
                {
                    defines += ";" + macro;
                }
                
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 移除宏定义
        /// </summary>
        private static void RemoveMacro(string macro)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            
            var defineList = defines.Split(';').Where(d => d != macro).ToArray();
            string newDefines = string.Join(";", defineList);
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newDefines);
            AssetDatabase.Refresh();
        }
    }
}

