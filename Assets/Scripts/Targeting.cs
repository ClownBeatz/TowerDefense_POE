using System.Collections.Generic;
using UnityEngine;

/// <summary>Finds targets in range.</summary>
public static class Targeting
{
    public static Collider FindClosest(Vector3 origin, float range, params string[] tags)
    {
        Collider closest = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in Physics.OverlapSphere(origin, range))
        {
            if (!IsLiveTarget(hit, tags)) continue;

            float dist = (hit.transform.position - origin).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit;
            }
        }

        return closest;
    }

    public static List<IDamageable> FindAll(Vector3 origin, float radius, params string[] tags)
    {
        List<IDamageable> results = new List<IDamageable>();

        foreach (Collider hit in Physics.OverlapSphere(origin, radius))
        {
            if (IsLiveTarget(hit, tags))
                results.Add(hit.GetComponent<IDamageable>());
        }

        return results;
    }

    private static bool IsLiveTarget(Collider hit, string[] tags)
    {
        bool tagMatches = false;
        foreach (string tag in tags)
        {
            if (hit.CompareTag(tag))
            {
                tagMatches = true;
                break;
            }
        }

        if (!tagMatches) return false;

        IDamageable damageable = hit.GetComponent<IDamageable>();
        return damageable != null && !damageable.IsDead;
    }
}
