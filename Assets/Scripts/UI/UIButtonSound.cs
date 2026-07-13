using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (UIAudioManager.Instance == null)
        {
            Debug.LogWarning(
                "UIAudioManager nije pronađen u aktivnoj igri."
            );

            return;
        }

        UIAudioManager.Instance.PlayButtonClick();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }
}