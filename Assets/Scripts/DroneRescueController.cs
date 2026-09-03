using UnityEngine;

public class DroneRescueController : MonoBehaviour
{
    public DroneMovement droneMovement;
    public VictimTrackingManager victimTrackingManager;

    public void SendDroneToVictim(int victimId)
    {
        if (droneMovement == null)
        {
            Debug.LogError("DroneMovement is not assigned!");
            return;
        }

        if (victimTrackingManager == null)
        {
            Debug.LogError("VictimTrackingManager is not assigned!");
            return;
        }

        // Make sure the drone is deployed
        droneMovement.Deploy();

        // Get all tracked victims
        var victims = victimTrackingManager.GetVictims();

        foreach (var victim in victims)
        {
            if (victim.id == victimId)
            {
                if (!victim.confirmed)
                {
                    Debug.LogWarning(
                        "Victim #" + victimId +
                        " is not confirmed yet."
                    );

                    return;
                }

                Debug.Log(
                    "Sending " + droneMovement.gameObject.name +
                    " to Victim #" + victimId +
                    " at " + victim.position
                );

                droneMovement.GoToTarget(victim.position);

                return;
            }
        }

        Debug.LogWarning(
            "Victim #" + victimId + " was not found."
        );
    }
}