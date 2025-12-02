using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BloodyBible", menuName = "Items/BloodyBible")]
public class BloodyBible_SO : Item_SO
{
    [Header("피의 성서 데이터")]
    [Tooltip("레벨별 쿨타임 (장판이 사라진 후부터 적용될 시간)")]
    public float[] cooldownByLevel = { 15f, 12f, 10f };

    [Header("생성 설정")]
    [Tooltip("맵의 X축 생성 범위")]
    public float spawnRangeX = 8.0f;

    [Header("프리팹")]
    public GameObject ZonePrefab;

    // 아이템 사용 시 호출되는 메인 함수
    public override void OnCooldownComplete(GameObject user, ItemInstance instance)
    {
        if (ZonePrefab == null) return;

        // 1. 랜덤 위치 계산 (절대 좌표 X)
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        Vector3 spawnPos = new Vector3(randomX, -8.65f, 0f);

        // 2. 장판 생성
        GameObject zoneObj = Instantiate(ZonePrefab, spawnPos, Quaternion.identity);

        // 3. 다음 쿨타임 시간 계산
        float nextCooldown = GetCooldownForLevel(instance.currentUpgrade);

        // 4. ✨ 장판에게 데이터 전달 (아이템 인스턴스 + 쿨타임 시간)
        BloodyZone zoneScript = zoneObj.GetComponent<BloodyZone>();
        if (zoneScript != null)
        {
            zoneScript.Initialize(instance, nextCooldown);
        }

        // ⚠️ 주의: 여기서 시스템이 자동으로 instance.currentCooldown을 설정하지 않도록 해야 합니다.
        // (만약 부모 클래스 로직이 강제로 쿨타임을 돌린다면, 여기서 instance.currentCooldown = 0 등으로 막아야 함)
    }

    public override float GetCooldownForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, cooldownByLevel.Length - 1);
        return cooldownByLevel[index];
    }

    // 툴팁 등 표시용
    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, cooldownByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Cooldown", cooldownByLevel[index].ToString() }
        };
    }

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        // 장착 시 비주얼이 필요하다면 사용 (필요 없으면 null 리턴)
        return InstantiateVisual(user);
    }
}