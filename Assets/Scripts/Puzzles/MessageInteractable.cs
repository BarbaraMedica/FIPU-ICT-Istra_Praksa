using UnityEngine;
using UnityEngine.Events;

public class MessageInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    public string promptText = "Pregledaj";

    [TextArea]
    public string messageText = "Ovdje možeš nešto naučiti.";

    public GameUIController uiController;
    public UnityEvent OnInteracted = new UnityEvent();

    [Header("Optional Door Link")]
    public EscapeRoomDoor doorToUnlock;
    public bool openDoorAfterUnlock;

    public string InteractionPrompt => promptText;

    public void Interact(PlayerInteractor interactor)
    {
        uiController?.ShowMessage(messageText);

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