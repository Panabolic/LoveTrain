using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Components
    protected SpriteRenderer    sprite;
    protected Collider2D        collision;
    protected Animator          animator;

    [Header("Enemy Specification")]
    [Tooltip("기본 체력")]
    [SerializeField] protected float maxHP;
    [Tooltip("대미지(km/h 단위)")]
    [SerializeField] protected float damage;

    protected float currentHP;
    protected bool  isAlive = true;
    
    protected float deathToDeactive; // 사망에서 비활성화까지 걸리는 시간

    // Target Components
    protected Rigidbody2D targetRigid;


    protected virtual void Awake()
    {
        sprite      = GetComponent<SpriteRenderer>();
        collision   = GetComponent<Collider2D>();
        animator    = GetComponent<Animator>();

        // Get Target Components
        targetRigid = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable()
    {
        // 체력 충전, isAlive는 true, Collider 활성화

        currentHP       = maxHP;
        isAlive         = true;
        sprite.enabled  = true;

        // foreach문 만들어야 됨
        Physics2D.IgnoreCollision(collision, targetRigid.GetComponent<Collider2D>(), true);
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;

        currentHP -= damageAmount;

        if (currentHP <= 0) StartCoroutine(Die());
    }

    protected virtual IEnumerator Die()
    {
        isAlive = false;

        Physics2D.IgnoreCollision(collision, targetRigid.GetComponent<Collider2D>(), false);

        animator.SetTrigger("Die");

        yield return new WaitForSeconds(deathToDeactive);
    }
}
