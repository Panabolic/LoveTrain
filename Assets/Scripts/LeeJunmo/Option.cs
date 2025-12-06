using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Option : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject optionPanel;
    public Slider BGMSlider;
    public Slider SFXSlider;

    void Start()
    {
        if (optionPanel != null) optionPanel.SetActive(false);

        // 사운드 매니저와 슬라이더 연동
        // (SoundManager가 아직 생성 안 됐을 경우를 대비해 null 체크)
        if (SoundManager.Instance != null)
        {
            // 0~1 사이 값으로 가져와서 0~100 슬라이더에 맞춤
            BGMSlider.value = SoundManager.Instance.GetBGMVolume() * 100f;
            SFXSlider.value = SoundManager.Instance.GetSFXVolume() * 100f;
        }
        else
        {
            BGMSlider.value = 100;
            SFXSlider.value = 100;
        }

        // 슬라이더 범위 설정 (0 ~ 100)
        BGMSlider.minValue = 0;
        BGMSlider.maxValue = 100;
        SFXSlider.minValue = 0;
        SFXSlider.maxValue = 100;

        // 이벤트 리스너 등록
        BGMSlider.onValueChanged.AddListener(UpdateBGMVolume);
        SFXSlider.onValueChanged.AddListener(UpdateSFXVolume);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleOptionPanel();
        }
    }

    public void ToggleOptionPanel()
    {
        if (GameManager.Instance == null) return;

        GameState currentState = GameManager.Instance.CurrentState;

        // Start, Playing, Boss 상태에서 옵션 열기 허용
        if (currentState == GameState.Playing || currentState == GameState.Boss || currentState == GameState.Start)
        {
            GameManager.Instance.PauseGame();
            optionPanel.SetActive(true);
        }
        else if (currentState == GameState.Pause)
        {
            GameManager.Instance.ResumeGame();
            optionPanel.SetActive(false);
        }
    }

    public void CloseOption()
    {
        if (GameManager.Instance.CurrentState == GameState.Pause)
        {
            // Pause 상태일 때만 닫기 동작 수행 (ResumeGame 호출)
            GameManager.Instance.ResumeGame();
            optionPanel.SetActive(false);
        }
    }

    void UpdateBGMVolume(float value)
    {
        // 0~100 값을 0~1로 변환하여 전달
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBGMVolume(value / 100f);
    }

    void UpdateSFXVolume(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSFXVolume(value / 100f);
    }
}