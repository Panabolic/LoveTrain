using UnityEngine;

public class EventTriggerObject : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이동 속도")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("오브젝트가 자동으로 파괴될 때까지의 시간 (메모리 관리용)")]
    [SerializeField] private float lifeTime = 15f;

    // 내부 변수
    private Vector3 moveDirection;
    private bool hasTriggered = false; // 이벤트 중복 발동 방지

    private void Start()
    {
        // 1. 자동 파괴 타이머 시작 (메모리 누수 방지)
        Destroy(gameObject, lifeTime);

        // 2. 플레이어(기차) 방향으로 이동 방향 설정
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 플레이어 쪽 X 방향으로 이동 (Y축 유도 여부는 기획에 따라 결정)
            float directionX = player.transform.position.x - transform.position.x;
            // 정규화된 방향 벡터 (왼쪽 or 오른쪽)
            moveDirection = new Vector3(Mathf.Sign(directionX), 0, 0);
        }
        else
        {
            // 플레이어를 못 찾으면 기본값(왼쪽)으로 설정
            moveDirection = Vector3.left;
        }
    }

    private void Update()
    {
        // 게임이 멈춰있지 않을 때만 이동
        if (Time.timeScale > 0 || GameManager.Instance.CurrentState != GameState.Die)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 발동했으면 무시
        if (hasTriggered) return;

        // 기차와 충돌했는지 확인 (Layer 또는 Tag)
        // 기존 코드 컨벤션에 따라 'Train' 레이어 체크
        if (collision.gameObject.layer == LayerMask.NameToLayer("Train") || collision.CompareTag("Player"))
        {
            // 3. 이벤트 매니저를 통해 이벤트 시작
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RandomEventStart();
                hasTriggered = true; // 중복 실행 방지 플래그 On
            }

            // (선택 사항) 충돌 후 시각적 피드백이 필요하면 여기서 처리
            // 예: 스프라이트 반투명화
            // var sprite = GetComponent<SpriteRenderer>();
            // if (sprite != null) sprite.color = new Color(1, 1, 1, 0.5f);
        }
    }
}