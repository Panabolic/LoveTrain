using UnityEngine;

public interface IRicochetSource
{
    GameObject GetRicochetPrefab();
    int GetBounceDepth();
    void SetBounceDepth(int depth);

    // ✨ [추가] 스탯 계승을 위한 Getter
    float GetDamage();
    float GetSpeed();
}