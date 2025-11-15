using UnityEngine;
using TMPro;
using DG.Tweening; // DOTween 사용
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using System; // Action 콜백을 위해 필요

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    [Header("UI 오브젝트")]
    [SerializeField] private GameObject eventUIPanel;
    [SerializeField] private RectTransform eventBoardRect;
    [SerializeField] private GameObject eventImage;
    [SerializeField] private GameObject eventSelections; // 선택지 버튼들의 부모 오브젝트

    [Header("텍스트 컴포넌트")]
    [SerializeField] private TextMeshProUGUI eventTitleBox;
    [SerializeField] private TextMeshProUGUI eventTextBox;

    [Header("애니메이션 설정")]
    [SerializeField] private float panelMoveDuration = 0.5f;

    [Header("패널 위치 (Anchored Position)")]
    [SerializeField] private Vector2 onScreenPosition = new Vector2(0, 0);
    [SerializeField] private Vector2 offScreenPeekPosition = new Vector2(800, 0);
    [SerializeField] private Vector2 offScreenHiddenPosition = new Vector2(1000, 0);

    [Header("데이터베이스")]
    [SerializeField] private EventDatabase eventDatabase; // (이벤트 DB 참조)

    [Header("이벤트 대상")]
    [Tooltip("이벤트 효과가 적용될 대상 (보통 '플레이어')")]
    [SerializeField] private GameObject playerObject;

    private SO_Event currentEvent;

    // --- 텍스트 출력을 위한 변수들 ---
    private Tween currentTypingTween;
    private bool isTyping = false;
    private string fullTextToSkipTo = "";
    private bool isShowingResultText = false;

    // --- 애니메이션 및 상태 관리를 위한 변수들 ---
    private bool isPanelOnScreen = false;
    private bool isTextFullyDisplayed = false;
    private bool hasSelectionBeenMade = false;
    private bool justSelected = false;
    private bool isAnimatingPanel = false;

    private List<Button> selectionButtons = new List<Button>();

    private void Awake()
    {
        Instance = this;
        // ... (싱글톤 및 참조 체크 로직) ...

        eventBoardRect.anchoredPosition = offScreenHiddenPosition;
        eventUIPanel.SetActive(false);

        if (eventSelections != null)
        {
            selectionButtons.AddRange(eventSelections.GetComponentsInChildren<Button>(true));
        }

        // UnscaledUpdate를 실행하기 위한 가짜 DOTween 루프
        DOTween.To(() => 0f, x => { }, 1f, 1f)
            .SetLoops(-1)
            .SetUpdate(true)
            .OnUpdate(UnscaledUpdate);
    }

    public void StartEvent(SO_Event e)
    {
        if (isAnimatingPanel) return;

        isTextFullyDisplayed = false;
        hasSelectionBeenMade = false;
        isShowingResultText = false;
        isTyping = false;
        fullTextToSkipTo = "";
        justSelected = false;

        Physics2D.simulationMode = SimulationMode2D.Script;
        Time.timeScale = 0f;

        currentEvent = e;
        if (eventTitleBox != null) eventTitleBox.text = e.EventTitle;
        if (eventTextBox != null) eventTextBox.text = ""; // 텍스트 초기화

        InitSelection();

        eventBoardRect.anchoredPosition = offScreenHiddenPosition;
        eventUIPanel.SetActive(true);

        AnimatePanelOnScreen(() =>
        {
            StartCoroutine(TypeText(e.EventText));
        });
    }

    // (TEstEvent 함수 - 테스트용)
    public void RandomEventStart()
    {
        if (isAnimatingPanel) return;

        SO_Event e = eventDatabase.GetRandomEvent();
        if (e != null)
        {
            StartEvent(e);
        }
    }

    private IEnumerator TypeText(string textToType)
    {
        string fullText = "";

        string[] lines = textToType.Split('\n');
        string completeSkippedText = "";
        foreach (string line in lines)
        {
            completeSkippedText += line.Trim() + "\n";
        }
        fullTextToSkipTo = completeSkippedText;
        isTyping = true;
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            int charCount = trimmedLine.Length;
            float duration = charCount * 0.05f;
            currentTypingTween = DOTween.To(
                () => 0,
                (charIndex) => { eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex); },
                charCount, duration
            ).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => { isTyping = false; });
            yield return new WaitUntil(() => !isTyping);
            fullText += trimmedLine + "\n";
            eventTextBox.text = fullText;
            isTyping = true;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        Debug.Log("텍스트 출력 완료");
        isTyping = false;
        fullTextToSkipTo = "";
        isTextFullyDisplayed = true;

        EnableSelections();
    }

    private void UnscaledUpdate()
    {
        if (justSelected) justSelected = false;

        bool skipInput = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        bool toggleInput = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

        // --- 스킵 로직 ---
        if (skipInput && isTyping && !justSelected)
        {
            bool wasResultText = isShowingResultText;

            StopAllCoroutines();
            if (currentTypingTween != null && currentTypingTween.IsActive())
            {
                currentTypingTween.Kill();
            }
            if (!string.IsNullOrEmpty(fullTextToSkipTo))
            {
                eventTextBox.text = fullTextToSkipTo;
            }
            isTyping = false;
            fullTextToSkipTo = "";
            isShowingResultText = false;

            if (!wasResultText)
            {
                isTextFullyDisplayed = true;
                EnableSelections();
            }

            if (wasResultText)
            {
                StartCoroutine(WaitAndClosePanel(2.0f));
            }
            return;
        }

        // --- 패널 토글 로직 ---
        if (toggleInput && !isTyping && isTextFullyDisplayed && !hasSelectionBeenMade)
        {
            if (isAnimatingPanel) return;

            if (isPanelOnScreen)
            {
                AnimatePanelToPeek();
            }
            else
            {
                AnimatePanelOnScreen(null);
            }
        }
    }

    public void SelectionChoice(int selectionIndex)
    {
        if (isAnimatingPanel || currentEvent == null || hasSelectionBeenMade) return;

        justSelected = true;
        hasSelectionBeenMade = true;

        if (isTyping && !isShowingResultText)
        {
            StopAllCoroutines();
            if (currentTypingTween != null && currentTypingTween.IsActive())
            {
                currentTypingTween.Kill();
            }
            if (!string.IsNullOrEmpty(fullTextToSkipTo))
            {
                eventTextBox.text = fullTextToSkipTo;
            }
            isTyping = false;
            fullTextToSkipTo = "";
            isTextFullyDisplayed = true;
        }

        if (eventSelections != null)
        {
            eventSelections.SetActive(false);
        }

        // 1. 선택한 'UI 정보'
        SO_Event.Selection chosenSelection = currentEvent.Selections[selectionIndex];

        // 2. [삭제] staticResultText 변수 삭제
        // string staticResultText = chosenSelection.selectionEndText; 

        // 3. 동적 결과 텍스트를 담을 리스트 (실행 결과)
        List<string> dynamicResultTexts = new List<string>();

        // 4. 실행할 로직(GameEventSO)이 있는지 확인
        if (chosenSelection.eventToTrigger != null)
        {
            if (playerObject != null)
            {
                // 5. [실행!] 로직을 실행하고 '결과 문자열 리스트'를 받습니다.
                dynamicResultTexts = chosenSelection.eventToTrigger.Trigger(playerObject);
            }
            else
            {
                Debug.LogError("EventManager에 'Player Object'가 연결되지 않았습니다!");
            }
        }

        // --- [핵심 수정] ---
        // 6. 모든 텍스트를 하나로 합칩니다.
        string finalResultText = ""; // 빈 문자열로 시작

        if (dynamicResultTexts.Count > 0)
        {
            // 동적 텍스트가 있을 때만 포맷팅하여 할당
            finalResultText = "→ " + string.Join("\n- ", dynamicResultTexts);
        }
        // --- [수정 끝] ---

        // 7. 합쳐진 텍스트로 타이핑 코루틴을 시작합니다.
        isShowingResultText = true;

        // [추가] 텍스트가 아예 없는 경우(예: '무시한다' 선택지)
        if (string.IsNullOrEmpty(finalResultText))
        {
            // 딜레이 없이 즉시 패널 닫기 시작
            StartCoroutine(WaitAndClosePanel(0f));
        }
        else
        {
            StartCoroutine(ShowResultText(finalResultText));
        }
    }

    private IEnumerator ShowResultText(string textToAnimate)
    {
        string fullText = eventTextBox.text + "\n";
        eventTextBox.text = fullText;

        string trimmedLine = textToAnimate.Trim();
        int charCount = trimmedLine.Length;
        float duration = charCount * 0.05f;
        fullTextToSkipTo = fullText + trimmedLine;
        isTyping = true;
        currentTypingTween = DOTween.To(
            () => 0,
            (charIndex) => { eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex); },
            charCount, duration
        ).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => {
            isTyping = false;
            fullTextToSkipTo = "";
        });
        yield return currentTypingTween.WaitForCompletion();
        isShowingResultText = false;
        StartCoroutine(WaitAndClosePanel(2.0f));
    }

    private IEnumerator WaitAndClosePanel(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        AnimatePanelToHidden(true);
    }

    public void InitSelection()
    {
        eventSelections.SetActive(true);
        for (int i = 0; i < eventSelections.transform.childCount; i++)
        {
            Transform selectionUIObject = eventSelections.transform.GetChild(i);
            Button button = selectionButtons.Find(b => b.transform == selectionUIObject);

            if (i < currentEvent.Selections.Count)
            {
                selectionUIObject.gameObject.SetActive(true);
                TextMeshProUGUI[] texts = selectionUIObject.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = currentEvent.Selections[i].selectionText;
                    texts[1].text = currentEvent.Selections[i].selectionUnderText;
                }

                if (button != null) button.interactable = false;
            }
            else
            {
                selectionUIObject.gameObject.SetActive(false);
            }
        }
    }

    private void EnableSelections()
    {
        foreach (Button button in selectionButtons)
        {
            if (button.gameObject.activeInHierarchy)
            {
                button.interactable = true;
            }
        }
    }


    #region --- 애니메이션 함수 ---

    private void AnimatePanelOnScreen(Action onComplete)
    {
        isPanelOnScreen = true;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(onScreenPosition, panelMoveDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false;
                onComplete?.Invoke();
            });
    }

    private void AnimatePanelToPeek()
    {
        isPanelOnScreen = false;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(offScreenPeekPosition, panelMoveDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false;
            });
    }

    private void AnimatePanelToHidden(bool eventEnded)
    {
        isPanelOnScreen = false;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(offScreenHiddenPosition, panelMoveDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false;
                eventUIPanel.SetActive(false);
                if (eventEnded)
                {
                    Time.timeScale = 1f;
                    Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.EndEvent();
                    }
                }
            });
    }

    #endregion
}