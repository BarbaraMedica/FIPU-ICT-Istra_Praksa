using System;
using UnityEngine;

public class MultipleChoicePuzzle : PuzzleBase
{
    [Header("Question")]
    [TextArea]
    public string questionText;

    public string correctAnswer;

    [TextArea]
    public string correctFeedback = "Točno.";

    [TextArea]
    public string wrongFeedback = "Nije točno. Pokušaj ponovno.";

    public void SubmitAnswer(string answer)
    {
        if (IsSolved)
        {
            ShowFeedback(solvedFeedback);
            return;
        }

        if (string.Equals(answer?.Trim(), correctAnswer?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ShowFeedback(correctFeedback);
            Solve();
            return;
        }

        ShowFeedback(wrongFeedback);
    }
}