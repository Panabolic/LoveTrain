using UnityEngine;

public class Revolver_SO : Item_SO
{
    [Header("리볼버 전용 데이터")]
    public int[] damageByLevel = { 200, 200, 500 };
    public int[] bulletNumByLevel = { 1, 2, 3 };
    public float[] cooldownByLevel = { 10f, 7f, 7f };

    [Header("리볼버 본체")]
    public GameObject revolverPrefab;

    [Header("탄환")]
    public GameObject BulletPrefab;


    public override GameObject OnEquip(GameObject user, ItemInstance instance)
    {
        if (revolverPrefab == null) return null;

        GameObject tempRevolver = Instantiate(revolverPrefab, user.transform);

        // 1. 실체화된 로직을 가져옵니다.
        Revolver logic = tempRevolver.GetComponent<Revolver>();
        if (logic == null)
        {
            Debug.LogError($"{revolverPrefab.name}에 revolverPrefab.cs가 없습니다!");
            return tempRevolver;
        }

        // 2. [핵심] 로직에게 "네 원본 데이터(SO)는 'this'야"라고 알려줍니다.
        logic.Initialize(this);

        // 3. 1레벨 스탯을 적용하도록 업그레이드 함수를 즉시 호출합니다.
        logic.UpgradeInstItem(instance);

        return tempRevolver;
    }
}
