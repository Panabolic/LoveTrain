using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image levelImage;
    [SerializeField] private RectTransform backgroundRect;

    [Header("설정")]
    [Tooltip("슬롯 중심으로부터의 오프셋 (필요시 조절)")]
    [SerializeField] private Vector2 offset = new Vector2(10f, -10f);

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        gameObject.SetActive(false);
    }

    // [삭제] Update() 함수 삭제 (마우스 추적 안 함)

    // [변경] targetPos(슬롯 위치)를 인자로 받음
    public void Show(string title, Sprite levelSprite, string content, Vector3 targetPos)
    {
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        titleText.text = title;
        contentText.text = content;

        if (levelSprite != null)
        {
            levelImage.sprite = levelSprite;
            levelImage.gameObject.SetActive(true);
        }
        else
        {
            levelImage.gameObject.SetActive(false);
        }

        // 1. 레이아웃 갱신 (크기 계산)
        LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);

        // 2. 위치 설정 및 화면 보정
        SetPosition(targetPos);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetPosition(Vector3 targetPos)
    {
        // 1. 일단 기본값으로 설정 (Pivot: 좌상단 (0, 1))
        // 이렇게 해야 오른쪽/아래로 뻗어나가는 기본 크기를 계산할 수 있습니다.
        backgroundRect.pivot = new Vector2(0, 1);
        transform.position = targetPos + (Vector3)offset;

        // 2. 현재 상태에서의 월드 좌표 모서리를 구합니다.
        Vector3[] corners = new Vector3[4];
        backgroundRect.GetWorldCorners(corners);

        // corners[2] = Top-Right (우측 상단 좌표)
        // corners[0] = Bottom-Left (좌측 하단 좌표)

        float newPivotX = 0; // 기본값 (좌)
        float newPivotY = 1; // 기본값 (상)

        // 오프셋도 방향에 따라 뒤집어야 하므로 임시 변수에 담습니다.
        Vector3 finalOffset = offset;

        // --- 가로(Horizontal) 체크 ---
        // 툴팁의 우측 끝이 화면 너비를 넘었나요?
        if (corners[2].x > Screen.width)
        {
            newPivotX = 1; // Pivot을 우측(1)으로 변경 -> 왼쪽으로 그려짐
            finalOffset.x = -offset.x; // 오프셋 X 반전 (왼쪽으로 띄우기)
        }

        // --- 세로(Vertical) 체크 ---
        // 툴팁의 하단 끝이 화면 아래(0)로 내려갔나요?
        if (corners[0].y < 0)
        {
            newPivotY = 0; // Pivot을 하단(0)으로 변경 -> 위쪽으로 그려짐
            finalOffset.y = -offset.y; // 오프셋 Y 반전 (위쪽으로 띄우기)
            // 참고: 원래 offset.y가 음수(-10)라면, -offset.y는 양수(+10)가 되어 위로 올라갑니다.
        }

        // 3. 계산된 Pivot과 위치를 최종 적용
        backgroundRect.pivot = new Vector2(newPivotX, newPivotY);
        transform.position = targetPos + finalOffset;
    }
}