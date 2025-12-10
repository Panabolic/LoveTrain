using UnityEngine;
using TMPro;

// Enemy를 상속받아 플레이어의 투사체에 맞을 수 있게 함
public class CreditEnemy : Enemy
{
    [Header("Credit Settings")]
    [SerializeField] private float dropSpeed = 2.0f;
    [SerializeField] private TextMeshPro contentText;

    // Enemy의 필수 변수들 초기화 (체력 등)
    protected override void Awake()
    {
        currentHP = hp;
        isAlive = true;

        collision = GetComponent<Collider2D>();

        // 물리 설정 (중력 영향 X)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb) rb.gravityScale = 0f;
    }

    public void Initialize(string text)
    {
        if (contentText != null)
        {
            contentText.text = text;
        }
        else
        {
            // 자식에서 찾기 시도
            contentText = GetComponentInChildren<TextMeshPro>();
            if (contentText) contentText.text = text;
        }
    }

    private void Update()
    {
        if (!isAlive) return;

        // 아래로 이동
        transform.Translate(Vector3.down * dropSpeed * Time.deltaTime);

        // 화면 밖으로 나가면 파괴 (좌표는 맵 크기에 맞춰 조정)
        if (transform.position.y < -20f)
        {
            Destroy(gameObject);
        }
    }

    // 피격 시 효과 (Enemy의 TakeDamage 사용)
    public override void TakeDamage(float damageAmount)
    {
        base.TakeDamage(damageAmount);
        // 추가: 텍스트가 흔들리거나 색이 변하는 연출 가능
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Train"))
        {
            Train train = collision.transform.GetComponentInParent<Train>();

            if (train != null)
            {
                train.TakeDamage(damage); // Train 스크립트에 맞게 수정 필요
                if (CameraShakeManager.Instance != null)
                {
                    CameraShakeManager.Instance.ShakeCamera(); // 기본 설정으로 흔들기
                                                               // 또는 원하는 값으로 흔들기: CameraShakeManager.Instance.ShakeCamera(0.3f, 1f, 15, 90f);
                }
            }

            StartCoroutine(Die());
        }
    }

    // 사망 처리 (부서지는 연출)
    protected override System.Collections.IEnumerator Die()
    {
        // 텍스트가 산산조각 나거나 페이드 아웃 되는 연출 추가 가능
        // 여기서는 간단하게 파티클 재생 후 삭제

        if (GameManager.Instance != null)
        {
            // 사운드 재생 (예: 종이 찢는 소리?)
            // SoundEventBus.Publish(SoundID.Credit_Break);
        }

        yield return null;
        Destroy(gameObject);
    }

    // 플레이어와 충돌해도 데미지를 주지 않으려면 OnTriggerEnter2D를 오버라이드해서 비워두거나,
    // Enemy의 로직을 수정해야 함. (여기서는 그대로 둠 -> 글자에 맞으면 아픔)
}