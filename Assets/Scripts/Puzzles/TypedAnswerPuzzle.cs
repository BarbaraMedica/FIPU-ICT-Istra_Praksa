using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// A puzzle solved by typing a free-text answer instead of pressing a multiple-choice block.
/// Supports several accepted answer strings (for spelling variants) and, optionally, numeric
/// tolerance so "36" and "36.0" both count as correct for math questions.
/// </summary>
public class TypedAnswerPuzzle : PuzzleBase, ITaskPuzzle
{
    [Header("Question")]
    [TextArea]
    public string questionText;

    [Tooltip("Any one of these counts as correct. Comparison is case-insensitive and trims whitespace.")]
    public string[] acceptedAnswers = { "0" };

    [Header("Numeric Matching")]
    [Tooltip("If true, the answer is parsed as a number and compared with tolerance instead of exact text matching.")]
    public bool treatAsNumeric;
    public float numericTolerance = 0.001f;

    [TextArea]
    public string correctFeedback = "Točno.";

    [TextArea]
    public string wrongFeedback = "Nije točno. Pokušaj ponovno.";

    public void SubmitAnswer(string rawAnswer)
    {
        if (IsSolved)
        {
            ShowFeedback(solvedFeedback);
            return;
        }

        if (IsCorrect(rawAnswer))
        {
            ShowFeedback(correctFeedback);
            Solve();
            return;
        }

        ShowFeedback(wrongFeedback);
    }

    private bool IsCorrect(string rawAnswer)
    {
        if (rawAnswer == null)
        {
            return false;
        }

        string trimmedAnswer = rawAnswer.Trim();

        if (treatAsNumeric)
        {
            if (!float.TryParse(trimmedAnswer, NumberStyles.Float, CultureInfo.InvariantCulture, out float submittedValue))
            {
                return false;
            }

            return acceptedAnswers.Any(accepted =>
                float.TryParse(accepted.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float acceptedValue) &&
                Mathf.Abs(submittedValue - acceptedValue) <= numericTolerance);
        }

        return acceptedAnswers.Any(accepted => string.Equals(accepted.Trim(), trimmedAnswer, StringComparison.OrdinalIgnoreCase));
    }
}
