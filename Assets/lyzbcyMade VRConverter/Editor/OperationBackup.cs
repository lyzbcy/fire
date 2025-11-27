using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OneClick.VRConverter.Editor
{
    /// <summary>
    /// 操作备份和回滚系统
    /// </summary>
    internal static class OperationBackup
    {
        private const string BackupFolderName = "VRConverterBackups";
        private static string _backupRootDirectory;
        private static readonly Dictionary<string, BackupInfo> _backupRegistry = new Dictionary<string, BackupInfo>();

        static OperationBackup()
        {
            Initialize();
        }

        private static void Initialize()
        {
            try
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _backupRootDirectory = Path.Combine(projectRoot, BackupFolderName);
                
                if (!Directory.Exists(_backupRootDirectory))
                {
                    Directory.CreateDirectory(_backupRootDirectory);
                }

                // 清理旧备份（保留最近 10 个）
                CleanOldBackups(10);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "初始化备份系统失败");
            }
        }

        /// <summary>
        /// 创建操作备份
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="filesToBackup">要备份的文件路径列表（相对于项目根目录）</param>
        /// <returns>备份 ID，用于后续恢复</returns>
        public static string CreateBackup(string operationName, IEnumerable<string> filesToBackup)
        {
            try
            {
                var backupId = $"{operationName}_{DateTime.Now:yyyyMMdd_HHmmss}";
                var backupDirectory = Path.Combine(_backupRootDirectory, backupId);

                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var backedUpFiles = new List<string>();

                foreach (var relativePath in filesToBackup)
                {
                    var sourcePath = Path.Combine(projectRoot, relativePath);
                    if (File.Exists(sourcePath))
                    {
                        var fileName = Path.GetFileName(relativePath);
                        var directoryName = Path.GetDirectoryName(relativePath)?.Replace("\\", "_").Replace("/", "_") ?? "";
                        var backupFileName = string.IsNullOrEmpty(directoryName) 
                            ? fileName 
                            : $"{directoryName}_{fileName}";
                        var backupPath = Path.Combine(backupDirectory, backupFileName);

                        // 确保目标目录存在
                        var backupFileDir = Path.GetDirectoryName(backupPath);
                        if (!string.IsNullOrEmpty(backupFileDir) && !Directory.Exists(backupFileDir))
                        {
                            Directory.CreateDirectory(backupFileDir);
                        }

                        File.Copy(sourcePath, backupPath, true);
                        backedUpFiles.Add(relativePath);
                        Logger.LogInfo($"已备份文件: {relativePath} -> {backupFileName}");
                    }
                }

                var backupInfo = new BackupInfo
                {
                    Id = backupId,
                    OperationName = operationName,
                    Timestamp = DateTime.Now,
                    Files = backedUpFiles,
                    BackupDirectory = backupDirectory
                };

                _backupRegistry[backupId] = backupInfo;
                SaveBackupRegistry();

                Logger.LogInfo($"创建备份成功: {backupId} ({backedUpFiles.Count} 个文件)");
                return backupId;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"创建备份失败: {operationName}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 恢复备份
        /// </summary>
        /// <param name="backupId">备份 ID</param>
        /// <returns>是否恢复成功</returns>
        public static bool RestoreBackup(string backupId)
        {
            try
            {
                if (!_backupRegistry.TryGetValue(backupId, out var backupInfo))
                {
                    Logger.LogError($"找不到备份: {backupId}");
                    return false;
                }

                if (!Directory.Exists(backupInfo.BackupDirectory))
                {
                    Logger.LogError($"备份目录不存在: {backupInfo.BackupDirectory}");
                    return false;
                }

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var restoredCount = 0;

                foreach (var relativePath in backupInfo.Files)
                {
                    var fileName = Path.GetFileName(relativePath);
                    var directoryName = Path.GetDirectoryName(relativePath)?.Replace("\\", "_").Replace("/", "_") ?? "";
                    var backupFileName = string.IsNullOrEmpty(directoryName)
                        ? fileName
                        : $"{directoryName}_{fileName}";
                    var backupPath = Path.Combine(backupInfo.BackupDirectory, backupFileName);

                    if (File.Exists(backupPath))
                    {
                        var targetPath = Path.Combine(projectRoot, relativePath);
                        var targetDir = Path.GetDirectoryName(targetPath);
                        
                        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }

                        File.Copy(backupPath, targetPath, true);
                        restoredCount++;
                        Logger.LogInfo($"已恢复文件: {relativePath}");
                    }
                }

                AssetDatabase.Refresh();
                Logger.LogInfo($"恢复备份成功: {backupId} ({restoredCount} 个文件)");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"恢复备份失败: {backupId}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有可用的备份
        /// </summary>
        public static IReadOnlyList<BackupInfo> GetAvailableBackups()
        {
            LoadBackupRegistry();
            return _backupRegistry.Values.OrderByDescending(b => b.Timestamp).ToList();
        }

        /// <summary>
        /// 删除备份
        /// </summary>
        public static bool DeleteBackup(string backupId)
        {
            try
            {
                if (!_backupRegistry.TryGetValue(backupId, out var backupInfo))
                {
                    return false;
                }

                if (Directory.Exists(backupInfo.BackupDirectory))
                {
                    Directory.Delete(backupInfo.BackupDirectory, true);
                }

                _backupRegistry.Remove(backupId);
                SaveBackupRegistry();
                Logger.LogInfo($"已删除备份: {backupId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"删除备份失败: {backupId}");
                return false;
            }
        }

        /// <summary>
        /// 创建关键文件的快速备份（用于 manifest.json 等）
        /// </summary>
        public static string CreateQuickBackup(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return string.Empty;
                }

                var backupPath = $"{filePath}.backup";
                File.Copy(filePath, backupPath, true);
                Logger.LogInfo($"快速备份: {filePath} -> {backupPath}");
                return backupPath;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"快速备份失败: {filePath}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 恢复快速备份
        /// </summary>
        public static bool RestoreQuickBackup(string originalPath)
        {
            try
            {
                var backupPath = $"{originalPath}.backup";
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, originalPath, true);
                    Logger.LogInfo($"恢复快速备份: {backupPath} -> {originalPath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"恢复快速备份失败: {originalPath}");
                return false;
            }
        }

        private static void SaveBackupRegistry()
        {
            try
            {
                var registryPath = Path.Combine(_backupRootDirectory, "backup_registry.json");
                var json = JsonUtility.ToJson(new BackupRegistry { Backups = _backupRegistry.Values.ToList() }, true);
                File.WriteAllText(registryPath, json);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "保存备份注册表失败");
            }
        }

        private static void LoadBackupRegistry()
        {
            try
            {
                var registryPath = Path.Combine(_backupRootDirectory, "backup_registry.json");
                if (File.Exists(registryPath))
                {
                    var json = File.ReadAllText(registryPath);
                    var registry = JsonUtility.FromJson<BackupRegistry>(json);
                    _backupRegistry.Clear();
                    foreach (var backup in registry.Backups)
                    {
                        _backupRegistry[backup.Id] = backup;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "加载备份注册表失败");
            }
        }

        private static void CleanOldBackups(int keepCount)
        {
            try
            {
                LoadBackupRegistry();
                var backups = _backupRegistry.Values.OrderByDescending(b => b.Timestamp).ToList();
                
                if (backups.Count > keepCount)
                {
                    var toDelete = backups.Skip(keepCount).ToList();
                    foreach (var backup in toDelete)
                    {
                        DeleteBackup(backup.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "清理旧备份失败");
            }
        }

        /// <summary>
        /// 获取备份目录路径
        /// </summary>
        public static string GetBackupDirectory()
        {
            return _backupRootDirectory ?? string.Empty;
        }
    }

    /// <summary>
    /// 备份信息
    /// </summary>
    [Serializable]
    internal class BackupInfo
    {
        public string Id;
        public string OperationName;
        public string TimestampString;
        public List<string> Files;
        public string BackupDirectory;

        public DateTime Timestamp
        {
            get => DateTime.TryParse(TimestampString, out var dt) ? dt : DateTime.MinValue;
            set => TimestampString = value.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    [Serializable]
    internal class BackupRegistry
    {
        public List<BackupInfo> Backups = new List<BackupInfo>();
    }
}

