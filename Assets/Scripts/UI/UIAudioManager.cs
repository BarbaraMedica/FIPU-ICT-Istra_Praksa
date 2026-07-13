using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("UI zvukovi")]
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Glasnoća")]
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        // Ako već postoji AudioManager, uništi novi duplikat.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // AudioManager ostaje aktivan tijekom promjene scena.
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlayButtonClick()
    {
        if (buttonClickSound == null)
        {
            Debug.LogWarning(
                "Button Click Sound nije postavljen na UIAudioManageru."
            );

            return;
        }

        audioSource.PlayOneShot(buttonClickSound, clickVolume);
    }
}