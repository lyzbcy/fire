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
            report.AppendLine("=== VR Converter 诊断报告 ===");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            // Unity 版本信息
            report.AppendLine("## Unity 版本信息");
            var versionInfo = UnityVersionChecker.GetCompatibilityInfo();
            report.AppendLine($"当前版本: {versionInfo.CurrentVersion}");
            report.AppendLine($"兼容性状态: {versionInfo.Status}");
            if (!string.IsNullOrEmpty(versionInfo.Recommendation))
            {
                report.AppendLine($"说明: {versionInfo.Recommendation}");
            }
            report.AppendLine();

            // 包信息
            report.AppendLine("## XR 包状态");
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
                report.AppendLine($"- {packageName}: {(isInstalled ? "✓ 已安装" : "✗ 未安装")}");
            }
            report.AppendLine();

            // 项目设置
            report.AppendLine("## 项目设置");
            report.AppendLine($"XR 管理已启用: {IsXrManagementEnabled()}");
            report.AppendLine($"OpenXR Loader 已启用: {IsOpenXrLoaderEnabled()}");
            report.AppendLine();

            // 场景信息
            report.AppendLine("## 当前场景信息");
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                report.AppendLine($"场景名称: {activeScene.name}");
                report.AppendLine($"场景路径: {activeScene.path}");
                report.AppendLine($"场景中是否有 XR Origin: {HasXrOriginInScene()}");
                report.AppendLine($"场景中是否有 Main Camera: {HasMainCameraInScene()}");
            }
            else
            {
                report.AppendLine("未加载场景");
            }
            report.AppendLine();

            // 系统信息
            report.AppendLine("## 系统信息");
            report.AppendLine($"操作系统: {SystemInfo.operatingSystem}");
            report.AppendLine($"处理器: {SystemInfo.processorType}");
            report.AppendLine($"内存: {SystemInfo.systemMemorySize} MB");
            report.AppendLine($"图形设备: {SystemInfo.graphicsDeviceName}");
            report.AppendLine($"图形 API: {SystemInfo.graphicsDeviceType}");
            report.AppendLine();

            // 日志信息
            report.AppendLine("## 日志信息");
            var logFile = Logger.GetCurrentLogFilePath();
            if (!string.IsNullOrEmpty(logFile) && File.Exists(logFile))
            {
                report.AppendLine($"日志文件路径: {logFile}");
                var logInfo = new FileInfo(logFile);
                report.AppendLine($"日志文件大小: {logInfo.Length / 1024.0:F2} KB");
                report.AppendLine($"最后修改时间: {logInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                report.AppendLine("日志文件不存在");
            }
            report.AppendLine();

            // 备份信息
            report.AppendLine("## 备份信息");
            var backups = OperationBackup.GetAvailableBackups();
            var backupCount = backups.Count;
            report.AppendLine($"备份数量: {backupCount}");
            if (backupCount > 0)
            {
                report.AppendLine("最近的备份:");
                foreach (var backup in backups.Take(5))
                {
                    report.AppendLine($"  - {backup.OperationName} ({backup.Timestamp:yyyy-MM-dd HH:mm:ss})");
                }
            }
            report.AppendLine();

            // 诊断建议
            report.AppendLine("## 诊断建议");
            var suggestions = GetDiagnosticSuggestions(versionInfo);
            if (suggestions.Count > 0)
            {
                foreach (var suggestion in suggestions)
                {
                    report.AppendLine($"- {suggestion}");
                }
            }
            else
            {
                report.AppendLine("未发现明显问题");
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
                suggestions.Add($"Unity 版本不兼容，建议使用 Unity {versionInfo.MinVersion} - {versionInfo.MaxVersion}");
            }
            else if (versionInfo.Status == CompatibilityStatus.Warning)
            {
                suggestions.Add("Unity 版本可能有兼容性问题，建议更新到最新版本");
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
                    suggestions.Add($"缺少必要的 XR 包: {packageName}，请执行第 1 步安装");
                }
            }

            if (!IsXrManagementEnabled())
            {
                suggestions.Add("XR Management 未启用，请执行第 2 步配置项目设置");
            }

            if (!IsOpenXrLoaderEnabled())
            {
                suggestions.Add("OpenXR Loader 未启用，请执行第 2 步配置项目设置");
            }

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.IsValid() && !HasXrOriginInScene())
            {
                suggestions.Add("当前场景中没有 XR Origin，请执行第 3 步转换场景");
            }

            return suggestions;
        }
    }
}

