using UnityEngine;

namespace PoseDrive.Runtime.Core
{
    /// <summary>
    /// PoseDrive 输入控制器，负责处理全局启用开关。
    /// 支持按住模式和切换模式。
    /// </summary>
    public class PoseDriveInput : MonoBehaviour
    {
        public enum ActivationMode
        {
            /// <summary>按住按键时激活，松开时停用</summary>
            Hold,
            /// <summary>按一次激活，再按一次停用</summary>
            Toggle
        }

        [Header("激活设置")]
        [SerializeField]
        [Tooltip("用于激活/停用 PoseDrive 的按键")]
        private KeyCode _activationKey = KeyCode.V;

        [SerializeField]
        [Tooltip("激活模式：按住或切换")]
        private ActivationMode _activationMode = ActivationMode.Toggle;

        [Header("状态")]
        [SerializeField]
        [Tooltip("当前 PoseDrive 是否激活")]
        private bool _poseDriveActive = false;

        /// <summary>
        /// PoseDrive 是否当前激活
        /// </summary>
        public bool PoseDriveActive => _poseDriveActive;

        /// <summary>
        /// PoseDrive 激活状态变化事件
        /// </summary>
        public event System.Action<bool> OnPoseDriveActiveChanged;

        private bool _wasKeyPressed = false;

        private void Update()
        {
            // 明确使用 UnityEngine.Input 避免与 PoseDrive.Runtime.Input 命名空间冲突
            bool isKeyPressed = UnityEngine.Input.GetKey(_activationKey);

            if (_activationMode == ActivationMode.Hold)
            {
                // 按住模式：按键按下时激活，松开时停用
                bool shouldBeActive = isKeyPressed;
                if (shouldBeActive != _poseDriveActive)
                {
                    SetPoseDriveActive(shouldBeActive);
                }
            }
            else // Toggle mode
            {
                // 切换模式：按键按下时切换状态（仅在按下瞬间触发）
                if (isKeyPressed && !_wasKeyPressed)
                {
                    SetPoseDriveActive(!_poseDriveActive);
                }
            }

            _wasKeyPressed = isKeyPressed;
        }

        /// <summary>
        /// 设置 PoseDrive 激活状态
        /// </summary>
        public void SetPoseDriveActive(bool active)
        {
            if (_poseDriveActive != active)
            {
                _poseDriveActive = active;
                OnPoseDriveActiveChanged?.Invoke(_poseDriveActive);

                if (Application.isPlaying)
                {
                    Debug.Log($"PoseDrive: {(active ? "已激活" : "已停用")}");
                }
            }
        }

        /// <summary>
        /// 获取当前激活按键
        /// </summary>
        public KeyCode GetActivationKey() => _activationKey;

        /// <summary>
        /// 设置激活按键
        /// </summary>
        public void SetActivationKey(KeyCode key) => _activationKey = key;

        /// <summary>
        /// 获取当前激活模式
        /// </summary>
        public ActivationMode GetActivationMode() => _activationMode;

        /// <summary>
        /// 设置激活模式
        /// </summary>
        public void SetActivationMode(ActivationMode mode) => _activationMode = mode;
    }
}

