using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tentacle : MonoBehaviour
{
    // Components
    private Animator        animator;
    private BoxCollider2D   collision;


    private float   damage = 50.0f;
    private float   attackWaitTime = 3.0f;

    private void Awake()
    {
        // Get Components
        animator    = GetComponent<Animator>();
        collision   = GetComponent<BoxCollider2D>();

        collision.enabled = false;
    }

    private void Start()
    {
        StartCoroutine(Attack());
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
        yield return new WaitForSeconds(attackWaitTime);

        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.2f);

        collision.enabled = true;

        yield return new WaitForSeconds(0.5f);

        Destroy(gameObject);
    }

    public void SetDamage(float damageAmount) { damage = damageAmount; }
}
