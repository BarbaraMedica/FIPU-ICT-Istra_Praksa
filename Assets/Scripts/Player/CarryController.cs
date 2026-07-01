using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lets the player hold one Carryable at a time in front of the camera and carry it around.
/// Pick-up is triggered by the interaction key (handled inside Carryable). Drop with Q or the
/// right mouse button. While held, the item's colliders are disabled so the interaction ray can
/// pass through to sockets and other interactables. ItemSocket calls TakeHeldItem() to place it.
/// </summary>
public class CarryController : MonoBehaviour
{
    [Header("References")]
    public Transform holdPoint;
    public Camera playerCamera;

    [Header("Feel")]
    [Tooltip("How quickly the held item springs toward the hold point.")]
    public float followSpeed = 18f;

    public Carryable HeldItem { get; private set; }
    public bool IsCarrying => HeldItem != null;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void Update()
    {
        if (GameUIController.IsInputBlocking)
        {
            return;
        }

        if (IsCarrying && WasDropPressed())
        {
            Drop();
        }
    }

    private void LateUpdate()
    {
        if (HeldItem == null || holdPoint == null)
        {
            return;
        }

        HeldItem.transform.position = Vector3.Lerp(HeldItem.transform.position, holdPoint.position, followSpeed * Time.deltaTime);
        HeldItem.transform.rotation = Quaternion.Slerp(HeldItem.transform.rotation, holdPoint.rotation, followSpeed * Time.deltaTime);
    }

    public void PickUp(Carryable item)
    {
        if (item == null)
        {
            return;
        }

        if (HeldItem != null)
        {
            Drop(); // swap: drop whatever we're already holding
        }

        HeldItem = item;

        if (item.Body != null)
        {
            item.Body.isKinematic = true;
            item.Body.useGravity = false;
        }

        SetCollidersEnabled(item, false);
        item.NotifyPickedUp(this);
    }

    public void Drop()
    {
        if (HeldItem == null)
        {
            return;
        }

        Carryable item = HeldItem;
        HeldItem = null;

        SetCollidersEnabled(item, true);

        if (item.Body != null)
        {
            item.Body.isKinematic = false;
            item.Body.useGravity = true;
        }

        item.NotifyReleased();
    }

    /// <summary>Removes the held item from the player's hands so a socket can place it. Stays kinematic.</summary>
    public Carryable TakeHeldItem()
    {
        Carryable item = HeldItem;
        HeldItem = null;

        if (item != null)
        {
            SetCollidersEnabled(item, false);
            item.NotifyReleased();
        }

        return item;
    }

    private static void SetCollidersEnabled(Carryable item, bool value)
    {
        foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = value;
        }
    }

    private static bool WasDropPressed()
    {
        bool keyPressed = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        return keyPressed || mousePressed;
    }
}
