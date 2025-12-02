using UnityEngine;

[System.Serializable]
public class ItemInstance
{
    public Item_SO itemData;
    public float currentCooldown;
    public float maxCooldown;
    private GameObject instantiatedObject = null; // 실체화된 오브젝트

    public int currentUpgrade = 1;

    public ItemInstance(Item_SO data)
    {
        itemData = data;
        currentUpgrade = 1;
        // 처음 생성 시 쿨타임 적용 (게임 시작 시 바로 발사 안 되게 하려면 값 유지, 바로 쏘려면 0)
        currentCooldown = itemData.GetCooldownForLevel(currentUpgrade);
    }

    public void HandleEquip(GameObject user)
    {
        instantiatedObject = itemData.OnEquip(user, this);
    }

    // ✨ [수정됨] 쿨타임 로직
    public void Tick(float deltaTime, GameObject user)
    {
        // 쿨타임이 없는 아이템은 무시 (GetCooldownForLevel이 0이면 패시브 등으로 간주)
        float levelMaxCooldown = itemData.GetCooldownForLevel(currentUpgrade);
        if (levelMaxCooldown <= 0f) return;

        // 쿨타임이 0보다 클 때만 줄임 (대기 상태일 때는 로직이 돌지 않음)
        if (currentCooldown > 0f)
        {
            currentCooldown -= deltaTime;

            // 쿨타임 완료!
            if (currentCooldown <= 0f)
            {
                // 1. 효과 발동 (장판 생성 등)
                itemData.OnCooldownComplete(user, this);

                // 2. ✨ 쿨타임 처리 분기
                if (itemData.IsManualCooldown)
                {
                    // [수동 모드] 시스템이 쿨타임을 리셋하지 않음.
                    // 대신 무한대 값(Wait 상태)으로 보내서 StartCooldownManual이 불릴 때까지 대기
                    currentCooldown = float.MaxValue;
                }
                else
                {
                    // [자동 모드] 기존 아이템들처럼 즉시 쿨타임 리셋
                    currentCooldown = levelMaxCooldown;
                }
            }
        }
    }

    // ✨ [추가됨] 외부(장판)에서 호출하여 쿨타임을 강제로 시작시키는 메서드
    public void StartCooldownManual(float cooldownTime)
    {
        this.maxCooldown = cooldownTime;
        this.currentCooldown = cooldownTime; // 여기서 값을 설정하면 Tick이 다시 돌기 시작함
        // Debug.Log($"[ItemInstance] 수동 쿨타임 시작: {cooldownTime}초");
    }

    public void UpgradeLevel()
    {
        itemData.UpgradeLevel(this);
    }

    public void instantiatedItemUpgrade()
    {
        if (instantiatedObject == null) return;

        IInstantiatedItem logic = instantiatedObject.GetComponent<IInstantiatedItem>();
        if (logic != null) logic.UpgradeInstItem(this);
        else ApplyVisualUpgrade();
    }

    private void ApplyVisualUpgrade()
    {
        int levelIndex = this.currentUpgrade - 1;
        if (levelIndex < 0 || itemData == null) return;

        // 애니메이터 교체 시도
        Animator animator = instantiatedObject.GetComponent<Animator>();
        if (animator != null && itemData.controllersByLevel != null && levelIndex < itemData.controllersByLevel.Length)
        {
            RuntimeAnimatorController newController = itemData.controllersByLevel[levelIndex];
            if (newController != null) { animator.runtimeAnimatorController = newController; return; }
        }

        // 스프라이트 교체 시도
        SpriteRenderer spriteRenderer = instantiatedObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && itemData.spritesByLevel != null && levelIndex < itemData.spritesByLevel.Length)
        {
            Sprite newSprite = itemData.spritesByLevel[levelIndex];
            if (newSprite != null) spriteRenderer.sprite = newSprite;
        }
    }
}