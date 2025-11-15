using UnityEngine;
using UnityEngine.InputSystem;

public class CursorConfine : MonoBehaviour
{
    // [추가] 현재 잠금 상태를 저장하는 변수
    private CursorLockMode currentLockMode = CursorLockMode.None;

    void Start()
    {
        // 게임이 시작되면 즉시 커서를 창 안에 가둡니다.
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
            // [수정] 현재 상태가 'None'이 아닐 때만 FreeCursor() 호출
            if (currentLockMode != CursorLockMode.None)
            {
                FreeCursor();
            }
        }

        // 마우스를 클릭했을 때
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // [수정] 현재 상태가 'Confined'가 아닐 때만 ConfineCursor() 호출
            if (currentLockMode != CursorLockMode.Confined)
            {
                ConfineCursor();
            }
        }
    }

    void ConfineCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        currentLockMode = CursorLockMode.Confined; // [추가] 현재 상태 저장
    }

    void FreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        currentLockMode = CursorLockMode.None; // [추가] 현재 상태 저장
    }
}