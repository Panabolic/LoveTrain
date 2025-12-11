using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tentacle : Enemy
{
    private EyeBoss owner;

    private float   attackWaitTime  = 3.0f;
    private float   toAttack;
    private float   toDestroy       = 0.3f;


    protected override void Start()
    {
        base.Start();

        toAttack = animator.GetNextAnimatorStateInfo(0).length;

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

        // 소멸까지
        yield return new WaitForSeconds(toDestroy);

        Destroy(gameObject);
    }

    public override void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;

        currentHP -= damageAmount;

        StartCoroutine(HitEffect());

        if (currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        owner.UnregisterTentacle(gameObject);
    }

    // Getters
    public float GetAttackWaitTime() { return attackWaitTime + toAttack; }

    // Setters
    public void SetDamage(float damageAmount) { damage = damageAmount; }
    public void SetAttackWaitTime(float time) { attackWaitTime = time; }

}
