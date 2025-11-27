using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    internal static class FocusBackgroundImporter
    {
        private const int MaxImportsPerCycle = 4;
        private const double MinIntervalSeconds = 0.75d;

        private static readonly List<string> Buffer = new();
        private static double _nextAllowedTime;

        internal static void ProcessIncrementalImports(bool featureEnabled)
        {
            if (!featureEnabled)
            {
                ResetIdleTimer();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < _nextAllowedTime)
            {
                return;
            }

            _nextAllowedTime = now + MinIntervalSeconds;

            Buffer.Clear();
            int acquired = FocusChangeTracker.TryConsumeChanges(Buffer, MaxImportsPerCycle);
            if (acquired == 0)
            {
                return;
            }

            foreach (var path in Buffer)
            {
                string assetPath = ToAssetRelativePath(path);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                try
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[焦点卡顿优化助手] 后台导入 {assetPath} 失败：{ex.Message}");
                }
            }

            Buffer.Clear();
        }

        internal static void ResetIdleTimer()
        {
            _nextAllowedTime = EditorApplication.timeSinceStartup + MinIntervalSeconds;
            Buffer.Clear();
        }

        private static string ToAssetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            string projectRoot = Directory.GetCurrentDirectory();
            if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string relative = fullPath.Substring(projectRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('\\', '/');

            if (string.IsNullOrEmpty(relative))
            {
                return null;
            }

            if (!relative.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                !relative.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return relative;
        }
    }
}

