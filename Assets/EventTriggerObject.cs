using UnityEngine;

public class EventTriggerObject : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이동 속도")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("오브젝트가 자동으로 파괴될 때까지의 시간 (메모리 관리용)")]
    [SerializeField] private float lifeTime = 15f;

    [SerializeField] private GameObject exclamateObj;
    [SerializeField] private bool isUpgradeEvent;
    public bool IsUpgradeEvent => isUpgradeEvent;
    // 내부 변수
    private Vector3 moveDirection;
    private bool hasTriggered = false; // 이벤트 중복 발동 방지
    private SO_Event eventOverride;

    public void SetEventOverride(SO_Event nextEvent)
    {
        eventOverride = nextEvent;
    }

    public void SetUpgradeEvent(bool upgrade)
    {
        isUpgradeEvent = upgrade;
        if (!upgrade) return;
        eventOverride = null;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            renderer.color = new Color(1f, 0.2f, 0.25f, renderer.color.a);
    }

    private void Start()
    {
        if (isUpgradeEvent) SetUpgradeEvent(true);
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
        GameManager manager = GameManager.Instance;
        if (manager == null || Time.timeScale <= 0f) return;
        if (manager.CurrentState == GameState.StageTransition || manager.CurrentState == GameState.Ending)
        {
            Destroy(gameObject);
            return;
        }
        if (manager.CurrentState == GameState.Playing || manager.CurrentState == GameState.Boss)
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 발동했으면 무시
        if (hasTriggered) return;
        GameManager manager = GameManager.Instance;
        if (manager == null ||
            (manager.CurrentState != GameState.Playing && manager.CurrentState != GameState.Boss)) return;

        // 기차와 충돌했는지 확인 (Layer 또는 Tag)
        // 기존 코드 컨벤션에 따라 'Train' 레이어 체크
        if (collision.gameObject.layer == LayerMask.NameToLayer("Train") || collision.CompareTag("Player"))
        {
            if (isUpgradeEvent)
            {
                if (LevelUpUIManager.Instance == null) return;
                hasTriggered = true;
                manager.RegisterUIQueue(() =>
                {
                    if (LevelUpUIManager.Instance != null) LevelUpUIManager.Instance.ShowUpgradeEvent();
                    else if (GameManager.Instance != null) GameManager.Instance.CloseUI();
                });
            }
            else if (EventManager.Instance != null)
            {
                hasTriggered = true; // 중복 실행 방지 플래그 On
                if (eventOverride != null) EventManager.Instance.RequestEvent(eventOverride);
                else EventManager.Instance.RandomEventStart();
            }
            if (hasTriggered && exclamateObj != null) exclamateObj.SetActive(false);
        }
    }
}
