using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CreditsUIController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("Credits Content")]
    [SerializeField] private TextAsset creditsText;

    private bool previousOptionInputBlocked;

    private bool IsPanelOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        PopulateCreditsText();
        HidePanelImmediate();
    }

    private void Start()
    {
        InitializeButtons();
        RefreshOpenButtonVisibility();
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(OpenCredits);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseCredits);
    }

    private void Update()
    {
        if (IsPanelOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseCredits();
        }

        RefreshOpenButtonVisibility();
    }

    public void OpenCredits()
    {
        if (!CanOpenCredits())
        {
            return;
        }

        previousOptionInputBlocked = Option.IsOptionInputBlocked;
        Option.SetOptionInputBlocked(true);

        PopulateCreditsText();
        if (panelRoot != null) panelRoot.SetActive(true);
        ResetScrollToTop();
        RefreshOpenButtonVisibility();
    }

    public void CloseCredits()
    {
        HidePanelImmediate();
        Option.SetOptionInputBlocked(previousOptionInputBlocked);
        RefreshOpenButtonVisibility();
    }

    private void InitializeButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenCredits);
            openButton.onClick.AddListener(OpenCredits);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseCredits);
            closeButton.onClick.AddListener(CloseCredits);
        }
    }

    private void PopulateCreditsText()
    {
        if (bodyText == null)
        {
            return;
        }

        bodyText.text = creditsText != null
            ? creditsText.text
            : "Credits text is missing.";
    }

    private bool CanOpenCredits()
    {
        if (IsPanelOpen || Option.IsOptionInputBlocked)
        {
            return false;
        }

        return GameManager.Instance == null
            || GameManager.Instance.CurrentState == GameState.Title;
    }

    private void RefreshOpenButtonVisibility()
    {
        if (openButton == null)
        {
            return;
        }

        bool isTitleState = GameManager.Instance == null
            || GameManager.Instance.CurrentState == GameState.Title;
        bool shouldShow = isTitleState && !IsPanelOpen;

        openButton.gameObject.SetActive(shouldShow);
        openButton.interactable = shouldShow && !Option.IsOptionInputBlocked;
    }

    private void HidePanelImmediate()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void ResetScrollToTop()
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
