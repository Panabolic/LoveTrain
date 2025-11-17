using UnityEngine;
using UnityEngine.InputSystem; // Input System 사용

public class CursorManager : MonoBehaviour
{
    [Header("커서 설정")]
    [Tooltip("사용할 커서 텍스처(이미지)")]
    [SerializeField] private Texture2D cursorTexture;

    [Tooltip("커서의 클릭 지점 (핫스팟)")]
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    // 현재 잠금 상태를 저장하는 변수
    private CursorLockMode currentLockMode = CursorLockMode.None;

    void Start()
    {
        // 1. 커서 모양을 '코드'로 설정합니다 (단 한 번)
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }

        // 2. 게임이 시작되면 즉시 커서를 창 안에 가둡니다.
        ConfineCursor();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        // 'Escape' 키를 누르면 커서 잠금을 해제합니다.
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            // 현재 상태가 'None'이 아닐 때만 FreeCursor() 호출
            if (currentLockMode != CursorLockMode.None)
            {
                FreeCursor();
            }
        }

        // 마우스를 클릭했을 때
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // 현재 상태가 'Confined'가 아닐 때만 ConfineCursor() 호출
            if (currentLockMode != CursorLockMode.Confined)
            {
                ConfineCursor();
            }
        }
    }

    /// <summary>
    /// 커서를 게임 창 안에 가두고, 보이게 만듭니다.
    /// </summary>
    void ConfineCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        currentLockMode = CursorLockMode.Confined; // 현재 상태 저장
    }

    /// <summary>
    /// 커서를 자유롭게 풀어줍니다.
    /// </summary>
    void FreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        currentLockMode = CursorLockMode.None; // 현재 상태 저장
    }
}