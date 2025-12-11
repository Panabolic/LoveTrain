using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MagicBullet", menuName = "Items/MagicBullet")]
public class MagicBullet_SO : Item_SO
{
    [Header("마탄 데이터")]
    public int[] bounceCounts = { 1, 2, 4 };

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        return InstantiateVisual(user);
    }

    public override void OnDealDamage(GameObject user, GameObject target, GameObject source, ItemInstance instance)
    {
        IRicochetSource ricochetSource = source.GetComponent<IRicochetSource>();
        if (ricochetSource == null) return;

        int idx = Mathf.Clamp(instance.currentUpgrade - 1, 0, bounceCounts.Length - 1);
        if (ricochetSource.GetBounceDepth() >= bounceCounts[idx]) return;

        // 타겟의 위치 기준으로 가장 가까운 적 찾기
        Transform nextTarget = FindNearestEnemy(target.transform.position, target);

        if (nextTarget != null)
        {
            SpawnRicochet(ricochetSource, target.transform.position, nextTarget, target);
        }
    }

    private void SpawnRicochet(IRicochetSource source, Vector3 startPos, Transform target, GameObject hitEnemy)
    {
        GameObject prefab = source.GetRicochetPrefab();
        if (prefab == null) return;

        Vector3 direction = (target.position - startPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        GameObject obj = Instantiate(prefab, startPos, rotation);

        Projectile proj = obj.GetComponent<Projectile>();
        if (proj != null)
        {
            // 1. 데미지 절반 감소
            float newDamage = source.GetDamage() * 0.35f;

            // 2. 기본 속도 가져오기
            float newSpeed = source.GetSpeed();

            // 레이저 등 속도가 0인 경우 프리팹 기본값 사용
            if (newSpeed <= 0.1f)
            {
                newSpeed = proj.GetSpeed();
                if (newSpeed <= 0f) newSpeed = 15f;
            }

            // 원본(0번)에서 튕길 때만 2배, 그 이후는 속도 유지
            if (source.GetBounceDepth() == 0)
            {
                newSpeed *= 2f;
            }

            // 3. 충돌 무시 설정
            Collider2D ignoreCol = hitEnemy.GetComponent<Collider2D>();

            // 투사체 초기화
            proj.Init(newDamage, newSpeed, direction, prefab, true, source.GetBounceDepth() + 1, ignoreCol);
        }
    }

    private Transform FindNearestEnemy(Vector3 origin, GameObject ignoreTarget)
    {
        Transform bestTarget = null;
        float closestDistSqr = Mathf.Infinity;

        if (PoolManager.instance != null)
        {
            foreach (Enemy enemy in PoolManager.instance.activeEnemies)
            {
                // ✨ [핵심 수정] 타겟팅 불가능한 적(사망, 화면 밖)은 제외
                if (enemy == null || !enemy.IsTargetable) continue;

                // 방금 맞은 적은 제외 (자기 자신에게 다시 튀지 않도록)
                if (enemy.gameObject == ignoreTarget) continue;

                float dSqr = (enemy.transform.position - origin).sqrMagnitude;
                if (dSqr < closestDistSqr)
                {
                    closestDistSqr = dSqr;
                    bestTarget = enemy.transform;
                }
            }
        }
        return bestTarget;
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, bounceCounts.Length - 1);
        return new Dictionary<string, string> { { "Bounce", bounceCounts[idx].ToString() } };
    }
}