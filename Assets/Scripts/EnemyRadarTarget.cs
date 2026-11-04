using System.Collections.Generic;
using UnityEngine;

public class EnemyRadarTarget : MonoBehaviour
{
    private static readonly List<EnemyRadarTarget> s_ActiveTargets = new List<EnemyRadarTarget>();
    public static IReadOnlyList<EnemyRadarTarget> ActiveTargets => s_ActiveTargets;

    private void OnEnable()
    {
        if (!s_ActiveTargets.Contains(this))
        {
            s_ActiveTargets.Add(this);
        }
    }

    private void OnDisable()
    {
        s_ActiveTargets.Remove(this);
    }
}
