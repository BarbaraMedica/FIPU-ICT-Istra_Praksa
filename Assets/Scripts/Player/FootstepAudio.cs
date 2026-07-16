using UnityEngine;

/// <summary>
/// Reproducira zvuk koraka dok se igrač kreće po tlu. Radi samostalno:
/// čita horizontalnu brzinu s CharacterControllera, pa ne treba mijenjati
/// FirstPersonController. Nasumično bira isječke iz walk/run skupa i pušta
/// ih u intervalu ovisnom o brzini kretanja.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FootstepAudio : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("AudioSource kroz koji se puštaju koraci. Ako je prazno, dodaje se automatski.")]
    public AudioSource audioSource;

    [Header("Isječci koraka")]
    [Tooltip("Zvukovi hodanja (nasumično se biraju).")]
    public AudioClip[] walkClips;

    [Tooltip("Zvukovi trčanja. Ako je prazno, koriste se walkClips.")]
    public AudioClip[] runClips;

    [Header("Tempo")]
    [Tooltip("Sekunde između koraka pri hodanju.")]
    public float walkStepInterval = 0.5f;

    [Tooltip("Sekunde između koraka pri trčanju.")]
    public float runStepInterval = 0.34f;

    [Tooltip("Minimalna horizontalna brzina da se koraci uopće čuju.")]
    public float minMoveSpeed = 0.6f;

    [Tooltip("Iznad ove brzine koristi se run skup i runStepInterval.")]
    public float runSpeedThreshold = 5.5f;

    [Header("Glasnoća i visina")]
    [Range(0f, 1f)]
    public float volume = 0.7f;

    [Tooltip("Nasumična varijacija visine tona da koraci ne zvuče identično.")]
    public float pitchVariation = 0.08f;

    private CharacterController characterController;
    private float stepTimer;
    private int lastClipIndex = -1;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        // Kad je otvoren popup zadatka (ili je igra pauzirana), kretanje je blokirano.
        // FirstPersonController tada ne zove CharacterController.Move(), pa velocity
        // zadrzi staru vrijednost > 0 i koraci bi se nastavili okidati. Zato ovdje
        // izlazimo i resetiramo tajmer da zvuk odmah utihne.
        if (Time.timeScale <= 0f || GameUIController.IsInputBlocking)
        {
            stepTimer = 0f;
            return;
        }

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;
        float speed = horizontalVelocity.magnitude;

        bool moving = characterController.isGrounded && speed >= minMoveSpeed;

        if (!moving)
        {
            // Kad staneš, sljedeći korak kreće brzo čim opet pođeš.
            stepTimer = 0f;
            return;
        }

        bool running = speed >= runSpeedThreshold;
        float interval = running ? runStepInterval : walkStepInterval;

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayStep(running);
            stepTimer = interval;
        }
    }

    private void PlayStep(bool running)
    {
        AudioClip[] set = (running && runClips != null && runClips.Length > 0) ? runClips : walkClips;

        if (set == null || set.Length == 0 || audioSource == null)
        {
            return;
        }

        int index = Random.Range(0, set.Length);

        // Izbjegni dvaput isti isječak za redom kad ih ima više.
        if (set.Length > 1 && index == lastClipIndex)
        {
            index = (index + 1) % set.Length;
        }

        lastClipIndex = index;

        AudioClip clip = set[index];
        if (clip == null)
        {
            return;
        }

        float previousPitch = audioSource.pitch;
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(clip, volume);
        audioSource.pitch = previousPitch;
    }
}
