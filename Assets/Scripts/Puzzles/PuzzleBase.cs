using UnityEngine;
using UnityEngine.Events;

public abstract class PuzzleBase : MonoBehaviour
{
    [Header("Puzzle")]
    public string puzzleName = "Zagonetka";
    public GameUIController uiController;
    public UnityEvent OnSolved = new UnityEvent();

    [TextArea]
    public string solvedFeedback = "Zagonetka je riješena.";

    [SerializeField]
    private bool isSolved;

    public bool IsSolved => isSolved;

    protected void Solve()
    {
        if (isSolved)
        {
            return;
        }

        isSolved = true;
        ShowFeedback(solvedFeedback);
        OnSolved.Invoke();
    }

    protected void ShowFeedback(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            uiController?.ShowMessage(message);
        }
    }

    public void ResetPuzzleState()
    {
        isSolved = false;
    }
}