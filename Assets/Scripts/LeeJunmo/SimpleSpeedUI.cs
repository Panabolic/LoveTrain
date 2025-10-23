using UnityEngine;
using TMPro; // TextMeshPro�� ����ϱ� ���� �ʼ�!

public class SimpleSpeedUI : MonoBehaviour
{
    [Header("������ ������Ʈ")]
    [Tooltip("�ӵ� ������ ������ TrainController")]
    public Train train;

    [Tooltip("�ӵ��� ǥ���� TextMeshPro UI ������Ʈ")]
    public TextMeshProUGUI speedText;

    void Awake()
    {
        if (speedText == null)
        {
            speedText = GetComponent<TextMeshProUGUI>();
        }
    }

    // Update�� �� �����Ӹ��� ȣ��˴ϴ�.
    void Update()
    {
        // train�� �Ҵ�Ǿ����� Ȯ��
        if (train != null)
        {
            float currentSpeed = train.CurrentSpeed;
            int displaySpeed = Mathf.RoundToInt(currentSpeed);

            speedText.text = $"{displaySpeed}";

        }
        else
        {
            // ������ �� �Ǿ��� �� ���� �޽��� ǥ��
            speedText.text = "Controller ����";
        }
    }
}