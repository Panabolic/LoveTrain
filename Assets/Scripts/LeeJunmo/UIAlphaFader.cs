using UnityEngine;
using DG.Tweening; // DOTween

[RequireComponent(typeof(CanvasGroup))]
public class UIAlphaFader : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("기본 페이드 시간 (함수 인자가 없을 때 사용)")]
    [SerializeField] private float defaultDuration = 0.5f;

    [Tooltip("Start() 호출 시 자동으로 페이드 인 실행 여부")]
    [SerializeField] private bool fadeInOnStart = true;

    [Tooltip("시작 시 투명하게(Alpha 0) 설정할지 여부")]
    [SerializeField] private bool startTransparent = true;

    private CanvasGroup canvasGroup;
    private bool hasInitializedCanvasGroup;

    private void Awake()
    {
        EnsureCanvasGroup();
    }

    private void Start()
    {
        if (fadeInOnStart)
        {
            FadeIn(defaultDuration);
        }
    }

    // -----------------------------------------------------------------------
    // ✨ [핵심 수정] 반환 타입을 Tween으로 변경하여 코루틴에서 기다릴 수 있게 함
    // -----------------------------------------------------------------------

    /// <summary>
    /// 화면을 밝게 만듭니다 (Alpha 0).
    /// </summary>
    public Tween FadeIn(float duration)
    {
        EnsureCanvasGroup();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        canvasGroup.DOKill();
        return canvasGroup.DOFade(1f, duration).SetUpdate(true);
    }

    /// <summary>
    /// 화면을 어둡게 만듭니다 (Alpha 1).
    /// </summary>
    public Tween FadeOut(float duration)
    {
        EnsureCanvasGroup();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        canvasGroup.DOKill();
        return canvasGroup.DOFade(0f, duration).SetUpdate(true);
    }

    // (매개변수 없는 버전 - 기본값 사용)
    public void FadeIn() => FadeIn(defaultDuration);
    public void FadeOut() => FadeOut(defaultDuration);

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (hasInitializedCanvasGroup)
        {
            return;
        }

        if (startTransparent)
        {
            canvasGroup.alpha = 0f;
        }

        hasInitializedCanvasGroup = true;
    }
}
