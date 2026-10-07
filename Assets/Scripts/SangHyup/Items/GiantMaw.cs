using UnityEngine;

public class GiantMaw : MonoBehaviour, IInstantiatedItem, IItemCooldownView
{
    private GiantMaw_SO itemData;
    private Train playerTrain;
    private Animator animator;
    private int damage = 100;
    private float cooldown = 2f;
    private int healAmount = 10;
    private Vector2 knockbackDirection = new Vector2(1f, 0.3f);
    private float knockbackPower = 15f;
    private readonly AttackCycle cycle = new AttackCycle();
    private bool isAvailable = true;
    private ObjectHost host;

    public bool HasCooldown => cooldown > 0f;
    public float GetCooldownFillAmount() => isAvailable ? 0f :
        ItemCooldownFill.FromRemaining(cycle.RemainingCooldown, cooldown);

    private void Awake()
    {
        animator = GetComponent<Animator>();
        host = new ObjectHost(gameObject);
        host.BindUpdate(StepCooldown);
        host.BindCollision(Hit);
    }

    private void Start() { cycle.Reset(); isAvailable = true; }
    private void Update() { host.Update(Time.deltaTime); }
    private void StepCooldown(float deltaTime)
    {
        if (isAvailable) return;
        cycle.Elapse(deltaTime);
        if (cycle.RemainingCooldown <= 0f) isAvailable = true;
    }
    private void OnTriggerEnter2D(Collider2D collision) { host.Collision(collision); }

    private void Hit(Collider2D collision)
    {
        if (!isAvailable || !collision.CompareTag("Mob") ||
            collision.gameObject.layer != LayerMask.NameToLayer("Enemy")) return;
        TargetHandle target = TargetRegistry.Resolve(collision);
        if (target == null || !target.SupportsKnockback)
        {
            Debug.Log("충돌한 몬스터에게 Mob이 연결되어 있지 않습니다.");
            return;
        }
        if (!target.IsAlive) return;
        host.Presentation.Trigger("eat");
        SoundEventBus.Publish(SoundID.Item_GiantMaw);
        target.RequestDamage(damage);
        target.Knockback(knockbackDirection, knockbackPower);
        if (!target.IsAlive && playerTrain != null)
        {
            playerTrain.ModifyFuel(healAmount);
            Debug.Log($"[GiantMaw] 냠냠! 적 처치로 {healAmount} 회복");
            SoundEventBus.Publish(SoundID.Item_GiantMaw2);
        }
        cycle.StartCooldown(cooldown);
        isAvailable = false;
    }

    public void Initialize(GiantMaw_SO so, GameObject user)
    {
        itemData = so;
        if (user != null) playerTrain = user.GetComponent<Train>();
        knockbackDirection = itemData.knockbackDirection;
        knockbackPower = itemData.knockbackPower;
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        if (itemData == null) return;
        int level = instance.currentUpgrade - 1;
        damage = itemData.mawDamageByLevel[level];
        cooldown = itemData.cooldownByLevel[level];
        healAmount = itemData.healAmountByLevel[level];
    }

    private void OnDestroy() { host?.Release(); }
}
