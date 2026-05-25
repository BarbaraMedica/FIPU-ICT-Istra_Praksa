using UnityEngine;
using UnityEngine.InputSystem;

public class GamePauseController : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePanel;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        SetPaused(false);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetPaused(!IsPaused);
        }
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }
}