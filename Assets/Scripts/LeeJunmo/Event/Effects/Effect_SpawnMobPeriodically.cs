using UnityEngine;

[CreateAssetMenu(fileName = "Effect_SpawnMobPeriodically", menuName = "Event System/Effects/Enemy/Spawn Mob Periodically (Forever)")]
public class Effect_SpawnMobPeriodically : GameEffectSO
{
    public override string Execute(GameObject target, EffectParameters parameters)
    {
        GameObject prefab = parameters.prefabReference; // 프리팹
        float interval = parameters.floatValue;         // 주기
        bool isFly = parameters.boolValue;              // 공중 여부
        // duration 미사용 (영구 지속)

        if (prefab == null) return EnglishLocalization.Get("result.error.monster_missing", "오류: 몬스터 프리팹이 없습니다.");
        if (interval <= 0.1f) interval = 1.0f;

        Spawner spawner = Spawner.Instance;
        if (spawner == null) spawner = FindObjectOfType<Spawner>();

        if (spawner != null)
        {
            // 영구 스폰 리스트에 추가
            spawner.AddPeriodicSpawnTask(prefab, interval, isFly);

            string typeText = isFly ? EnglishLocalization.Get("result.type.sky", "하늘") : EnglishLocalization.Get("result.type.ground", "지상");
            return EnglishLocalization.Format("result.periodic_monsters", "이제부터 {0}초마다 {1}에서 {2}이(가) 나타납니다!", interval, typeText, prefab.name);
        }
        return EnglishLocalization.Get("result.error.spawner", "Spawner 오류");
    }
}