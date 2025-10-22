using UnityEngine;
using TMPro; // TextMeshPro�� ����ϱ� ���� �ʼ�!

public class SimpleSpeedUI : MonoBehaviour
{
    [Header("������ ������Ʈ")]
    [Tooltip("�ӵ� ������ ������ TrainController")]
    public TrainController trainController;

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
        // TrainController�� �Ҵ�Ǿ����� Ȯ��
        if (trainController != null)
        {
            float currentSpeed = trainController.CurrentSpeed;

            int displaySpeed = Mathf.RoundToInt(currentSpeed);

            speedText.text = $"Speed {displaySpeed} Km/h";

        }
        else
        {
            // ������ �� �Ǿ��� �� ���� �޽��� ǥ��
            speedText.text = "Controller ����";
        }
    }
}