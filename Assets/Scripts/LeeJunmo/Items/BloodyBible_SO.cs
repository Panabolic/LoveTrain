using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BloodyBible", menuName = "Items/BloodyBible")]
public class BloodyBible_SO : Item_SO
{
    [Header("피의 성서 데이터")]
    [Tooltip("레벨별 쿨타임")]
    public float[] cooldownByLevel = { 15f, 12f, 10f };

    [Header("생성 설정")]
    [Tooltip("맵의 X축 생성 범위 (예: 8로 설정하면 -8 ~ +8 사이의 절대 좌표에서 랜덤 생성)")]
    public float spawnRangeX = 8.0f; // ✨ [수정] 플레이어 위치 무관한 절대 범위

    [Header("프리팹")]
    public GameObject ZonePrefab;

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        return InstantiateVisual(user);
    }

    public override float GetCooldownForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, cooldownByLevel.Length - 1);
        return cooldownByLevel[index];
    }

    public override void OnCooldownComplete(GameObject user, ItemInstance instance)
    {
        if (ZonePrefab == null) return;

        // ✨ [핵심 수정] 플레이어 X위치를 더하지 않음!
        // 맵의 절대적인 범위 (-Range ~ +Range) 내에서 무작위 X 결정
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);

        // Y축은 플레이어(기차)와 동일한 높이 유지 (선로 위)
        Vector3 spawnPos = new Vector3(randomX, user.transform.position.y, 0f);

        // 장판 생성
        Instantiate(ZonePrefab, spawnPos, Quaternion.identity);

        Debug.Log($"[{itemName}] 장판 생성! 절대 위치 X: {randomX:F2}");
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, cooldownByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Cooldown", cooldownByLevel[index].ToString() }
        };
    }
}