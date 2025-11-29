using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoisonMissileLauncher", menuName = "Items/PoisonMissileLauncher")]
public class PoisonMissileLauncher_SO : Item_SO
{
    [Header("미사일 포트 스탯")]
    public float[] gasDamageByLevel = { 5f, 8f, 12f };     // 틱당 데미지
    public float[] gasTickRateByLevel = { 0.5f, 0.5f, 0.5f }; // 데미지 주기
    public float[] cooldownByLevel = { 5f, 4.5f, 4f };     // 발사 쿨타임

    [Header("미사일 및 독가스 설정")]
    public float missileSpeed = 8f;
    public float verticalDistance = 3f; // 솟구치는 높이

    [Tooltip("독가스가 왼쪽으로 흘러가는 속도 (배경 속도와 맞추면 좋음)")]
    public float gasMoveSpeed = 3f;

    [Header("프리팹 연결")]
    public GameObject MissilePrefab;
    public GameObject GasPrefab;

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        // 1. 비주얼 생성
        GameObject obj = InstantiateVisual(user);
        if (obj == null) return null;

        // 2. 로직 컴포넌트 확인 및 추가
        PoisonMissileLauncher logic = obj.GetComponent<PoisonMissileLauncher>();
        if (logic == null) logic = obj.AddComponent<PoisonMissileLauncher>();

        // 3. 초기화
        logic.Initialize(this, user);
        logic.UpgradeInstItem(instance);

        return obj;
    }

    // 툴팁용 텍스트 변환
    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, gasDamageByLevel.Length - 1);
        return new Dictionary<string, string>
        {
            { "Damage", gasDamageByLevel[index].ToString() },
            { "Cooldown", cooldownByLevel[index].ToString() }
        };
    }
}