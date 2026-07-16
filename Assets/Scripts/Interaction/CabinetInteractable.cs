using UnityEngine;

/// <summary>
/// Interaktivni ormar. Na klik se "otvara" i "zatvara" te pušta odgovarajući
/// zvuk iz Door/Cabinet sound packa. Vizualno pomiče vrata ormara (child objekt)
/// ako je dodijeljen; ako nije, radi samo kao zvučni + poruka prop.
///
/// Zamjenjuje ranije placeholder kocke ormara. Kada nabaviš pravi 3D model
/// ormara, samo ga stavi kao child i dodijeli mu transform u polje doorPivot.
/// </summary>
public class CabinetInteractable : MonoBehaviour, IInteractable
{
    [Header("Interakcija")]
    public string openPrompt = "Otvori ormar";
    public string closePrompt = "Zatvori ormar";

    [TextArea]
    [Tooltip("Poruka koja se prikaže kad se ormar otvori (npr. trag ili sadržaj).")]
    public string openMessage = "Ormar je prazan... za sada.";

    public float messageDuration = 3f;
    public GameUIController uiController;

    [Header("Zvuk")]
    public AudioClip openClip;
    public AudioClip closeClip;
    [Range(0f, 1f)]
    public float volume = 0.8f;

    [Header("Otvaranje (opcionalno)")]
    [Tooltip("Transform vrata ormara koji se zakreće pri otvaranju. Ostavi prazno za samo zvuk.")]
    public Transform doorPivot;
    public Vector3 openEulerOffset = new Vector3(0f, 95f, 0f);
    public float openSpeed = 6f;

    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private AudioSource audioSource;

    public string InteractionPrompt => isOpen ? closePrompt : openPrompt;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f; // 3D zvuk vezan uz poziciju ormara

        if (doorPivot != null)
        {
            closedRotation = doorPivot.localRotation;
            openRotation = Quaternion.Euler(doorPivot.localEulerAngles + openEulerOffset);
        }
    }

    private void Update()
    {
        if (doorPivot == null)
        {
            return;
        }

        Quaternion target = isOpen ? openRotation : closedRotation;
        doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, target, openSpeed * Time.deltaTime);
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        isOpen = true;
        PlayClip(openClip);

        if (!string.IsNullOrWhiteSpace(openMessage))
        {
            uiController?.ShowMessage(openMessage, messageDuration);
        }
    }

    public void Close()
    {
        isOpen = false;
        PlayClip(closeClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
