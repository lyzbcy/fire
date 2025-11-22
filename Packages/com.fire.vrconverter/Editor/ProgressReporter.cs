using System;
using UnityEditor;
using UnityEngine;

namespace OneClick.VRConverter.Editor
{
    /// <summary>
    /// 进度报告器，用于显示长时间操作的进度
    /// </summary>
    public class ProgressReporter : IDisposable
    {
        private readonly string _title;
        private readonly string _info;
        private float _currentProgress;
        private bool _disposed;

        public ProgressReporter(string title, string info = "")
        {
            _title = title;
            _info = info;
            _currentProgress = 0f;
            EditorUtility.DisplayProgressBar(_title, _info, 0f);
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="progress">进度值 (0.0 - 1.0)</param>
        /// <param name="info">进度信息</param>
        public void UpdateProgress(float progress, string info = null)
        {
            if (_disposed) return;
            
            _currentProgress = Mathf.Clamp01(progress);
            var displayInfo = info ?? _info;
            EditorUtility.DisplayProgressBar(_title, displayInfo, _currentProgress);
        }

        /// <summary>
        /// 更新进度信息（不改变进度值）
        /// </summary>
        public void UpdateInfo(string info)
        {
            if (_disposed) return;
            
            EditorUtility.DisplayProgressBar(_title, info, _currentProgress);
        }

        /// <summary>
        /// 完成进度
        /// </summary>
        public void Complete()
        {
            if (!_disposed)
            {
                EditorUtility.ClearProgressBar();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Complete();
        }
    }
}

