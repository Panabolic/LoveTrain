using UnityEngine;
using DG.Tweening; // DOTween�� ����ϱ� ���� �ʼ�

// �� ��ũ��Ʈ�� �߰��ϸ� CanvasGroup ������Ʈ�� �ڵ����� �߰��˴ϴ�.
[RequireComponent(typeof(CanvasGroup))]
public class UIAlphaFader : MonoBehaviour
{
    [Header("���̵� ����")]
    [Tooltip("���̵� ��/�ƿ��� �ɸ��� �ð� (��)")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("Start() �Լ� ȣ�� �� �ڵ����� ���̵� ���� �������� ����")]
    [SerializeField] private bool fadeInOnStart = true;

    [Tooltip("Awake() ������ UI�� �����ϰ�(alpha=0) �������� ����")]
    [SerializeField] private bool startTransparent = true;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // RequireComponent�� ���� canvasGroup�� �׻� ������
        canvasGroup = GetComponent<CanvasGroup>();

        if (startTransparent)
        {
            // ���� �� ���� �����ϰ� ����
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
    /// UI �׷��� ������ ��Ÿ���� �մϴ� (Fade In).
    /// </summary>
    public void FadeIn()
    {
        // DOTween�� ����� alpha ���� 1��(������) ����
        canvasGroup.DOFade(1f, fadeDuration)
            .SetUpdate(true); // Time.timeScale�� 0�� ���� (�Ͻ����� ��) �۵�
    }

    /// <summary>
    /// UI �׷��� ������ ������� �մϴ� (Fade Out).
    /// </summary>
    public void FadeOut()
    {
        // DOTween�� ����� alpha ���� 0����(����) ����
        canvasGroup.DOFade(0f, fadeDuration)
            .SetUpdate(true); // Time.timeScale�� 0�� ���� (�Ͻ����� ��) �۵�
    }
}