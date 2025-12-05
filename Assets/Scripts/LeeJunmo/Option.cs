using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Option : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject optionPanel;
    public Slider BGMSlider;
    public Slider SFXSlider;

    // 내부 상태 변수는 이제 필요 없거나 최소화됩니다.
    // GameManager의 상태(GameState.Pause)를 신뢰합니다.

    void Start()
    {
        if (optionPanel != null) optionPanel.SetActive(false);

        // 사운드 매니저 연동 초기화
        if (SoundManager.instance != null)
        {
            BGMSlider.value = SoundManager.instance.GetBGMVolume() * 100f;
            SFXSlider.value = SoundManager.instance.GetSFXVolume() * 100f;
        }
        else
        {
            BGMSlider.value = 100;
            SFXSlider.value = 100;
        }

        BGMSlider.minValue = 0;
        BGMSlider.maxValue = 100;
        SFXSlider.minValue = 0;
        SFXSlider.maxValue = 100;

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

        // 1. 게임 진행 중 (Playing, Boss) -> 일시정지 (옵션 열기)
        if (currentState == GameState.Playing || currentState == GameState.Boss)
        {
            GameManager.Instance.PauseGame();
            optionPanel.SetActive(true);
        }
        // 2. 일시정지 상태 (Pause) -> 게임 재개 (옵션 닫기)
        else if (currentState == GameState.Pause)
        {
            GameManager.Instance.ResumeGame();
            optionPanel.SetActive(false);
        }
        // 3. 그 외 (Event, Die, Start) -> 무시 (열지 않음)
        else
        {
            // Debug.Log("현재 상태에서는 옵션을 열 수 없습니다.");
        }
    }

    // 닫기 버튼용
    public void CloseOption()
    {
        if (GameManager.Instance.CurrentState == GameState.Pause)
        {
            ToggleOptionPanel(); // ResumeGame 호출됨
        }
    }

    void UpdateBGMVolume(float value)
    {
        if (SoundManager.instance != null) SoundManager.instance.SetBGMVolume(value / 100f);
    }

    void UpdateSFXVolume(float value)
    {
        if (SoundManager.instance != null) SoundManager.instance.SetSFXVolume(value / 100f);
    }
}