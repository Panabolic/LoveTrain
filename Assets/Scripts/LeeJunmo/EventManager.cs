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

    // ✨ [추가] 선택지 버튼 컴포넌트 리스트 (활성화/비활성화용)
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

        // 선택지 버튼 컴포넌트 미리 찾아두기
        if (eventSelections != null)
        {
            selectionButtons.AddRange(eventSelections.GetComponentsInChildren<Button>(true)); // 비활성화된 것도 포함
        }

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
        justSelected = false;

        Physics2D.simulationMode = SimulationMode2D.Script;
        Time.timeScale = 0f;

        currentEvent = e;
        if (eventTitleBox != null) eventTitleBox.text = e.EventTitle;
        if (eventTextBox != null) eventTextBox.text = ""; // 텍스트 초기화

        InitSelection(); // ✨ 여기서 선택지 내용을 채우고 '비활성화'함

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

        // ✨ [추가] 텍스트 출력이 끝나면 선택지 활성화
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

            // 메인 텍스트 스킵 시 처리
            if (!wasResultText)
            {
                isTextFullyDisplayed = true;
                // ✨ [추가] 메인 텍스트 스킵 시에도 선택지 활성화
                EnableSelections();
            }

            // 결과 텍스트 스킵 시 처리
            if (wasResultText)
            {
                StartCoroutine(WaitAndClosePanel(2.0f));
            }
            return;
        }

        // --- 패널 토글 로직 ---
        if (toggleInput && !isTyping && isTextFullyDisplayed && !hasSelectionBeenMade)
        {
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

    /// <summary>
    /// ✨ [수정됨] 선택지 클릭 시, 본문 텍스트가 타이핑 중이었다면 즉시 완료시킴
    /// </summary>
    public void SelectionChoice(int selectionIndex)
    {
        if (currentEvent == null || hasSelectionBeenMade) return; // 이미 선택했으면 무시

        // 1. 방금 선택했음을 기록 (클릭이 스킵으로 오인되는 것 방지)
        justSelected = true;
        hasSelectionBeenMade = true;

        // 2. 만약 아직 메인 텍스트가 타이핑 중이었다면(스킵 안 하고 바로 선택지 클릭 시) 즉시 완료
        if (isTyping && !isShowingResultText)
        {
            StopAllCoroutines(); // TypeText 코루틴 중지
            if (currentTypingTween != null && currentTypingTween.IsActive())
            {
                currentTypingTween.Kill(); // DOTween 중지
            }
            // 최종 텍스트 표시 (fullTextToSkipTo는 TypeText 시작 시 이미 계산됨)
            if (!string.IsNullOrEmpty(fullTextToSkipTo))
            {
                eventTextBox.text = fullTextToSkipTo;
            }
            isTyping = false;
            fullTextToSkipTo = "";
            isTextFullyDisplayed = true; // 메인 텍스트 완료됨
        }

        // 3. 선택지 UI 숨기기
        if (eventSelections != null)
        {
            eventSelections.SetActive(false);
        }

        // 4. 결과 텍스트 출력 시작
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
        fullTextToSkipTo = fullText + trimmedLine; // 결과 텍스트 스킵용
        isTyping = true; // 결과 텍스트 타이핑 시작
        currentTypingTween = DOTween.To(
            () => 0,
            (charIndex) => { eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex); },
            charCount, duration
        ).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => {
            isTyping = false;
            fullTextToSkipTo = "";
        });
        yield return currentTypingTween.WaitForCompletion();
        isShowingResultText = false; // 결과 텍스트 타이핑 끝
        StartCoroutine(WaitAndClosePanel(2.0f));
    }

    private IEnumerator WaitAndClosePanel(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        AnimatePanelToHidden(true);
    }

    /// <summary>
    /// ✨ [수정됨] 선택지 UI를 초기화하고 '비활성화' 상태로 만듦
    /// </summary>
    public void InitSelection()
    {
        eventSelections.SetActive(true); // 우선 부모는 활성화
        for (int i = 0; i < eventSelections.transform.childCount; i++)
        {
            Transform selectionUIObject = eventSelections.transform.GetChild(i);
            Button button = selectionButtons.Find(b => b.transform == selectionUIObject); // 미리 찾아둔 버튼 가져오기

            if (i < currentEvent.Selections.Count)
            {
                selectionUIObject.gameObject.SetActive(true);
                TextMeshProUGUI[] texts = selectionUIObject.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = currentEvent.Selections[i].selectionText;
                    texts[1].text = currentEvent.Selections[i].selectionUnderText;
                }

                // ✨ 버튼을 '클릭 불가능' 상태로 만듦
                if (button != null) button.interactable = false;
            }
            else
            {
                selectionUIObject.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ✨ [새 함수] 선택지 버튼들을 '클릭 가능' 상태로 만듦
    /// </summary>
    private void EnableSelections()
    {
        foreach (Button button in selectionButtons)
        {
            // 활성화된 버튼들만 클릭 가능하게 변경
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