using UnityEngine;
using TMPro;
using DG.Tweening;

public class CreditEnemy : Enemy
{
    [Header("Credit Settings")]
    [SerializeField] private float dropSpeed = 2.0f;
    [SerializeField] private TextMeshPro contentText;
    [Tooltip("콜라이더 여백 (글자보다 살짝 크게)")]
    [SerializeField] private Vector2 colliderPadding = new Vector2(0.5f, 0.2f);

    private Color originTextColor;

    protected override void Awake()
    {
        // 1. 부모(Enemy)의 Awake 실행 (플레이어 참조 가져오기 등)
        // Enemy.cs가 Null-Safe하게 수정되었으므로 Sprite/Animator가 없어도 안전함
        base.Awake();

        // 2. CreditEnemy 전용 초기화
        currentHP = hp;
        isAlive = true;

        if (collision == null) collision = GetComponent<Collider2D>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb) rb.gravityScale = 0f; // 중력 영향 받지 않게 설정

        if (contentText == null) contentText = GetComponentInChildren<TextMeshPro>();
    }

    /// <summary>
    /// 크레딧 텍스트 설정 및 콜라이더 크기 자동 조절
    /// </summary>
    public void Initialize(string text)
    {
        if (contentText != null)
        {
            // 텍스트 및 색상 설정
            contentText.text = text;
            originTextColor = contentText.color;

            // [핵심] 텍스트 메쉬를 강제로 갱신해야 정확한 Bounds 계산 가능
            contentText.ForceMeshUpdate();

            // 렌더링된 텍스트의 영역 가져오기
            Bounds bounds = contentText.textBounds;

            // BoxCollider2D 크기 및 오프셋 조정
            if (collision is BoxCollider2D boxCol)
            {
                boxCol.size = new Vector2(bounds.size.x + colliderPadding.x, bounds.size.y + colliderPadding.y);
                boxCol.offset = bounds.center;
            }
        }
    }

    protected override void Update()
    {
        // [중요] 부모의 Update를 실행해야 'CheckScreenEntry'가 작동하여 
        // 화면 진입 시 타겟팅 가능 상태(IsTargetable)가 됨
        base.Update();

        if (!isAlive) return;
        if (Time.timeScale == 0) return;

        // 아래로 이동
        transform.Translate(Vector3.down * dropSpeed * Time.deltaTime);

        // 화면 밖으로 벗어나면 삭제
        if (transform.position.y < -20f)
        {
            Destroy(gameObject);
        }
    }

    public override void TakeDamage(float damageAmount)
    {
        // 아직 화면에 안 들어왔거나(IsTargetable false), 죽었으면 무시
        if (!isAlive || !IsTargetable) return;

        // 부모의 TakeDamage 호출 (HP 감소, 사망 처리, 사운드 등)
        base.TakeDamage(damageAmount);

        // 텍스트 전용 피격 연출 (빨간색 깜빡임)
        if (contentText != null)
        {
            contentText.DOKill(); // 기존 트윈 중단
            contentText.color = Color.red;
            contentText.DOColor(originTextColor, 0.3f).SetEase(Ease.OutQuad);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 화면 밖이면 충돌 무시
        if (!IsTargetable) return;

        // 기차와 충돌 시
        if (collision.gameObject.layer == LayerMask.NameToLayer("Train"))
        {
            Train train = collision.transform.GetComponentInParent<Train>();

            if (train != null)
            {
                train.TakeDamage(damage, true);
                if (CameraShakeManager.Instance != null)
                {
                    CameraShakeManager.Instance.ShakeCamera();
                }
            }
            StartCoroutine(Die());
        }
    }

    protected override System.Collections.IEnumerator Die()
    {
        // 트윈 정리
        if (contentText != null) contentText.DOKill();

        yield return null;
        Destroy(gameObject);
    }
}