using UnityEngine;

/// <summary>
/// A single clickable object (a cube now, later a book on a shelf, a poster, anything with a
/// collider) that opens its task when interacted with. Multiple-choice tasks open a choice popup;
/// typed tasks open the text-input popup. Put this on the same GameObject as the puzzle component
/// so the whole thing is one toggleable unit the RoomSequenceController can reveal/hide.
/// </summary>
public class TaskStation : MonoBehaviour, IInteractable
{
    public enum TaskMode
    {
        MultipleChoice,
        Typed
    }

    [Header("Task")]
    public TaskMode mode = TaskMode.MultipleChoice;

    [TextArea]
    public string questionText;

    [Tooltip("Answer options (multiple choice only).")]
    public string[] options;

    public string promptText = "Otvori zadatak";

    [TextArea]
    public string alreadySolvedMessage = "Ovaj zadatak je vec rijesen.";

    [Tooltip("Trajanje prikaza poruka ovog zadatka (sekundi). 0 = zadano trajanje.")]
    public float messageDuration = 6f;

    [Tooltip("The puzzle this station submits answers to (MultipleChoicePuzzle or TypedAnswerPuzzle).")]
    public PuzzleBase puzzle;

    public GameUIController uiController;

    public string InteractionPrompt => (puzzle != null && puzzle.IsSolved) ? string.Empty : promptText;

    public void Interact(PlayerInteractor interactor)
    {
        if (puzzle != null && puzzle.IsSolved)
        {
            if (!string.IsNullOrWhiteSpace(alreadySolvedMessage))
            {
                if (messageDuration > 0f)
                {
                    uiController?.ShowMessage(alreadySolvedMessage, messageDuration);
                }
                else
                {
                    uiController?.ShowMessage(alreadySolvedMessage);
                }
            }
            return;
        }

        if (uiController == null)
        {
            return;
        }

        if (mode == TaskMode.MultipleChoice)
        {
            uiController.ShowMultipleChoice(questionText, options, SubmitAnswer);
        }
        else
        {
            uiController.ShowAnswerInput(questionText, SubmitAnswer);
        }
    }

    private void SubmitAnswer(string answer)
    {
        if (puzzle is ITaskPuzzle answerable)
        {
            answerable.SubmitAnswer(answer);
        }
    }
}
