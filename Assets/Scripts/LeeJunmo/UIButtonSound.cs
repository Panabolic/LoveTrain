using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField] private SoundID clickSound = SoundID.UI_Click; // 기본 클릭음

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        // 1. 이벤트 버스 방식 (추천)
        SoundEventBus.Publish(clickSound);

        // 2. 혹은 SoundManager 직접 호출 방식 (사용자님이 만드신 함수가 있다면)
        // SoundManager.Instance.PlayButtonSound(); 
    }
}