using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace OneClick.VRConverter.Editor
{
    /// <summary>
    /// Unity 版本兼容性检查工具
    /// </summary>
    internal static class UnityVersionChecker
    {
        // 支持的 Unity 版本范围
        private static readonly Version MinSupportedVersion = new Version(2020, 3);
        private static readonly Version MaxSupportedVersion = new Version(2025, 1);

        /// <summary>
        /// 解析 Unity 版本字符串为 Version 对象
        /// 处理格式如 "2021.3.0f1", "2020.3.15f1", "2022.1.0a1" 等
        /// </summary>
        private static Version ParseUnityVersion(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
            {
                throw new ArgumentException("版本字符串不能为空", nameof(versionString));
            }

            // 移除 "-" 后面的部分（如 "2021.3.0f1-2021.3.0f1"）
            var versionPart = versionString.Split('-')[0];

            // 移除所有字母及其后面的内容（如 "f1", "a1", "b1" 等）
            // 使用正则表达式匹配第一个字母出现的位置，然后移除该字母及其后面的所有内容
            var match = Regex.Match(versionPart, @"^(\d+\.\d+(?:\.\d+)?)");
            if (match.Success)
            {
                versionPart = match.Groups[1].Value;
            }
            else
            {
                // 如果没有匹配到标准格式，尝试移除所有字母
                var sb = new StringBuilder();
                foreach (var c in versionPart)
                {
                    if (char.IsDigit(c) || c == '.')
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        break; // 遇到第一个非数字非点号字符就停止
                    }
                }
                versionPart = sb.ToString();
            }

            // 确保版本字符串至少包含主版本号和次版本号
            var parts = versionPart.Split('.');
            if (parts.Length < 2)
            {
                throw new FormatException($"无法解析 Unity 版本: {versionString}");
            }

            // 补全缺失的版本号部分（Version 构造函数需要至少两个部分）
            if (parts.Length == 2)
            {
                versionPart = $"{parts[0]}.{parts[1]}.0";
            }

            return new Version(versionPart);
        }

        /// <summary>
        /// 检查当前 Unity 版本是否兼容
        /// </summary>
        /// <param name="showWarning">是否显示警告对话框</param>
        /// <returns>是否兼容</returns>
        public static bool CheckCompatibility(bool showWarning = true)
        {
            try
            {
                var currentVersion = ParseUnityVersion(Application.unityVersion);
                var isCompatible = currentVersion >= MinSupportedVersion && currentVersion <= MaxSupportedVersion;

                if (!isCompatible && showWarning)
                {
                    var message = $"当前 Unity 版本 {Application.unityVersion} 可能不完全兼容。\n\n" +
                                  $"推荐版本范围：{MinSupportedVersion} - {MaxSupportedVersion}\n\n" +
                                  "某些功能可能无法正常工作。是否继续？";
                    
                    if (!EditorUtility.DisplayDialog("版本兼容性警告", message, "继续", "取消"))
                    {
                        return false;
                    }
                }

                Logger.LogInfo($"Unity 版本检查: {Application.unityVersion} (兼容性: {(isCompatible ? "是" : "警告")})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Unity 版本检查失败");
                if (showWarning)
                {
                    EditorUtility.DisplayDialog("版本检查失败", 
                        "无法检查 Unity 版本，但可以继续使用。如果遇到问题，请检查 Unity 版本。", "确定");
                }
                return true; // 检查失败时允许继续，避免阻塞用户
            }
        }

        /// <summary>
        /// 获取版本兼容性信息
        /// </summary>
        public static VersionCompatibilityInfo GetCompatibilityInfo()
        {
            try
            {
                var currentVersion = ParseUnityVersion(Application.unityVersion);
                var isCompatible = currentVersion >= MinSupportedVersion && currentVersion <= MaxSupportedVersion;
                var status = isCompatible ? CompatibilityStatus.Supported : CompatibilityStatus.Warning;

                if (currentVersion < MinSupportedVersion)
                {
                    status = CompatibilityStatus.Unsupported;
                }

                return new VersionCompatibilityInfo
                {
                    CurrentVersion = Application.unityVersion,
                    IsCompatible = isCompatible,
                    Status = status,
                    MinVersion = MinSupportedVersion.ToString(),
                    MaxVersion = MaxSupportedVersion.ToString(),
                    Recommendation = GetRecommendation(status)
                };
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "获取版本兼容性信息失败");
                return new VersionCompatibilityInfo
                {
                    CurrentVersion = Application.unityVersion,
                    IsCompatible = true, // 默认允许继续
                    Status = CompatibilityStatus.Unknown,
                    Recommendation = "无法确定版本兼容性，建议使用 Unity 2020.3 或更高版本"
                };
            }
        }

        private static string GetRecommendation(CompatibilityStatus status)
        {
            return status switch
            {
                CompatibilityStatus.Supported => "当前版本完全支持所有功能",
                CompatibilityStatus.Warning => "当前版本可能部分功能受限，建议升级到推荐版本",
                CompatibilityStatus.Unsupported => "当前版本不受支持，强烈建议升级到 Unity 2020.3 或更高版本",
                _ => "无法确定版本兼容性"
            };
        }

        /// <summary>
        /// 检查特定功能是否在当前版本可用
        /// </summary>
        public static bool IsFeatureAvailable(string featureName)
        {
            try
            {
                var currentVersion = ParseUnityVersion(Application.unityVersion);
                
                // 根据功能名称检查版本要求
                return featureName switch
                {
                    "XR Management" => currentVersion >= new Version(2020, 3),
                    "OpenXR" => currentVersion >= new Version(2020, 3),
                    "XR Interaction Toolkit" => currentVersion >= new Version(2020, 3),
                    "Input System" => currentVersion >= new Version(2019, 1),
                    _ => true
                };
            }
            catch
            {
                return true; // 检查失败时默认允许
            }
        }
    }

    /// <summary>
    /// 版本兼容性状态
    /// </summary>
    internal enum CompatibilityStatus
    {
        Supported,      // 完全支持
        Warning,        // 警告（可能部分功能受限）
        Unsupported,    // 不支持
        Unknown         // 未知
    }

    /// <summary>
    /// 版本兼容性信息
    /// </summary>
    internal struct VersionCompatibilityInfo
    {
        public string CurrentVersion;
        public bool IsCompatible;
        public CompatibilityStatus Status;
        public string MinVersion;
        public string MaxVersion;
        public string Recommendation;
    }
}

