using System;
using System.IO;
using UnityEngine;

namespace PoseDrive.Editor
{
    /// <summary>
    /// PoseDrive 日志系统，支持将日志写入文件并分级管理
    /// </summary>
    internal static class PoseDriveLogger
    {
        private const string LogFolderName = "Logs";
        private const string PluginName = "PoseDrive";
        private static string _logDirectory;
        private static string _currentLogFile;
        private static readonly object _lockObject = new object();

        static PoseDriveLogger()
        {
            Initialize();
        }

        private static void Initialize()
        {
            try
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _logDirectory = Path.Combine(projectRoot, LogFolderName, PluginName);
                
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // 创建当前会话的日志文件
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _currentLogFile = Path.Combine(_logDirectory, $"log_{timestamp}.txt");

                // 写入日志文件头
                WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ========== PoseDrive 日志开始 ==========");
                WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Unity 版本: {Application.unityVersion}");
                WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 项目路径: {projectRoot}");
                WriteToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ==========================================");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PoseDrive Logger] 初始化日志系统失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void LogInfo(string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}";
            WriteToFile(logMessage);
            Debug.Log($"[PoseDrive] {message}");
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void LogWarning(string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARNING] {message}";
            WriteToFile(logMessage);
            Debug.LogWarning($"[PoseDrive] {message}");
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void LogError(string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}";
            WriteToFile(logMessage);
            Debug.LogError($"[PoseDrive] {message}");
        }

        /// <summary>
        /// 记录异常日志
        /// </summary>
        public static void LogException(Exception ex, string context = "")
        {
            var message = string.IsNullOrEmpty(context)
                ? $"异常: {ex.GetType().Name} - {ex.Message}\n堆栈跟踪:\n{ex.StackTrace}"
                : $"{context}\n异常: {ex.GetType().Name} - {ex.Message}\n堆栈跟踪:\n{ex.StackTrace}";
            
            LogError(message);
        }

        /// <summary>
        /// 记录调试日志（仅写入文件，不输出到 Unity 控制台）
        /// </summary>
        public static void LogDebug(string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] {message}";
            WriteToFile(logMessage);
        }

        /// <summary>
        /// 写入文件（线程安全）
        /// </summary>
        private static void WriteToFile(string message)
        {
            lock (_lockObject)
            {
                try
                {
                    if (string.IsNullOrEmpty(_currentLogFile))
                    {
                        Initialize();
                    }

                    if (!string.IsNullOrEmpty(_currentLogFile))
                    {
                        File.AppendAllText(_currentLogFile, message + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    // 如果写入文件失败，至少输出到 Unity 控制台
                    Debug.LogError($"[PoseDrive Logger] 写入日志文件失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取当前日志文件路径
        /// </summary>
        public static string GetCurrentLogFilePath()
        {
            return _currentLogFile ?? string.Empty;
        }

        /// <summary>
        /// 获取日志目录路径
        /// </summary>
        public static string GetLogDirectory()
        {
            return _logDirectory ?? string.Empty;
        }

        /// <summary>
        /// 清理旧日志文件（保留最近 N 天的日志）
        /// </summary>
        public static void CleanOldLogs(int daysToKeep = 7)
        {
            try
            {
                if (string.IsNullOrEmpty(_logDirectory) || !Directory.Exists(_logDirectory))
                {
                    return;
                }

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(_logDirectory, "log_*.txt");

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                        LogInfo($"已删除旧日志文件: {Path.GetFileName(logFile)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PoseDrive Logger] 清理旧日志失败: {ex.Message}");
            }
        }
    }
}

