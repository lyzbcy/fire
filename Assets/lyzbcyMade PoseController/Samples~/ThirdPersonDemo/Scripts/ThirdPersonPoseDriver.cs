using PoseController.Runtime.Core;
using UnityEngine;

namespace PoseController.Samples.ThirdPersonDemo
{
    /// <summary>
    /// 使用 PoseController 的第三人称角色示例。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonPoseDriver : MonoBehaviour
    {
        [SerializeField]
        private PoseControllerManager _manager;

        [SerializeField]
        private Transform _cameraPivot;

        [SerializeField]
        private float _moveSpeed = 3f;

        [SerializeField]
        private float _jumpForce = 4f;

        [SerializeField]
        private float _gravity = 9.81f;

        private CharacterController _controller;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_manager == null)
            {
                _manager = FindObjectOfType<PoseControllerManager>();
            }
        }

        private void OnEnable()
        {
            if (_manager != null)
            {
                _manager.OnGestureTriggered += HandleGesture;
            }
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.OnGestureTriggered -= HandleGesture;
            }
        }

        private void Update()
        {
            if (_manager == null)
            {
                return;
            }

            Vector2 move = _manager.MoveVector;
            Vector3 desired = new Vector3(move.x, 0f, move.y);
            desired = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * desired;
            _controller.Move(desired * _moveSpeed * Time.deltaTime);

            if (_controller.isGrounded)
            {
                _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }

            if (_manager.ViewDelta.sqrMagnitude > 0.001f && _cameraPivot != null)
            {
                _cameraPivot.Rotate(Vector3.up, _manager.ViewDelta.x, Space.World);
                _cameraPivot.Rotate(Vector3.right, -_manager.ViewDelta.y, Space.Self);
            }

            Vector3 verticalMove = Vector3.up * _verticalVelocity * Time.deltaTime;
            _controller.Move(verticalMove);
        }

        /// <summary>供 UnityEvent 触发跳跃。</summary>
        public void Jump()
        {
            if (_controller.isGrounded)
            {
                _verticalVelocity = _jumpForce;
            }
        }

        private void HandleGesture(string gesture)
        {
            if (gesture == "HandsUp" || gesture == "RaiseHand")
            {
                Jump();
            }
        }
    }
}

