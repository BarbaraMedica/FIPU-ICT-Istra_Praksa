using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera rotated vertically by mouse look.")]
    public Camera playerCamera;

    [Tooltip("Input System action reference for 2D movement.")]
    public InputActionReference moveAction;

    [Tooltip("Input System action reference for pointer or stick look.")]
    public InputActionReference lookAction;

    [Tooltip("Optional Input System action reference for jumping.")]
    public InputActionReference jumpAction;

    [Header("Movement")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 6.5f;
    public float gravity = -18f;
    public bool allowJump = true;
    public float jumpHeight = 1.2f;

    [Header("Look")]
    public float mouseSensitivity = 0.12f;
    public float controllerSensitivity = 110f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private CharacterController characterController;
    private float pitch;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(lookAction);
        EnableAction(jumpAction);
        LockCursor();
    }

    private void OnDisable()
    {
        DisableAction(moveAction);
        DisableAction(lookAction);
        DisableAction(jumpAction);
        UnlockCursor();
    }

    private void Update()
    {
        HandleCursorLock();

        if (Time.timeScale <= 0f)
        {
            return;
        }

        HandleLook();
        HandleMovement();
    }

    private void HandleCursorLock()
    {
        if (Time.timeScale > 0f && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private void HandleLook()
    {
        Vector2 lookInput = ReadLookInput();

        if (lookInput.sqrMagnitude <= 0f)
        {
            return;
        }

        bool usingMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0f;
        float sensitivity = usingMouse ? mouseSensitivity : controllerSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * lookInput.x * sensitivity);
        pitch = Mathf.Clamp(pitch - lookInput.y * sensitivity, minPitch, maxPitch);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = ReadMoveInput();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);

        float activeSpeed = IsSprintHeld() ? sprintSpeed : walkSpeed;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (allowJump && characterController.isGrounded && WasJumpPressed())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = move * activeSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private Vector2 ReadMoveInput()
    {
        if (moveAction != null && moveAction.action != null)
        {
            return moveAction.action.ReadValue<Vector2>();
        }

        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;
        input.y += Keyboard.current.wKey.isPressed ? 1f : 0f;
        input.y -= Keyboard.current.sKey.isPressed ? 1f : 0f;
        input.x += Keyboard.current.dKey.isPressed ? 1f : 0f;
        input.x -= Keyboard.current.aKey.isPressed ? 1f : 0f;
        return Vector2.ClampMagnitude(input, 1f);
    }

    private Vector2 ReadLookInput()
    {
        if (lookAction != null && lookAction.action != null)
        {
            return lookAction.action.ReadValue<Vector2>();
        }

        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
    }

    private bool WasJumpPressed()
    {
        if (jumpAction != null && jumpAction.action != null)
        {
            return jumpAction.action.WasPressedThisFrame();
        }

        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private bool IsSprintHeld()
    {
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }

    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Enable();
        }
    }

    private static void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Disable();
        }
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}