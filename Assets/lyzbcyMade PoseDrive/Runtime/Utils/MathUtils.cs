using System;
using PoseDrive.Runtime.Core;
using UnityEngine;

namespace PoseDrive.Runtime.Utils
{
    /// <summary>
    /// 数学与姿态相关的辅助函数。
    /// </summary>
    public static class MathUtils
    {
        /// <summary>计算三点构成的夹角（单位：度）。</summary>
        public static float AngleBetween(PoseKeypoint a, PoseKeypoint b, PoseKeypoint c)
        {
            Vector2 ab = new Vector2(a.X - b.X, a.Y - b.Y);
            Vector2 cb = new Vector2(c.X - b.X, c.Y - b.Y);
            float angle = Vector2.Angle(ab, cb);
            return float.IsNaN(angle) ? 0f : angle;
        }

        /// <summary>估计身体前倾程度。</summary>
        public static float EstimateForwardLean(PoseData pose, int shoulderIndex, int hipIndex)
        {
            if (!PoseData.IsValidIndex(shoulderIndex) || !PoseData.IsValidIndex(hipIndex))
            {
                return 0f;
            }

            PoseKeypoint shoulder = pose.Keypoints[shoulderIndex];
            PoseKeypoint hip = pose.Keypoints[hipIndex];
            return Mathf.Clamp((hip.Y - shoulder.Y) * 4f, -1f, 1f);
        }

        /// <summary>计算两肩连线的水平偏转，近似头部旋转。</summary>
        public static float EstimateYaw(PoseData pose, int leftShoulder, int rightShoulder)
        {
            if (!PoseData.IsValidIndex(leftShoulder) || !PoseData.IsValidIndex(rightShoulder))
            {
                return 0f;
            }

            PoseKeypoint left = pose.Keypoints[leftShoulder];
            PoseKeypoint right = pose.Keypoints[rightShoulder];
            return Mathf.Clamp((left.Y - right.Y) * 4f, -1.5f, 1.5f);
        }

        /// <summary>估计双手举起程度。</summary>
        public static float EstimateHandRaise(PoseData pose, int leftWrist, int rightWrist, int nose)
        {
            if (!PoseData.IsValidIndex(leftWrist) || !PoseData.IsValidIndex(rightWrist) || !PoseData.IsValidIndex(nose))
            {
                return 0f;
            }

            PoseKeypoint nosePoint = pose.Keypoints[nose];
            float left = nosePoint.Y - pose.Keypoints[leftWrist].Y;
            float right = nosePoint.Y - pose.Keypoints[rightWrist].Y;
            return Mathf.Clamp01((left + right) * 2f);
        }

        /// <summary>估计手部张开程度。</summary>
        public static float EstimateHandOpenness(PoseData pose, int wrist, int indexFinger, int pinky)
        {
            if (!PoseData.IsValidIndex(wrist) || !PoseData.IsValidIndex(indexFinger) || !PoseData.IsValidIndex(pinky))
            {
                return 0f;
            }

            PoseKeypoint w = pose.Keypoints[wrist];
            PoseKeypoint index = pose.Keypoints[indexFinger];
            PoseKeypoint pinkyPoint = pose.Keypoints[pinky];

            float spread = Vector2.Distance(new Vector2(index.X, index.Y), new Vector2(pinkyPoint.X, pinkyPoint.Y));
            float length = Vector2.Distance(new Vector2(index.X, index.Y), new Vector2(w.X, w.Y));
            if (length < 1e-5f)
            {
                return 0f;
            }

            return Mathf.Clamp01(spread / length);
        }

        /// <summary>计算单位向量。</summary>
        public static Vector2 Normalized(Vector2 vector)
        {
            if (vector.sqrMagnitude < 1e-6f)
            {
                return Vector2.zero;
            }

            return vector.normalized;
        }
    }
}

