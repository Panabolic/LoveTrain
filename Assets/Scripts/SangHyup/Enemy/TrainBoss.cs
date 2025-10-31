using System;
using System.Collections;
using UnityEngine;

public class TrainBoss : Boss
{
    // Components
    protected Rigidbody2D rigid2D;

    [Tooltip("이동 속도 (km/h)")]
    [SerializeField] private float moveSpeed = 20.0f;
    [Tooltip("Phase 1 피격 넉백 퍼센트 (%)")]
    [Range(0f, 1f)]
    [SerializeField] private float p1KnockbackRatio = 0.9f;
    [Tooltip("Phase 2 전환 기준")]
    [Range(0f, 1f)]
    [SerializeField] private float p2HpRatio = 0.4f;
    [Tooltip("Phase 2 피격 넉백 퍼센트 (%)")]
    [Range(0f, 1f)]
    [SerializeField] private float p2KnockbackRatio = 0.7f;
    [Tooltip("넉백 지속시간 (second)")]
    [Range(0f, 3f)]
    [SerializeField] private float knockbackDuration    = 0.2f;
                     private float knockbackTimer       = 0f;

    private bool isPhase2 = false;

    private Vector2 moveDirection = Vector2.zero;

    protected override void Awake()
    {
        base.Awake();

        // Get Components
        rigid2D = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    private void FixedUpdate()
    {
        if (!isAlive)
        {
            moveDirection = Vector2.left;
            float deathMoveSpeed = 30.0f;

            rigid2D.linearVelocity = new Vector2(deathMoveSpeed * moveDirection.x, rigid2D.linearVelocity.y);

            return;
        }

        // Move Direction Setting
        SetMoveDirection(targetRigid.position);

        if (knockbackTimer <= 0f && isAlive)
        {
            rigid2D.linearVelocity = new Vector2(moveSpeed * moveDirection.x, rigid2D.linearVelocity.y);

            return;
        }

        knockbackTimer -= Time.fixedDeltaTime;
    }

    /// <summary>
    /// 1D move direction setting for TrainBoss
    /// </summary>
    protected virtual void SetMoveDirection(Vector2 targetPos)
    {
        float x = targetPos.x - transform.position.x;

        if (x > 0f)
        {
            moveDirection = Vector2.right;
            sprite.flipX = (moveDirection.x > 0f);
            return;
        }
        if (x < 0f)
        {
            moveDirection = Vector2.left;
            sprite.flipX = (moveDirection.x > 0f);
            return;
        }

        moveDirection = Vector2.zero;
        sprite.flipX = (moveDirection.x > 0f);
        return;
    }

    public override void TakeDamage(float damageAmount)
    {
        base.TakeDamage(damageAmount);

        /* ��� ���� �ʿ��ϸ� ���⿡ �߰� */

        CheckPhase();

        // Knockback to opposite direction
        Knockback();
    }

    private void CheckPhase()
    {
        if (currentHP > maxHP * p2HpRatio && isPhase2 == false) return;

        isPhase2 = true;

        animator.SetTrigger("phase2");

        Debug.Log("TrainBoss: Entered Phase 2!");
    }

    private void Knockback()
    {
        float   knockbackRatio  = isPhase2 ? p2KnockbackRatio : p1KnockbackRatio;
        float   knockbackSpeed  = moveSpeed * knockbackRatio;
        Vector2 power           = new Vector2(-moveDirection.x * knockbackSpeed, rigid2D.linearVelocity.y);

        knockbackTimer = knockbackDuration;

        rigid2D.AddForce(power, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Train"))
        {
            Train train = collision.transform.GetComponentInParent<Train>();

            if (train != null)
            {
                train.TakeDamage(damage); // Train ��ũ��Ʈ�� �°� ���� �ʿ�
                if (CameraShakeManager.Instance != null)
                {
                    CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f);
                }
            }

            /* �÷��̾� ���̱� */
        }
    }

    protected override IEnumerator Die()
    {
        yield return base.Die();

        yield return new WaitForSeconds(3.0f);

        Destroy(gameObject);
    }
}
