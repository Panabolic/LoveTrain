using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class SpeedMeterUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("기차의 Train 스크립트")]
    [SerializeField] private Train train;
    [Tooltip("계기판 바늘의 RectTransform")]
    [SerializeField] private RectTransform needleRectTransform;

    [Header("효과 참조")]
    [SerializeField] private RectTransform lineRectTransform;
    [SerializeField] private RectTransform panelRectTransform;

    [Header("점멸 참조")]
    [SerializeField] private Image lineImage;
    [SerializeField] private Image panelImage;

    [Header("회전 설정 (구간별 각도)")]
    [Tooltip("속도가 0일 때 (완전 사망) 바늘 각도 (예: 180)")]
    [SerializeField] private float angleAtZeroSpeed = 180f;

    [Tooltip("속도가 사망 임계값(160)일 때 바늘 각도 (예: 135)")]
    [SerializeField] private float angleAtThreshold = 135f;

    [Tooltip("속도가 최대일 때 바늘 각도 (예: 0 or -45)")]
    [SerializeField] private float angleAtMaxSpeed = 0f;

    [Header("아날로그 설정")]
    [SerializeField] private float needleSmoothSpeed = 5f;

    [Header("피격 효과 설정")]
    [SerializeField] private float effectDuration = 0.5f;
    [SerializeField] private float shakeStrength = 15f;
    [SerializeField] private int shakeVibrato = 20;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private int flashCount = 3;

    private float currentAngleZ;
    private bool isEffectPlaying = false;
    private Color originalLineColor;
    private Color originalPanelColor;

    void Start()
    {
        // 시작 시 최대 속도 각도로 초기화 (혹은 현재 속도에 맞춰짐)
        currentAngleZ = angleAtMaxSpeed;

        if (needleRectTransform != null)
            needleRectTransform.rotation = Quaternion.Euler(0, 0, currentAngleZ);

        if (train != null) train.OnTrainDamaged += PlayDamageEffect;

        if (lineImage != null) originalLineColor = lineImage.color;
        if (panelImage != null) originalPanelColor = panelImage.color;
    }

    void OnDestroy()
    {
        if (train != null) train.OnTrainDamaged -= PlayDamageEffect;
        DOTween.Kill(this);
    }

    void Update()
    {
        if (train == null || needleRectTransform == null) return;

        // 1. 현재 속도 및 기준값 가져오기
        float currentSpeed = train.CurrentSpeed;
        float thresholdSpeed = train.GetDeathSpeed(); // 160
        float maxSpeed = train.MaxSpeedValue;         // 460

        float targetAngleZ = 0f;

        // 2. 구간별 각도 계산 (핵심 수정)
        if (currentSpeed >= thresholdSpeed)
        {
            // [정상 구간] 임계값(160) ~ 최대(460)
            // Ratio: 0(임계값) ~ 1(최대)
            float range = maxSpeed - thresholdSpeed;
            float ratio = (range > 0) ? (currentSpeed - thresholdSpeed) / range : 0f;

            // 135도 -> 0도(설정값)로 이동
            targetAngleZ = Mathf.Lerp(angleAtThreshold, angleAtMaxSpeed, Mathf.Clamp01(ratio));
        }
        else
        {
            // [위험 구간] 0 ~ 임계값(160)
            // Ratio: 0(정지) ~ 1(임계값)
            float ratio = (thresholdSpeed > 0) ? currentSpeed / thresholdSpeed : 0f;

            // 180도 -> 135도로 이동
            targetAngleZ = Mathf.Lerp(angleAtZeroSpeed, angleAtThreshold, Mathf.Clamp01(ratio));
        }

        // 3. 부드러운 회전 적용
        currentAngleZ = Mathf.LerpAngle(currentAngleZ, targetAngleZ, Time.deltaTime * needleSmoothSpeed);
        needleRectTransform.rotation = Quaternion.Euler(0, 0, currentAngleZ);
    }

    private void PlayDamageEffect()
    {
        if (isEffectPlaying) return;
        DOTween.Kill(this);
        isEffectPlaying = true;

        float flashLoopDuration = effectDuration / (flashCount * 2);

        if (lineImage != null)
        {
            lineImage.DOColor(flashColor, flashLoopDuration)
                .SetTarget(this).SetLoops(flashCount * 2, LoopType.Yoyo)
                .SetEase(Ease.Linear).OnComplete(() => lineImage.color = originalLineColor);
        }

        if (panelImage != null)
        {
            panelImage.DOColor(flashColor, flashLoopDuration)
                .SetTarget(this).SetLoops(flashCount * 2, LoopType.Yoyo)
                .SetEase(Ease.Linear).OnComplete(() => panelImage.color = originalPanelColor);
        }

        Tween lineShake = null;
        if (lineRectTransform != null)
        {
            lineShake = lineRectTransform.DOShakeAnchorPos(effectDuration, new Vector3(shakeStrength, shakeStrength, 0), shakeVibrato, 90, false, true).SetTarget(this);
        }

        if (panelRectTransform != null)
        {
            panelRectTransform.DOShakeAnchorPos(effectDuration, new Vector3(shakeStrength, shakeStrength, 0), shakeVibrato, 90, false, true)
                .SetTarget(this).OnComplete(() => isEffectPlaying = false);
        }
        else
        {
            lineShake?.OnComplete(() => isEffectPlaying = false);
        }
    }
}