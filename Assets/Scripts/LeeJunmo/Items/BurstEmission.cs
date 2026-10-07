using System;
using System.Collections;
using UnityEngine;

/// <summary>Reusable emission loops; delegates intentionally read the current upgrade values at each shot.</summary>
internal static class BurstEmission
{
    public static IEnumerator Sequence(Func<int> count, Action emit, Func<float> delay, bool waitAfterLast,
        Action begin = null, Action complete = null)
    {
        begin?.Invoke();
        for (int index = 0; index < count(); index++)
        {
            emit();
            if (waitAfterLast || index < count() - 1) yield return new WaitForSeconds(delay());
        }
        complete?.Invoke();
    }

    public static void Fan(int count, float spreadAngle, Action<float> emit)
    {
        if (count <= 1) { emit(0f); return; }
        float first = -((count - 1) * spreadAngle) / 2f;
        for (int index = 0; index < count; index++) emit(first + index * spreadAngle);
    }
}
