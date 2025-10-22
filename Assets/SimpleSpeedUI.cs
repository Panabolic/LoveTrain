using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필수!

public class SimpleSpeedUI : MonoBehaviour
{
    [Header("참조할 컴포넌트")]
    [Tooltip("속도 정보를 가져올 TrainController")]
    public TrainController trainController;

    [Tooltip("속도를 표시할 TextMeshPro UI 컴포넌트")]
    public TextMeshProUGUI speedText;

    void Awake()
    {
        if (speedText == null)
        {
            speedText = GetComponent<TextMeshProUGUI>();
        }
    }

    // Update는 매 프레임마다 호출됩니다.
    void Update()
    {
        // TrainController가 할당되었는지 확인
        if (trainController != null)
        {
            float currentSpeed = trainController.CurrentSpeed;

            int displaySpeed = Mathf.RoundToInt(currentSpeed);

            speedText.text = $"Speed {displaySpeed} Km/h";

        }
        else
        {
            // 연결이 안 되었을 때 오류 메시지 표시
            speedText.text = "Controller 없음";
        }
    }
}