using UnityEngine;
using UnityEngine.UI; // Slider�� ����ϱ� ���� �ʼ�!

public class ProgressUIController : MonoBehaviour
{
    [Header("�ð� ����")]
    [Tooltip("��ǥ �ð�(�� ����). (�⺻��: 3�� = 180��)")]
    [SerializeField] private float totalTimeInSeconds = 180f;

    [Header("UI ����")]
    [Tooltip("���൵�� ǥ���� �����̴�(Slider) ������Ʈ")]
    [SerializeField] private Slider progressSlider;

    // ������� �帥 �ð��� ������ ����
    private float currentTime = 0f;

    // Ÿ�̸Ӱ� ���� ������ Ȯ���ϴ� ����
    private bool isTimerRunning = true;

    void Start()
    {
        // ���� �� �����̴� �� 0���� �ʱ�ȭ
        if (progressSlider != null)
        {
            progressSlider.value = 0;
        }
        currentTime = 0f;
        isTimerRunning = true;
    }

    void Update()
    {
        // Ÿ�̸Ӱ� ����ų� �����̴��� ������� �ʾ����� �������� ����
        if (!isTimerRunning || progressSlider == null)
        {
            return;
        }

        // 1. �� �����Ӹ��� �ð�(Time.deltaTime)�� �����ݴϴ�.
        currentTime += Time.deltaTime;

        // 2. ���� ���൵ ��� (���� �ð� / �� �ð�)
        //    Mathf.Clamp01�� ����� ���� 0.0 ~ 1.0 ������ ����� �ʰ� �մϴ�.
        float progress = Mathf.Clamp01(currentTime / totalTimeInSeconds);

        // 3. ���� ���൵�� �����̴��� value ���� �����մϴ�.
        progressSlider.value = progress;

        // 4. ���൵�� 1.0 (100%)�� �����ϸ� Ÿ�̸Ӹ� ����ϴ�.
        if (progress >= 1.0f)
        {
            isTimerRunning = false;
            OnTimeFinished();
        }
    }

    /// <summary>
    /// �ð��� ��� ������� �� ȣ��Ǵ� �Լ�
    /// </summary>
    private void OnTimeFinished()
    {
        Debug.Log("�ð� ����!");
        // (����) ���⿡ �������� ���� �Ǵ� ���� ������� �Ѿ�� ������ �߰��� �� �ֽ��ϴ�.
    }
}