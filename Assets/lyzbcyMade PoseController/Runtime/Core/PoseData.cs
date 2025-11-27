using System;
using UnityEngine;

namespace PoseController.Runtime.Core
{
    /// <summary>
    /// 表示单个关键点的归一化坐标与置信度。
    /// </summary>
    [Serializable]
    public struct PoseKeypoint
    {
        /// <summary>关键点在纹理宽方向的归一化坐标。</summary>
        public float X;

        /// <summary>关键点在纹理高方向的归一化坐标。</summary>
        public float Y;

        /// <summary>关键点置信度。</summary>
        public float Confidence;
    }

    /// <summary>
    /// 使用 33 个关键点描述当前姿态信息。
    /// </summary>
    [Serializable]
    public class PoseData
    {
        /// <summary>关键点总数。</summary>
        public const int KeypointCount = 33;

        [SerializeField]
        private PoseKeypoint[] _keypoints = new PoseKeypoint[KeypointCount];

        /// <summary>返回所有关键点的快照。</summary>
        public PoseKeypoint[] Keypoints => _keypoints;

        /// <summary>当前姿态的时间戳（秒）。</summary>
        public float Timestamp { get; set; }

        /// <summary>计算指定关键点之间的归一化距离。</summary>
        public float Distance(int indexA, int indexB)
        {
            if (!IsValidIndex(indexA) || !IsValidIndex(indexB))
            {
                return 0f;
            }

            Vector2 a = new Vector2(_keypoints[indexA].X, _keypoints[indexA].Y);
            Vector2 b = new Vector2(_keypoints[indexB].X, _keypoints[indexB].Y);
            return Vector2.Distance(a, b);
        }

        /// <summary>判断关键点索引是否合法。</summary>
        public static bool IsValidIndex(int index) => index >= 0 && index < KeypointCount;

        /// <summary>设置指定索引的关键点。</summary>
        public void SetKeypoint(int index, PoseKeypoint keypoint)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            _keypoints[index] = keypoint;
        }

        /// <summary>复制所有关键点。</summary>
        public void CopyTo(PoseData destination)
        {
            if (destination == null)
            {
                return;
            }

            for (int i = 0; i < KeypointCount; i++)
            {
                destination._keypoints[i] = _keypoints[i];
            }

            destination.Timestamp = Timestamp;
        }
    }
}

