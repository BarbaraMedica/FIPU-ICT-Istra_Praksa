using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Marks an object the player can pick up and carry. Works through the existing interaction system:
/// looking at it shows a prompt and pressing interact hands it to the player's CarryController.
/// Fires <see cref="onPickedUp"/> when collected so other systems (e.g. RoomSequenceController) can
/// react - for example, unlocking a door when the room's key is picked up.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Carryable : MonoBehaviour, IInteractable
{
    [Header("Item")]
    public string itemName = "Predmet";

    [Tooltip("Identifier other systems can match against.")]
    public string itemId = "";

    public string pickUpPrompt = "Uzmi";

    [Tooltip("Optional clue/message shown when the player picks the item up.")]
    [TextArea]
    public string pickUpMessage = "";

    public GameUIController uiController;

    [Tooltip("Invoked when this item is picked up by the player.")]
    public UnityEvent onPickedUp = new UnityEvent();

    public Rigidbody Body { get; private set; }
    private CarryController carrier;

    public bool IsHeld => carrier != null;
    public string InteractionPrompt => IsHeld ? string.Empty : pickUpPrompt + ": " + itemName;

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void Interact(PlayerInteractor interactor)
    {
        CarryController controller = interactor.GetComponent<CarryController>();
        if (controller == null)
        {
            return;
        }

        controller.PickUp(this);

        if (!string.IsNullOrWhiteSpace(pickUpMessage))
        {
            uiController?.ShowMessage(pickUpMessage);
        }
    }

    public void NotifyPickedUp(CarryController controller)
    {
        carrier = controller;
        onPickedUp?.Invoke();
    }

    public void NotifyReleased()
    {
        carrier = null;
    }
}
