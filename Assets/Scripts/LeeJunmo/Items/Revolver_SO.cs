using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Revolver", menuName = "Items/Revolver")]
public class Revolver_SO : Item_SO
{
    [Header("리볼버 전용 데이터")]
    public int[] damageByLevel = { 200, 200, 500 };
    public int[] bulletNumByLevel = { 1, 2, 3 };
    public float[] cooldownByLevel = { 10f, 7f, 7f };

    [Header("탄환")]
    public GameObject BulletPrefab;

    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        // 1. 부모의 공통 함수를 호출해 '로직+시각' 프리팹 생성
        GameObject revolverGO = InstantiateVisual(user);
        if (revolverGO == null) return null;

        // --- 여기서부터 'Revolver'만의 추가 로직 ---
        Revolver logic = revolverGO.GetComponent<Revolver>();
        if (logic == null)
        {
            Debug.LogError($"{instantiatedPrefab.name}에 Revolver.cs가 없습니다!");
            return revolverGO;
        }

        logic.Initialize(this);
        logic.UpgradeInstItem(instance);
        // --- 추가 로직 끝 ---

        return revolverGO;
    }

    protected override Dictionary<string, string> GetStatReplacements(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, damageByLevel.Length - 1);

        return new Dictionary<string, string>
        {
            { "Damage", damageByLevel[index].ToString() },
            { "BulletNum", bulletNumByLevel[index].ToString() },
            { "CoolTime", cooldownByLevel[index].ToString() }
        };
    }

}
