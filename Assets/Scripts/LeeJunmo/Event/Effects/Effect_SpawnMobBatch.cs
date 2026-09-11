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

        if (prefab == null) return EnglishLocalization.Get("result.error.monster_missing", "오류: 몬스터 프리팹이 없습니다.");
        if (count <= 0) count = 1;
        if (delay <= 0.05f) delay = 0.2f;

        Spawner spawner = Spawner.Instance;
        if (spawner == null) spawner = FindFirstObjectByType<Spawner>();

        if (spawner != null)
        {
            spawner.SpawnMobBatch(prefab, count, delay, isFly);

            string typeText = isFly ? EnglishLocalization.Get("result.type.flying", "공중") : EnglishLocalization.Get("result.type.ground", "지상");
            return EnglishLocalization.Format("result.monsters_spawned", "{0} 몬스터 출현! ({1} x{2})", typeText, prefab.name, count);
        }
        return EnglishLocalization.Get("result.error.spawner", "Spawner 오류");
    }
}