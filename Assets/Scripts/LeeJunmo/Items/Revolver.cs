using UnityEngine;

public class Revolver : MonoBehaviour, IInstantiatedItem
{
    //아이템 SO데이터 저장 변수
    private Revolver_SO itemData;

    //현재 스탯
    private int currentDamage;
    private int currentBulletNum;
    private float currentCooldown;

    /// <summary>
    /// [추가] OnEquip에서 SO가 호출할 초기화 함수
    /// </summary>
    public void Initialize(Revolver_SO so)
    {
        this.itemData = so;
    }

    private void Update()
    {
        
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        if (itemData == null) return;

        int levelIndex = instance.currentUpgrade - 1;

        // SO 데이터로 이 MonoBehaviour의 스탯을 갱신
        this.currentDamage = itemData.damageByLevel[levelIndex];
        this.currentBulletNum = itemData.bulletNumByLevel[levelIndex];
        this.currentCooldown = itemData.cooldownByLevel[levelIndex];
    }
}
