using System.Collections.Generic;
using UnityEngine;

public class EnemyRadar : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public RectTransform radarArea;
    public RectTransform markerContainer;
    public RectTransform playerMarker;
    public GameObject enemyMarkerPrefab;

    [Header("Radar Settings")]
    public float radarRange = 20f;
    public float markerEdgePadding = 10f;
    public float refreshInterval = 0.25f;

    private readonly Dictionary<Transform, RectTransform> markers = new Dictionary<Transform, RectTransform>();
    private readonly HashSet<Transform> currentEnemies = new HashSet<Transform>();
    private readonly List<Transform> removedMarkers = new List<Transform>();

    private float refreshTimer;

    void Start()
    {
        if (player == null ||
            radarArea == null ||
            markerContainer == null ||
            playerMarker == null ||
            enemyMarkerPrefab == null)
        {
            Debug.LogError("EnemyRadar: Assign all references in the Inspector.", this);
            enabled = false;
            return;
        }

        RefreshEnemies();
        UpdateMarkerPositions();
    }

    void Update()
    {
        if (player == null)
            return;

        refreshTimer += Time.deltaTime;

        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshEnemies();
        }

        UpdateMarkerPositions();
    }

    void LateUpdate()
    {
        if (player == null || playerMarker == null)
            return;

        // Rotate only the green player marker.
        playerMarker.localEulerAngles = new Vector3(
            0f,
            0f,
            -player.eulerAngles.y
        );
    }

    void RefreshEnemies()
    {
        currentEnemies.Clear();
        IReadOnlyList<EnemyRadarTarget> activeTargets = EnemyRadarTarget.ActiveTargets;

        for (int i = 0; i < activeTargets.Count; i++)
        {
            EnemyRadarTarget enemy = activeTargets[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            Transform enemyTransform = enemy.transform;
            if (enemyTransform == player)
                continue;

            currentEnemies.Add(enemyTransform);

            if (!markers.ContainsKey(enemyTransform))
            {
                GameObject marker = Instantiate(enemyMarkerPrefab, markerContainer);
                RectTransform markerRect = marker.GetComponent<RectTransform>();

                if (markerRect == null)
                {
                    Debug.LogError("Enemy marker prefab needs a RectTransform.");
                    Destroy(marker);
                    continue;
                }

                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.localScale = Vector3.one;

                markers.Add(enemyTransform, markerRect);
            }
        }

        removedMarkers.Clear();
        foreach (var entry in markers)
        {
            if (entry.Key == null || !currentEnemies.Contains(entry.Key))
            {
                if (entry.Value != null)
                    Destroy(entry.Value.gameObject);

                removedMarkers.Add(entry.Key);
            }
        }

        for (int i = 0; i < removedMarkers.Count; i++)
        {
            markers.Remove(removedMarkers[i]);
        }
    }

    void UpdateMarkerPositions()
    {
        if (radarArea == null || radarRange <= 0.001f || player == null)
            return;

        float halfWidth = radarArea.rect.width * 0.5f;
        float halfHeight = radarArea.rect.height * 0.5f;

        float usableRadiusX = Mathf.Max(0f, halfWidth - markerEdgePadding);
        float usableRadiusY = Mathf.Max(0f, halfHeight - markerEdgePadding);

        Vector3 playerPos = player.position;
        float invRange = 1f / radarRange;
        float rangeSqr = radarRange * radarRange;

        foreach (var entry in markers)
        {
            Transform enemy = entry.Key;
            RectTransform marker = entry.Value;

            if (enemy == null || marker == null)
                continue;

            Vector3 diff = enemy.position - playerPos;
            Vector2 direction = new Vector2(diff.x, diff.z);

            float sqrDist = direction.sqrMagnitude;
            if (sqrDist > rangeSqr)
            {
                direction = direction.normalized * radarRange;
            }

            Vector2 uiPosition = new Vector2(
                (direction.x * invRange) * usableRadiusX,
                (direction.y * invRange) * usableRadiusY
            );

            marker.anchoredPosition = uiPosition;
        }
    }
}
