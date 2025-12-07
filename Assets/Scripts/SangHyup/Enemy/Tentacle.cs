using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tentacle : MonoBehaviour
{
    // Components
    private Animator        animator;
    private BoxCollider2D   collision;
    private BoxCollider2D   rangeBox;

    private EyeBoss owner;

    private float   hp;
    private float   damage;

    private float   attackWaitTime  = 3.0f;
    private float   toAttack        = 0.2f;
    private float   toDestroy       = 0.3f;


    private void Awake()
    {
        // Get Components
        animator    = GetComponent<Animator>();
        collision   = GetComponent<BoxCollider2D>();
        rangeBox    = transform.GetChild(0).GetComponent<BoxCollider2D>();

        collision.enabled   = true;
        rangeBox.enabled    = false;
    }

    private void Start()
    {
        StartCoroutine(Attack());
    }

    public void Initialize(EyeBoss owner)
    {
        this.owner = owner;

        owner.RegisterTentacle(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<Train>().TakeDamage(damage);
        }
    }

    private IEnumerator Attack()
    {
        // 공격 경고
        yield return new WaitForSeconds(attackWaitTime);

        animator.SetTrigger("attack");

        // 피격 활성화까지
        yield return new WaitForSeconds(toAttack);

        collision.enabled   = false;
        rangeBox.enabled    = true;

        // 소멸까지
        yield return new WaitForSeconds(toDestroy);

        Destroy(gameObject);
    }

    public void TakeDamage(float damageAmount)
    {
        hp -= damageAmount;

        // 피격 파티클 재생

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        owner.UnregisterTentacle(gameObject);
    }

    public void SetHP(float hpAmount)           { hp = hpAmount; }
    public void SetDamage(float damageAmount)   { damage = damageAmount; }
}
