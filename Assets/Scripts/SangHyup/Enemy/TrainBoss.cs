using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TrainBoss : Boss
{
    // Components
    protected Rigidbody2D rigid2D;

    [Tooltip("기차 보스의 이동속도 km/h")]
    [SerializeField] private float moveSpeed = 20.0f;
    [Tooltip("피격 시 넉백 속도 km/h")]
    [SerializeField] private float knockbackSpeed = 5.0f;
    [Tooltip("2 페이즈 진입하는 남은 체력 조건")]
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

        /* 사망 연출 필요하면 여기에 추가 */

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
                train.TakeDamage(damage); // Train 스크립트에 맞게 수정 필요
                if (CameraShakeManager.Instance != null)
                {
                    CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f);
                                                               // 또는 원하는 값으로 흔들기: CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f);
                }
            }

            /* 플레이어 죽이기 */
        }
    }

    protected override IEnumerator Die()
    {
        return base.Die();
    }
}
