using UnityEngine;

public class GiantMaw : MonoBehaviour, IInstantiatedItem
{
    private GiantMaw_SO itemData;
    private Train playerTrain;
    // Components
    private Animator animator;

    // 값 초기화는 나중에 없애야 함
    private int     damage              = 100;
    private float   cooldown            = 2.0f;
    private int     healAmount          = 10;
    private float   currentCoolTime;
    private Vector2 knockbackDirection  = new Vector2(1.0f, 0.3f);
    private float   knockbackPower      = 15.0f;

    private bool    isAvailable = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        isAvailable = true;
        currentCoolTime = 0f;
    }

    private void Update()
    {
        if (!isAvailable)
        {
            currentCoolTime -= Time.deltaTime;
            
            if (currentCoolTime <= 0f)
                isAvailable = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAvailable) return;

        if (collision.CompareTag("Mob") && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Mob mob = collision.GetComponent<Mob>();
            if (mob == null)
            {
                Debug.Log("충돌한 몬스터에게 Mob이 연결되어 있지 않습니다.");
            }

            if (!mob.GetIsAlive()) return;

            // Attack
            animator.SetTrigger("eat");
            SoundEventBus.Publish(SoundID.Item_GiantMaw);

            mob.TakeDamage(damage);
            mob.Knockback(knockbackDirection, knockbackPower);

            if (!mob.GetIsAlive())
            {
                if (playerTrain != null)
                {
                    // Train.ModifySpeed는 양수일 경우 회복으로 동작함
                    playerTrain.ModifySpeed(healAmount);
                    Debug.Log($"[GiantMaw] 냠냠! 적 처치로 {healAmount} 회복");
                }
            }

            // Management
            currentCoolTime = cooldown;
            isAvailable     = false;
        }
    }

    public void Initialize(GiantMaw_SO so, GameObject user)
    {
        this.itemData = so;

        if (user != null)
        {
            this.playerTrain = user.GetComponent<Train>();
        }

        knockbackDirection  = itemData.knockbackDirection;
        knockbackPower      = itemData.knockbackPower;
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        if (itemData == null) return;

        int levelIndex = instance.currentUpgrade - 1;

        // SO 데이터로 이 MonoBehaviour의 스탯을 갱신
        this.damage     = itemData.mawDamageByLevel[levelIndex];
        this.cooldown   = itemData.cooldownByLevel[levelIndex];
        this.healAmount = itemData.healAmountByLevel[levelIndex];
        /*애니메이션 교체 구현 필요*/

    }
}