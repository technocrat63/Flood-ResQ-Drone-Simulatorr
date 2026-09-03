using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RescueDashboardController : MonoBehaviour
{
    [Header("Dashboard UI")]
    public TMP_Text droneInfoText;
    public TMP_Text victimInfoText;
    public TMP_Text aiMissionStatusText;

    [Header("References")]
    public VictimTrackingManager victimTrackingManager;

    [Header("Drones")]
    public DroneMovement[] drones;

    [Header("Display")]
    public int maxVictimsDisplayed = 8;

    private void Start()
    {
        if (victimTrackingManager == null)
        {
            victimTrackingManager =
                FindFirstObjectByType<VictimTrackingManager>();
        }

        if (drones == null || drones.Length == 0)
        {
            drones =
                FindObjectsByType<DroneMovement>(
                    FindObjectsSortMode.None
                );
        }

        Debug.Log(
            "DASHBOARD: Controller initialized."
        );

        Debug.Log(
            "DASHBOARD: Drones found = " +
            drones.Length
        );
    }

    private void Update()
    {
        UpdateDronePanel();
        UpdateVictimPanel();
        UpdateMissionPanel();
    }

    // =========================================================
    // DRONE PANEL
    // =========================================================

    private void UpdateDronePanel()
    {
        if (droneInfoText == null)
            return;

        if (drones == null || drones.Length == 0)
        {
            droneInfoText.text =
                "DRONE FLEET\n\nNO DRONES";
            return;
        }

        string text =
            "DRONE FLEET\n\n";

        int activeCount = 0;

        foreach (DroneMovement drone in drones)
        {
            if (drone == null)
                continue;

            if (drone.IsDeployed)
                activeCount++;

            string status =
                drone.IsDeployed
                    ? "ACTIVE"
                    : "READY";

            text +=
                drone.gameObject.name +
                " : " +
                status +
                "\n";
        }

        text +=
            "\nACTIVE: " +
            activeCount +
            " / " +
            drones.Length;

        droneInfoText.text = text;
    }

    // =========================================================
    // VICTIM PANEL
    // =========================================================

    private void UpdateVictimPanel()
    {
        if (victimInfoText == null)
            return;

        if (victimTrackingManager == null)
        {
            victimInfoText.text =
                "DETECTED VICTIMS\n\n" +
                "TRACKING SYSTEM OFFLINE";

            return;
        }

        List<VictimTrackingManager.Victim> victims =
            victimTrackingManager.GetVictims();

        if (victims == null || victims.Count == 0)
        {
            victimInfoText.text =
                "DETECTED VICTIMS\n\n" +
                "NO VICTIMS DETECTED";

            return;
        }

        string text =
            "DETECTED VICTIMS\n\n";

        int displayed = 0;

        foreach (
            VictimTrackingManager.Victim victim
            in victims
        )
        {
            if (victim == null)
                continue;

            // Only display confirmed victims
            if (!victim.confirmed)
                continue;

            Vector3 p =
                victim.position;

            text +=
                "VICTIM #" +
                victim.id +
                "\n";

            text +=
                "STATUS: " +
                (victim.confirmed
                    ? "CONFIRMED"
                    : "TRACKING") +
                "\n";

            text +=
                "CONFIDENCE: " +
                (victim.confidence * 100f)
                    .ToString("F0") +
                "%\n";

            text +=
                "LOCATION: (" +
                p.x.ToString("F1") +
                ", " +
                p.y.ToString("F1") +
                ", " +
                p.z.ToString("F1") +
                ")\n\n";

            displayed++;

            if (displayed >= maxVictimsDisplayed)
                break;
        }

        if (displayed == 0)
        {
            text +=
                "SCANNING...\n\n" +
                "No confirmed victims yet.";
        }

        victimInfoText.text = text;
    }

    // =========================================================
    // MISSION STATUS
    // =========================================================

    private void UpdateMissionPanel()
    {
        if (aiMissionStatusText == null)
            return;

        int victimCount = 0;

        if (victimTrackingManager != null)
        {
            List<VictimTrackingManager.Victim> victims =
                victimTrackingManager.GetVictims();

            foreach (
                VictimTrackingManager.Victim victim
                in victims
            )
            {
                if (victim != null && victim.confirmed)
                    victimCount++;
            }
        }

        bool anyDroneActive = false;

        if (drones != null)
        {
            foreach (DroneMovement drone in drones)
            {
                if (drone != null && drone.IsDeployed)
                {
                    anyDroneActive = true;
                    break;
                }
            }
        }

        string mission;

        if (anyDroneActive)
        {
            mission =
                victimCount > 0
                    ? "RESCUE IN PROGRESS"
                    : "AUTONOMOUS SCAN";
        }
        else
        {
            mission = "STANDBY";
        }

        aiMissionStatusText.text =
            "AI RESCUE SYSTEM\n" +
            "ACTIVE\n" +
            "MISSION: " +
            mission +
            "\n" +
            "FLEET: " +
            (drones != null
                ? drones.Length
                : 0) +
            " DRONES\n" +
            "DETECTION: YOLO";
    }
}