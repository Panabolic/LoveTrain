using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Option : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject optionPanel;

    [Header("Volume Sliders (0 ~ 100)")]
    public Slider BGMSlider;
    public Slider SFXSlider;

    [Header("Function Buttons")]
    public Button restartButton; // ✨ 인스펙터에서 연결
    public Button quitButton;    // ✨ 인스펙터에서 연결

    void Start()
    {
        // 1. 패널 초기화
        if (optionPanel != null) optionPanel.SetActive(false);

        // 2. 버튼 기능 연결 (씬 변경 시 참조 끊김 방지를 위해 코드로 연결)
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    // 옵션 창을 닫고(TimeScale 복구 등) 재시작
                    CloseOption();
                    GameManager.Instance.RestartGame();
                }
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            });
        }

        // 3. 슬라이더 초기 설정 (0 ~ 100 범위)
        if (BGMSlider != null) { BGMSlider.minValue = 0; BGMSlider.maxValue = 100; }
        if (SFXSlider != null) { SFXSlider.minValue = 0; SFXSlider.maxValue = 100; }

        // 4. 사운드 매니저와 슬라이더 값 동기화
        if (SoundManager.Instance != null)
        {
            // 현재 실제 볼륨(0~1)을 가져와서 슬라이더(0~100)에 반영
            if (BGMSlider != null) BGMSlider.value = SoundManager.Instance.GetBGMVolume() * 100f;
            if (SFXSlider != null) SFXSlider.value = SoundManager.Instance.GetSFXVolume() * 100f;
        }
        else
        {
            // 매니저가 없으면 기본값
            if (BGMSlider != null) BGMSlider.value = 100;
            if (SFXSlider != null) SFXSlider.value = 100;
        }

        // 5. 이벤트 리스너 등록 (값이 바뀔 때 실행)
        if (BGMSlider != null) BGMSlider.onValueChanged.AddListener(UpdateBGMVolume);
        if (SFXSlider != null) SFXSlider.onValueChanged.AddListener(UpdateSFXVolume);
    }

    void Update()
    {
        // ESC 키 입력 감지 (Input System)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleOptionPanel();
        }
    }

    public void ToggleOptionPanel()
    {
        if (GameManager.Instance == null) return;

        GameState currentState = GameManager.Instance.CurrentState;

        // 게임 진행 중이거나 보스전, 혹은 시작 대기 상태일 때만 옵션 열기 가능
        if (currentState == GameState.Playing || currentState == GameState.Boss || currentState == GameState.Start)
        {
            GameManager.Instance.PauseGame();
            if (optionPanel != null) optionPanel.SetActive(true);
        }
        else if (currentState == GameState.Pause)
        {
            CloseOption();
        }
    }

    public void CloseOption()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Pause)
        {
            // Pause 상태일 때만 닫기 동작 수행 (ResumeGame 호출)
            GameManager.Instance.ResumeGame();
            if (optionPanel != null) optionPanel.SetActive(false);
        }
    }

    void UpdateBGMVolume(float value)
    {
        // 0~100 값을 0~1로 변환하여 SoundManager에 전달
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBGMVolume(value / 100f);
    }

    void UpdateSFXVolume(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSFXVolume(value / 100f);
    }
}