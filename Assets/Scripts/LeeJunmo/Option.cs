using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // ✨ [필수 추가]

public class Option : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject optionPanel;
    public Slider BGMSlider;
    public Slider SFXSlider;

    [Header("State")]
    private bool isOptionOpen = false;
    private float previousTimeScale = 1f;

    void Start()
    {
        if (optionPanel != null)
            optionPanel.SetActive(false);

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
        // ✨ [핵심 수정] 구버전 Input 대신 신버전 InputSystem 사용
        // 키보드가 연결되어 있고, ESC 키가 눌렸는지 확인
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleOptionPanel();
        }
    }

    public void ToggleOptionPanel()
    {
        isOptionOpen = !isOptionOpen;

        if (isOptionOpen)
        {
            optionPanel.SetActive(true);
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            optionPanel.SetActive(false);
            Time.timeScale = previousTimeScale;
        }
    }

    public void CloseOption()
    {
        if (isOptionOpen)
        {
            ToggleOptionPanel();
        }
    }

    void UpdateBGMVolume(float value)
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetBGMVolume(value / 100f);
        }
    }

    void UpdateSFXVolume(float value)
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetSFXVolume(value / 100f);
        }
    }
}