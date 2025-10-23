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
    [SerializeField] private GameObject eventSelections;

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
    private bool isTextFullyDisplayed = false;
    private bool hasSelectionBeenMade = false;


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

        DOTween.To(() => 0f, x => { }, 1f, 1f)
            .SetLoops(-1)
            .SetUpdate(true)
            .OnUpdate(UnscaledUpdate);
    }

    public void StartEvent(SO_Event e)
    {
        isTextFullyDisplayed = false;
        hasSelectionBeenMade = false;
        isShowingResultText = false;
        isTyping = false;
        fullTextToSkipTo = "";

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
        SO_Event e = eventDatabase.GetRandomEvent();
        if (e != null)
        {
            StartEvent(e);
        }
    }

    private IEnumerator TypeText(string textToType)
    {
        string fullText = "";
        // eventTextBox.text = fullText; // StartEvent로 이동

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
    }

    private void UnscaledUpdate()
    {
        // 1. 스킵 입력 감지 (마우스 클릭)
        bool skipInput = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        // 2. 패널 토글 입력 감지 (스페이스바)
        bool toggleInput = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

        // --- 스킵 로직 ---
        if (skipInput && isTyping)
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

            if (!wasResultText) isTextFullyDisplayed = true;

            if (wasResultText)
            {
                StartCoroutine(WaitAndClosePanel(2.0f));
            }
            return;
        }

        // --- ✨ [수정] 패널 토글 로직 ---
        if (toggleInput)
        {
            // 스페이스바가 눌리면, 토글 함수 호출
            TogglePanelVisibility();
        }
    }

    /// <summary>
    /// ✨ [새 함수] 버튼(OnClick) 또는 스페이스바에서 호출할 수 있는 공용 토글 함수
    /// </summary>
    public void TogglePanelVisibility()
    {
        // 타이핑 중이거나, 텍스트가 다 안 나왔거나, 선택을 이미 했으면 토글 불가
        if (isTyping || !isTextFullyDisplayed || hasSelectionBeenMade)
        {
            return;
        }

        // 토글 실행
        if (isPanelOnScreen)
        {
            AnimatePanelToPeek(); // 화면에 있으면 -> 살짝 숨기기
        }
        else
        {
            AnimatePanelOnScreen(null); // 숨겨져 있으면 -> 다시 보이기
        }
    }

    public void SelectionChoice(int selectionIndex)
    {
        if (currentEvent == null) return;
        hasSelectionBeenMade = true;
        if (eventSelections != null)
        {
            eventSelections.SetActive(false);
        }
        if (currentTypingTween != null && currentTypingTween.IsActive())
        {
            currentTypingTween.Kill();
        }
        StopAllCoroutines();
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
            if (i < currentEvent.Selections.Count)
            {
                selectionUIObject.gameObject.SetActive(true);
                TextMeshProUGUI[] texts = selectionUIObject.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
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
                selectionUIObject.gameObject.SetActive(false);
            }
        }
    }

    #region --- 애니메이션 함수 ---

    private void AnimatePanelOnScreen(Action onComplete)
    {
        isPanelOnScreen = true;
        eventBoardRect.DOAnchorPos(onScreenPosition, panelMoveDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() => {
                onComplete?.Invoke();
            });
    }

    private void AnimatePanelToPeek()
    {
        isPanelOnScreen = false;
        eventBoardRect.DOAnchorPos(offScreenPeekPosition, panelMoveDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true);
    }

    private void AnimatePanelToHidden(bool eventEnded)
    {
        isPanelOnScreen = false;
        eventBoardRect.DOAnchorPos(offScreenHiddenPosition, panelMoveDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => {
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