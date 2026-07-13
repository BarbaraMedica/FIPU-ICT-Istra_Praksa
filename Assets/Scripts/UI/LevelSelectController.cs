using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectController : MonoBehaviour
{
    public void OpenGrade4()
    {
        SceneManager.LoadScene("Grade4Level");
    }

    public void OpenGrade5()
    {
        SceneManager.LoadScene("Grade5Level");
    }

    public void OpenGrade6()
    {
        SceneManager.LoadScene("Grade6Level");
    }

    public void OpenGrade7()
    {
        SceneManager.LoadScene("Grade7Level");
    }

    public void OpenGrade8()
    {
        SceneManager.LoadScene("Grade8Level");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}