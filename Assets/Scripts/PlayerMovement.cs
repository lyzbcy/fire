using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float verticalRotationLimit = 80f;
    public float jumpForce = 5f;
    public float gravity = -15f;
    public float coyoteTime = 0.1f; // “郊狼时间”容错跳跃
    public Camera playerCamera;
    public Transform movementBasis;
    public bool lockCursorOnDesktop = true;

    private CharacterController characterController;
    private bool useDetachedCameraFollow = false;
    private Vector3 detachedCameraLocalPosition = Vector3.zero;
    private float verticalRotation = 0f;
    private float verticalVelocity = 0f;
    private bool canUseCharacterController = false;
    private bool useTransformFallback = false;
    private Behaviour[] xrDesktopDisabledBehaviours;
    private bool? lastVrActiveState;

    private float lastGroundedTime = -10f;
    private bool jumpRequested = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (!LooksLikePlayerRoot())
        {
            enabled = false;
            return;
        }

        canUseCharacterController = characterController != null && characterController.enabled;
        useTransformFallback = !canUseCharacterController;

        if (characterController != null)
        {
            ClampCharacterControllerSettings();
        }

        if (movementBasis == null)
        {
            movementBasis = FindMovementBasis();
        }

        if (playerCamera == null || !playerCamera.isActiveAndEnabled)
        {
            playerCamera = FindActivePlayerCamera();
        }

        if (playerCamera != null)
        {
            useDetachedCameraFollow = !playerCamera.transform.IsChildOf(transform);
            if (useDetachedCameraFollow)
            {
                detachedCameraLocalPosition = transform.InverseTransformPoint(playerCamera.transform.position);
            }
        }

        CacheXrDesktopDisabledBehaviours();
        UpdateXrDesktopDisabledBehaviours();

        if (useTransformFallback)
        {
            Debug.LogWarning($"PlayerMovement on {name} is using transform fallback because the CharacterController is missing or disabled.");
        }

        if (!IsVrActive())
        {
            ApplyDesktopCursorState();
        }
    }

    void Update()
    {
        if (!enabled)
        {
            return;
        }

        if (characterController != null)
        {
            canUseCharacterController = characterController.enabled && characterController.gameObject.activeInHierarchy;
            useTransformFallback = !canUseCharacterController;
        }

        if (IsVrActive())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
        }
        else
        {
            HandleMouseVisibility();
        }

        UpdateXrDesktopDisabledBehaviours();

        if (characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        HandleJump();       // 判断是否请求跳跃
        HandleMovement();   // 移动+跳跃实际执行
        HandleCameraRotation();
    }

    void LateUpdate()
    {
        if (!enabled)
        {
            return;
        }

        SyncDetachedCamera();
    }

    void HandleMouseVisibility()
    {
        if (!lockCursorOnDesktop)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

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
        if (useTransformFallback)
        {
            return;
        }

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

        Vector3 forward = GetMovementForwardBasis();
        Vector3 right = GetMovementRightBasis(forward);

        Vector3 movementDirection = forward * verticalInput + right * horizontalInput;
        movementDirection.Normalize();
        Vector3 horizontalMovement = movementDirection * moveSpeed * Time.deltaTime;

        if (movementDirection.sqrMagnitude < 0.0001f)
        {
            horizontalMovement = Vector3.zero;
        }

        if (canUseCharacterController && characterController.isGrounded)
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

        if (canUseCharacterController)
        {
            characterController.Move(horizontalMovement + verticalMovement);
        }
        else
        {
            transform.position += horizontalMovement;
        }
    }

    void HandleCameraRotation()
    {
        if (IsVrActive())
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);

        if (playerCamera != null && !useDetachedCameraFollow)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
        else if (Camera.main != null && !useDetachedCameraFollow)
        {
            Camera.main.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    private void SyncDetachedCamera()
    {
        if (IsVrActive() || !useDetachedCameraFollow)
        {
            return;
        }

        if (playerCamera == null)
        {
            return;
        }

        playerCamera.transform.position = transform.TransformPoint(detachedCameraLocalPosition);
        playerCamera.transform.rotation = Quaternion.Euler(verticalRotation, transform.eulerAngles.y, 0f);
    }

    private bool IsVrActive()
    {
        return XRSettings.isDeviceActive;
    }

    private void CacheXrDesktopDisabledBehaviours()
    {
        List<Behaviour> matchedBehaviours = new List<Behaviour>();
        Behaviour[] behaviours = GetComponentsInChildren<Behaviour>(true);
        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (typeName.Contains("MoveProvider") || typeName.Contains("TurnProvider"))
            {
                matchedBehaviours.Add(behaviour);
            }
        }

        xrDesktopDisabledBehaviours = matchedBehaviours.ToArray();
    }

    private void UpdateXrDesktopDisabledBehaviours()
    {
        bool vrActive = IsVrActive();
        if (lastVrActiveState.HasValue && lastVrActiveState.Value == vrActive)
        {
            return;
        }

        if (xrDesktopDisabledBehaviours != null)
        {
            foreach (Behaviour behaviour in xrDesktopDisabledBehaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = vrActive;
                }
            }
        }

        lastVrActiveState = vrActive;
    }

    private void ApplyDesktopCursorState()
    {
        if (lockCursorOnDesktop)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private Vector3 GetMovementForwardBasis()
    {
        Transform basis = movementBasis != null ? movementBasis : GetViewTransform();
        Vector3 forward = basis != null ? basis.forward : transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            return transform.forward;
        }

        return forward.normalized;
    }

    private Vector3 GetMovementRightBasis(Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        if (right.sqrMagnitude < 0.0001f)
        {
            return transform.right;
        }

        return right.normalized;
    }

    private Transform GetViewTransform()
    {
        if (playerCamera != null && playerCamera.isActiveAndEnabled)
        {
            return playerCamera.transform;
        }

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            return Camera.main.transform;
        }

        return null;
    }

    private Camera FindActivePlayerCamera()
    {
        Camera[] cameras = GetComponentsInChildren<Camera>(true);
        foreach (Camera cameraCandidate in cameras)
        {
            if (cameraCandidate != null && cameraCandidate.isActiveAndEnabled)
            {
                return cameraCandidate;
            }
        }

        foreach (Camera cameraCandidate in cameras)
        {
            if (cameraCandidate != null)
            {
                return cameraCandidate;
            }
        }

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            return Camera.main;
        }

        return null;
    }

    private Transform FindMovementBasis()
    {
        Transform modelRoot = FindModelRoot(transform);
        if (modelRoot != null)
        {
            return modelRoot;
        }

        return transform;
    }

    private Transform FindModelRoot(Transform root)
    {
        foreach (Transform child in root)
        {
            if (IsPreferredMovementBasis(child))
            {
                return child;
            }

            Transform nestedMatch = FindModelRoot(child);
            if (nestedMatch != null)
            {
                return nestedMatch;
            }
        }

        return null;
    }

    private bool IsPreferredMovementBasis(Transform candidate)
    {
        if (candidate.GetComponent<Camera>() != null)
        {
            return false;
        }

        if (candidate.GetComponent<CharacterController>() != null)
        {
            return false;
        }

        if (candidate.GetComponent<Canvas>() != null)
        {
            return false;
        }

        if (candidate.GetComponentInChildren<Renderer>(true) == null)
        {
            return false;
        }

        string lowerName = candidate.name.ToLowerInvariant();
        if (lowerName.Contains("controller") || lowerName.Contains("hand") || lowerName.Contains("camera") || lowerName.Contains("rig") || lowerName.Contains("offset"))
        {
            return false;
        }

        return lowerName.Contains("player") || lowerName.Contains("model") || lowerName.Contains("body") || lowerName.Contains("avatar") || lowerName.Contains("character");
    }

    private void ClampCharacterControllerSettings()
    {
        if (characterController == null)
        {
            return;
        }

        float scaledHeight = Mathf.Abs(characterController.height * transform.lossyScale.y);
        float scaledRadiusX = Mathf.Abs(characterController.radius * transform.lossyScale.x);
        float scaledRadiusZ = Mathf.Abs(characterController.radius * transform.lossyScale.z);
        float scaledRadius = Mathf.Max(scaledRadiusX, scaledRadiusZ);
        float maxStepOffset = Mathf.Max(0f, scaledHeight + (scaledRadius * 2f) - 0.001f);

        if (characterController.stepOffset > maxStepOffset)
        {
            characterController.stepOffset = maxStepOffset;
        }
    }

    private bool LooksLikePlayerRoot()
    {
        string lowerName = name.ToLowerInvariant();
        if (lowerName.Contains("player") || lowerName.Contains("xr origin") || lowerName.Contains("xrorigin") || lowerName.Contains("rig") || lowerName.Contains("camera offset") || lowerName.Contains("头显") || lowerName.Contains("玩家"))
        {
            return true;
        }

        return transform.parent == null && (GetComponentInChildren<Camera>(true) != null || GetComponentInChildren<CharacterController>(true) != null);
    }
}
