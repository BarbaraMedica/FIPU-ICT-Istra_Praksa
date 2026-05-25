using UnityEngine;

public class SequenceButton : MonoBehaviour, IInteractable
{
    [Header("Sequence Step")]
    public int stepNumber = 1;
    public string displayText;
    public SequencePuzzle puzzle;
    public string promptText = "Korak niza";

    public string InteractionPrompt => string.IsNullOrWhiteSpace(displayText) ? $"{promptText} {stepNumber}" : $"{promptText}: {displayText}";

    public void Interact(PlayerInteractor interactor)
    {
        puzzle?.PressStep(stepNumber);
    }
}