using UnityEngine;
using System.Collections.Generic;

public class VictimTrackingManager : MonoBehaviour
{
    [System.Serializable]
    public class Victim
    {
        public int id;

        public Vector3 position;

        public float confidence;

        public GameObject marker;

        public int detectionCount;

        public float timeSinceLastDetection;

        public bool confirmed;

        public bool rescued;
    }


    // =========================================================
    // MARKER
    // =========================================================

    [Header("Marker")]
    public GameObject detectionMarkerPrefab;

    [Tooltip("Height above the victim where the marker appears.")]
    public float markerHeight = 2f;


    // =========================================================
    // TRACKING
    // =========================================================

    [Header("Victim Tracking")]

    [Tooltip(
        "X/Z distance within which detections are considered " +
        "the same victim."
    )]
    public float matchingDistance = 12f;

    [Tooltip(
        "Additional radius used when cleaning duplicate victims."
    )]
    public float duplicateMergeDistance = 10f;


    // =========================================================
    // CONFIRMATION
    // =========================================================

    [Header("Confirmation")]

    [Tooltip(
        "Number of detections required before a victim " +
        "becomes confirmed."
    )]
    public int requiredDetections = 3;

    [Tooltip(
        "Minimum YOLO confidence accepted."
    )]
    [Range(0f, 1f)]
    public float minimumConfidence = 0.55f;


    // =========================================================
    // LOST CANDIDATES
    // =========================================================

    [Header("Lost Candidate")]

    [Tooltip(
        "Unconfirmed candidates disappear after this time " +
        "without another detection."
    )]
    public float lostVictimTimeout = 8f;


    // =========================================================
    // POSITION STABILITY
    // =========================================================

    [Header("Position Stabilization")]

    [Tooltip(
        "How strongly candidate position follows new detections."
    )]
    [Range(0.05f, 1f)]
    public float positionSmoothing = 0.25f;


    // =========================================================
    // INTERNAL DATA
    // =========================================================

    private readonly List<Victim> victims =
        new List<Victim>();

    private int nextVictimId = 1;

    private float mergeTimer = 0f;

    [Tooltip(
        "How often duplicate victims are checked and merged."
    )]
    public float mergeCheckInterval = 1f;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdateVictimTimers();

        RemoveLostCandidates();

        mergeTimer += Time.deltaTime;

