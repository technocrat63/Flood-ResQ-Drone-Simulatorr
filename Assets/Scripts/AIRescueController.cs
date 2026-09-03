using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIRescueController : MonoBehaviour
{
    [Header("Drone Fleet")]
    public DroneMovement[] drones;

    [Header("Victim Tracking")]
    public VictimTrackingManager victimTrackingManager;

    [Header("Optional Manual Victims")]
    public Transform[] victims;

    [Header("Rescue Settings")]
    public float minimumVictimDistance = 0.5f;
    public float rescueDelay = 1f;

    [Header("Automatic Rescue")]
    public bool automaticRescue = false;

    private VictimTrackingManager.Victim selectedVictim;

    private DroneMovement assignedDrone;

    private Coroutine rescueCoroutine;

    private bool rescueInProgress = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (
            drones == null ||
            drones.Length == 0
        )
        {
            drones =
                FindObjectsByType<DroneMovement>(
                    FindObjectsSortMode.None
                );
        }


        if (victimTrackingManager == null)
        {
            victimTrackingManager =
                FindFirstObjectByType<
                    VictimTrackingManager
                >();
        }


        Debug.Log(
            "AI RESCUE SYSTEM: " +
            (drones != null ? drones.Length : 0) +
            " drones detected."
        );


        Debug.Log(
            "AI RESCUE SYSTEM: Victim Tracking Manager = " +
            (victimTrackingManager != null
                ? "FOUND"
                : "NOT FOUND")
        );
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!automaticRescue)
        {
            return;
        }

        if (rescueInProgress)
        {
            return;
        }


        FindAndRescueVictim();
    }


    // =========================================================
    // FIND AND RESCUE
    // =========================================================

    private void FindAndRescueVictim()
    {
        if (victimTrackingManager == null)
        {
            return;
        }


        List<VictimTrackingManager.Victim>
            confirmedVictims =
            victimTrackingManager
                .GetConfirmedVictims();


        if (
            confirmedVictims == null ||
            confirmedVictims.Count == 0
        )
        {
            return;
        }


        VictimTrackingManager.Victim nearestVictim =
            FindNearestAvailableVictim(
                confirmedVictims
            );


        if (nearestVictim == null)
        {
            return;
        }


        DroneMovement nearestDrone =
            FindNearestAvailableDrone(
                nearestVictim.position
            );


        if (nearestDrone == null)
        {
            return;
        }


        selectedVictim =
            nearestVictim;

        assignedDrone =
            nearestDrone;


        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "AI RESCUE SYSTEM: VICTIM SELECTED"
        );

        Debug.Log(
            "Victim ID = #" +
            nearestVictim.id
        );

        Debug.Log(
            "Victim Position = " +
            nearestVictim.position
        );

        Debug.Log(
            "Confidence = " +
            nearestVictim.confidence.ToString("F2")
        );

        Debug.Log(
            "Assigned Drone = " +
            nearestDrone.gameObject.name
        );

        Debug.Log(
            "========================================"
        );


        rescueCoroutine =
            StartCoroutine(
                SendDroneToVictim(
                    nearestDrone,
                    nearestVictim
                )
            );
    }


    // =========================================================
    // FIND NEAREST VICTIM
    // =========================================================

    private VictimTrackingManager.Victim
        FindNearestAvailableVictim(
            List<VictimTrackingManager.Victim>
                confirmedVictims
        )
    {
        VictimTrackingManager.Victim
            nearestVictim = null;

        float shortestDistance =
            Mathf.Infinity;


        foreach (
            VictimTrackingManager.Victim victim
            in confirmedVictims
        )
        {
            if (victim == null)
            {
                continue;
            }


            // Skip victims already rescued.
            if (victim.rescued)
            {
                continue;
            }


            float distance =
                Vector3.Distance(
                    transform.position,
                    victim.position
                );


            if (
                distance <
                shortestDistance
            )
            {
                shortestDistance =
                    distance;

                nearestVictim =
                    victim;
            }
        }


        return nearestVictim;
    }


    // =========================================================
    // FIND NEAREST AVAILABLE DRONE
    // =========================================================

    private DroneMovement
        FindNearestAvailableDrone(
            Vector3 victimPosition
        )
    {
        DroneMovement nearestDrone =
            null;

        float shortestDistance =
            Mathf.Infinity;


        if (
            drones == null ||
            drones.Length == 0
        )
        {
            return null;
        }


        foreach (
            DroneMovement drone
            in drones
        )
        {
            if (drone == null)
            {
                continue;
            }


            if (drone.IsStopped)
            {
                continue;
            }


            if (!drone.IsDeployed)
            {
                continue;
            }


            if (drone.IsBusy)
            {
                continue;
            }


            float distance =
                Vector3.Distance(
                    drone.transform.position,
                    victimPosition
                );


            if (
                distance <
                shortestDistance
            )
            {
                shortestDistance =
                    distance;

                nearestDrone =
                    drone;
            }
        }


        return nearestDrone;
    }


    // =========================================================
    // SEND DRONE TO VICTIM
    // =========================================================

    private IEnumerator SendDroneToVictim(
        DroneMovement drone,
        VictimTrackingManager.Victim victim
    )
    {
        rescueInProgress = true;


        if (
            drone == null ||
            victim == null
        )
        {
            rescueInProgress =
                false;

            rescueCoroutine =
                null;

            yield break;
        }


        Vector3 victimPosition =
            victim.position;


        drone.GoToPosition(
            victimPosition
        );


        Debug.Log(
            "AI RESCUE SYSTEM: " +
            drone.gameObject.name +
            " flying to Victim #" +
            victim.id
        );


        // -----------------------------------------------------
        // WAIT UNTIL DRONE ARRIVES OR IS STOPPED
        // -----------------------------------------------------

        while (
            drone != null &&
            drone.IsBusy
        )
        {
            yield return null;
        }


        // -----------------------------------------------------
        // STOPPED DURING RESCUE
        // -----------------------------------------------------

        if (
            drone == null ||
            drone.IsStopped
        )
        {
            Debug.Log(
                "AI RESCUE SYSTEM: Rescue cancelled."
            );

            selectedVictim =
                null;

            assignedDrone =
                null;

            rescueInProgress =
                false;

            rescueCoroutine =
                null;

            yield break;
        }


        // -----------------------------------------------------
        // ARRIVED
        // -----------------------------------------------------

        Debug.Log(
            "AI RESCUE SYSTEM: " +
            drone.gameObject.name +
            " reached Victim #" +
            victim.id
        );


        yield return new WaitForSeconds(
            rescueDelay
        );


        // -----------------------------------------------------
        // CHECK AGAIN BEFORE RETURNING
        // -----------------------------------------------------

        if (
            drone == null ||
            drone.IsStopped ||
            !automaticRescue
        )
        {
            Debug.Log(
                "AI RESCUE SYSTEM: Drone was stopped " +
                "before patrol return."
            );


            selectedVictim =
                null;

            assignedDrone =
                null;

            rescueInProgress =
                false;

            rescueCoroutine =
                null;

            yield break;
        }


        // -----------------------------------------------------
        // MARK VICTIM RESCUED
        // -----------------------------------------------------

        victim.rescued = true;


        Debug.Log(
            "AI RESCUE SYSTEM: Victim #" +
            victim.id +
            " marked RESCUED."
        );


        drone.ReturnToPatrol();


        Debug.Log(
            "AI RESCUE SYSTEM: " +
            drone.gameObject.name +
            " returning to patrol."
        );


        selectedVictim =
            null;

        assignedDrone =
            null;

        rescueInProgress =
            false;

        rescueCoroutine =
            null;
    }


    // =========================================================
    // MANUAL RESCUE
    // =========================================================

    public void RescueVictim(
        Transform victimTransform
    )
    {
        if (victimTransform == null)
        {
            Debug.LogWarning(
                "AI RESCUE SYSTEM: Victim is null."
            );

            return;
        }


        if (rescueInProgress)
        {
            Debug.Log(
                "AI RESCUE SYSTEM: Rescue already in progress."
            );

            return;
        }


        DroneMovement nearestDrone =
            FindNearestAvailableDrone(
                victimTransform.position
            );


        if (nearestDrone == null)
        {
            Debug.LogWarning(
                "AI RESCUE SYSTEM: No available drone."
            );

            return;
        }


        nearestDrone.GoToPosition(
            victimTransform.position
        );
    }


    // =========================================================
    // ENABLE AUTOMATIC RESCUE
    // =========================================================

    public void EnableAutomaticRescue()
    {
        automaticRescue = true;

        Debug.Log(
            "AI RESCUE SYSTEM: Automatic rescue ENABLED."
        );
    }


    // =========================================================
    // DISABLE AUTOMATIC RESCUE
    // =========================================================

    public void DisableAutomaticRescue()
    {
        automaticRescue = false;


        // THIS FIXES THE RED DRONE STOP ISSUE.

        if (rescueCoroutine != null)
        {
            StopCoroutine(
                rescueCoroutine
            );

            rescueCoroutine =
                null;
        }


        selectedVictim =
            null;

        assignedDrone =
            null;

        rescueInProgress =
            false;


        Debug.Log(
            "AI RESCUE SYSTEM: Automatic rescue DISABLED."
        );

        Debug.Log(
            "AI RESCUE SYSTEM: Active rescue coroutine CANCELLED."
        );
    }


    // =========================================================
    // STATUS
    // =========================================================

    public Transform GetSelectedVictim()
    {
        if (selectedVictim == null)
        {
            return null;
        }


        if (selectedVictim.marker != null)
        {
            return selectedVictim.marker.transform;
        }


        return null;
    }


    public bool IsRescueInProgress()
    {
        return rescueInProgress;
    }


    public string GetMissionStatus()
    {
        if (rescueInProgress)
        {
            return "RESCUE IN PROGRESS";
        }


        if (automaticRescue)
        {
            return "AUTONOMOUS RESCUE ACTIVE";
        }


        return "STANDBY";
    }
}