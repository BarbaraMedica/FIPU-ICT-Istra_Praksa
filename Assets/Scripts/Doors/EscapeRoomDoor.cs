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

    [Header("Audio")]
    [Tooltip("Zvuk otvaranja vrata.")]
    public AudioClip openClip;
    [Tooltip("Zvuk zatvaranja vrata.")]
    public AudioClip closeClip;
    [Tooltip("Zvuk kad su vrata zakljucana (pokusaj otvaranja).")]
    public AudioClip lockedClip;
    [Tooltip("Zvuk otkljucavanja (kad se vrata otkljucaju).")]
    public AudioClip unlockClip;
    [Range(0f, 1f)]
    public float volume = 0.85f;

    private AudioSource audioSource;

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f; // 3D zvuk vezan uz poziciju vrata
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
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
            PlayClip(lockedClip);
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
        bool wasLocked = isLocked;
        isLocked = false;
        if (wasLocked)
        {
            PlayClip(unlockClip);
        }
    }

    public void Lock()
    {
        isLocked = true;
        isOpen = false;
    }

    public void Open()
    {
        if (!isLocked && !isOpen)
        {
            isOpen = true;
            PlayClip(openClip);
        }
    }

    public void Close()
    {
        if (isOpen)
        {
            isOpen = false;
            PlayClip(closeClip);
        }
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