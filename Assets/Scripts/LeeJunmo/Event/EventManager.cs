using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    [Header("UI 오브젝트")]
    [SerializeField] private GameObject eventUIPanel;
    [SerializeField] private RectTransform eventBoardRect;
    [SerializeField] private GameObject eventImage;
    [SerializeField] private GameObject eventSelections;

    [Header("텍스트 및 스크롤")]
    [SerializeField] private TextMeshProUGUI eventTitleBox;
    [SerializeField] private TextMeshProUGUI eventTextBox;
    [SerializeField] private ScrollRect eventTextScrollRect; // ✨ 스크롤 뷰

    [Header("애니메이션 설정")]
    [SerializeField] private float panelMoveDuration = 0.5f;
    [SerializeField] private Vector2 onScreenPosition = new Vector2(0, 0);
    [SerializeField] private Vector2 offScreenPeekPosition = new Vector2(800, 0);
    [SerializeField] private Vector2 offScreenHiddenPosition = new Vector2(1000, 0);

    [Header("데이터베이스")]
    [SerializeField] private EventDatabase eventDatabase;

    [Header("이벤트 대상")]
    [SerializeField] private GameObject playerObject;

    private SO_Event currentEvent;
    private List<Button> selectionButtons = new List<Button>();

    // --- 상태 변수들 ---
    private Tween currentTypingTween;
    private bool isTyping = false;
    private string fullTextToSkipTo = "";
    private bool isShowingResultText = false;
    private bool isPanelOnScreen = false;
    private bool isTextFullyDisplayed = false;
    private bool hasSelectionBeenMade = false;
    private bool justSelected = false;
    private bool isAnimatingPanel = false;

    private void Awake()
    {
        Instance = this;
        eventBoardRect.anchoredPosition = offScreenHiddenPosition;
        eventUIPanel.SetActive(false);

        if (eventSelections != null)
        {
            selectionButtons.AddRange(eventSelections.GetComponentsInChildren<Button>(true));
        }

        DOTween.To(() => 0f, x => { }, 1f, 1f)
            .SetLoops(-1)
            .SetUpdate(true)
            .OnUpdate(UnscaledUpdate);
    }

    // ✨ [수정] 외부에서 이벤트를 요청할 때 사용 (큐에 등록)
    public void RequestEvent(SO_Event e)
    {
        GameManager.Instance.RegisterUIQueue(() => ProcessEvent(e));
    }

    // ✨ [추가] 큐에서 호출되는 실제 이벤트 실행 로직
    private void ProcessEvent(SO_Event e)
    {
        if (isAnimatingPanel) return;

        isTextFullyDisplayed = false;
        hasSelectionBeenMade = false;
        isShowingResultText = false;
        isTyping = false;
        fullTextToSkipTo = "";
        justSelected = false;

        // GameManager에서 이미 시간을 멈췄으므로 여기서 Time.timeScale 조작 안 함

        currentEvent = e;
        if (eventTitleBox != null) eventTitleBox.text = e.EventTitle;
        if (eventTextBox != null) eventTextBox.text = "";

        // 스크롤 초기화
        if (eventTextScrollRect != null) eventTextScrollRect.verticalNormalizedPosition = 1f;

        InitSelection();

        eventBoardRect.anchoredPosition = offScreenHiddenPosition;
        eventUIPanel.SetActive(true);

        AnimatePanelOnScreen(() =>
        {
            StartCoroutine(TypeText(e.EventText));
        });
    }

    public void RandomEventStart()
    {
        if (isAnimatingPanel) return;
        SO_Event e = eventDatabase.GetRandomEvent();
        if (e != null) RequestEvent(e); // 큐 등록 함수 호출
    }

    // ✨ 스크롤을 맨 아래로 내리는 헬퍼
    private void ForceScrollToBottom()
    {
        if (eventTextScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            eventTextScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private IEnumerator TypeText(string textToType)
    {
        string fullText = "";
        string[] lines = textToType.Split('\n');

        string completeSkippedText = "";
        foreach (string line in lines) completeSkippedText += line.Trim() + "\n";
        fullTextToSkipTo = completeSkippedText;

        isTyping = true;
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            int charCount = trimmedLine.Length;
            float duration = charCount * 0.05f;

            currentTypingTween = DOTween.To(
                () => 0,
                (charIndex) =>
                {
                    eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex);
                    ForceScrollToBottom(); // ✨ 스크롤 추적
                },
                charCount, duration
            ).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => { isTyping = false; });

            yield return new WaitUntil(() => !isTyping);

            fullText += trimmedLine + "\n";
            eventTextBox.text = fullText;
            isTyping = true;
            yield return new WaitForSecondsRealtime(0.5f);
        }

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

        if (skipInput && isTyping && !justSelected)
        {
            bool wasResultText = isShowingResultText;

            StopAllCoroutines();
            if (currentTypingTween != null && currentTypingTween.IsActive()) currentTypingTween.Kill();

            if (!string.IsNullOrEmpty(fullTextToSkipTo))
            {
                eventTextBox.text = fullTextToSkipTo;
                ForceScrollToBottom(); // ✨ 스킵 시에도 스크롤 하단 이동
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

        if (toggleInput && !isTyping && isTextFullyDisplayed && !hasSelectionBeenMade)
        {
            if (isAnimatingPanel) return;

            if (isPanelOnScreen) AnimatePanelToPeek();
            else AnimatePanelOnScreen(null);
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
            if (currentTypingTween != null && currentTypingTween.IsActive()) currentTypingTween.Kill();
            if (!string.IsNullOrEmpty(fullTextToSkipTo)) eventTextBox.text = fullTextToSkipTo;

            isTyping = false;
            fullTextToSkipTo = "";
            isTextFullyDisplayed = true;
            ForceScrollToBottom();
        }

        if (eventSelections != null) eventSelections.SetActive(false);

        SO_Event.Selection chosenSelection = currentEvent.Selections[selectionIndex];
        List<string> dynamicResultTexts = new List<string>();

        if (chosenSelection.eventToTrigger != null)
        {
            if (playerObject != null)
                dynamicResultTexts = chosenSelection.eventToTrigger.Trigger(playerObject);
            else
                Debug.LogError("Player Object Missing!");
        }

        string finalResultText = "";
        if (dynamicResultTexts.Count > 0)
        {
            finalResultText = "→ " + string.Join("\n- ", dynamicResultTexts);
        }

        isShowingResultText = true;

        if (string.IsNullOrEmpty(finalResultText))
        {
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
            (charIndex) => {
                eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex);
                ForceScrollToBottom();
            },
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
        AnimatePanelToHidden();
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
            if (button.gameObject.activeInHierarchy) button.interactable = true;
        }
    }

    #region --- 애니메이션 함수 ---

    private void AnimatePanelOnScreen(Action onComplete)
    {
        isPanelOnScreen = true;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(onScreenPosition, panelMoveDuration)
            .SetEase(Ease.OutBack).SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false;
                onComplete?.Invoke();
            });
    }

    public void AnimatePanelToPeek()
    {
        isPanelOnScreen = false;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(offScreenPeekPosition, panelMoveDuration)
            .SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => { isAnimatingPanel = false; });
    }

    private void AnimatePanelToHidden()
    {
        isPanelOnScreen = false;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(offScreenHiddenPosition, panelMoveDuration)
            .SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false;
                eventUIPanel.SetActive(false);

                // ✨ [핵심] 이벤트 종료 시 GameManager에게 알림
                // 다음 대기 중인 팝업(레벨업 등)이 있다면 실행됨
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CloseUI();
                }
            });
    }
    #endregion
}