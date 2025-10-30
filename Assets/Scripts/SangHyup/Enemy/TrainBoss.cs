using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField] private float phase2HpRatio = 0.4f;
    [Tooltip("Phase 2 피격 넉백 퍼센트 (%)")]
    [Range(0f, 1f)]
    [SerializeField] private float p2KnockbackRatio = 0.7f;


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
        // Move Direction Setting
        SetMoveDirection(targetRigid.position);


        rigid2D.linearVelocity = new Vector2(moveSpeed * moveDirection.x, rigid2D.linearVelocity.y);
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

        EnterPhase2();

        // Knockback to opposite direction
        rigid2D.linearVelocity = new Vector2(moveSpeed * p1KnockbackRatio * -moveDirection.x, rigid2D.linearVelocity.y);
    }

    private void EnterPhase2()
    {
        if (currentHP > maxHP * phase2HpRatio) return;

        animator.SetTrigger("phase2");


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
                                                               // �Ǵ� ���ϴ� ������ ����: CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f);
                }
            }

            /* �÷��̾� ���̱� */
        }
    }

    protected override IEnumerator Die()
    {
        return base.Die();
    }
}
