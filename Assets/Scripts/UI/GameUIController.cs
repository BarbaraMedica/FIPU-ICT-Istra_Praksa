using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("Prompt")]
    [Tooltip("Prikazuje se kada igrač gleda predmet za interakciju.")]
    public Text interactionPromptText;

    [Header("Messages")]
    [Tooltip("Panel koji se prikazuje dok su privremene poruke vidljive.")]
    public GameObject messagePanel;

    [Tooltip("Tekst za povratne informacije u uvodu i zagonetkama.")]
    public Text messageText;

    [Tooltip("Zadano trajanje prikaza poruka kada trajanje nije zadano.")]
    public float defaultMessageDuration = 3f;

    [Header("Objective")]
    [Tooltip("Neobavezni tekst cilja u kutu zaslona.")]
    public Text objectiveText;

    private float messageHideTime;

    private void Awake()
    {
        ClearInteractionPrompt();

        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (messagePanel != null && messagePanel.activeSelf && Time.time >= messageHideTime)
        {
            messagePanel.SetActive(false);
        }
    }

    public void SetInteractionPrompt(string prompt)
    {
        if (interactionPromptText == null)
        {
            return;
        }

        interactionPromptText.text = prompt;
        interactionPromptText.enabled = !string.IsNullOrWhiteSpace(prompt);
    }

    public void ClearInteractionPrompt()
    {
        if (interactionPromptText == null)
        {
            return;
        }

        interactionPromptText.text = string.Empty;
        interactionPromptText.enabled = false;
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, defaultMessageDuration);
    }

    public void ShowMessage(string message, float duration)
    {
        if (messageText == null)
        {
            return;
        }

        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
        }

        messageText.text = message;
        messageHideTime = Time.time + Mathf.Max(0.1f, duration);
    }

    public void SetObjective(string objective)
    {
        if (objectiveText == null)
        {
            return;
        }

        objectiveText.text = objective;
        objectiveText.enabled = !string.IsNullOrWhiteSpace(objective);
    }
}