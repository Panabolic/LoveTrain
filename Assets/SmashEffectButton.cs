using UnityEngine;
using UnityEngine.UI; // Image 사용을 위해 필수
using UnityEngine.Events;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class SmashEffectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [Tooltip("기차 오브젝트 (클릭 전엔 꺼져있음)")]
    [SerializeField] private GameObject trainObj;

    [Tooltip("색상을 바꿀 글자 이미지들")]
    [SerializeField] private List<Image> letterImages; // TMP 대신 Image 리스트 사용

    [Header("Target Positions")]
    [Tooltip("글자가 부서지는 충돌 지점 (글자들 위치)")]
    [SerializeField] private Transform smashTriggerPoint;

    [Tooltip("기차가 뚫고 지나가서 멈출 최종 도착 지점")]
    [SerializeField] private Transform finalDestination;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.5f, 0.5f); // 예: 연한 빨강

    [Header("Physics Settings")]
    [Tooltip("글자가 날아가는 힘")]
    [SerializeField] private float smashForce = 2000f; // 힘을 좀 더 강하게!
    [Tooltip("글자 회전력")]
    [SerializeField] private float smashTorque = 1000f;

    [Header("Animation Settings")]
    [Tooltip("기차 이동 시간 (0.3초 정도로 매우 빠르게)")]
    [SerializeField] private float trainMoveDuration = 0.3f;
    [Tooltip("기차 도착 후 기능 실행까지 대기 시간")]
    [SerializeField] private float delayAfterArrival = 1.0f;
    [Tooltip("다른 버튼")]
    [SerializeField] private SmashEffectButton anotherButton;

    [Header("Events")]
    public UnityEvent OnAnimationComplete;

    private HorizontalLayoutGroup layoutGroup;
    private Vector3 trainStartPos;
    private bool isClicked = false;
    public bool canClick = true;
    private List<Rigidbody2D> letterRbs = new List<Rigidbody2D>();

    private void Awake()
    {
        layoutGroup = GetComponent<HorizontalLayoutGroup>();

        if (trainObj != null)
        {
            trainStartPos = trainObj.transform.localPosition;
            trainObj.SetActive(false);
        }

        // 이미지들의 물리 컴포넌트(Rigidbody2D) 미리 찾아두기 및 초기화
        foreach (var img in letterImages)
        {
            Rigidbody2D rb = img.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                // 없으면 자동으로 추가
                rb = img.gameObject.AddComponent<Rigidbody2D>();
                img.gameObject.AddComponent<BoxCollider2D>(); // 충돌체도 추가
            }

            // 평소에는 물리 영향 안 받게 설정 (Kinematic)
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f; // 중력 끄기 (화면 밖으로 떨어지지 않게)
            letterRbs.Add(rb);

            // 초기 색상 설정
            img.color = normalColor;
        }
    }

    // 마우스 올림: 이미지 색 변경 (기차는 안 나옴)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isClicked || !canClick) return;
        foreach (var img in letterImages) img.color = hoverColor;
    }

    // 마우스 내림: 이미지 색 복구
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isClicked) return;
        foreach (var img in letterImages) img.color = normalColor;
    }

    // 클릭: 기차 발진 및 물리 효과 시작
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canClick) return;
        if (isClicked) return;
        isClicked = true;
        anotherButton.canClick = false;
        foreach (var img in letterImages) img.color = normalColor;

        // 1. 레이아웃 해제 (글자들이 개별적으로 움직이도록)
        if (layoutGroup != null) layoutGroup.enabled = false;

        // 2. 기차 활성화 및 초기화
        if (trainObj != null)
        {
            trainObj.SetActive(true);
            trainObj.transform.localPosition = trainStartPos;
        }

        // 3. 충돌 타이밍 계산
        float totalDist = Vector3.Distance(trainStartPos, finalDestination.localPosition);
        float smashDist = Vector3.Distance(trainStartPos, smashTriggerPoint.localPosition);

        // 기차가 워낙 빠르므로 타이밍 계산이 중요
        float smashTime = (smashDist / totalDist) * trainMoveDuration;

        // 4. 시퀀스 실행
        Sequence seq = DOTween.Sequence();

        // [이동] Ease.Linear로 설정하여 가속/감속 없이 등속도로 돌진
        seq.Append(trainObj.transform.DOLocalMove(finalDestination.localPosition, trainMoveDuration).SetEase(Ease.Linear));

        // [충돌] 물리 효과 발동
        seq.InsertCallback(smashTime, SmashPhysics);

        // [종료]
        seq.AppendInterval(delayAfterArrival);
        seq.OnComplete(() => OnAnimationComplete?.Invoke());
    }

    private void SmashPhysics()
    {
        foreach (Rigidbody2D rb in letterRbs)
        {
            // 1. 물리 시뮬레이션 켜기 (Dynamic)
            rb.bodyType = RigidbodyType2D.Dynamic;

            // 2. 랜덤한 방향으로 힘 가하기 (오른쪽 위주로 튕겨나가게)
            Vector2 randomDir = Random.insideUnitCircle;
            randomDir.x = Mathf.Abs(randomDir.x) + 0.3f; // 오른쪽 방향 성분 강화

            rb.AddForce(randomDir.normalized * smashForce);
            rb.AddTorque(Random.Range(-smashTorque, smashTorque));
        }

        // 화면 흔들림 효과
        if (CameraShakeManager.Instance != null)
        {
            // 강도와 진동수를 높여서 타격감 강화
            CameraShakeManager.Instance.ShakeCamera(0.2f, 15f, 50, 90f);
        }
    }
}