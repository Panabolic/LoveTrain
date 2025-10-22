using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;
using NUnit.Framework;
using System.Collections; // 코루틴 사용을 위해 추가
using UnityEngine.InputSystem;
public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    [SerializeField]
    private GameObject eventUIPanel;
    [SerializeField]
    private GameObject eventImage;
    [SerializeField]
    private GameObject eventSelections;
    [SerializeField]
    private TextMeshProUGUI eventTextBox; // TextMeshProUGUI 컴포넌트를 직접 참조하도록 변경
    [Header("텍스트 컴포넌트")]
    [SerializeField]
    private TextMeshProUGUI eventTitleBox; // ✨ [추가] 제목 텍스트

    // --- 텍스트 출력을 위한 변수들 ---
    private Tween currentTypingTween; // 현재 실행 중인 DOText 트윈을 제어하기 위한 변수
    private bool isTyping = false; // 텍스트가 타이핑 중인지 확인하는 플래그

    private void Awake()
    {
        Instance = this;

        if (Instance == null)
        {
            Instantiate(this);
        }
    }

    [SerializeField]
    private EventDatabase eventDatabase;

    private SO_Event currentEvent;

    public void StartEvent(SO_Event e)
    {
        currentEvent = e;
        eventUIPanel.SetActive(true);
        eventImage.GetComponent<Image>().sprite = e.EventSprite;
        InitSelection();

        // 기존에 실행 중인 코루틴이 있다면 중지하고 새로 시작
        StopAllCoroutines();
        StartCoroutine(TypeText(e.EventText));
    }


    public void TEstEvent()
    {
        SO_Event e = eventDatabase.GetRandomEvent();
        currentEvent = e;
        eventUIPanel.SetActive(true);
        eventImage.GetComponent<Image>().sprite = e.EventSprite;

        // 기존에 실행 중인 코루틴이 있다면 중지하고 새로 시작
        StopAllCoroutines();
        StartCoroutine(TypeText(e.EventText));
    }


    /// <summary>
    /// DOTween을 사용해 텍스트를 출력하고, 출력이 끝나면 스크롤을 활성화하는 코루틴
    /// </summary>
    private IEnumerator TypeText(string textToType)
    {
        // --- 1. 코루틴 시작 시 스크롤 기능 비활성화 ---
        if (eventScrollRect != null)
        {
            eventScrollRect.enabled = false; // 사용자 스크롤 입력을 막습니다.
        }
        if (verticalScrollbar != null)
        {
            verticalScrollbar.gameObject.SetActive(false); // 스크롤바를 숨깁니다.
        }

        string fullText = "";
        eventTextBox.text = fullText;

        string[] lines = textToType.Split('\n');

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            int charCount = trimmedLine.Length;
            // 새 줄의 길이에 비례한 정확한 시간
            float duration = charCount * 0.05f;

            isTyping = true;

            // --- DOText 대신 DOTween.To()를 사용합니다 ---
            // 0부터 새 줄의 글자 수(charCount)까지 숫자를 변화시키는 애니메이션을 만듭니다.
            currentTypingTween = DOTween.To(
                () => 0, // 시작 값
                (charIndex) => { // 애니메이션이 진행되는 동안 매 프레임 실행
                                 // 이전 텍스트 + 현재 줄의 일부를 합쳐서 실시간으로 표시
                    eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex);
                },
                charCount, // 최종 값
                duration   // 지속 시간
            ).SetEase(Ease.Linear).OnComplete(() => {
                isTyping = false;
            });

            yield return new WaitUntil(() => !isTyping);

            // 한 줄이 끝나면 전체 텍스트를 업데이트하고 줄바꿈을 추가
            fullText += trimmedLine + "\n";
            eventTextBox.text = fullText; // 최종 텍스트 보정

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("텍스트 출력 완료");
        InitSelection();
        // --- 2. 모든 텍스트 출력이 끝난 후 스크롤 기능 활성화 ---
        yield return null; // Content Fitter가 최종 높이를 계산할 시간을 줍니다.

        if (eventScrollRect != null)
        {
            // 스크롤이 필요할 경우에만 스크롤 기능을 다시 활성화합니다.
            if (eventTextBox.rectTransform.rect.height > eventScrollRect.viewport.rect.height)
            {
                eventScrollRect.enabled = true;
                CheckScrollbarVisibility(); // 스크롤바를 표시할지 결정
            }
        }
    }

    private void Update()
    {
        // 스킵 입력 감지
        bool skipInputPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (skipInputPressed && isTyping)
        {
            currentTypingTween.Complete();
        }

        // 타이핑 중 자동 스크롤 (사용자 입력과 무관하게 코드로 제어)
        if (isTyping && eventScrollRect != null)
        {
            eventScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void CheckScrollbarVisibility()
    {
        if (eventScrollRect != null && verticalScrollbar != null)
        {
            // Content(텍스트박스)의 높이가 Viewport보다 클 때만 스크롤바를 활성화합니다.
            bool requiresScroll = eventTextBox.rectTransform.rect.height > eventScrollRect.viewport.rect.height;
            verticalScrollbar.gameObject.SetActive(requiresScroll);
        }
    }


    /// <summary>
    /// 선택지 버튼을 클릭했을 때 호출될 함수
    /// </summary>
    /// <param name="selectionIndex">몇 번째 선택지를 골랐는지 (0부터 시작)</param>
    public void SelectionChoice(int selectionIndex)
    {
        // 현재 이벤트 데이터가 없으면 함수 종료
        if (currentEvent == null) return;

        // 다른 선택지를 또 누르지 못하도록 선택지 UI를 비활성화
        if (eventSelections != null)
        {
            eventSelections.SetActive(false);
        }

        // 선택한 결과 텍스트를 가져옴
        string resultText = currentEvent.Selections[selectionIndex].selectionEndText;

        // 결과 텍스트를 출력하는 새로운 코루틴을 시작
        StartCoroutine(ShowResultText(resultText));
    }

    /// <summary>
    /// 결과 텍스트를 타이핑 효과로 출력하는 코루틴
    /// </summary>
    private IEnumerator ShowResultText(string textToAnimate)
    {
        // 기존 텍스트에 공백을 만들기 위해 두 번 줄바꿈
        string fullText = eventTextBox.text + "\n";
        eventTextBox.text = fullText;

        // 스크롤이 필요할 수 있으니 맨 아래로 이동
        yield return null;
        if (eventScrollRect != null) eventScrollRect.verticalNormalizedPosition = 0f;

        string trimmedLine = textToAnimate.Trim();
        int charCount = trimmedLine.Length;
        float duration = charCount * 0.05f;

        isTyping = true;

        // 이전에 사용했던 DOTween.To() 방식으로 결과 텍스트를 타이핑
        currentTypingTween = DOTween.To(
            () => 0,
            (charIndex) => {
                eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex);
            },
            charCount,
            duration
        ).SetEase(Ease.Linear).OnComplete(() => {
            isTyping = false;
        });

        // 타이핑이 끝날 때까지 대기
        yield return new WaitUntil(() => !isTyping);

        // 모든 출력이 끝났으니 최종적으로 스크롤 기능 활성화 여부 결정
        if (eventScrollRect != null)
        {
            if (eventTextBox.rectTransform.rect.height > eventScrollRect.viewport.rect.height)
            {
                eventScrollRect.enabled = true;
                CheckScrollbarVisibility();
            }
        }
    }

    public void InitSelection()
    {
        eventSelections.SetActive(true);
        // UI에 배치된 모든 선택지 슬롯(Selection1, Selection2 등)을 순회합니다.
        for (int i = 0; i < eventSelections.transform.childCount; i++)
        {
            // 현재 순번의 UI 선택지 자식 오브젝트를 가져옵니다. (예: "Selection1")
            Transform selectionUIObject = eventSelections.transform.GetChild(i);

            // SO_Event에 현재 UI 슬롯에 해당하는 데이터가 있는지 확인합니다.
            if (i < currentEvent.Selections.Count)
            {
                // 이전에 비활성화되었을 수 있으니, UI 오브젝트를 활성화합니다.
                selectionUIObject.gameObject.SetActive(true);

                // 이 UI 오브젝트의 자식들에서 TextMeshPro 컴포넌트를 모두 찾습니다.
                // 첫 번째 컴포넌트가 메인 텍스트, 두 번째가 보조 텍스트라고 가정합니다.
                TextMeshProUGUI[] texts = selectionUIObject.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 2)
                {
                    // 스크립터블 오브젝트의 텍스트를 할당합니다.
                    texts[0].text = currentEvent.Selections[i].selectionText;
                    texts[1].text = currentEvent.Selections[i].selectionUnderText;
                }
                else
                {
                    Debug.LogWarning($"선택지 오브젝트 '{selectionUIObject.name}'에 TextMeshProUGUI 컴포넌트가 2개 미만입니다.", this);
                }
            }
            else
            {
                // 만약 이 UI 슬롯에 해당하는 데이터가 없다면, 비활성화하여 보이지 않게 합니다.
                selectionUIObject.gameObject.SetActive(false);
            }
        }
    }
}
