using UnityEngine;
using DG.Tweening; // DOTween을 사용하기 위해 필요

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [Header("카메라 흔들림 설정")]
    [SerializeField] private float defaultShakeDuration = 0.2f;
    [SerializeField] private float defaultShakeStrength = 0.5f;
    [SerializeField] private int defaultShakeVibrato = 10;
    [SerializeField] private float defaultShakeRandomness = 90f;

    private Transform cameraTransform; // 흔들릴 카메라 Transform

    // ✨ [추가] 카메라의 원래 로컬 위치를 저장할 변수
    private Vector3 originalLocalPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;

            // ✨ [추가] 시작 시 카메라의 '로컬' 위치를 저장합니다.
            // (카메라가 다른 부모 오브젝트에 속해 있을 경우를 대비해 월드 좌표 대신 로컬 좌표 사용)
            originalLocalPosition = cameraTransform.localPosition;
        }
        else
        {
            Debug.LogError("씬에 'MainCamera' 태그를 가진 카메라가 없습니다! 카메라를 찾을 수 없습니다.", this);
            this.enabled = false;
        }
    }

    /// <summary>
    /// 카메라를 기본 설정으로 흔듭니다.
    /// </summary>
    public void ShakeCamera()
    {
        if (cameraTransform == null) return;

        // ✨ [핵심 수정 1] 기존 흔들림 중지 및 위치 리셋
        // 현재 진행 중인 위치 관련 DOTween 트윈을 즉시 완료 상태로 중지
        cameraTransform.DOKill(true);
        // 카메라 위치를 원래 로컬 위치로 즉시 복원
        cameraTransform.localPosition = originalLocalPosition;

        // ✨ [핵심 수정 2] 새 흔들림 시작
        cameraTransform.DOShakePosition(
            defaultShakeDuration,
            defaultShakeStrength,
            defaultShakeVibrato,
            defaultShakeRandomness
        ).OnComplete(() => {
            // ✨ [추가] 흔들림이 '정상적으로' 완료되었을 때도 위치를 보정
            // (혹시 모를 미세한 오차 방지)
            cameraTransform.localPosition = originalLocalPosition;
        });
    }

    /// <summary>
    /// 사용자 정의 설정으로 카메라를 흔듭니다.
    /// </summary>
    public void ShakeCamera(float duration, float strength, int vibrato = 10, float randomness = 90f)
    {
        if (cameraTransform == null) return;

        // ✨ [핵심 수정 1] 기존 흔들림 중지 및 위치 리셋
        cameraTransform.DOKill(true);
        cameraTransform.localPosition = originalLocalPosition;

        // ✨ [핵심 수정 2] 새 흔들림 시작
        cameraTransform.DOShakePosition(duration, strength, vibrato, randomness)
            .OnComplete(() => {
                // ✨ [추가] 흔들림 완료 시 위치 보정
                cameraTransform.localPosition = originalLocalPosition;
            });
    }

    /// <summary>
    /// 카메라 흔들림을 즉시 멈추고 원래 위치로 복원합니다.
    /// </summary>
    public void StopShake()
    {
        if (cameraTransform != null)
        {
            // ✨ [수정] 트윈을 멈추고 즉시 원래 위치로 복원
            cameraTransform.DOKill(true);
            cameraTransform.localPosition = originalLocalPosition;
        }
    }
}