using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TrainBoss : Boss
{
    // Components
    protected Rigidbody2D rigid2D;

    [Tooltip("���� ������ �̵��ӵ� km/h")]
    [SerializeField] private float moveSpeed = 20.0f;
    [Tooltip("�ǰ� �� �˹� �ӵ� km/h")]
    [SerializeField] private float knockbackSpeed = 5.0f;
    [Tooltip("2 ������ �����ϴ� ���� ü�� ����")]
    [Range(0f, 1f)]
    [SerializeField] private float phase2HpRatio = 0.4f;


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

        // HP exceeds 2 phase condition
        if (currentHP <= maxHP * phase2HpRatio)
        {
            
        }

        // Knockback to opposite direction
        rigid2D.linearVelocity = new Vector2(knockbackSpeed * -moveDirection.x, rigid2D.linearVelocity.y);
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
