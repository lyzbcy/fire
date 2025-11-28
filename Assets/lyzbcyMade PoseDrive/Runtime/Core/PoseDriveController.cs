using UnityEngine;
using PoseDrive.Runtime.Utils;

namespace PoseDrive.Runtime.Core
{
    /// <summary>
    /// PoseDrive 控制器，实现头部姿态控制镜头旋转和身体姿态控制角色移动。
    /// </summary>
    [RequireComponent(typeof(PoseDriveInput))]
    public class PoseDriveController : MonoBehaviour
    {
        [Header("依赖组件")]
        [SerializeField]
        [Tooltip("姿态检测器")]
        private PoseDetector _poseDetector;

        [SerializeField]
        [Tooltip("PoseDrive 输入控制器")]
        private PoseDriveInput _poseDriveInput;

        [SerializeField]
        [Tooltip("要控制的摄像机（如果为空则使用主摄像机）")]
        private Camera _targetCamera;

        [Header("镜头旋转设置")]
        [SerializeField]
        [Tooltip("死区角度（度），在此范围内视为不动")]
        [Range(0f, 30f)]
        private float _deadZoneAngle = 5f;

        [SerializeField]
        [Tooltip("最大姿态角度（度），超过此角度后速度不再增加")]
        [Range(30f, 90f)]
        private float _maxPoseAngle = 60f;

        [SerializeField]
        [Tooltip("最小旋转速度（度/秒）")]
        [Range(10f, 100f)]
        private float _minRotateSpeed = 30f;

        [SerializeField]
        [Tooltip("最大旋转速度（度/秒）")]
        [Range(50f, 360f)]
        private float _maxRotateSpeed = 180f;

        [SerializeField]
        [Tooltip("旋转速度曲线（X: 归一化角度 0-1, Y: 速度倍数 0-1）")]
        private AnimationCurve _rotationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("移动控制设置")]
        [SerializeField]
        [Tooltip("移动死区角度（度），在此范围内视为不动")]
        [Range(0f, 10f)]
        private float _moveDeadZone = 2f;

        [SerializeField]
        [Tooltip("最大前倾角度（度）")]
        [Range(10f, 45f)]
        private float _maxLeanAngle = 30f;

        [SerializeField]
        [Tooltip("最大移动速度（单位/秒）")]
        [Range(0.5f, 10f)]
        private float _maxMoveSpeed = 5f;

        [Header("调试")]
        [SerializeField]
        private bool _debugMode = false;

        // 内部状态
        private Vector3 _lastHeadRotation;
        private bool _lastPoseDriveActive = false;
        private Vector2 _currentRotationVelocity;
        private float _currentMoveSpeed;

        /// <summary>
        /// 当前旋转速度（度/秒）
        /// </summary>
        public Vector2 CurrentRotationVelocity => _currentRotationVelocity;

        /// <summary>
        /// 当前移动速度（单位/秒，正值前进，负值后退）
        /// </summary>
        public float CurrentMoveSpeed => _currentMoveSpeed;

