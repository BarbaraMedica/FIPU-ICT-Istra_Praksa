using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameUIController uiController;
    public InputActionReference interactAction;

    [Tooltip("Shown in parentheses after every interaction prompt.")]
    public string interactKeyHint = "E";

    [Header("Raycast")]
    public float interactionRange = 3f;
    public LayerMask interactionMask = ~0;

    private IInteractable currentInteractable;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Disable();
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || GameUIController.IsInputBlocking)
        {
            currentInteractable = null;
            uiController?.ClearInteractionPrompt();
            return;
        }

        UpdateCurrentInteractable();

        if (currentInteractable != null && WasInteractPressed())
        {
            currentInteractable.Interact(this);
        }
    }

    private void UpdateCurrentInteractable()
    {
        currentInteractable = null;

        if (playerCamera == null)
        {
            uiController?.ClearInteractionPrompt();
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionMask, QueryTriggerInteraction.Ignore))
        {
            currentInteractable = FindInteractable(hit.collider);
        }

        if (currentInteractable == null)
        {
            uiController?.ClearInteractionPrompt();
            return;
        }

        uiController?.SetInteractionPrompt(FormatInteractionPrompt(currentInteractable.InteractionPrompt));
    }

    private string FormatInteractionPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(interactKeyHint))
        {
            return prompt.Trim();
        }

        string trimmedPrompt = prompt.Trim();
        string formattedHint = "(" + interactKeyHint.Trim() + ")";

        if (trimmedPrompt.Contains(formattedHint))
        {
            return trimmedPrompt;
        }

        return trimmedPrompt + " " + formattedHint;
    }

    private static IInteractable FindInteractable(Collider hitCollider)
    {
        MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private bool WasInteractPressed()
    {
        if (interactAction != null && interactAction.action != null)
        {
            return interactAction.action.WasPressedThisFrame();
        }

        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }
}