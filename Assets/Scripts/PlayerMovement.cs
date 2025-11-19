using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float verticalRotationLimit = 80f;
    public float jumpForce = 5f;
    public float gravity = -15f;
    public float coyoteTime = 0.1f; // “郊狼时间”容错跳跃
    public Camera playerCamera;

    private CharacterController characterController;
    private float verticalRotation = 0f;
    private float verticalVelocity = 0f;

    private float lastGroundedTime = -10f;
    private bool jumpRequested = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseVisibility();

        if (characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        HandleJump();       // 判断是否请求跳跃
        HandleMovement();   // 移动+跳跃实际执行
        HandleCameraRotation();
    }

    void HandleMouseVisibility()
    {
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleJump()
    {
        if (Time.time - lastGroundedTime <= coyoteTime && Input.GetButtonDown("Jump"))
        {
            Debug.Log("✨ Jump Requested!");
            jumpRequested = true;
        }
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movementDirection = transform.forward * verticalInput + transform.right * horizontalInput;
        movementDirection.Normalize();
        Vector3 horizontalMovement = movementDirection * moveSpeed * Time.deltaTime;

        if (characterController.isGrounded)
        {
            // 保证稳定贴地
            if (!jumpRequested)
            {
                verticalVelocity = -2f;
            }
        }

        if (jumpRequested)
        {
            verticalVelocity = jumpForce;
            jumpRequested = false;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 verticalMovement = Vector3.up * verticalVelocity * Time.deltaTime;
        characterController.Move(horizontalMovement + verticalMovement);
    }

    void HandleCameraRotation()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
        else
        {
            Camera.main.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }
}
