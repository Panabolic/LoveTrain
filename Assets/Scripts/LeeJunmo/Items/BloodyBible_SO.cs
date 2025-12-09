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

    public GameObject ZonePrefab; // 장판 프리팹

    // 아이템 사용 시 호출되는 함수 (Active Item 로직)
    // 구조상 ItemInstance에서 호출하거나, 별도의 ActiveItem 로직에서 호출될 것입니다.
    public void UseSkill(GameObject user, ItemInstance instance)
    {
        if (ZonePrefab == null) return;

        // 1. 현재 레벨에 맞는 스탯 계산
        int levelIndex = Mathf.Clamp(instance.currentUpgrade - 1, 0, durationByLevel.Length - 1);

        float currentDuration = durationByLevel[levelIndex];
        float currentBuff = buffAmountByLevel[levelIndex];
        float currentCooldown = cooldownByLevel[levelIndex];

        // 2. 장판 생성 (플레이어 위치 또는 마우스 위치 등 기획에 따라 다름)
        // 여기서는 플레이어 위치에 생성한다고 가정
        GameObject zoneObj = Instantiate(ZonePrefab, user.transform.position, Quaternion.identity);

        // 3. 데이터 주입
        BloodyZone zoneLogic = zoneObj.GetComponent<BloodyZone>();
        if (zoneLogic != null)
        {
            // ✨ user(플레이어) 정보도 같이 넘겨서 충돌 체크에 활용
            zoneLogic.Initialize(instance, currentCooldown, currentDuration, currentBuff, user);
        }
    }

    // (참고) 툴팁용 정보 갱신
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