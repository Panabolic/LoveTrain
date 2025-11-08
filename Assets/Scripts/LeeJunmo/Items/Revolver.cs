using UnityEngine;

public class Revolver : MonoBehaviour, IInstantiatedItem
{
    //아이템 SO데이터 저장 변수
    private Revolver_SO itemData;

    private SpriteRenderer spriteRenderer;
    private Animator animator; // [추가]

    //현재 스탯
    private int currentDamage;
    private int currentBulletNum;
    private float currentCooldown;

    private void Awake()
    {
        // 컴포넌트를 미리 찾아둡니다.
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>(); // [추가]
    }

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

        if (animator != null && itemData.controllersByLevel != null &&
            levelIndex >= 0 && levelIndex < itemData.controllersByLevel.Length)
        {
            RuntimeAnimatorController newController = itemData.controllersByLevel[levelIndex];  

            if (newController != null)
            {
                // 애니메이터 컨트롤러를 교체합니다.
                this.animator.runtimeAnimatorController = newController;
                // (이 컨트롤러의 Entry State가 올바른 스프라이트를 설정해야 함)
                return; // 컨트롤러를 교체했으면 스프라이트 교체는 건너뜀
            }
        }

        // 우선순위 2: (컨트롤러가 없거나 지정 안됐을 때) '정적 스프라이트'가 지정되어 있는가?
        if (spriteRenderer != null && itemData.spritesByLevel != null &&
            levelIndex >= 0 && levelIndex < itemData.spritesByLevel.Length)
        {
            Sprite newSprite = itemData.spritesByLevel[levelIndex];
            if (newSprite != null)
            {
                this.spriteRenderer.sprite = newSprite;
            }
        }

    }
}
