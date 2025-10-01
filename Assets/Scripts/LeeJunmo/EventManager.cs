using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;
using NUnit.Framework;
using System.Collections; // �ڷ�ƾ ����� ���� �߰�
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
    private TextMeshProUGUI eventTextBox; // TextMeshProUGUI ������Ʈ�� ���� �����ϵ��� ����
    [SerializeField]
    private ScrollRect eventScrollRect; // <<-- ScrollRect ������Ʈ ������ �߰��մϴ�.
    [SerializeField]
    private Scrollbar verticalScrollbar; // <<-- ���� ��ũ�ѹ� ������Ʈ ������ �߰��մϴ�.

    // --- �ؽ�Ʈ ����� ���� ������ ---
    private Tween currentTypingTween; // ���� ���� ���� DOText Ʈ���� �����ϱ� ���� ����
    private bool isTyping = false; // �ؽ�Ʈ�� Ÿ���� ������ Ȯ���ϴ� �÷���

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

        // ������ ���� ���� �ڷ�ƾ�� �ִٸ� �����ϰ� ���� ����
        StopAllCoroutines();
        StartCoroutine(TypeText(e.EventText));
    }


    public void TEstEvent()
    {
        SO_Event e = eventDatabase.GetRandomEvent();
        currentEvent = e;
        eventUIPanel.SetActive(true);
        eventImage.GetComponent<Image>().sprite = e.EventSprite;

        // ������ ���� ���� �ڷ�ƾ�� �ִٸ� �����ϰ� ���� ����
        StopAllCoroutines();
        StartCoroutine(TypeText(e.EventText));
    }


    /// <summary>
    /// DOTween�� ����� �ؽ�Ʈ�� ����ϰ�, ����� ������ ��ũ���� Ȱ��ȭ�ϴ� �ڷ�ƾ
    /// </summary>
    private IEnumerator TypeText(string textToType)
    {
        // --- 1. �ڷ�ƾ ���� �� ��ũ�� ��� ��Ȱ��ȭ ---
        if (eventScrollRect != null)
        {
            eventScrollRect.enabled = false; // ����� ��ũ�� �Է��� �����ϴ�.
        }
        if (verticalScrollbar != null)
        {
            verticalScrollbar.gameObject.SetActive(false); // ��ũ�ѹٸ� ����ϴ�.
        }

        string fullText = "";
        eventTextBox.text = fullText;

        string[] lines = textToType.Split('\n');

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            int charCount = trimmedLine.Length;
            // �� ���� ���̿� ����� ��Ȯ�� �ð�
            float duration = charCount * 0.05f;

            isTyping = true;

            // --- DOText ��� DOTween.To()�� ����մϴ� ---
            // 0���� �� ���� ���� ��(charCount)���� ���ڸ� ��ȭ��Ű�� �ִϸ��̼��� ����ϴ�.
            currentTypingTween = DOTween.To(
                () => 0, // ���� ��
                (charIndex) => { // �ִϸ��̼��� ����Ǵ� ���� �� ������ ����
                                 // ���� �ؽ�Ʈ + ���� ���� �Ϻθ� ���ļ� �ǽð����� ǥ��
                    eventTextBox.text = fullText + trimmedLine.Substring(0, charIndex);
                },
                charCount, // ���� ��
                duration   // ���� �ð�
            ).SetEase(Ease.Linear).OnComplete(() => {
                isTyping = false;
            });

            yield return new WaitUntil(() => !isTyping);

            // �� ���� ������ ��ü �ؽ�Ʈ�� ������Ʈ�ϰ� �ٹٲ��� �߰�
            fullText += trimmedLine + "\n";
            eventTextBox.text = fullText; // ���� �ؽ�Ʈ ����

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("�ؽ�Ʈ ��� �Ϸ�");
        InitSelection();
        // --- 2. ��� �ؽ�Ʈ ����� ���� �� ��ũ�� ��� Ȱ��ȭ ---
        yield return null; // Content Fitter�� ���� ���̸� ����� �ð��� �ݴϴ�.

        if (eventScrollRect != null)
        {
            // ��ũ���� �ʿ��� ��쿡�� ��ũ�� ����� �ٽ� Ȱ��ȭ�մϴ�.
            if (eventTextBox.rectTransform.rect.height > eventScrollRect.viewport.rect.height)
            {
                eventScrollRect.enabled = true;
                CheckScrollbarVisibility(); // ��ũ�ѹٸ� ǥ������ ����
            }
        }
    }

    private void Update()
    {
        // ��ŵ �Է� ����
        bool skipInputPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (skipInputPressed && isTyping)
        {
            currentTypingTween.Complete();
        }

        // Ÿ���� �� �ڵ� ��ũ�� (����� �Է°� �����ϰ� �ڵ�� ����)
        if (isTyping && eventScrollRect != null)
        {
            eventScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void CheckScrollbarVisibility()
    {
        if (eventScrollRect != null && verticalScrollbar != null)
        {
            // Content(�ؽ�Ʈ�ڽ�)�� ���̰� Viewport���� Ŭ ���� ��ũ�ѹٸ� Ȱ��ȭ�մϴ�.
            bool requiresScroll = eventTextBox.rectTransform.rect.height > eventScrollRect.viewport.rect.height;
            verticalScrollbar.gameObject.SetActive(requiresScroll);
        }
    }


    /// <summary>
    /// ������ ��ư�� Ŭ������ �� ȣ��� �Լ�
    /// </summary>
    /// <param name="selectionIndex">�� ��° �������� ������� (0���� ����)</param>
    public void SelectionChoice(int selectionIndex)
    {
        // ���� �̺�Ʈ �����Ͱ� ������ �Լ� ����
        if (currentEvent == null) return;

        // �ٸ� �������� �� ������ ���ϵ��� ������ UI�� ��Ȱ��ȭ
        if (eventSelections != null)
        {
            eventSelections.SetActive(false);
        }

        // ������ ��� �ؽ�Ʈ�� ������
        string resultText = currentEvent.Selections[selectionIndex].selectionEndText;

        // ��� �ؽ�Ʈ�� ����ϴ� ���ο� �ڷ�ƾ�� ����
        StartCoroutine(ShowResultText(resultText));
    }

    /// <summary>
    /// ��� �ؽ�Ʈ�� Ÿ���� ȿ���� ����ϴ� �ڷ�ƾ
    /// </summary>
    private IEnumerator ShowResultText(string textToAnimate)
    {
        // ���� �ؽ�Ʈ�� ������ ����� ���� �� �� �ٹٲ�
        string fullText = eventTextBox.text + "\n";
        eventTextBox.text = fullText;

        // ��ũ���� �ʿ��� �� ������ �� �Ʒ��� �̵�
        yield return null;
        if (eventScrollRect != null) eventScrollRect.verticalNormalizedPosition = 0f;

        string trimmedLine = textToAnimate.Trim();
        int charCount = trimmedLine.Length;
        float duration = charCount * 0.05f;

        isTyping = true;

        // ������ ����ߴ� DOTween.To() ������� ��� �ؽ�Ʈ�� Ÿ����
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

        // Ÿ������ ���� ������ ���
        yield return new WaitUntil(() => !isTyping);

        // ��� ����� �������� ���������� ��ũ�� ��� Ȱ��ȭ ���� ����
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
        // UI�� ��ġ�� ��� ������ ����(Selection1, Selection2 ��)�� ��ȸ�մϴ�.
        for (int i = 0; i < eventSelections.transform.childCount; i++)
        {
            // ���� ������ UI ������ �ڽ� ������Ʈ�� �����ɴϴ�. (��: "Selection1")
            Transform selectionUIObject = eventSelections.transform.GetChild(i);

            // SO_Event�� ���� UI ���Կ� �ش��ϴ� �����Ͱ� �ִ��� Ȯ���մϴ�.
            if (i < currentEvent.Selections.Count)
            {
                // ������ ��Ȱ��ȭ�Ǿ��� �� ������, UI ������Ʈ�� Ȱ��ȭ�մϴ�.
                selectionUIObject.gameObject.SetActive(true);

                // �� UI ������Ʈ�� �ڽĵ鿡�� TextMeshPro ������Ʈ�� ��� ã���ϴ�.
                // ù ��° ������Ʈ�� ���� �ؽ�Ʈ, �� ��°�� ���� �ؽ�Ʈ��� �����մϴ�.
                TextMeshProUGUI[] texts = selectionUIObject.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 2)
                {
                    // ��ũ���ͺ� ������Ʈ�� �ؽ�Ʈ�� �Ҵ��մϴ�.
                    texts[0].text = currentEvent.Selections[i].selectionText;
                    texts[1].text = currentEvent.Selections[i].selectionUnderText;
                }
                else
                {
                    Debug.LogWarning($"������ ������Ʈ '{selectionUIObject.name}'�� TextMeshProUGUI ������Ʈ�� 2�� �̸��Դϴ�.", this);
                }
            }
            else
            {
                // ���� �� UI ���Կ� �ش��ϴ� �����Ͱ� ���ٸ�, ��Ȱ��ȭ�Ͽ� ������ �ʰ� �մϴ�.
                selectionUIObject.gameObject.SetActive(false);
            }
        }
    }
}