        private void Awake()
        {
            // 自动查找依赖组件
            if (_poseDetector == null)
            {
                _poseDetector = FindObjectOfType<PoseDetector>();
            }

            if (_poseDriveInput == null)
            {
                _poseDriveInput = GetComponent<PoseDriveInput>();
                if (_poseDriveInput == null)
                {
                    _poseDriveInput = gameObject.AddComponent<PoseDriveInput>();
                }
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            // 初始化旋转曲线（如果未设置）
            if (_rotationCurve == null || _rotationCurve.length == 0)
            {
                _rotationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
        }

        private void OnEnable()
        {
            if (_poseDriveInput != null)
            {
                _poseDriveInput.OnPoseDriveActiveChanged += HandlePoseDriveActiveChanged;
            }
        }

        private void OnDisable()
        {
            if (_poseDriveInput != null)
            {
                _poseDriveInput.OnPoseDriveActiveChanged -= HandlePoseDriveActiveChanged;
            }
        }

        private void Update()
        {
            // 检查是否激活
            bool isActive = _poseDriveInput != null && _poseDriveInput.PoseDriveActive;

            if (!isActive)
            {
                // 未激活时重置状态
                _currentRotationVelocity = Vector2.zero;
                _currentMoveSpeed = 0f;
                return;
            }

            // 检查姿态数据是否有效
            if (_poseDetector == null || !_poseDetector.IsPoseValid)
            {
                _currentRotationVelocity = Vector2.zero;
                _currentMoveSpeed = 0f;
                return;
            }

            PoseData pose = _poseDetector.CurrentPose;
            if (pose == null)
            {
                return;
            }

            // 更新镜头旋转
            UpdateCameraRotation(pose);

            // 更新角色移动
            UpdateCharacterMovement(pose);
        }

        private void UpdateCameraRotation(PoseData pose)
        {
            // 计算头部相对旋转（yaw 和 pitch）
            // 使用肩膀和鼻子位置来估算头部方向
            Vector2 headYaw = EstimateHeadYaw(pose);
            Vector2 headPitch = EstimateHeadPitch(pose);

            // 应用死区
            float yawAngle = ApplyDeadZone(headYaw.x, _deadZoneAngle);
            float pitchAngle = ApplyDeadZone(headPitch.x, _deadZoneAngle);

            // 计算旋转速度
            float yawSpeed = CalculateRotationSpeed(yawAngle);
            float pitchSpeed = CalculateRotationSpeed(pitchAngle);

            _currentRotationVelocity = new Vector2(yawSpeed, pitchSpeed);

            // 应用旋转到摄像机
            if (_targetCamera != null)
            {
                Vector3 eulerAngles = _targetCamera.transform.eulerAngles;
                eulerAngles.y += yawSpeed * Time.deltaTime; // Yaw (左右)
                eulerAngles.x -= pitchSpeed * Time.deltaTime; // Pitch (上下，取反因为通常向上为正)
                eulerAngles.x = ClampAngle(eulerAngles.x, -90f, 90f); // 限制上下视角
                _targetCamera.transform.eulerAngles = eulerAngles;
            }

            if (_debugMode)
            {
                Debug.Log($"PoseDrive: Yaw={yawAngle:F1}°, Pitch={pitchAngle:F1}°, Speed=({yawSpeed:F1}, {pitchSpeed:F1})°/s");
            }
        }

        private void UpdateCharacterMovement(PoseData pose)
        {
            // 计算身体前倾角度
            float leanAngle = MathUtils.EstimateForwardLean(pose, PoseJoints.LeftShoulder, PoseJoints.LeftHip);

            // 转换为角度（假设 leanAngle 是归一化的，需要转换为实际角度）
            // 这里使用一个简化的估算：通过肩膀和髋部的相对位置来估算前倾角度
            PoseKeypoint leftShoulder = pose.Keypoints[PoseJoints.LeftShoulder];
            PoseKeypoint leftHip = pose.Keypoints[PoseJoints.LeftHip];

            // 计算肩膀相对于髋部的垂直偏移
            float verticalOffset = leftHip.Y - leftShoulder.Y; // Y 值越大越靠下
            float horizontalDistance = Mathf.Abs(leftShoulder.X - leftHip.X);

            // 估算前倾角度（简化计算）
            float estimatedAngle = 0f;
            if (verticalOffset > 0.01f) // 肩膀在髋部上方
            {
                // 前倾：肩膀向前移动（X 方向变化）或垂直距离减小
                // 这里使用垂直偏移的变化来估算
                estimatedAngle = Mathf.Atan2(horizontalDistance, verticalOffset) * Mathf.Rad2Deg;
            }

            // 应用死区
            float leanAngleWithDeadZone = ApplyDeadZone(leanAngle * 90f, _moveDeadZone);

            // 计算移动速度
            if (Mathf.Abs(leanAngleWithDeadZone) < _moveDeadZone)
            {
                _currentMoveSpeed = 0f;
            }
            else
            {
                // 归一化角度到 0-1 范围
                float normalizedAngle = Mathf.Clamp01(Mathf.Abs(leanAngleWithDeadZone) / _maxLeanAngle);
                
                // 计算速度（前倾为正，后仰为负）
                float speed = normalizedAngle * _maxMoveSpeed;
                _currentMoveSpeed = leanAngleWithDeadZone > 0 ? speed : -speed;
            }

            if (_debugMode)
            {
                Debug.Log($"PoseDrive: Lean={leanAngle:F3}, Angle={estimatedAngle:F1}°, Speed={_currentMoveSpeed:F2}");
            }
        }

        /// <summary>
        /// 估算头部 Yaw（左右旋转）
        /// </summary>
        private Vector2 EstimateHeadYaw(PoseData pose)
        {
            PoseKeypoint leftShoulder = pose.Keypoints[PoseJoints.LeftShoulder];
            PoseKeypoint rightShoulder = pose.Keypoints[PoseJoints.RightShoulder];
            PoseKeypoint nose = pose.Keypoints[PoseJoints.Nose];

            // 计算肩膀中心
            Vector2 shoulderCenter = new Vector2(
                (leftShoulder.X + rightShoulder.X) * 0.5f,
                (leftShoulder.Y + rightShoulder.Y) * 0.5f
            );

            // 计算鼻子相对于肩膀中心的偏移
            Vector2 noseOffset = new Vector2(
                nose.X - shoulderCenter.x,
                nose.Y - shoulderCenter.y
            );

            // 估算 yaw 角度（简化：使用 X 偏移）
            float yawAngle = noseOffset.x * 90f; // 归一化到 -90 到 90 度

            return new Vector2(yawAngle, 0f);
        }

        /// <summary>
        /// 估算头部 Pitch（上下旋转）
        /// </summary>
        private Vector2 EstimateHeadPitch(PoseData pose)
        {
            PoseKeypoint nose = pose.Keypoints[PoseJoints.Nose];
            PoseKeypoint mouth = pose.Keypoints[PoseJoints.Mouth];

            // 使用鼻子和嘴巴的垂直距离来估算 pitch
            float verticalDistance = mouth.Y - nose.Y; // Y 值越大越靠下

            // 估算 pitch 角度
            float pitchAngle = verticalDistance * 90f; // 归一化到 -90 到 90 度

            return new Vector2(pitchAngle, 0f);
        }

        /// <summary>
        /// 应用死区
        /// </summary>
        private float ApplyDeadZone(float value, float deadZone)
        {
            if (Mathf.Abs(value) < deadZone)
            {
                return 0f;
            }

            // 将死区外的值重新映射
            float sign = Mathf.Sign(value);
            float absValue = Mathf.Abs(value);
            return sign * (absValue - deadZone);
        }

        /// <summary>
        /// 计算旋转速度
        /// </summary>
        private float CalculateRotationSpeed(float angle)
        {
            if (Mathf.Abs(angle) < _deadZoneAngle)
            {
                return 0f;
            }

            // 限制角度范围
            float clampedAngle = Mathf.Clamp(Mathf.Abs(angle), 0f, _maxPoseAngle);

            // 归一化到 0-1
            float normalized = clampedAngle / _maxPoseAngle;

            // 应用曲线
            float curveValue = _rotationCurve.Evaluate(normalized);

            // 计算速度
            float speed = Mathf.Lerp(_minRotateSpeed, _maxRotateSpeed, curveValue);

            // 应用方向
            return Mathf.Sign(angle) * speed;
        }

        /// <summary>
        /// 限制角度范围
        /// </summary>
        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        private void HandlePoseDriveActiveChanged(bool isActive)
        {
            if (isActive != _lastPoseDriveActive)
            {
                _lastPoseDriveActive = isActive;
                
                // 重置状态
                _currentRotationVelocity = Vector2.zero;
                _currentMoveSpeed = 0f;
                _lastHeadRotation = Vector3.zero;

                if (_debugMode)
                {
                    Debug.Log($"PoseDrive: 状态变化 - {(isActive ? "激活" : "停用")}");
                }
            }
        }

        /// <summary>
        /// 获取移动向量（用于角色控制器）
        /// </summary>
        public Vector3 GetMoveVector()
        {
            if (!_poseDriveInput.PoseDriveActive)
            {
                return Vector3.zero;
            }

            // 返回前进方向的速度
            if (_targetCamera != null)
            {
                Vector3 forward = _targetCamera.transform.forward;
                forward.y = 0f; // 保持水平
                forward.Normalize();
                return forward * _currentMoveSpeed;
            }

            return Vector3.forward * _currentMoveSpeed;
        }
    }
}

