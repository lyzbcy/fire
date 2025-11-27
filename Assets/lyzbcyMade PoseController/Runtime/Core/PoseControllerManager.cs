using System;
using System.Collections.Generic;
using PoseController.Runtime.Input;
using PoseController.Runtime.Utils;
using UnityEngine;

namespace PoseController.Runtime.Core
{
    /// <summary>
    /// 姿态控制主入口，负责特征提取、动作分类与输入映射。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class PoseControllerManager : MonoBehaviour
    {
        [SerializeField]
        private PoseDetector _poseDetector;

        [SerializeField]
        private ActionClassifier _actionClassifier;

        [SerializeField]
        private PoseInputMapper _inputMapper;

        [SerializeField]
        private bool _enableMotionMapping = true;

        [SerializeField]
        private bool _enableClassifier = true;

        [SerializeField]
        private float _leanSensitivity = 1.5f;

        [SerializeField]
        private float _viewSensitivity = 100f;

        [SerializeField]
        private bool _debugMode;

        private readonly float[] _featureCache = new float[64];
        private readonly Dictionary<string, float> _gestureTimers = new Dictionary<string, float>();

        /// <summary>视角增量。</summary>
        public Vector2 ViewDelta { get; private set; }

        /// <summary>移动向量。</summary>
        public Vector2 MoveVector { get; private set; }

        /// <summary>手势触发事件。</summary>
        public event Action<string> OnGestureTriggered;

        private void Awake()
        {
            if (_poseDetector == null)
            {
                _poseDetector = FindObjectOfType<PoseDetector>();
            }

            if (_actionClassifier == null)
            {
                _actionClassifier = FindObjectOfType<ActionClassifier>();
            }

            if (_inputMapper == null)
            {
                _inputMapper = FindObjectOfType<PoseInputMapper>();
            }
        }

        private void OnEnable()
        {
            if (_actionClassifier != null)
            {
                _actionClassifier.OnActionRecognized += HandleActionRecognized;
            }
        }

        private void OnDisable()
        {
            if (_actionClassifier != null)
            {
                _actionClassifier.OnActionRecognized -= HandleActionRecognized;
            }
        }

        private void Update()
        {
            if (_poseDetector == null || !_poseDetector.IsPoseValid)
            {
                MoveVector = Vector2.zero;
                ViewDelta = Vector2.zero;
                return;
            }

            PoseData pose = _poseDetector.CurrentPose;
            UpdateMotionMapping(pose);

            if (_enableClassifier && _actionClassifier != null)
            {
                float[] features = ExtractFeatureVector(pose);
                _actionClassifier.Evaluate(features);
            }

            UpdateGestureTimers();
        }

        private void UpdateMotionMapping(PoseData pose)
        {
            if (!_enableMotionMapping || pose == null)
            {
                return;
            }

            float leanForward = MathUtils.EstimateForwardLean(pose, PoseJoints.LeftShoulder, PoseJoints.LeftHip);
            float leanSide = pose.Keypoints[PoseJoints.LeftShoulder].X - pose.Keypoints[PoseJoints.RightShoulder].X;
            float yaw = MathUtils.EstimateYaw(pose, PoseJoints.LeftShoulder, PoseJoints.RightShoulder);
            float pitch = pose.Keypoints[PoseJoints.Nose].Y - pose.Keypoints[PoseJoints.Mouth].Y;

            MoveVector = new Vector2(Mathf.Clamp(leanSide * _leanSensitivity, -1f, 1f),
                Mathf.Clamp(leanForward * _leanSensitivity, -1f, 1f));

            ViewDelta = new Vector2(Mathf.Clamp(yaw * _viewSensitivity, -180f, 180f),
                Mathf.Clamp(pitch * _viewSensitivity, -180f, 180f)) * Time.deltaTime;

            DetectSimpleGestures(pose, leanForward, leanSide);
        }

        private void DetectSimpleGestures(PoseData pose, float leanForward, float leanSide)
        {
            float handRaise = MathUtils.EstimateHandRaise(pose, PoseJoints.LeftWrist, PoseJoints.RightWrist, PoseJoints.Nose);

            if (handRaise > 0.5f)
            {
                TriggerGesture("HandsUp");
            }

            if (leanForward > 0.3f)
            {
                TriggerGesture("LeanForward");
            }
            else if (leanForward < -0.3f)
            {
                TriggerGesture("LeanBackward");
            }

            if (leanSide > 0.2f)
            {
                TriggerGesture("LeanRight");
            }
            else if (leanSide < -0.2f)
            {
                TriggerGesture("LeanLeft");
            }
        }

        private void HandleActionRecognized(string gesture)
        {
            TriggerGesture(gesture);
        }

        private void TriggerGesture(string gesture)
        {
            if (string.IsNullOrEmpty(gesture))
            {
                return;
            }

            OnGestureTriggered?.Invoke(gesture);
            _gestureTimers[gesture] = 0.1f;
            _inputMapper?.TriggerGesture(gesture);

            if (_debugMode)
            {
                Debug.Log($"PoseController: 触发手势 {gesture}");
            }
        }

        private void UpdateGestureTimers()
        {
            if (_gestureTimers.Count == 0 || _inputMapper == null)
            {
                return;
            }

            var keys = new List<string>(_gestureTimers.Keys);
            foreach (string key in keys)
            {
                _gestureTimers[key] -= Time.deltaTime;
                if (_gestureTimers[key] <= 0f)
                {
                    _inputMapper.ReleaseGesture(key);
                    _gestureTimers.Remove(key);
                }
            }
        }

        /// <summary>从姿态数据中提取特征向量。</summary>
        public float[] ExtractFeatureVector(PoseData data)
        {
            if (data == null)
            {
                return Array.Empty<float>();
            }

            float[] cache = _featureCache;
            int cursor = 0;

            void Write(float value)
            {
                if (cursor < cache.Length)
                {
                    cache[cursor++] = value;
                }
            }

            PoseKeypoint nose = data.Keypoints[PoseJoints.Nose];
            PoseKeypoint leftWrist = data.Keypoints[PoseJoints.LeftWrist];
            PoseKeypoint rightWrist = data.Keypoints[PoseJoints.RightWrist];
            PoseKeypoint leftHip = data.Keypoints[PoseJoints.LeftHip];
            PoseKeypoint rightHip = data.Keypoints[PoseJoints.RightHip];
            PoseKeypoint leftAnkle = data.Keypoints[PoseJoints.LeftAnkle];
            PoseKeypoint rightAnkle = data.Keypoints[PoseJoints.RightAnkle];

            Write(nose.X);
            Write(nose.Y);
            Write(leftWrist.X - nose.X);
            Write(leftWrist.Y - nose.Y);
            Write(rightWrist.X - nose.X);
            Write(rightWrist.Y - nose.Y);
            Write(leftHip.X - rightHip.X);
            Write(leftHip.Y - rightHip.Y);
            Write(Vector2.Distance(new Vector2(leftAnkle.X, leftAnkle.Y), new Vector2(rightAnkle.X, rightAnkle.Y)));
            Write(MathUtils.AngleBetween(data.Keypoints[PoseJoints.LeftShoulder], data.Keypoints[PoseJoints.LeftElbow], leftWrist));
            Write(MathUtils.AngleBetween(data.Keypoints[PoseJoints.RightShoulder], data.Keypoints[PoseJoints.RightElbow], rightWrist));
            Write(MathUtils.EstimateForwardLean(data, PoseJoints.LeftShoulder, PoseJoints.LeftHip));
            Write(MathUtils.EstimateYaw(data, PoseJoints.LeftShoulder, PoseJoints.RightShoulder));

            float leftHandOpen = MathUtils.EstimateHandOpenness(data, PoseJoints.LeftWrist, PoseJoints.LeftIndex, PoseJoints.LeftPinky);
            float rightHandOpen = MathUtils.EstimateHandOpenness(data, PoseJoints.RightWrist, PoseJoints.RightIndex, PoseJoints.RightPinky);
            Write(leftHandOpen);
            Write(rightHandOpen);

            for (int i = cursor; i < cache.Length; i++)
            {
                cache[i] = 0f;
            }

            float[] result = new float[cache.Length];
            Array.Copy(_featureCache, result, cache.Length);
            return result;
        }
    }

    /// <summary>
    /// 定义 MoveNet 关键点索引。
    /// </summary>
    public static class PoseJoints
    {
        public const int Nose = 0;
        public const int LeftEye = 1;
        public const int RightEye = 2;
        public const int LeftEar = 3;
        public const int RightEar = 4;
        public const int Mouth = 9;
        public const int LeftShoulder = 11;
        public const int RightShoulder = 12;
        public const int LeftElbow = 13;
        public const int RightElbow = 14;
        public const int LeftWrist = 15;
        public const int RightWrist = 16;
        public const int LeftHip = 23;
        public const int RightHip = 24;
        public const int LeftKnee = 25;
        public const int RightKnee = 26;
        public const int LeftAnkle = 27;
        public const int RightAnkle = 28;
        public const int LeftIndex = 19;
        public const int RightIndex = 20;
        public const int LeftPinky = 21;
        public const int RightPinky = 22;
    }
}

