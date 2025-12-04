using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI; // ScrollRect 사용을 위해 필수
using System.Collections.Generic;
using System.Collections;
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

    [Header("텍스트 및 스크롤 컴포넌트")]
    [SerializeField] private TextMeshProUGUI eventTitleBox;
    [SerializeField] private TextMeshProUGUI eventTextBox;

    // ✨ [추가] 스크롤 뷰 연결 변수
    [SerializeField] private ScrollRect eventTextScrollRect;

    [Header("애니메이션 설정")]
    [SerializeField] private float panelMoveDuration = 0.5f;

    [Header("패널 위치")]
    [SerializeField] private Vector2 onScreenPosition = new Vector2(0, 0);
    [SerializeField] private Vector2 offScreenPeekPosition = new Vector2(800, 0);
    [SerializeField] private Vector2 offScreenHiddenPosition = new Vector2(1000, 0);

    [Header("데이터베이스")]
    [SerializeField] private EventDatabase eventDatabase;

    [Header("이벤트 대상")]
    [SerializeField] private GameObject playerObject;

    private SO_Event currentEvent;

    // --- 변수들 ---
    private Tween currentTypingTween;
    private bool isTyping = false;
    private string fullTextToSkipTo = "";
    private bool isShowingResultText = false;
    private bool isPanelOnScreen = false;
    private bool isTextFullyDisplayed = false;
    private bool hasSelectionBeenMade = false;
    private bool justSelected = false;
    private bool isAnimatingPanel = false;

    private List<Button> selectionButtons = new List<Button>();

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

    public void StartEvent(SO_Event e)
    {
        if (isAnimatingPanel) return;

        // 초기화
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
        if (eventTextBox != null) eventTextBox.text = "";

        // ✨ [추가] 텍스트 초기화 후 스크롤을 맨 위로 올림
        if (eventTextScrollRect != null) eventTextScrollRect.verticalNormalizedPosition = 1f;

        InitSelection();

        eventBoardRect.anchoredPosition = offScreenHiddenPosition;
        eventUIPanel.SetActive(true);

        AnimatePanelOnScreen(() =>
        {
            StartCoroutine(TypeText(e.EventText));
        });
    }

    // ✨ [추가] 스크롤을 강제로 맨 아래로 내리는 헬퍼 함수
    private void ForceScrollToBottom()
    {
        if (eventTextScrollRect != null)
        {
            // 텍스트가 추가된 직후 UI 사이즈 계산이 늦을 수 있으므로 강제 업데이트
            Canvas.ForceUpdateCanvases();

            // 스크롤 위치를 0(바닥)으로 설정
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
                    // ✨ [추가] 글자가 써질 때마다 스크롤을 아래로 유지
                    ForceScrollToBottom();
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

        // 다 출력된 후에는 사용자가 위로 올려볼 수 있으니 강제 스크롤은 중지해도 됨
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
            ForceScrollToBottom(); // 스킵했을 때도 맨 아래로
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
            (charIndex) =>
            {
                eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex);
                // ✨ [추가] 결과 텍스트 나올 때도 스크롤 추적
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
                ForceScrollToBottom(); // 스킵 시 맨 아래로
            }

            isTyping = false;
            fullTextToSkipTo = "";
            isShowingResultText = false;

            if (!wasResultText)
            {
                isTextFullyDisplayed = true;
                EnableSelections();
            }
            if (wasResultText) StartCoroutine(WaitAndClosePanel(2.0f));
            return;
        }
        // ... (패널 토글 로직 동일) ...
        if (toggleInput && !isTyping && isTextFullyDisplayed && !hasSelectionBeenMade)
        {
            if (isAnimatingPanel) return;
            if (isPanelOnScreen) AnimatePanelToPeek();
            else AnimatePanelOnScreen(null);
        }
    }

    // ... (나머지 초기화, 애니메이션 함수들은 기존과 동일) ...
    public void RandomEventStart()
    {
        if (isAnimatingPanel) return;
        SO_Event e = eventDatabase.GetRandomEvent();
        if (e != null) StartEvent(e);
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
            if (button.gameObject.activeInHierarchy) button.interactable = true;
        }
    }

    private void AnimatePanelOnScreen(Action onComplete)
    {
        isPanelOnScreen = true;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(onScreenPosition, panelMoveDuration)
            .SetEase(Ease.OutBack).SetUpdate(true)
            .OnComplete(() => { isAnimatingPanel = false; onComplete?.Invoke(); });
    }

    private void AnimatePanelToPeek()
    {
        isPanelOnScreen = false;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(offScreenPeekPosition, panelMoveDuration)
            .SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => { isAnimatingPanel = false; });
    }

    private void AnimatePanelToHidden(bool eventEnded)
    {
        isPanelOnScreen = false;
        isAnimatingPanel = true;
        eventBoardRect.DOAnchorPos(offScreenHiddenPosition, panelMoveDuration)
            .SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false;
                eventUIPanel.SetActive(false);
                if (eventEnded)
                {
                    Time.timeScale = 1f;
                    Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
                    if (GameManager.Instance != null) GameManager.Instance.EndEvent();
                }
            });
    }
}