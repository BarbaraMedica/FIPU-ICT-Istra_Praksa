using UnityEngine;
using UnityEngine.Events;

public class MessageInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    public string promptText = "Pregledaj";

    [TextArea]
    public string messageText = "Ovdje možeš nešto naučiti.";

    [Tooltip("Koliko dugo (sekundi) poruka ostaje na ekranu. 0 = koristi zadano trajanje iz GameUIControllera.")]
    public float messageDuration = 6f;

    public GameUIController uiController;
    public UnityEvent OnInteracted = new UnityEvent();

    [Header("Optional Door Link")]
    public EscapeRoomDoor doorToUnlock;
    public bool openDoorAfterUnlock;

    public string InteractionPrompt => promptText;

    public void Interact(PlayerInteractor interactor)
    {
        if (messageDuration > 0f)
        {
            uiController?.ShowMessage(messageText, messageDuration);
        }
        else
        {
            uiController?.ShowMessage(messageText);
        }

        if (doorToUnlock != null)
        {
            doorToUnlock.Unlock();

            if (openDoorAfterUnlock)
            {
                doorToUnlock.Open();
            }
        }

        OnInteracted.Invoke();
    }
}