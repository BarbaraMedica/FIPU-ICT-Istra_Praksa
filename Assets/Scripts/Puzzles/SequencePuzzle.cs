using UnityEngine;

public class SequencePuzzle : PuzzleBase
{
    [Header("Sequence")]
    public int[] expectedSequence = { 1, 2, 3 };

    [TextArea]
    public string progressFeedback = "Dobro. Nastavi.";

    [TextArea]
    public string resetFeedback = "Pogrešan redoslijed. Niz je vraćen na početak.";

    private int currentIndex;

    public void PressStep(int stepNumber)
    {
        if (IsSolved)
        {
            ShowFeedback(solvedFeedback);
            return;
        }

        if (expectedSequence == null || expectedSequence.Length == 0)
        {
            Solve();
            return;
        }

        if (expectedSequence[currentIndex] == stepNumber)
        {
            currentIndex++;

            if (currentIndex >= expectedSequence.Length)
            {
                Solve();
                return;
            }

            ShowFeedback(progressFeedback);
            return;
        }

        currentIndex = 0;
        ShowFeedback(resetFeedback);
    }

    public void ResetSequence()
    {
        currentIndex = 0;
    }
}