using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필수!

public class SimpleSpeedUI : MonoBehaviour
{
    [Header("참조할 컴포넌트")]
    [Tooltip("속도 정보를 가져올 TrainController")]
    public Train train;

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
        // train이 할당되었는지 확인
        if (train != null && GameManager.Instance.CurrentState != GameState.Start)
        {
            float currentSpeed = train.CurrentSpeed;
            int displaySpeed = Mathf.RoundToInt(currentSpeed);

            speedText.text = $"{displaySpeed}";
            foreach (Animator carAnim in train.carsAnim)
            {
                Debug.Log(carAnim.GetFloat("moveSpeed"));
            }

            // displaySpeed에 따른 Train animation clip speed 조절
            foreach (Animator carAnim in train.carsAnim)
            {
                carAnim.SetFloat("moveSpeed", Mathf.Clamp(displaySpeed, 0, 300) * 0.05f);
            }
        }
        else
        {
            foreach (Animator carAnim in train.carsAnim)
            {
                Debug.Log(carAnim.GetFloat("moveSpeed"));
            }
            // 연결이 안 되었을 때 오류 메시지 표시
            speedText.text = "Controller 없음";
        }
    }
}