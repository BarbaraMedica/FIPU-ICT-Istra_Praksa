using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("Typed Answer Input")]
    [Tooltip("Panel shown when a task asks the player to type an answer.")]
    public GameObject answerInputPanel;
    public Text answerInputQuestionText;
    public InputField answerInputField;

    [Header("Multiple Choice Popup")]
    [Tooltip("Panel shown when a task asks the player to pick one of several answers.")]
    public GameObject answerChoicePanel;
    public Text answerChoiceQuestionText;
    [Tooltip("Pre-created option buttons; extra ones are hidden when a task has fewer options.")]
    public Button[] answerChoiceButtons;

    [Tooltip("True while the typed-answer panel is open.")]
    public static bool IsTextInputActive { get; private set; }

    [Tooltip("True while the multiple-choice popup is open.")]
    public static bool IsChoiceActive { get; private set; }

    /// <summary>True while any modal popup is open. Movement/look/interaction scripts pause on this.</summary>
    public static bool IsInputBlocking => IsTextInputActive || IsChoiceActive;

    private Action<string> pendingAnswerSubmitCallback;
    private Action<string> pendingChoiceCallback;

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

        if (answerInputPanel != null)
        {
            answerInputPanel.SetActive(false);
        }

        if (answerChoicePanel != null)
        {
            answerChoicePanel.SetActive(false);
        }

        IsTextInputActive = false;
        IsChoiceActive = false;
    }

    private void Update()
    {
        if (messagePanel != null && messagePanel.activeSelf && Time.time >= messageHideTime)
        {
            messagePanel.SetActive(false);
        }

        if (IsTextInputActive && Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                SubmitAnswerInput();
            }
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HideAnswerInput();
            }
        }

        if (IsChoiceActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HideMultipleChoice();
        }
    }

    // -------------------------------------------------------------------------
    // Typed answer popup
    // -------------------------------------------------------------------------

    /// <summary>Opens the typed-answer panel, unlocks the cursor, and remembers who to report the answer back to.</summary>
    public void ShowAnswerInput(string question, Action<string> onSubmit)
    {
        if (answerInputPanel == null || answerInputField == null)
        {
            Debug.LogWarning("GameUIController: answerInputPanel/answerInputField nije postavljen, ne mogu prikazati upis odgovora.");
            return;
        }

        answerInputPanel.SetActive(true);
        IsTextInputActive = true;
        pendingAnswerSubmitCallback = onSubmit;

        if (answerInputQuestionText != null)
        {
            answerInputQuestionText.text = question;
        }

        answerInputField.text = string.Empty;
        answerInputField.ActivateInputField();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Called by the panel's submit button (or Enter) to report the typed text back to the task.</summary>
    public void SubmitAnswerInput()
    {
        Action<string> callback = pendingAnswerSubmitCallback;
        string answer = answerInputField != null ? answerInputField.text : string.Empty;

        HideAnswerInput();
        callback?.Invoke(answer);
    }

    /// <summary>Closes the typed-answer panel without submitting (Escape / cancel button).</summary>
    public void HideAnswerInput()
    {
        if (answerInputPanel != null)
        {
            answerInputPanel.SetActive(false);
        }

        IsTextInputActive = false;
        pendingAnswerSubmitCallback = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // -------------------------------------------------------------------------
    // Multiple choice popup
    // -------------------------------------------------------------------------

    /// <summary>Opens the choice popup with one button per option and reports the chosen text back.</summary>
    public void ShowMultipleChoice(string question, IList<string> options, Action<string> onSelect)
    {
        if (answerChoicePanel == null || answerChoiceButtons == null)
        {
            Debug.LogWarning("GameUIController: answerChoicePanel/answerChoiceButtons nije postavljen, ne mogu prikazati izbor odgovora.");
            return;
        }

        pendingChoiceCallback = onSelect;

        if (answerChoiceQuestionText != null)
        {
            answerChoiceQuestionText.text = question;
        }

        for (int i = 0; i < answerChoiceButtons.Length; i++)
        {
            Button button = answerChoiceButtons[i];
            if (button == null)
            {
                continue;
            }

            bool hasOption = options != null && i < options.Count;
            button.gameObject.SetActive(hasOption);

            if (!hasOption)
            {
                continue;
            }

            string option = options[i];
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = option;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ChooseAnswer(option));
        }

        answerChoicePanel.SetActive(true);
        IsChoiceActive = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ChooseAnswer(string option)
    {
        Action<string> callback = pendingChoiceCallback;
        HideMultipleChoice();
        callback?.Invoke(option);
    }

    /// <summary>Closes the choice popup without choosing (Escape).</summary>
    public void HideMultipleChoice()
    {
        if (answerChoicePanel != null)
        {
            answerChoicePanel.SetActive(false);
        }

        IsChoiceActive = false;
        pendingChoiceCallback = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // -------------------------------------------------------------------------
    // Prompt / messages / objective
    // -------------------------------------------------------------------------

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
