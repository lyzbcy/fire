using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FireTools.FocusOptimizer
{
    [InitializeOnLoad]
    internal static class FocusChangeTracker
    {
        private static readonly string[] WatchDirectories =
        {
            "Assets",
            "Packages",
            "ProjectSettings"
        };

        private static readonly List<FileSystemWatcher> Watchers = new();
        private static readonly HashSet<string> ChangedPaths = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object Locker = new();

        private static bool _tracking;
        private static bool _initialized;

        static FocusChangeTracker()
        {
            AssemblyReloadEvents.beforeAssemblyReload += DisposeWatchers;
            EditorApplication.quitting += DisposeWatchers;
        }

        internal static void StartTrackingIfEnabled(bool enabled)
        {
            if (!enabled)
            {
                StopTracking();
                return;
            }

            EnsureWatchers();
            if (Watchers.Count == 0)
            {
                return;
            }

            lock (Locker)
            {
                ChangedPaths.Clear();
            }

            foreach (var watcher in Watchers)
            {
                try
                {
                    watcher.EnableRaisingEvents = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[焦点卡顿优化助手] 无法启动文件监控：{ex.Message}");
                }
            }

            _tracking = true;
        }

        internal static int StopTrackingAndGetChangeCount()
        {
            StopTracking();

            lock (Locker)
            {
                int count = ChangedPaths.Count;
                ChangedPaths.Clear();
                return count;
            }
        }

        private static void StopTracking()
        {
            if (!_tracking)
            {
                return;
            }

            foreach (var watcher in Watchers)
            {
                watcher.EnableRaisingEvents = false;
            }

            _tracking = false;
        }

        private static void EnsureWatchers()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            string projectRoot = Directory.GetCurrentDirectory();

            foreach (var folder in WatchDirectories)
            {
                string fullPath = Path.Combine(projectRoot, folder);
                if (!Directory.Exists(fullPath))
                {
                    continue;
                }

                try
                {
                    var watcher = new FileSystemWatcher(fullPath)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName |
                                       NotifyFilters.DirectoryName |
                                       NotifyFilters.LastWrite |
                                       NotifyFilters.Size
                    };
                    watcher.Changed += OnChanged;
                    watcher.Created += OnChanged;
                    watcher.Deleted += OnChanged;
                    watcher.Renamed += OnRenamed;
                    watcher.Error += OnError;
                    watcher.EnableRaisingEvents = false;
                    Watchers.Add(watcher);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[焦点卡顿优化助手] 创建文件监控失败（{fullPath}）：{ex.Message}");
                }
            }
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (!_tracking) return;
            if (string.IsNullOrEmpty(e.FullPath)) return;

            lock (Locker)
            {
                if (ChangedPaths.Count < 5000)
                {
                    ChangedPaths.Add(e.FullPath);
                }
            }
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            OnChanged(sender, e);
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            Debug.LogWarning($"[焦点卡顿优化助手] 文件监控缓冲区溢出或错误：{e.GetException()?.Message}");
        }

        private static void DisposeWatchers()
        {
            StopTracking();

            foreach (var watcher in Watchers)
            {
                watcher.Dispose();
            }

            Watchers.Clear();
            _initialized = false;
        }
    }
}