        if (mergeTimer >= mergeCheckInterval)
        {
            mergeTimer = 0f;

            MergeDuplicateVictims();
        }
    }


    // =========================================================
    // TIMER UPDATE
    // =========================================================

    private void UpdateVictimTimers()
    {
        foreach (Victim victim in victims)
        {
            if (victim == null)
            {
                continue;
            }

            victim.timeSinceLastDetection +=
                Time.deltaTime;
        }
    }


    // =========================================================
    // REMOVE LOST CANDIDATES
    // =========================================================

    private void RemoveLostCandidates()
    {
        for (
            int i = victims.Count - 1;
            i >= 0;
            i--
        )
        {
            Victim victim =
                victims[i];

            if (victim == null)
            {
                victims.RemoveAt(i);
                continue;
            }


            // ONLY unconfirmed candidates are removed.

            if (
                !victim.confirmed &&
                victim.timeSinceLastDetection >
                lostVictimTimeout
            )
            {
                RemoveVictim(
                    victim
                );
            }
        }
    }


    // =========================================================
    // PROCESS DETECTION
    // =========================================================

    public void ProcessDetection(
        Vector3 detectedPosition,
        float confidence
    )
    {
        // -----------------------------------------------------
        // Confidence filter
        // -----------------------------------------------------

        if (
            confidence <
            minimumConfidence
        )
        {
            Debug.Log(
                "VICTIM TRACKING: " +
                "Ignored weak detection. Confidence = " +
                confidence.ToString("F2")
            );

            return;
        }


        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "VICTIM DETECTION RECEIVED"
        );

        Debug.Log(
            "Position = " +
            FormatPosition(
                detectedPosition
            )
        );

        Debug.Log(
            "Confidence = " +
            confidence.ToString("F2")
        );

        Debug.Log(
            "========================================"
        );


        // -----------------------------------------------------
        // FIRST PRIORITY:
        // Match an existing CONFIRMED victim.
        // -----------------------------------------------------

        Victim existingVictim =
            FindMatchingConfirmedVictim(
                detectedPosition
            );


        if (existingVictim != null)
        {
            UpdateExistingVictim(
                existingVictim,
                detectedPosition,
                confidence
            );

            return;
        }


        // -----------------------------------------------------
        // SECOND PRIORITY:
        // Match an existing candidate.
        // -----------------------------------------------------

        existingVictim =
            FindMatchingCandidate(
                detectedPosition
            );


        if (existingVictim != null)
        {
            UpdateExistingVictim(
                existingVictim,
                detectedPosition,
                confidence
            );

            return;
        }


        // -----------------------------------------------------
        // No match → create candidate.
        // -----------------------------------------------------

        CreateNewCandidate(
            detectedPosition,
            confidence
        );


        // Immediately clean duplicates.
        MergeDuplicateVictims();
    }


    // =========================================================
    // FIND CONFIRMED VICTIM
    // =========================================================

    private Victim FindMatchingConfirmedVictim(
        Vector3 position
    )
    {
        Victim bestVictim =
            null;

        float bestDistance =
            matchingDistance;


        foreach (Victim victim in victims)
        {
            if (
                victim == null ||
                !victim.confirmed
            )
            {
                continue;
            }


            float distance =
                HorizontalDistance(
                    victim.position,
                    position
                );


            if (
                distance <=
                bestDistance
            )
            {
                bestDistance =
                    distance;

                bestVictim =
                    victim;
            }
        }


        return bestVictim;
    }


    // =========================================================
    // FIND CANDIDATE
    // =========================================================

    private Victim FindMatchingCandidate(
        Vector3 position
    )
    {
        Victim bestVictim =
            null;

        float bestDistance =
            matchingDistance;


        foreach (Victim victim in victims)
        {
            if (
                victim == null ||
                victim.confirmed
            )
            {
                continue;
            }


            float distance =
                HorizontalDistance(
                    victim.position,
                    position
                );


            if (
                distance <=
                bestDistance
            )
            {
                bestDistance =
                    distance;

                bestVictim =
                    victim;
            }
        }


        return bestVictim;
    }


    // =========================================================
    // UPDATE EXISTING VICTIM
    // =========================================================

    private void UpdateExistingVictim(
        Victim victim,
        Vector3 detectedPosition,
        float confidence
    )
    {
        if (victim == null)
        {
            return;
        }


        victim.timeSinceLastDetection =
            0f;


        victim.detectionCount++;


        // Keep highest confidence.

        if (
            confidence >
            victim.confidence
        )
        {
            victim.confidence =
                confidence;
        }


        // -----------------------------------------------------
        // Candidate position can move.
        // Confirmed position stays frozen.
        // -----------------------------------------------------

        if (!victim.confirmed)
        {
            victim.position =
                Vector3.Lerp(
                    victim.position,
                    detectedPosition,
                    positionSmoothing
                );
        }


        // -----------------------------------------------------
        // Confirm victim
        // -----------------------------------------------------

        if (
            !victim.confirmed &&
            victim.detectionCount >=
            requiredDetections
        )
        {
            ConfirmVictim(
                victim
            );
        }


        Debug.Log(
            "VICTIM TRACKING: Updated Victim #" +
            victim.id +
            " | Position = " +
            FormatPosition(
                victim.position
            ) +
            " | Detections = " +
            victim.detectionCount
        );
    }


    // =========================================================
    // CREATE NEW CANDIDATE
    // =========================================================

    private void CreateNewCandidate(
        Vector3 detectedPosition,
        float confidence
    )
    {
        Victim newVictim =
            new Victim();


        newVictim.id =
            nextVictimId++;


        newVictim.position =
            detectedPosition;


        newVictim.confidence =
            confidence;


        newVictim.detectionCount =
            1;


        newVictim.timeSinceLastDetection =
            0f;


        newVictim.confirmed =
            false;


        newVictim.rescued =
            false;


        newVictim.marker =
            null;


        victims.Add(
            newVictim
        );


        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "NEW VICTIM CANDIDATE #" +
            newVictim.id
        );

        Debug.Log(
            "Position = " +
            FormatPosition(
                newVictim.position
            )
        );

        Debug.Log(
            "========================================"
        );
    }


    // =========================================================
    // CONFIRM VICTIM
    // =========================================================

    private void ConfirmVictim(
        Victim victim
    )
    {
        if (victim == null)
        {
            return;
        }


        victim.confirmed =
            true;


        victim.rescued =
            false;


        CreateMarker(
            victim
        );


        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "CONFIRMED VICTIM #" +
            victim.id
        );

        Debug.Log(
            "Coordinates = " +
            FormatPosition(
                victim.position
            )
        );

        Debug.Log(
            "Confidence = " +
            victim.confidence.ToString("F2")
        );

        Debug.Log(
            "========================================"
        );
    }


    // =========================================================
    // MERGE DUPLICATE VICTIMS
    // =========================================================

    private void MergeDuplicateVictims()
    {
        if (victims.Count < 2)
        {
            return;
        }


        bool mergedSomething;


        do
        {
            mergedSomething =
                false;


            for (
                int i = 0;
                i < victims.Count;
                i++
            )
            {
                if (victims[i] == null)
                {
                    continue;
                }


                for (
                    int j = i + 1;
                    j < victims.Count;
                    j++
                )
                {
                    if (victims[j] == null)
                    {
                        continue;
                    }


                    float distance =
                        HorizontalDistance(
                            victims[i].position,
                            victims[j].position
                        );


                    if (
                        distance <=
                        duplicateMergeDistance
                    )
                    {
                        Debug.Log(
                            "VICTIM TRACKING: " +
                            "Duplicate victims detected. " +
                            "Merging #" +
                            victims[i].id +
                            " and #" +
                            victims[j].id +
                            " | Distance = " +
                            distance.ToString("F2")
                        );


                        MergeVictims(
                            victims[i],
                            victims[j]
                        );


                        mergedSomething =
                            true;


                        break;
                    }
                }


                if (mergedSomething)
                {
                    break;
                }
            }

        } while (
            mergedSomething &&
            victims.Count > 1
        );
    }


    // =========================================================
    // MERGE TWO VICTIMS
    // =========================================================

    private void MergeVictims(
        Victim first,
        Victim second
    )
    {
        if (
            first == null ||
            second == null
        )
        {
            return;
        }


        // -----------------------------------------------------
        // Decide which victim survives.
        //
        // Priority:
        // 1. Confirmed victim
        // 2. More detections
        // 3. Higher confidence
        // -----------------------------------------------------

        Victim keeper;
        Victim duplicate;


        if (
            first.confirmed &&
            !second.confirmed
        )
        {
            keeper = first;
            duplicate = second;
        }
        else if (
            !first.confirmed &&
            second.confirmed
        )
        {
            keeper = second;
            duplicate = first;
        }
        else if (
            first.detectionCount >=
            second.detectionCount
        )
        {
            keeper = first;
            duplicate = second;
        }
        else
        {
            keeper = second;
            duplicate = first;
        }


        // -----------------------------------------------------
        // Preserve strongest information.
        // -----------------------------------------------------

        keeper.detectionCount +=
            duplicate.detectionCount;


        if (
            duplicate.confidence >
            keeper.confidence
        )
        {
            keeper.confidence =
                duplicate.confidence;
        }


        keeper.timeSinceLastDetection =
            0f;


        // If either is confirmed,
        // the merged victim is confirmed.

        if (
            keeper.confirmed ||
            duplicate.confirmed
        )
        {
            keeper.confirmed =
                true;

            keeper.rescued =
                keeper.rescued ||
                duplicate.rescued;
        }


        // -----------------------------------------------------
        // Marker handling
        // -----------------------------------------------------

        if (
            keeper.marker == null &&
            duplicate.marker != null
        )
        {
            keeper.marker =
                duplicate.marker;

            duplicate.marker =
                null;
        }
        else if (
            keeper.marker != null &&
            duplicate.marker != null
        )
        {
            // DESTROY EXTRA RED MARKER

            Destroy(
                duplicate.marker
            );

            duplicate.marker =
                null;
        }


        // -----------------------------------------------------
        // Keep confirmed position.
        // -----------------------------------------------------

        if (
            !keeper.confirmed
        )
        {
            keeper.position =
                Vector3.Lerp(
                    keeper.position,
                    duplicate.position,
                    0.5f
                );
        }


        // -----------------------------------------------------
        // Remove duplicate victim record.
        // -----------------------------------------------------

        victims.Remove(
            duplicate
        );


        Debug.Log(
            "VICTIM TRACKING: " +
            "Merged duplicate into Victim #" +
            keeper.id
        );


        // Make sure confirmed keeper has a marker.

        if (
            keeper.confirmed &&
            keeper.marker == null
        )
        {
            CreateMarker(
                keeper
            );
        }
    }


    // =========================================================
    // CREATE MARKER
    // =========================================================

    private void CreateMarker(
        Victim victim
    )
    {
        if (victim == null)
        {
            return;
        }


        // NEVER create two markers.

        if (
            victim.marker != null
        )
        {
            return;
        }


        if (
            detectionMarkerPrefab == null
        )
        {
            Debug.LogWarning(
                "VICTIM TRACKING: " +
                "Detection Marker Prefab is not assigned " +
                "on VictimTrackingManager."
            );

            return;
        }


        Vector3 markerPosition =
            victim.position +
            Vector3.up *
            markerHeight;


        victim.marker =
            Instantiate(
                detectionMarkerPrefab,
                markerPosition,
                Quaternion.identity
            );


        victim.marker.name =
            "VictimMarker_" +
            victim.id;


        Debug.Log(
            "VICTIM TRACKING: " +
            "Created marker for Victim #" +
            victim.id
        );
    }


    // =========================================================
    // REMOVE VICTIM
    // =========================================================

    private void RemoveVictim(
        Victim victim
    )
    {
        if (victim == null)
        {
            return;
        }


        if (
            victim.marker != null
        )
        {
            Destroy(
                victim.marker
            );
        }


        Debug.Log(
            "VICTIM TRACKING: " +
            "Removed Victim #" +
            victim.id
        );


        victims.Remove(
            victim
        );
    }


    // =========================================================
    // HORIZONTAL DISTANCE
    // =========================================================

    private float HorizontalDistance(
        Vector3 a,
        Vector3 b
    )
    {
        Vector2 aXZ =
            new Vector2(
                a.x,
                a.z
            );


        Vector2 bXZ =
            new Vector2(
                b.x,
                b.z
            );


        return Vector2.Distance(
            aXZ,
            bXZ
        );
    }


    // =========================================================
    // FORMAT POSITION
    // =========================================================

    private string FormatPosition(
        Vector3 position
    )
    {
        return
            "X: " +
            position.x.ToString("F2") +
            " | Y: " +
            position.y.ToString("F2") +
            " | Z: " +
            position.z.ToString("F2");
    }


    // =========================================================
    // PUBLIC ACCESS
    // =========================================================

    public List<Victim>
        GetVictims()
    {
        return victims;
    }


    public List<Victim>
        GetConfirmedVictims()
    {
        List<Victim>
            confirmedVictims =
            new List<Victim>();


        foreach (
            Victim victim
            in victims
        )
        {
            if (
                victim != null &&
                victim.confirmed
            )
            {
                confirmedVictims.Add(
                    victim
                );
            }
        }


        return confirmedVictims;
    }


    // =========================================================
    // CLEAR ALL
    // =========================================================

    public void ClearAllVictims()
    {
        for (
            int i = victims.Count - 1;
            i >= 0;
            i--
        )
        {
            Victim victim =
                victims[i];


            if (
                victim != null &&
                victim.marker != null
            )
            {
                Destroy(
                    victim.marker
                );
            }
        }


        victims.Clear();

        nextVictimId = 1;

        mergeTimer = 0f;


        Debug.Log(
            "VICTIM TRACKING: " +
            "All victims cleared."
        );
    }
}