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
    private Tween unscaledUpdateTween;

    private void Awake()
    {
        Instance = this;

        if (eventSelections != null)
        {
            selectionButtons.AddRange(eventSelections.GetComponentsInChildren<Button>(true));
        }

        ResetEventUIState();

        unscaledUpdateTween = DOTween.To(() => 0f, x => { }, 1f, 1f)
            .SetLoops(-1)
            .SetUpdate(true)
            .OnUpdate(UnscaledUpdate);
    }

    private void OnDestroy()
    {
        if (currentTypingTween != null && currentTypingTween.IsActive())
        {
            currentTypingTween.Kill();
        }

        if (unscaledUpdateTween != null && unscaledUpdateTween.IsActive())
        {
            unscaledUpdateTween.Kill();
        }
    }

    // ✨ [수정] 외부에서 이벤트를 요청할 때 사용 (큐에 등록)
    public void RequestEvent(SO_Event e)
    {
        if (e == null || GameManager.Instance == null) return;

        SoundEventBus.Publish(SoundID.UI_Event);
        GameManager.Instance.RegisterUIQueue(() => ProcessEvent(e));
    }

    // ✨ [추가] 큐에서 호출되는 실제 이벤트 실행 로직
    private void ProcessEvent(SO_Event e)
    {
        if (isAnimatingPanel) return;
        if (e == null ||
            e.Selections == null ||
            e.Selections.Count == 0 ||
            eventBoardRect == null ||
            eventUIPanel == null ||
            eventSelections == null ||
            eventTextBox == null)
        {
            CloseInvalidQueuedEvent();
            return;
        }

        ResetEventRuntimeFlags();
        SoundEventBus.Publish(SoundID.UI_Event);

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
            StartCoroutine(TypeText(e.EventText ?? string.Empty));
        });
    }

    public void RandomEventStart()
    {
        if (isAnimatingPanel || eventDatabase == null) return;

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

        // ✨ [추가] 전체 누적 글자 수 추적용 변수
        int totalCharCount = 0;
        // ✨ [추가] 중복 재생 방지용 (같은 인덱스에서 소리 두 번 나는 것 방지)
        int lastSoundIndex = -1;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            int charCount = trimmedLine.Length;
            float duration = charCount * 0.025f;

            currentTypingTween = DOTween.To(
                () => 0,
                (charIndex) =>
                {
                    eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex);
                    ForceScrollToBottom();

                    // ✨ [수정] 현재 줄(charIndex) + 이전 줄까지의 합(totalCharCount) = 전체 인덱스
                    int currentGlobalIndex = totalCharCount + charIndex;

                    // 1. 인덱스가 0이 아니고
                    // 2. 전체 기준으로 4번째 글자이며
                    // 3. 방금 소리 낸 인덱스가 아닐 때만 재생 (중복 방지)
                    if (currentGlobalIndex > 0 &&
                       currentGlobalIndex % 2 == 0 &&
                       currentGlobalIndex != lastSoundIndex)
                    {
                        SoundEventBus.Publish(SoundID.UI_Typing);
                        lastSoundIndex = currentGlobalIndex; // 소리 낸 인덱스 기록
                    }
                },
                charCount, duration
            ).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => {
                // 여기서는 isTyping을 끄지 않고 모든 줄이 끝난 뒤에 끕니다
            });

            yield return currentTypingTween.WaitForCompletion(); // 트윈이 끝날 때까지 대기

            fullText += trimmedLine + "\n";
            eventTextBox.text = fullText;

            // ✨ [추가] 이 줄의 글자 수를 전체 누적 합계에 더함
            totalCharCount += charCount;

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
        if (currentEvent.Selections == null ||
            selectionIndex < 0 ||
            selectionIndex >= currentEvent.Selections.Count)
        {
            return;
        }

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
        float duration = charCount * 0.025f;

        fullTextToSkipTo = fullText + trimmedLine;
        isTyping = true;

        // ✨ [추가] 소리 중복 재생 방지용 변수
        int lastSoundIndex = -1;

        currentTypingTween = DOTween.To(
            () => 0,
            (charIndex) => {
                eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex);
                ForceScrollToBottom();

                // ✨ [추가] 타이핑 사운드 로직
                // 1. 인덱스가 0보다 크고
                // 2. 4번째 글자마다 (% 4 == 0)
                // 3. 방금 소리 냈던 인덱스가 아닐 경우 (DOTween 업데이트 빈도 차이로 인한 중복 방지)
                if (charIndex > 0 && charIndex % 2 == 0 && charIndex != lastSoundIndex)
                {
                    SoundEventBus.Publish(SoundID.UI_Typing);
                    lastSoundIndex = charIndex; // 소리 낸 시점 기록
                }
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
            if (button == null) continue;
            if (button.gameObject.activeInHierarchy) button.interactable = true;
        }
    }

    private void CloseInvalidQueuedEvent()
    {
        ResetEventUIState();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CloseUI();
        }
    }

    private void ResetEventRuntimeFlags()
    {
        if (currentTypingTween != null && currentTypingTween.IsActive())
        {
            currentTypingTween.Kill();
        }

        currentTypingTween = null;
        isTyping = false;
        fullTextToSkipTo = "";
        isShowingResultText = false;
        isPanelOnScreen = false;
        isTextFullyDisplayed = false;
        hasSelectionBeenMade = false;
        justSelected = false;
        isAnimatingPanel = false;
    }

    private void ResetEventUIState()
    {
        StopAllCoroutines();
        ResetEventRuntimeFlags();
        currentEvent = null;

        if (eventBoardRect != null)
        {
            eventBoardRect.anchoredPosition = offScreenHiddenPosition;
        }

        if (eventTitleBox != null) eventTitleBox.text = "";
        if (eventTextBox != null) eventTextBox.text = "";
        if (eventTextScrollRect != null) eventTextScrollRect.verticalNormalizedPosition = 1f;
        if (eventSelections != null) eventSelections.SetActive(false);
        if (eventUIPanel != null) eventUIPanel.SetActive(false);
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
