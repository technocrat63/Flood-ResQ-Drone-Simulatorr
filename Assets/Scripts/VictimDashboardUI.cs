using UnityEngine;
using TMPro;

public class VictimDashboardUI : MonoBehaviour
{
    public VictimTrackingManager trackingManager;

    public TMP_Text victimInfoText;

    void Update()
    {
        if (trackingManager == null || victimInfoText == null)
            return;

        UpdateVictimDisplay();
    }

    void UpdateVictimDisplay()
    {
        var victims = trackingManager.GetVictims();

        if (victims.Count == 0)
        {
            victimInfoText.text =
                "NO VICTIMS DETECTED";
            return;
        }

        string display = "";

        foreach (var victim in victims)
        {
            display +=
                $"VICTIM #{victim.id}\n" +
                $"Status: TRACKING\n" +
                $"Confidence: {victim.confidence * 100f:F0}%\n" +
                $"Location: " +
                $"({victim.position.x:F1}, " +
                $"{victim.position.y:F1}, " +
                $"{victim.position.z:F1})\n\n";
        }

        victimInfoText.text = display;
    }
}