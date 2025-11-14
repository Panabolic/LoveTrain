using NUnit.Framework;
using UnityEngine;

public class GiantMaw : MonoBehaviour, IInstantiatedItem
{
    private GiantMaw_SO itemData;

    // Components
    private Animator animator;
    //private Collider2D collision;

    private int     damage              = 100;
    private float   cooldown            = 2.0f;
    private float   currentCoolTime;
    private Vector2 knockbackDirection  = new Vector2(1.0f, 0.3f);
    private float   knockbackPower      = 10.0f;

    private bool isAvailable = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        //collision = GetComponent<Collider2D>();
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
            {
                Debug.Log("아가리 활성화");
                isAvailable = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAvailable) return;

        if (collision.CompareTag("Mob"))
        {
            Mob mob = collision.GetComponent<Mob>();
            if (mob == null)
            {
                Debug.Log("충돌한 몬스터에게 Mob이 연결되어 있지 않습니다.");
            }

            // Attack
            animator.SetTrigger("eat");
            mob.Knockback(knockbackDirection, knockbackPower);
            mob.TakeDamage(damage);

            // Management
            currentCoolTime = cooldown;
            isAvailable     = false;

            Debug.Log("거대한 아가리 발동!");
        }
    }

    public void Initialize(GiantMaw_SO so)
    {
        this.itemData = so;

        knockbackDirection  = itemData.knockbackDirection;
        knockbackPower      = itemData.knockbackPower;
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        if (itemData == null) return;

        int levelIndex = instance.currentUpgrade - 1;

        // SO 데이터로 이 MonoBehaviour의 스탯을 갱신
        this.damage = itemData.mawDamageByLevel[levelIndex];
        this.cooldown = itemData.cooldownByLevel[levelIndex];

        /*애니메이션 교체*/

    }
}