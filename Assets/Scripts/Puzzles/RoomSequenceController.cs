using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives a room as an escape-room sequence: only the first task is visible. Solving a task shows a
/// hint about where the next one is and reveals it. Solving the LAST task reveals a carryable key.
/// Picking up that key unlocks the room's exit door. There is no visible socket - finding and taking
/// the key is itself the final step before progressing.
/// </summary>
public class RoomSequenceController : MonoBehaviour
{
    [Header("Identity")]
    public string roomLabel = "Soba";
    public GameUIController uiController;

    [Header("Tasks (parallel lists, in solving order)")]
    public List<PuzzleBase> taskPuzzles = new List<PuzzleBase>();
    public List<GameObject> taskObjects = new List<GameObject>();

    [Tooltip("Message shown AFTER solving task i (points to the next task, or announces the key for the last task).")]
    [TextArea]
    public List<string> hints = new List<string>();

    [Header("Key + Door")]
    public GameObject keyObject;
    public KeyPickup keyPickup;
    public EscapeRoomDoor exitDoor;

    [Header("Messages")]
    [TextArea] public string keyAppearedMessage = "Pojavio se kljuc - uzmi ga!";
    [TextArea] public string doorUnlockedMessage = "Imas kljuc! Vrata su otkljucana.";
    public float messageDuration = 6f;

    private int solvedCount;
    private bool keyCollected;

    private void Start()
    {
        // Show only the first task; hide the rest and the key until earned.
        for (int i = 0; i < taskObjects.Count; i++)
        {
            if (taskObjects[i] != null)
            {
                taskObjects[i].SetActive(i == 0);
            }
        }

        if (keyObject != null)
        {
            keyObject.SetActive(false);
        }

        for (int i = 0; i < taskPuzzles.Count; i++)
        {
            int index = i; // capture per-iteration index for the listener
            if (taskPuzzles[i] != null)
            {
                taskPuzzles[i].OnSolved.AddListener(() => OnTaskSolved(index));
            }
        }

        if (keyPickup != null)
        {
            keyPickup.onCollected.AddListener(OnKeyCollected);
        }
    }

    private void OnTaskSolved(int index)
    {
        solvedCount = Mathf.Max(solvedCount, index + 1);

        if (index + 1 < taskObjects.Count)
        {
            // Reveal the next task and hint at where it is.
            if (taskObjects[index + 1] != null)
            {
                taskObjects[index + 1].SetActive(true);
            }
        }
        else
        {
            // Last task solved -> reveal the key.
            if (keyObject != null)
            {
                keyObject.SetActive(true);
            }
        }

        ShowMessageForStep(index);
        RefreshObjective();
    }

    private void ShowMessageForStep(int index)
    {
        string message = index >= 0 && index < hints.Count && !string.IsNullOrWhiteSpace(hints[index])
            ? hints[index]
            : keyAppearedMessage;

        uiController?.ShowMessage(message, messageDuration);
    }

    private void OnKeyCollected()
    {
        if (keyCollected)
        {
            return;
        }

        keyCollected = true;

        if (exitDoor != null)
        {
            exitDoor.Unlock();
        }

        uiController?.ShowMessage(doorUnlockedMessage, messageDuration);
        RefreshObjective();
    }

    /// <summary>Updates the shared objective line to reflect this room's current step. Called by the room trigger on enter.</summary>
    public void RefreshObjective()
    {
        if (uiController == null)
        {
            return;
        }

        string line;
        if (keyCollected)
        {
            line = roomLabel + ": imas kljuc - otvori vrata i idi dalje.";
        }
        else if (taskPuzzles.Count > 0 && solvedCount >= taskPuzzles.Count)
        {
            line = roomLabel + ": uzmi kljuc koji se pojavio!";
        }
        else
        {
            line = roomLabel + ": rijesi zadatak " + (solvedCount + 1) + "/" + taskPuzzles.Count + ".";
        }

        uiController.SetObjective(line);
    }
}
