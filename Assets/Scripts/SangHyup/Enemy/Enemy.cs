using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Components
    protected SpriteRenderer    sprite;
    protected Rigidbody2D       rigid2D;
    protected Rigidbody2D       targetRigid;
    protected Collider2D        collision;
    protected Animator          animator;

    [Header("Enemy 제원")]
    [SerializeField] protected float maxHP      = 50;
    [SerializeField] protected float moveSpeed  = 2.0f;
    [SerializeField] protected float damage     = 0.0f;

    private float   currentHP;
    private bool    isAlive;

    private float   moveDirection;


    private void Awake()
    {
        sprite      = GetComponent<SpriteRenderer>();
        rigid2D     = GetComponent<Rigidbody2D>();
        targetRigid = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
        collision   = GetComponent<Collider2D>();
        animator    = GetComponent<Animator>();

    }

    private void OnEnable()
    {
        currentHP = maxHP;

        isAlive = true;
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;

        moveDirection = Mathf.Sign(targetRigid.position.x - rigid2D.position.x);
        Vector2 nextXPosition = new Vector2(moveDirection * moveSpeed, rigid2D.position.y) * Time.fixedDeltaTime;
        rigid2D.MovePosition(rigid2D.position + nextXPosition);
        rigid2D.linearVelocity = Vector2.zero;
    }

    public void TakeDamage(float damageAmount)
    {
        if (!isAlive) return;

        currentHP -= damageAmount;

        if (currentHP <= 0) StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        collision.enabled = false;  //Collision 잠시 false

        animator.SetTrigger("Die");

        /* rigid2D.linearVelocityX = 현재 기차 이동속도만큼 왼쪽으로*/

        yield return new WaitForSeconds(3.0f/*사망 애니메이션 길이만큼*/);

        collision.enabled   = true;
        isAlive             = false;
        enabled             = false;
    }

    private void OnDisable()
    {
        
    }
}
