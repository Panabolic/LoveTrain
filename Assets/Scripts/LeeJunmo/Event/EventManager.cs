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
    [SerializeField] private EventDatabase eventDatabase;

    private SO_Event currentEvent;

    // --- 텍스트 출력을 위한 변수들 ---
    private Tween currentTypingTween;
    private bool isTyping = false;
    private string fullTextToSkipTo = "";
    private bool isShowingResultText = false;

    // --- 애니메이션 및 상태 관리를 위한 변수들 ---
    private bool isPanelOnScreen = false;
    private bool isTextFullyDisplayed = false; // 메인 텍스트 출력이 완료되었는지
    private bool hasSelectionBeenMade = false; // 선택지를 선택했는지
    private bool justSelected = false; // 방금 선택지를 클릭했는지 (스킵 방지용)

    // ✨ [추가] 패널 애니메이션이 재생 중인지 확인하는 플래그
    private bool isAnimatingPanel = false;

    private List<Button> selectionButtons = new List<Button>();

    private void Awake()
    {
        Instance = this;
        if (Instance == null)
        {
            Instantiate(this);
        }

        if (eventUIPanel == null || eventBoardRect == null)
        {
            Debug.LogError("EventManager에 eventUIPanel 또는 eventBoardRect가 연결되지 않았습니다!", this);
            return;
        }

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
        // ✨ 애니메이션 중복 실행 방지 (이미 실행 중이면 무시)
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

    public void TEstEvent()
    {
        // ✨ 애니메이션 중복 실행 방지
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
            // ✨ 애니메이션 중에는 토글 불가
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
        // ✨ [핵심 수정] 애니메이션 중이거나, 이미 선택했으면 무시
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

        string resultText = currentEvent.Selections[selectionIndex].selectionEndText;
        isShowingResultText = true;
        StartCoroutine(ShowResultText(resultText));
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
        isAnimatingPanel = true; // ✨ 애니메이션 시작
        eventBoardRect.DOAnchorPos(onScreenPosition, panelMoveDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false; // ✨ 애니메이션 완료
                onComplete?.Invoke();
            });
    }

    private void AnimatePanelToPeek()
    {
        isPanelOnScreen = false;
        isAnimatingPanel = true; // ✨ 애니메이션 시작
        eventBoardRect.DOAnchorPos(offScreenPeekPosition, panelMoveDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false; // ✨ 애니메이션 완료
            });
    }

    private void AnimatePanelToHidden(bool eventEnded)
    {
        isPanelOnScreen = false;
        isAnimatingPanel = true; // ✨ 애니메이션 시작
        eventBoardRect.DOAnchorPos(offScreenHiddenPosition, panelMoveDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => {
                isAnimatingPanel = false; // ✨ 애니메이션 완료
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