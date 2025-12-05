using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BloodyBible", menuName = "Items/BloodyBible")]
public class BloodyBible_SO : Item_SO
{
    [Header("피의 성서 데이터")]
    [Tooltip("장판이 사라진 후 적용될 쿨타임")]
    public float[] cooldownByLevel = { 15f, 12f, 10f };

    [Header("생성 설정")]
    [Tooltip("맵의 X축 생성 범위")]
    public float spawnRangeX = 8.0f;

    [Header("프리팹")]
    public GameObject ZonePrefab;

    // ✨ 중요: 이 아이템은 수동으로 쿨타임을 관리한다고 선언
    public override bool IsManualCooldown => true;

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

        // 1. 랜덤 위치 계산 (절대 좌표 X, 높이는 사용자 Y)
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        Vector3 spawnPos = new Vector3(randomX, -8.65f, 0f);

        // 2. 장판 생성
        GameObject zoneObj = Instantiate(ZonePrefab, spawnPos, Quaternion.identity);

        // 3. 실제 적용할 쿨타임 값 가져오기
        float realCooldown = GetCooldownForLevel(instance.currentUpgrade);

        // 4. ✨ 장판에게 "네가 사라지면 이 아이템 쿨타임을 시작해라"고 전달
        BloodyZone zoneScript = zoneObj.GetComponent<BloodyZone>();
        if (zoneScript != null)
        {
            zoneScript.Initialize(instance, realCooldown);
        }

        Debug.Log($"[{itemName}] 장판 생성됨. (쿨타임 대기 중)");
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