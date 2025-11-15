using UnityEngine;
using DG.Tweening; // DOTween을 사용하기 위해 필수

// 이 스크립트를 추가하면 CanvasGroup 컴포넌트가 자동으로 추가됩니다.
[RequireComponent(typeof(CanvasGroup))]
public class UIAlphaFader : MonoBehaviour
{
    [Header("페이드 설정")]
    [Tooltip("페이드 인/아웃에 걸리는 시간 (초)")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("Start() 함수 호출 시 자동으로 페이드 인을 실행할지 여부")]
    [SerializeField] private bool fadeInOnStart = true;

    [Tooltip("Awake() 시점에 UI를 투명하게(alpha=0) 설정할지 여부")]
    [SerializeField] private bool startTransparent = true;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // RequireComponent로 인해 canvasGroup은 항상 존재함
        canvasGroup = GetComponent<CanvasGroup>();

        if (startTransparent)
        {
            // 시작 시 완전 투명하게 설정
            canvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        if (fadeInOnStart)
        {
            FadeIn();
        }
    }

    /// <summary>
    /// UI 그룹을 서서히 나타나게 합니다 (Fade In).
    /// </summary>
    public void FadeIn()
    {
        // DOTween을 사용해 alpha 값을 1로(불투명) 변경
        canvasGroup.DOFade(1f, fadeDuration)
            .SetUpdate(true); // Time.timeScale이 0일 때도 (일시정지 중) 작동
    }

    /// <summary>
    /// UI 그룹을 서서히 사라지게 합니다 (Fade Out).
    /// </summary>
    public void FadeOut()
    {
        // DOTween을 사용해 alpha 값을 0으로(투명) 변경
        canvasGroup.DOFade(0f, fadeDuration)
            .SetUpdate(true); // Time.timeScale이 0일 때도 (일시정지 중) 작동
    }
}