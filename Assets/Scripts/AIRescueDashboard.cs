using UnityEngine;
using TMPro;

public class AIRescueDashboard : MonoBehaviour
{
    [Header("Dashboard Text")]
    public TMP_Text statusText;

    [Header("AI System")]
    public AIRescueController aiController;


    private void Start()
    {
        if (statusText != null)
        {
            statusText.text =
                "AI RESCUE SYSTEM\n" +
                "Status: STANDBY";
        }

        if (aiController == null)
        {
            aiController =
                FindFirstObjectByType<AIRescueController>();
        }
    }

    public void ShowSearching()
    {
        if (statusText == null)
            return;

        statusText.text =
            "AI RESCUE SYSTEM\n" +
            "Status: ANALYZING...\n\n" +
            "Searching for confirmed victims...";
    }

    public void ShowNoVictim()
    {
        if (statusText == null)
            return;

        statusText.text =
            "AI RESCUE SYSTEM\n" +
            "Status: STANDBY\n\n" +
            "No confirmed victim available.";
    }

    public void ShowMission(
        int victimId,
        float confidence,
        string droneName,
        float distance
    )
    {
        if (statusText == null)
            return;

        statusText.text =
            "AI RESCUE SYSTEM\n" +
            "Status: AUTONOMOUS\n\n" +

            "PRIORITY VICTIM: #" +
            victimId + "\n" +

            "CONFIDENCE: " +
            (confidence * 100f).ToString("F0") +
            "%\n\n" +

            "ASSIGNED DRONE: " +
            droneName + "\n" +

            "DISTANCE: " +
            distance.ToString("F1") +
            " m\n\n" +

            "MISSION: RESCUE IN PROGRESS";
    }
}