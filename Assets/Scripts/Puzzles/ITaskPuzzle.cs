/// <summary>
/// Implemented by any puzzle that can accept a free-form answer string (multiple choice or typed).
/// Lets a single TaskStation submit answers without knowing the concrete puzzle type.
/// </summary>
public interface ITaskPuzzle
{
    void SubmitAnswer(string answer);
}
