using UnityEngine;

public class DroneMovement : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Vector3[] waypoints;
    public float speed = 5f;
    public float waypointReachDistance = 0.2f;

    [Tooltip("Altitude used during patrol.")]
    public float patrolAltitude = 15f;

    [Header("Rescue Settings")]
    public float rescueSpeed = 7f;
    public float rescueReachDistance = 0.5f;

    [Tooltip("Keep drone at patrol altitude while going to victim.")]
    public bool keepRescueAltitude = true;

    private int currentWaypointIndex = 0;

    private bool deployed = false;
    private bool goingToTarget = false;
    private bool hardStopped = false;

    private Vector3 rescueTarget;
    private float lockedAltitude;


    public bool IsBusy
    {
        get { return goingToTarget; }
    }

    public bool IsDeployed
    {
        get { return deployed; }
    }

    public bool IsStopped
    {
        get { return hardStopped; }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // HARD STOP
        if (hardStopped)
        {
            return;
        }

        if (!deployed)
        {
            return;
        }

        if (goingToTarget)
        {
            MoveToRescueTarget();
        }
        else
        {
            Patrol();
        }
    }


    // =========================================================
    // PATROL
    // =========================================================

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning(
                gameObject.name +
                ": No patrol waypoints assigned."
            );

            return;
        }

        if (currentWaypointIndex >= waypoints.Length)
        {
            currentWaypointIndex = 0;
        }

        Vector3 waypoint =
            waypoints[currentWaypointIndex];

        // IMPORTANT:
        // Ignore waypoint Y and keep a fixed patrol altitude.

        Vector3 target =
            new Vector3(
                waypoint.x,
                lockedAltitude,
                waypoint.z
            );


        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime
            );


        // Horizontal rotation only.
        Vector3 direction =
            target - transform.position;

        direction.y = 0f;


        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    5f * Time.deltaTime
                );
        }


        if (
            Vector3.Distance(
                transform.position,
                target
            ) <= waypointReachDistance
        )
        {
            Debug.Log(
                gameObject.name +
                " reached waypoint " +
                currentWaypointIndex
            );

            currentWaypointIndex++;

            if (
                currentWaypointIndex >=
                waypoints.Length
            )
            {
                currentWaypointIndex = 0;
            }
        }
    }


    // =========================================================
    // RESCUE MOVEMENT
    // =========================================================

    private void MoveToRescueTarget()
    {
        if (hardStopped)
        {
            return;
        }

        Vector3 target =
            rescueTarget;

        if (keepRescueAltitude)
        {
            target.y = lockedAltitude;
        }


        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                rescueSpeed * Time.deltaTime
            );


        // Horizontal rotation only.
        Vector3 direction =
            target - transform.position;

        direction.y = 0f;


        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    5f * Time.deltaTime
                );
        }


        if (
            Vector3.Distance(
                transform.position,
                target
            ) <= rescueReachDistance
        )
        {
            goingToTarget = false;

            Debug.Log(
                gameObject.name +
                " reached rescue target."
            );
        }
    }


    // =========================================================
    // DEPLOY
    // =========================================================

    public void Deploy()
    {
        hardStopped = false;
        deployed = true;
        goingToTarget = false;

        currentWaypointIndex = 0;

        lockedAltitude =
            patrolAltitude;

        Vector3 position =
            transform.position;

        position.y =
            lockedAltitude;

        transform.position =
            position;


        Debug.Log(
            "========================================"
        );

        Debug.Log(
            gameObject.name +
            " DEPLOYED."
        );

        Debug.Log(
            "Patrol altitude = " +
            lockedAltitude
        );

        Debug.Log(
            "Waypoint count = " +
            (waypoints != null
                ? waypoints.Length
                : 0)
        );

        Debug.Log(
            "========================================"
        );
    }


    // =========================================================
    // GO TO VICTIM
    // =========================================================

    public void GoToPosition(
        Vector3 target)
    {
        // DO NOT allow an old rescue coroutine
        // to resurrect a stopped drone.

        if (hardStopped)
        {
            Debug.LogWarning(
                gameObject.name +
                ": Rescue command ignored because drone is STOPPED."
            );

            return;
        }


        if (!deployed)
        {
            Deploy();
        }


        rescueTarget =
            target;

        goingToTarget =
            true;


        Debug.Log(
            gameObject.name +
            " heading to rescue position: " +
            rescueTarget
        );
    }


    public void GoToTarget(
        Vector3 target)
    {
        GoToPosition(target);
    }


    // =========================================================
    // RETURN TO PATROL
    // =========================================================

    public void ReturnToPatrol()
    {
        // NEVER restart a hard-stopped drone.

        if (hardStopped)
        {
            Debug.Log(
                gameObject.name +
                " ignored ReturnToPatrol because it is STOPPED."
            );

            return;
        }

        goingToTarget = false;
        deployed = true;


        Debug.Log(
            gameObject.name +
            " returned to patrol."
        );
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetPatrol()
    {
        currentWaypointIndex = 0;
    }


    // =========================================================
    // HARD STOP
    // =========================================================

    public void StopDrone()
    {
        // This is the important part.

        goingToTarget = false;
        deployed = false;
        hardStopped = true;


        Debug.Log(
            "========================================"
        );

        Debug.Log(
            gameObject.name +
            " HARD STOPPED."
        );

        Debug.Log(
            "========================================"
        );
    }
}