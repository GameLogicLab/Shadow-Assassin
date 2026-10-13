
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

    private readonly Dictionary<Transform, RectTransform>
        markers = new Dictionary<Transform, RectTransform>();

    private float refreshTimer;

    void Start()
    {
        if (player == null ||
            radarArea == null ||
            markerContainer == null ||
            playerMarker == null ||
            enemyMarkerPrefab == null)
        {
            Debug.LogError(
                "EnemyRadar: Assign all references in the Inspector.",
                this
            );

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
        // Assumes the arrow points upward by default.
        playerMarker.localEulerAngles = new Vector3(
            0f,
            0f,
            -player.eulerAngles.y
        );
    }

    void RefreshEnemies()
    {
        EnemyRadarTarget[] enemies =
            FindObjectsByType<EnemyRadarTarget>(
                FindObjectsSortMode.None
            );

        HashSet<Transform> currentEnemies =
            new HashSet<Transform>();

        foreach (EnemyRadarTarget enemy in enemies)
        {
            if (enemy == null ||
                !enemy.gameObject.activeInHierarchy)
                continue;

            Transform enemyTransform = enemy.transform;

            if (enemyTransform == player)
                continue;

            currentEnemies.Add(enemyTransform);

            if (!markers.ContainsKey(enemyTransform))
            {
                GameObject marker = Instantiate(
                    enemyMarkerPrefab,
                    markerContainer
                );

                RectTransform markerRect =
                    marker.GetComponent<RectTransform>();

                if (markerRect == null)
                {
                    Debug.LogError(
                        "Enemy marker prefab needs a RectTransform."
                    );

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

        List<Transform> removed = new List<Transform>();

        foreach (var entry in markers)
        {
            if (entry.Key == null ||
                !currentEnemies.Contains(entry.Key))
            {
                if (entry.Value != null)
                    Destroy(entry.Value.gameObject);

                removed.Add(entry.Key);
            }
        }

        foreach (Transform enemy in removed)
        {
            markers.Remove(enemy);
        }
    }

    void UpdateMarkerPositions()
    {
        if (radarArea == null || radarRange <= 0f)
            return;

        float halfWidth = radarArea.rect.width * 0.5f;
        float halfHeight = radarArea.rect.height * 0.5f;

        float usableRadiusX = Mathf.Max(
            0f,
            halfWidth - markerEdgePadding
        );

        float usableRadiusY = Mathf.Max(
            0f,
            halfHeight - markerEdgePadding
        );

        foreach (var entry in markers)
        {
            Transform enemy = entry.Key;
            RectTransform marker = entry.Value;

            if (enemy == null || marker == null)
                continue;

            // Enemy position relative to player.
            Vector3 difference =
                enemy.position - player.position;

            // Ignore height differences.
            difference.y = 0f;

            // FIXED RADAR:
            // Do not rotate this direction by the player's rotation.
            Vector2 direction = new Vector2(
                difference.x,
                difference.z
            );

            // Keep distant enemies at the radar edge.
            float distance = direction.magnitude;

            if (distance > radarRange)
            {
                direction =
                    direction.normalized * radarRange;
            }

            // Convert world position to radar UI position.
            Vector2 uiPosition = new Vector2(
                direction.x / radarRange * usableRadiusX,
                direction.y / radarRange * usableRadiusY
            );

            marker.anchoredPosition = uiPosition;
        }
    }
}
