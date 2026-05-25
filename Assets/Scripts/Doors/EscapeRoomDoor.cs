using UnityEngine;

public class EscapeRoomDoor : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    public string interactionPrompt = "Otvori vrata";
    public string closePrompt = "Zatvori vrata";
    public string lockedPrompt = "Zaključano";

    [TextArea]
    public string lockedMessage = "Vrata su zaključana.";

    [TextArea]
    public string openMessage;

    public float messageDuration = 4f;
    public GameUIController uiController;

    [Header("Locking")]
    public bool startsLocked;
    public PuzzleBase requiredPuzzle;
    public bool unlockWhenPuzzleSolved = true;
    public bool openWhenPuzzleSolved = true;

    [Header("Opening")]
    public Vector3 openEulerOffset = new Vector3(0f, 95f, 0f);
    public float openSpeed = 5f;

    private bool isLocked;
    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string InteractionPrompt => isLocked ? lockedPrompt : isOpen ? closePrompt : interactionPrompt;
    public bool IsLocked => isLocked;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        closedRotation = transform.localRotation;
        openRotation = Quaternion.Euler(transform.localEulerAngles + openEulerOffset);
        isLocked = startsLocked || (requiredPuzzle != null && !requiredPuzzle.IsSolved);
    }

    private void OnEnable()
    {
        if (requiredPuzzle != null)
        {
            requiredPuzzle.OnSolved.AddListener(HandlePuzzleSolved);
        }
    }

    private void OnDisable()
    {
        if (requiredPuzzle != null)
        {
            requiredPuzzle.OnSolved.RemoveListener(HandlePuzzleSolved);
        }
    }

    private void Start()
    {
        if (requiredPuzzle != null && requiredPuzzle.IsSolved)
        {
            HandlePuzzleSolved();
        }
    }

    private void Update()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, openSpeed * Time.deltaTime);
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (isLocked)
        {
            uiController?.ShowMessage(lockedMessage, messageDuration);
            return;
        }

        if (isOpen)
        {
            Close();
            return;
        }

        Open();

        if (!string.IsNullOrWhiteSpace(openMessage))
        {
            uiController?.ShowMessage(openMessage, messageDuration);
        }
    }

    public void Unlock()
    {
        isLocked = false;
    }

    public void Lock()
    {
        isLocked = true;
        isOpen = false;
    }

    public void Open()
    {
        if (!isLocked)
        {
            isOpen = true;
        }
    }

    public void Close()
    {
        isOpen = false;
    }

    public void UnlockAndOpen()
    {
        Unlock();
        Open();
    }

    private void HandlePuzzleSolved()
    {
        if (unlockWhenPuzzleSolved)
        {
            Unlock();
        }

        if (openWhenPuzzleSolved)
        {
            Open();
        }
    }
}