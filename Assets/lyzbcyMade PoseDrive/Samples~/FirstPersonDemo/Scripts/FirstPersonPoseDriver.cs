using PoseDrive.Runtime.Core;
using UnityEngine;

namespace PoseDrive.Samples.FirstPersonDemo
{
    /// <summary>
    /// 第一人称示例控制器。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPoseDriver : MonoBehaviour
    {
        [SerializeField]
        private PoseDriveManager _manager;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private float _moveSpeed = 4f;

        [SerializeField]
        private float _gravity = 9.81f;

        [SerializeField]
        private float _interactDistance = 3f;

        private CharacterController _controller;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_manager == null)
            {
                _manager = FindObjectOfType<PoseDriveManager>();
            }

            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
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
            Vector3 worldMove = transform.right * move.x + transform.forward * move.y;
            _controller.Move(worldMove * (_moveSpeed * Time.deltaTime));

            if (_controller.isGrounded)
            {
                _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }

            _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);

            Vector2 view = _manager.ViewDelta;
            transform.Rotate(Vector3.up, view.x, Space.World);
            if (_camera != null)
            {
                _camera.transform.Rotate(Vector3.right, -view.y, Space.Self);
            }
        }

        private void HandleGesture(string gesture)
        {
            if (gesture == "Grab")
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            if (_camera == null)
            {
                return;
            }

            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, _interactDistance))
            {
                hit.collider.gameObject.SendMessage("OnPoseInteract", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}

