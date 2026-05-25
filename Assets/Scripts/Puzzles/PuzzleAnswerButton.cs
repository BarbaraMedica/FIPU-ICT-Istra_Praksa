using UnityEngine;

public class PuzzleAnswerButton : MonoBehaviour, IInteractable
{
    [Header("Answer")]
    public string answerText;
    public MultipleChoicePuzzle puzzle;
    public string promptText = "Odaberi odgovor";

    public string InteractionPrompt => $"{promptText}: {answerText}";

    public void Interact(PlayerInteractor interactor)
    {
        puzzle?.SubmitAnswer(answerText);
    }
}