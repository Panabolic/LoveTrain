using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BloodyBible", menuName = "Items/BloodyBible")]
public class BloodyBible_SO : Item_SO
{
    [Header("피의 성경 밸런스 설정")]
    [Tooltip("장판 지속 시간 (레벨별)")]
    public float[] durationByLevel = { 5.0f, 6.0f, 7.0f };

    [Tooltip("공격 속도 증가량 (0.5 = 50%, 1.0 = 100%)")]
    public float[] buffAmountByLevel = { 0.5f, 0.7f, 1.0f };

    [Tooltip("스킬 쿨타임 (레벨별)")]
    public float[] cooldownByLevel = { 15.0f, 14.0f, 12.0f };

    [Header("소환 설정")]
    [Tooltip("월드 좌표 기준 좌우 랜덤 소환 범위 (예: 3.0이면 월드 X좌표 -3.0 ~ +3.0 사이 랜덤)")]
    [SerializeField] private float spawnRangeX = 3.0f;

    public GameObject ZonePrefab;

    // 수동 쿨타임 설정 (장판 파괴 시 시작)
    public override bool IsManualCooldown => true;

    public override void OnCooldownComplete(GameObject user, ItemInstance instance)
    {
        if (ZonePrefab == null) return;

        // 1. 스탯 계산
        int levelIndex = Mathf.Clamp(instance.currentUpgrade - 1, 0, durationByLevel.Length - 1);

        float currentDuration = durationByLevel[levelIndex];
        float currentBuff = buffAmountByLevel[levelIndex];
        float currentCooldown = cooldownByLevel[levelIndex];

        // 2. ✨ [수정] 월드 좌표 기준 랜덤 위치 계산
        // Y와 Z는 플레이어(기차)의 높이/깊이를 따라가되, X는 월드 절대좌표를 사용
        Vector3 spawnPos = user.transform.position;
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);

        spawnPos.x = randomX; // += 가 아니라 = 로 변경하여 절대 위치 적용

        // 3. 장판 생성
        GameObject zoneObj = Instantiate(ZonePrefab, spawnPos, Quaternion.identity);

        SoundEventBus.Publish(SoundID.Item_Bible);
        // 4. 데이터 주입
        BloodyZone zoneLogic = zoneObj.GetComponent<BloodyZone>();
        if (zoneLogic != null)
        {
            zoneLogic.Initialize(instance, currentCooldown, currentDuration, currentBuff, user);
        }
    }

    /// <summary>
    /// 이 아이템의 현재 레벨에 맞는 쿨타임을 Inventory에게 알려줍니다.
    /// </summary>
    public override float GetCooldownForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, cooldownByLevel.Length - 1);
        return cooldownByLevel[index];
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, durationByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Duration", durationByLevel[index].ToString() },
            { "Buff", (buffAmountByLevel[index] * 100).ToString() + "%" },
            { "Cooldown", cooldownByLevel[index].ToString() }
        };
    }
}