using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BeatingHeart", menuName = "Items/BeatingHeart")]
public class BeatingHeart_SO : Item_SO
{
    [Header("박동하는 심장 전용 데이터")]
    public int[] damageByLevel = { 200, 200, 500 };
    public float[] cooldownByLevel = { 10f, 7f, 7f };

    [Header("시각 효과")]
    public GameObject EffectPrefab;

    /// <summary>
    /// [추가] '박동하는 심장'도 비주얼이 있으므로 OnEquip 재정의
    /// </summary>
    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        // 1. 부모의 공통 함수를 호출해 '시각적' 프리팹만 생성
        GameObject visualGO = InstantiateVisual(user);

        // 2. ItemInstance가 참조할 수 있도록 반환
        return visualGO;
    }

    /// <summary>
    /// 이 아이템의 현재 레벨에 맞는 쿨타임을 Inventory에게 알려줍니다.
    /// </summary>
    public override float GetCooldownForLevel(int level)
    {
        // 배열 범위를 벗어나지 않도록 Clamp (안전장치)
        int index = Mathf.Clamp(level - 1, 0, cooldownByLevel.Length - 1);
        return cooldownByLevel[index];
    }

    /// <summary>
    /// 쿨타임이 완료될 때마다 Inventory에 의해 호출됩니다.
    /// </summary>
    public override void OnCooldownComplete(GameObject user, ItemInstance instance)
    {
        // 1. 데미지 계산 (기존과 동일)
        int level = instance.currentUpgrade;
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);
        int currentDamage = damageByLevel[index];

        // --- 2. "화면 내" 적 공격 (PoolManager 참조로 수정) ---
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // **성능UP**: 씬 전체가 아닌, '활성화된 적 리스트'만 가져옵니다!
        if (PoolManager.instance == null) return;
        List<Enemy> activeEnemies = PoolManager.instance.activeEnemies;
        // --- 수정 끝 ---

        int hitCount = 0;

        // 가장 빠르고 정확한 리스트를 순회합니다.
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];

            // (enemy.isAlive 체크는 이미 Enemy.cs의 OnDisable에서 처리되지만
            //  이중으로 체크해서 나쁠 건 없습니다)
            if (enemy == null || !enemy.GetIsAlive())
            {
                continue;
            }

            // "화면 안에 있는가?" 체크
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(enemy.transform.position);

            bool isVisibleOnScreen = viewportPos.z > 0 &&
                                     viewportPos.x >= 0 && viewportPos.x <= 1 &&
                                     viewportPos.y >= 0 && viewportPos.y <= 1;

            if (isVisibleOnScreen)
            {
                enemy.TakeDamage(currentDamage);
                hitCount++;
            }
        }

        Debug.Log($"[{itemName}] (Lv.{level}) 발동! {hitCount}명의 보이는 적 공격!");

        // 3. 이펙트 소환 (기존과 동일)
        if (EffectPrefab != null)
        {
            Instantiate(EffectPrefab, user.transform.position, Quaternion.identity);
        }
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() },
            { "CoolTime", cooldownByLevel[index].ToString() }
        };
    }

}