using UnityEngine;
using System.Collections.Generic;

public class RescueMapController : MonoBehaviour
{
    [Header("Map")]
    public RectTransform mapArea;

    [Header("Victim Tracking")]
    public VictimTrackingManager victimTrackingManager;

    [Header("Victim Marker")]
    public GameObject victimMapMarkerPrefab;

    [Header("Drone Marker")]
    public GameObject droneMapMarkerPrefab;

    [Header("Drones")]
    public Transform[] drones;

    [Header("World Map Range")]
    public float worldMinX = -30f;
    public float worldMaxX = 30f;

    public float worldMinZ = -30f;
    public float worldMaxZ = 30f;

    private readonly Dictionary<int, GameObject> victimMarkers =
        new Dictionary<int, GameObject>();

    private readonly List<GameObject> droneMarkers =
        new List<GameObject>();

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        CreateDroneMarkers();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdateVictimMarkers();
        UpdateDroneMarkers();
    }

    // =========================================================
    // VICTIMS
    // =========================================================

    private void UpdateVictimMarkers()
    {
        if (victimTrackingManager == null ||
            mapArea == null)
        {
            return;
        }

        // IMPORTANT:
        // Only confirmed victims are displayed.
        // Unconfirmed YOLO candidates are NOT shown on the map.
        List<VictimTrackingManager.Victim> victims =
            victimTrackingManager.GetConfirmedVictims();

        HashSet<int> activeVictimIds =
            new HashSet<int>();

        foreach (VictimTrackingManager.Victim victim in victims)
        {
            activeVictimIds.Add(victim.id);

            if (!victimMarkers.ContainsKey(victim.id))
            {
                CreateVictimMarker(victim);
            }

            UpdateVictimMarkerPosition(victim);
        }

        // Remove map markers for victims that no longer exist.
        List<int> idsToRemove =
            new List<int>();

        foreach (KeyValuePair<int, GameObject> pair in victimMarkers)
        {
            if (!activeVictimIds.Contains(pair.Key))
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }

                idsToRemove.Add(pair.Key);
            }
        }

        foreach (int id in idsToRemove)
        {
            victimMarkers.Remove(id);
        }
    }

    private void CreateVictimMarker(
        VictimTrackingManager.Victim victim)
    {
        if (victimMapMarkerPrefab == null)
        {
            Debug.LogWarning(
                "Victim Map Marker Prefab is not assigned."
            );

            return;
        }

        GameObject marker =
            Instantiate(
                victimMapMarkerPrefab,
                mapArea
            );

        marker.name =
            "VictimMapMarker_" + victim.id;

        victimMarkers.Add(
            victim.id,
            marker
        );
    }

    private void UpdateVictimMarkerPosition(
        VictimTrackingManager.Victim victim)
    {
        if (!victimMarkers.TryGetValue(
            victim.id,
            out GameObject marker))
        {
            return;
        }

        if (marker == null)
        {
            victimMarkers.Remove(victim.id);
            return;
        }

        RectTransform rect =
            marker.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        Vector2 mapPosition =
            WorldToMapPosition(victim.position);

        rect.anchoredPosition =
            mapPosition;
    }

    // =========================================================
    // DRONES
    // =========================================================

    private void CreateDroneMarkers()
    {
        if (droneMapMarkerPrefab == null)
        {
            Debug.LogWarning(
                "Drone Map Marker Prefab is not assigned."
            );

            return;
        }

        if (drones == null)
        {
            return;
        }

        foreach (Transform drone in drones)
        {
            if (drone == null)
            {
                continue;
            }

            GameObject marker =
                Instantiate(
                    droneMapMarkerPrefab,
                    mapArea
                );

            marker.name =
                drone.name + "_MapMarker";

            droneMarkers.Add(marker);
        }
    }

    private void UpdateDroneMarkers()
    {
        if (drones == null)
        {
            return;
        }

        for (int i = 0; i < drones.Length; i++)
        {
            if (drones[i] == null)
            {
                continue;
            }

            if (i >= droneMarkers.Count)
            {
                continue;
            }

            GameObject marker =
                droneMarkers[i];

            if (marker == null)
            {
                continue;
            }

            RectTransform rect =
                marker.GetComponent<RectTransform>();

            if (rect == null)
            {
                continue;
            }

            Vector2 mapPosition =
                WorldToMapPosition(
                    drones[i].position
                );

            rect.anchoredPosition =
                mapPosition;
        }
    }

    // =========================================================
    // WORLD -> MAP
    // =========================================================

    private Vector2 WorldToMapPosition(
        Vector3 worldPosition)
    {
        if (mapArea == null)
        {
            return Vector2.zero;
        }

        float normalizedX =
            Mathf.InverseLerp(
                worldMinX,
                worldMaxX,
                worldPosition.x
            );

        float normalizedZ =
            Mathf.InverseLerp(
                worldMinZ,
                worldMaxZ,
                worldPosition.z
            );

        float mapX =
            (normalizedX - 0.5f) *
            mapArea.rect.width;

        float mapY =
            (normalizedZ - 0.5f) *
            mapArea.rect.height;

        return new Vector2(
            mapX,
            mapY
        );
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        foreach (GameObject marker in victimMarkers.Values)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        victimMarkers.Clear();

        foreach (GameObject marker in droneMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        droneMarkers.Clear();
    }
}
