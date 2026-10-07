using System;
using System.Collections.Generic;
using UnityEngine;

internal static class TargetQuery
{
    public static TargetHandle Nearest(IEnumerable<TargetHandle> targets, Vector3 origin,
        Func<TargetHandle, bool> eligible, GameObject excluded = null)
    {
        TargetHandle selected = null;
        float distance = Mathf.Infinity;
        foreach (TargetHandle target in targets)
        {
            if (target == null || target.Owner == null || target.Owner == excluded || !eligible(target)) continue;
            float candidateDistance = (target.Position - origin).sqrMagnitude;
            if (candidateDistance < distance)
            {
                distance = candidateDistance;
                selected = target;
            }
        }
        return selected;
    }

    public static bool VisibleAlive(TargetHandle target, Camera camera)
    {
        if (target == null || !target.IsActive || !target.IsAlive) return false;
        Vector3 viewport = camera.WorldToViewportPoint(target.Position);
        return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }

    public static TargetHandle RandomVisible(IEnumerable<TargetHandle> targets, Camera camera)
    {
        var candidates = new List<TargetHandle>();
        foreach (TargetHandle target in targets) if (VisibleAlive(target, camera)) candidates.Add(target);
        return candidates.Count == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    public static bool AnyVisible(IEnumerable<TargetHandle> targets, Camera camera)
    {
        foreach (TargetHandle target in targets) if (VisibleAlive(target, camera)) return true;
        return false;
    }
}
