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


    private void Awake()
    {
        sprite      = GetComponent<SpriteRenderer>();
        collision   = GetComponent<Collider2D>();
        animator    = GetComponent<Animator>();

        // Get Target Components
        targetRigid     = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        currentHP   = maxHP;
        isAlive     = true;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;

        currentHP -= damageAmount;

        if (currentHP <= 0) StartCoroutine(Die());
    }

    protected virtual IEnumerator Die()
    {
        collision.enabled = false;  //Collision 잠시 false

        animator.SetTrigger("Die");

        yield return new WaitForSeconds(deathToDeactive);
    }
}
