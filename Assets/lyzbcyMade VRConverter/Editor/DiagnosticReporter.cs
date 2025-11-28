using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OneClick.VRConverter.Editor
{
    /// <summary>
    /// 诊断报告生成器，用于生成项目诊断报告
    /// </summary>
    public static class DiagnosticReporter
    {
        /// <summary>
        /// 生成诊断报告
        /// </summary>
        public static string GenerateReport()
        {
            var report = new StringBuilder();
            report.AppendLine(Localization.Get("Diagnostic.Report.Title"));
            report.AppendLine(Localization.Get("Diagnostic.Report.GeneratedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            report.AppendLine();

            // Unity 版本信息
            report.AppendLine(Localization.Get("Diagnostic.Report.UnityVersion"));
            var versionInfo = UnityVersionChecker.GetCompatibilityInfo();
            report.AppendLine(Localization.Get("Diagnostic.Report.CurrentVersion", versionInfo.CurrentVersion));
            report.AppendLine(Localization.Get("Diagnostic.Report.CompatibilityStatus", versionInfo.Status.ToString()));
            if (!string.IsNullOrEmpty(versionInfo.Recommendation))
            {
                report.AppendLine(Localization.Get("Diagnostic.Report.Description", versionInfo.Recommendation));
            }
            report.AppendLine();

            // 包信息
            report.AppendLine(Localization.Get("Diagnostic.Report.XRPackageStatus"));
            var requiredPackages = new[]
            {
                "com.unity.xr.management",
                "com.unity.xr.openxr",
                "com.unity.xr.interaction.toolkit",
                "com.unity.inputsystem"
            };

            foreach (var packageName in requiredPackages)
            {
                var isInstalled = IsPackageInstalled(packageName);
                report.AppendLine(Localization.Get("Diagnostic.Report.PackageStatus", 
                    packageName, 
                    isInstalled ? Localization.Get("Diagnostic.Report.Installed") : Localization.Get("Diagnostic.Report.NotInstalled")));
            }
            report.AppendLine();

            // 项目设置
            report.AppendLine(Localization.Get("Diagnostic.Report.ProjectSettings"));
            report.AppendLine(Localization.Get("Diagnostic.Report.XRManagementEnabled", IsXrManagementEnabled().ToString()));
            report.AppendLine(Localization.Get("Diagnostic.Report.OpenXREnabled", IsOpenXrLoaderEnabled().ToString()));
            report.AppendLine();

            // 场景信息
            report.AppendLine(Localization.Get("Diagnostic.Report.SceneInfo"));
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                report.AppendLine(Localization.Get("Diagnostic.Report.SceneName", activeScene.name));
                report.AppendLine(Localization.Get("Diagnostic.Report.ScenePath", activeScene.path));
                report.AppendLine(Localization.Get("Diagnostic.Report.HasXROrigin", HasXrOriginInScene().ToString()));
                report.AppendLine(Localization.Get("Diagnostic.Report.HasMainCamera", HasMainCameraInScene().ToString()));
            }
            else
            {
                report.AppendLine(Localization.Get("Diagnostic.Report.NoSceneLoaded"));
            }
            report.AppendLine();

            // 系统信息
            report.AppendLine(Localization.Get("Diagnostic.Report.SystemInfo"));
            report.AppendLine(Localization.Get("Diagnostic.Report.OS", SystemInfo.operatingSystem));
            report.AppendLine(Localization.Get("Diagnostic.Report.Processor", SystemInfo.processorType));
            report.AppendLine(Localization.Get("Diagnostic.Report.Memory", SystemInfo.systemMemorySize));
            report.AppendLine(Localization.Get("Diagnostic.Report.GraphicsDevice", SystemInfo.graphicsDeviceName));
            report.AppendLine(Localization.Get("Diagnostic.Report.GraphicsAPI", SystemInfo.graphicsDeviceType.ToString()));
            report.AppendLine();

            // 日志信息
            report.AppendLine(Localization.Get("Diagnostic.Report.LogInfo"));
            var logFile = Logger.GetCurrentLogFilePath();
            if (!string.IsNullOrEmpty(logFile) && File.Exists(logFile))
            {
                report.AppendLine(Localization.Get("Diagnostic.Report.LogFilePath", logFile));
                var logInfo = new FileInfo(logFile);
                report.AppendLine(Localization.Get("Diagnostic.Report.LogFileSize", (logInfo.Length / 1024.0).ToString("F2")));
                report.AppendLine(Localization.Get("Diagnostic.Report.LogLastModified", logInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            else
            {
                report.AppendLine(Localization.Get("Diagnostic.Report.LogFileNotExists"));
            }
            report.AppendLine();

            // 备份信息
            report.AppendLine(Localization.Get("Diagnostic.Report.BackupInfo"));
            var backups = OperationBackup.GetAvailableBackups();
            var backupCount = backups.Count;
            report.AppendLine(Localization.Get("Diagnostic.Report.BackupCount", backupCount));
            if (backupCount > 0)
            {
                report.AppendLine(Localization.Get("Diagnostic.Report.RecentBackups"));
                foreach (var backup in backups.Take(5))
                {
                    report.AppendLine(Localization.Get("Diagnostic.Report.BackupItem", 
                        backup.OperationName, 
                        backup.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")));
                }
            }
            report.AppendLine();

            // 诊断建议
            report.AppendLine(Localization.Get("Diagnostic.Report.Suggestions"));
            var suggestions = GetDiagnosticSuggestions(versionInfo);
            if (suggestions.Count > 0)
            {
                foreach (var suggestion in suggestions)
                {
                    report.AppendLine(Localization.Get("Diagnostic.Report.SuggestionItem", suggestion));
                }
            }
            else
            {
                report.AppendLine(Localization.Get("Diagnostic.Report.NoIssues"));
            }

            return report.ToString();
        }

        /// <summary>
        /// 导出诊断报告到文件
        /// </summary>
        public static bool ExportReport(string filePath, out string error)
        {
            try
            {
                var report = GenerateReport();
                File.WriteAllText(filePath, report, Encoding.UTF8);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsPackageInstalled(string packageName)
        {
            try
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{packageName}");
                return packageInfo != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsXrManagementEnabled()
        {
#if UNITY_XR_MANAGEMENT
            try
            {
                var perBuildTargetType = Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
                if (perBuildTargetType == null)
                {
                    return false;
                }

                var method = perBuildTargetType.GetMethod("XRGeneralSettingsForBuildTarget", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method == null)
                {
                    return false;
                }

                var generalSettings = method.Invoke(null, new object[] { BuildTargetGroup.Standalone });
                if (generalSettings == null)
                {
                    return false;
                }

                var managerProp = generalSettings.GetType().GetProperty("Manager");
                if (managerProp == null)
                {
                    return false;
                }

                var manager = managerProp.GetValue(generalSettings);
                return manager != null;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private static bool IsOpenXrLoaderEnabled()
        {
#if UNITY_XR_MANAGEMENT
            try
            {
                var perBuildTargetType = Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
                if (perBuildTargetType == null)
                {
                    return false;
                }

                var method = perBuildTargetType.GetMethod("XRGeneralSettingsForBuildTarget", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method == null)
                {
                    return false;
                }

                var generalSettings = method.Invoke(null, new object[] { BuildTargetGroup.Standalone });
                if (generalSettings == null)
                {
                    return false;
                }

                var managerProp = generalSettings.GetType().GetProperty("Manager");
                if (managerProp == null)
                {
                    return false;
                }

                var manager = managerProp.GetValue(generalSettings);
                if (manager == null)
                {
                    return false;
                }

                var activeLoadersProp = manager.GetType().GetProperty("activeLoaders");
                if (activeLoadersProp == null)
                {
                    return false;
                }

                var loaders = activeLoadersProp.GetValue(manager) as System.Collections.IEnumerable;
                if (loaders == null)
                {
                    return false;
                }

                foreach (var loader in loaders)
                {
                    if (loader != null && loader.GetType().Name.Contains("OpenXR"))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private static bool HasXrOriginInScene()
        {
#if UNITY_XR_INTERACTION_TOOLKIT || UNITY_XR_CORE_UTILS
            try
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (!activeScene.IsValid()) return false;

                var xrOriginType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
                if (xrOriginType == null)
                {
                    // 尝试其他可能的命名空间
                    xrOriginType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XROrigin, UnityEngine.XR.Interaction.Toolkit");
                }

                if (xrOriginType == null)
                {
                    return false;
                }

                var rootObjects = activeScene.GetRootGameObjects();
                foreach (var obj in rootObjects)
                {
                    var component = obj.GetComponentInChildren(xrOriginType);
                    if (component != null)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private static bool HasMainCameraInScene()
        {
            try
            {
                return Camera.main != null;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> GetDiagnosticSuggestions(VersionCompatibilityInfo versionInfo)
        {
            var suggestions = new List<string>();

            if (versionInfo.Status == CompatibilityStatus.Unsupported)
            {
                suggestions.Add(Localization.Get("Diagnostic.Suggestion.UnsupportedVersion", 
                    versionInfo.MinVersion, versionInfo.MaxVersion));
            }
            else if (versionInfo.Status == CompatibilityStatus.Warning)
            {
                suggestions.Add(Localization.Get("Diagnostic.Suggestion.VersionWarning"));
            }

            var requiredPackages = new[]
            {
                "com.unity.xr.management",
                "com.unity.xr.openxr",
                "com.unity.xr.interaction.toolkit",
                "com.unity.inputsystem"
            };

            foreach (var packageName in requiredPackages)
            {
                if (!IsPackageInstalled(packageName))
                {
                    suggestions.Add(Localization.Get("Diagnostic.Suggestion.MissingPackage", packageName));
                }
            }

            if (!IsXrManagementEnabled())
            {
                suggestions.Add(Localization.Get("Diagnostic.Suggestion.XRManagementNotEnabled"));
            }

            if (!IsOpenXrLoaderEnabled())
            {
                suggestions.Add(Localization.Get("Diagnostic.Suggestion.OpenXRNotEnabled"));
            }

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.IsValid() && !HasXrOriginInScene())
            {
                suggestions.Add(Localization.Get("Diagnostic.Suggestion.NoXROrigin"));
            }

            return suggestions;
        }
    }
}

