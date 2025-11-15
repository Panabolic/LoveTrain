using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening; // DOTween 사용

public class SpeedMeterUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("기차의 Train 스크립트")]
    [SerializeField] private Train train;
    [Tooltip("계기판 바늘의 RectTransform")]
    [SerializeField] private RectTransform needleRectTransform;

    [Header("효과 참조")]
    [Tooltip("테두리 (흔들림 대상 1)")]
    [SerializeField] private RectTransform lineRectTransform;
    [Tooltip("계기판 패널 (흔들림 대상 2)")]
    [SerializeField] private RectTransform panelRectTransform;

    [Header("점멸 참조")]
    [Tooltip("Line 오브젝트의 Image 컴포넌트")]
    [SerializeField] private Image lineImage;
    [Tooltip("Panal 오브젝트의 Image 컴포넌트")]
    [SerializeField] private Image panelImage;

    [Header("회전 설정")]
    [Tooltip("속도가 최소(사망)일 때의 바늘 각도")]
    [SerializeField] private float minAngle = 0f;
    [Tooltip("속도가 최대일 때의 바늘 각도")]
    [SerializeField] private float maxAngle = -180f;

    [Header("아날로그 설정")]
    [Tooltip("바늘이 목표 각도를 따라가는 속도")]
    [SerializeField] private float needleSmoothSpeed = 5f;

    [Header("피격 효과 설정")]
    [Tooltip("총 효과 지속 시간")]
    [SerializeField] private float effectDuration = 0.5f;
    [Tooltip("좌우/상하 흔들림의 강도")]
    [SerializeField] private float shakeStrength = 15f;
    [Tooltip("흔들림 횟수 (진동)")]
    [SerializeField] private int shakeVibrato = 20;
    [Tooltip("점멸할 때 변할 색상")]
    [SerializeField] private Color flashColor = Color.red;
    [Tooltip("총 점멸 횟수 (왕복)")]
    [SerializeField] private int flashCount = 3;

    // 바늘의 현재 각도
    private float currentAngleZ;
    // 효과가 중복 재생되는 것을 방지하는 플래그
    private bool isEffectPlaying = false;

    // 원래 색상을 저장하기 위한 변수
    private Color originalLineColor;
    private Color originalPanelColor;

    void Start()
    {
        currentAngleZ = minAngle;
        if (needleRectTransform != null)
        {
            needleRectTransform.rotation = Quaternion.Euler(0, 0, currentAngleZ);
        }

        if (train != null)
        {
            train.OnTrainDamaged += PlayDamageEffect;
        }

        if (lineImage != null)
        {
            originalLineColor = lineImage.color;
        }
        else
        {
            Debug.LogWarning("SpeedMeterUI: Line Image가 연결되지 않았습니다.");
        }

        if (panelImage != null)
        {
            originalPanelColor = panelImage.color;
        }
        else
        {
            Debug.LogWarning("SpeedMeterUI: Panel Image가 연결되지 않았습니다.");
        }
    }

    void OnDestroy()
    {
        if (train != null)
        {
            train.OnTrainDamaged -= PlayDamageEffect;
        }
        DOTween.Kill(this);
    }

    void Update()
    {
        if (train == null || needleRectTransform == null) return;

        // 속도 비율 계산
        float currentSpeed = train.CurrentSpeed;
        float minSpeed = train.GetDeathSpeed();
        float maxSpeed = train.MaxSpeedValue;
        float speedRange = maxSpeed - minSpeed;
        float speedOffset = currentSpeed - minSpeed;
        float speedRatio = 0f;
        if (speedRange > 0) speedRatio = Mathf.Clamp01(speedOffset / speedRange);

        // 바늘 회전
        float targetAngleZ = Mathf.Lerp(minAngle, maxAngle, speedRatio);
        currentAngleZ = Mathf.LerpAngle(
            currentAngleZ,
            targetAngleZ,
            Time.deltaTime * needleSmoothSpeed
        );
        needleRectTransform.rotation = Quaternion.Euler(0, 0, currentAngleZ);
    }

    /// <summary>
    /// 피격 효과를 재생하는 함수 (Train 이벤트가 호출)
    /// </summary>
    private void PlayDamageEffect()
    {
        if (isEffectPlaying) return;
        DOTween.Kill(this);
        isEffectPlaying = true;

        float flashLoopDuration = effectDuration / (flashCount * 2);

        // 1. Line 점멸
        if (lineImage != null)
        {
            lineImage.DOColor(flashColor, flashLoopDuration)
                .SetTarget(this)
                .SetLoops(flashCount * 2, LoopType.Yoyo)
                .SetEase(Ease.Linear)
                .OnComplete(() => {
                    lineImage.color = originalLineColor;
                });
        }

        // 2. Panal 점멸
        if (panelImage != null)
        {
            panelImage.DOColor(flashColor, flashLoopDuration)
                .SetTarget(this)
                .SetLoops(flashCount * 2, LoopType.Yoyo)
                .SetEase(Ease.Linear)
                .OnComplete(() => {
                    panelImage.color = originalPanelColor;
                });
        }

        // --- [핵심 수정] ---

        // 3. Line 흔들기 (X, Y축 모두 랜덤)
        Tween lineShake = null; // [수정] 트윈을 저장할 변수
        if (lineRectTransform != null)
        {
            // [수정] 트윈을 변수에 저장
            lineShake = lineRectTransform.DOShakeAnchorPos(
                effectDuration,
                new Vector3(shakeStrength, shakeStrength, 0),
                shakeVibrato, 90, false, true
            ).SetTarget(this);
        }

        // 4. Panal 흔들기 (X, Y축 모두 랜덤 - Line과 독립적으로)
        if (panelRectTransform != null)
        {
            panelRectTransform.DOShakeAnchorPos(
                effectDuration,
                new Vector3(shakeStrength, shakeStrength, 0),
                shakeVibrato, 90, false, true
            ).SetTarget(this).OnComplete(() => {
                isEffectPlaying = false; // Panal이 끝나면 플래그 해제
            });
        }
        else
        {
            // [수정] Panal이 없다면, 'lineShake' 트윈에 OnComplete를 연결
            lineShake?.OnComplete(() => {
                isEffectPlaying = false; // Line이 끝나면 플래그 해제
            });
        }
        // --- [수정 끝] ---
    }
}