using UnityEngine;

[CreateAssetMenu(fileName = "Effect_SpawnMobBatch", menuName = "Event System/Effects/Enemy/Spawn Mob Batch (Prefab)")]
public class Effect_SpawnMobBatch : GameEffectSO
{
    public override string Execute(GameObject target, EffectParameters parameters)
    {
        GameObject prefab = parameters.prefabReference; // 프리팹
        int count = parameters.intValue;                // 수량
        float delay = parameters.floatValue;            // 딜레이
        bool isFly = parameters.boolValue;              // 공중 여부

        if (prefab == null) return "오류: 몬스터 프리팹이 없습니다.";
        if (count <= 0) count = 1;
        if (delay <= 0.05f) delay = 0.2f;

        Spawner spawner = Spawner.Instance;
        if (spawner == null) spawner = FindObjectOfType<Spawner>();

        if (spawner != null)
        {
            spawner.SpawnMobBatch(prefab, count, delay, isFly);

            string typeText = isFly ? "공중" : "지상";
            return $"{typeText} 몬스터 출현! ({prefab.name} x{count})";
        }
        return "Spawner 오류";
    }
}