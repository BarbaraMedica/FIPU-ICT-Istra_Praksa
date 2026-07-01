using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A key the player collects on interact. It does NOT get carried in the hands - interacting with
/// it "uses it up": it fires <see cref="onCollected"/> (which the RoomSequenceController uses to
/// unlock the exit door) and then hides itself. No inventory system needed; once collected the door
/// stays unlocked forever.
/// </summary>
public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Key")]
    public string keyName = "Kljuc";
    public string pickUpPrompt = "Uzmi kljuc";

    [TextArea]
    public string pickUpMessage = "";

    public GameUIController uiController;

    [Tooltip("If true the key object disappears once collected (it has been 'used' on the door).")]
    public bool hideOnCollect = true;

    [Tooltip("Invoked once when the key is collected.")]
    public UnityEvent onCollected = new UnityEvent();

    private bool collected;

    public string InteractionPrompt => collected ? string.Empty : pickUpPrompt + ": " + keyName;

    public void Interact(PlayerInteractor interactor)
    {
        if (collected)
        {
            return;
        }

        collected = true;
        onCollected?.Invoke();

        if (!string.IsNullOrWhiteSpace(pickUpMessage))
        {
            uiController?.ShowMessage(pickUpMessage);
        }

        if (hideOnCollect)
        {
            gameObject.SetActive(false);
        }
    }
}
