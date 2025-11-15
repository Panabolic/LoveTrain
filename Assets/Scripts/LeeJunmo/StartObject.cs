using UnityEngine;
using System.Collections; // 코루틴을 사용하기 위해 추가

[RequireComponent(typeof(Collider2D))]
public class StartObject : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이 오브젝트를 트리거할 수 있는 총알의 레이어(Layer)를 선택해주세요.")]
    [SerializeField] private LayerMask projectileLayer;
    [SerializeField] private GameObject otherUI;
    [SerializeField] private Sprite leverAfter;
    [SerializeField] private GameObject MouseUI;
    [SerializeField] private GameObject KeyUI;

    [SerializeField] private SO_Event startEvent;

    [Header("✨ 퇴장 애니메이션")]
    [Tooltip("스프라이트 변경 후 -x축(왼쪽)으로 이동할 속도")]
    [SerializeField] private float moveSpeed = 20f;
    [Tooltip("이동을 시작하고 몇 초 뒤에 오브젝트를 파괴할지")]
    [SerializeField] private float destroyDelay = 4.0f;

    private bool hasBeenTriggered = false;
    private bool isDeactivating = false; // 퇴장(이동) 중인지 확인하는 플래그

    private void Start()
    {
        // 트리거 콜라이더가 없으면 경고
        Collider2D col = GetComponent<Collider2D>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogWarning($"StartObject '{name}'에 isTrigger가 체크된 Collider2D가 없습니다!", this);
        }
    }

    /// <summary>
    /// ✨ [추가] 퇴장 애니메이션(이동)을 처리할 Update
    /// </summary>
    private void Update()
    {
        // isDeactivating 플래그가 true일 때만 왼쪽으로 계속 이동합니다.
        if (isDeactivating)
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 이미 발동했으면 무시
        if (hasBeenTriggered)
        {
            return;
        }

        // 2. 설정된 레이어가 아니면 무시
        if ((projectileLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        // 3. 한 번만 발동하도록 플래그 설정
        hasBeenTriggered = true;

        // 4. GameManager에게 게임 시작을 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
            EventManager.Instance.StartEvent(startEvent);
            otherUI.SetActive(true);
            otherUI.GetComponent<UIAlphaFader>().FadeIn();
        }

        // 5. 부딪힌 총알 비활성화
        other.gameObject.SetActive(false);

        // ✨ [수정] 6. 퇴장 시퀀스 코루틴을 시작합니다.
        StartCoroutine(DeactivationSequence());
    }

    /// <summary>
    /// ✨ [새 코루틴] 스프라이트를 변경하고, 이동을 시작하며, 4초 뒤에 오브젝트를 파괴합니다.
    /// </summary>
    private IEnumerator DeactivationSequence()
    {
        // 1. 스프라이트 변경
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = leverAfter;
        }

        MouseUI.SetActive(false);
        KeyUI.SetActive(true);
        // 2. 콜라이더를 끕니다 (다른 총알에 또 맞지 않도록)
        GetComponent<Collider2D>().enabled = false;

        // 3. Update()에서 이동을 시작하도록 플래그 설정
        isDeactivating = true;

        // 4. 설정된 시간(4초) 동안 대기
        yield return new WaitForSeconds(destroyDelay);
        KeyUI.SetActive(false);

        // 5. 오브젝트 파괴
        Destroy(gameObject);
    }
}