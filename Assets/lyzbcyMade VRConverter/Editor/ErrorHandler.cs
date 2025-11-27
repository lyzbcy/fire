using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OneClick.VRConverter.Editor
{
    /// <summary>
    /// 统一的错误处理类，将技术性错误转换为用户友好的提示信息
    /// </summary>
    internal static class ErrorHandler
    {
        private static readonly Dictionary<Type, string> ErrorMessages = new Dictionary<Type, string>
        {
            { typeof(FileNotFoundException), "找不到必要的文件，请检查项目完整性" },
            { typeof(DirectoryNotFoundException), "找不到必要的文件夹，请检查项目结构" },
            { typeof(UnauthorizedAccessException), "没有文件访问权限，请检查文件是否被其他程序占用" },
            { typeof(IOException), "文件操作失败，可能是磁盘空间不足或文件被占用" },
            { typeof(ArgumentNullException), "参数错误，请检查输入是否有效" },
            { typeof(ArgumentException), "参数无效，请检查输入格式" },
            { typeof(NotSupportedException), "当前操作不受支持，可能是 Unity 版本不兼容" },
            { typeof(InvalidOperationException), "操作无效，可能是项目状态不正确" },
            { typeof(ReflectionTypeLoadException), "类型加载失败，可能是程序集缺失或版本不匹配" },
            { typeof(MissingMemberException), "缺少必要的组件或方法，可能是包版本不兼容" }
        };

        /// <summary>
        /// 处理异常并显示用户友好的错误提示
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="context">操作上下文描述</param>
        /// <param name="showDialog">是否显示对话框</param>
        /// <param name="logToFile">是否记录到日志文件</param>
        /// <returns>用户友好的错误消息</returns>
        public static string HandleException(Exception ex, string context = "", bool showDialog = false, bool logToFile = true)
        {
            var userMessage = TranslateException(ex, context);
            var technicalMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex.GetType().Name} - {ex.Message}\n堆栈跟踪:\n{ex.StackTrace}";

            // 记录到日志文件
            if (logToFile)
            {
                Logger.LogError(technicalMessage);
            }

            // 显示对话框
            if (showDialog)
            {
                EditorUtility.DisplayDialog("操作失败", userMessage, "确定");
            }

            return userMessage;
        }

        /// <summary>
        /// 将技术性异常转换为用户友好的消息
        /// </summary>
        private static string TranslateException(Exception ex, string context = "")
        {
            // 首先检查是否有预定义的错误消息
            if (ErrorMessages.TryGetValue(ex.GetType(), out var predefinedMessage))
            {
                return string.IsNullOrEmpty(context) 
                    ? predefinedMessage 
                    : $"{context}：{predefinedMessage}";
            }

            // 检查异常消息中是否包含关键字
            var message = ex.Message.ToLower();
            if (message.Contains("permission") || message.Contains("access"))
            {
                return string.IsNullOrEmpty(context)
                    ? "没有足够的权限执行此操作，请检查文件访问权限"
                    : $"{context}：没有足够的权限执行此操作";
            }

            if (message.Contains("not found") || message.Contains("找不到"))
            {
                return string.IsNullOrEmpty(context)
                    ? "找不到必要的资源，请检查项目完整性"
                    : $"{context}：找不到必要的资源";
            }

            if (message.Contains("version") || message.Contains("版本"))
            {
                return string.IsNullOrEmpty(context)
                    ? "版本不兼容，请检查 Unity 版本和包版本"
                    : $"{context}：版本不兼容";
            }

            // 默认返回通用错误消息
            return string.IsNullOrEmpty(context)
                ? $"发生错误：{ex.Message}"
                : $"{context}：{ex.Message}";
        }

        /// <summary>
        /// 验证操作是否安全，如果不安全则显示警告
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="warningMessage">警告消息</param>
        /// <returns>用户是否确认继续</returns>
        public static bool ConfirmDangerousOperation(string operationName, string warningMessage)
        {
            var fullMessage = $"⚠️ 警告：{operationName}\n\n{warningMessage}\n\n确定要继续吗？";
            return EditorUtility.DisplayDialog("确认操作", fullMessage, "继续", "取消");
        }

        /// <summary>
        /// 验证文件路径是否安全
        /// </summary>
        public static bool ValidatePath(string path, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "路径不能为空";
                return false;
            }

            // 检查路径遍历攻击
            if (path.Contains("..") || path.Contains("//") || path.Contains("\\\\"))
            {
                error = "路径包含非法字符";
                return false;
            }

            // 检查路径长度（Windows 限制）
            if (path.Length > 260)
            {
                error = "路径过长，可能在某些系统上无法访问";
                return false;
            }

            return true;
        }
    }
}

