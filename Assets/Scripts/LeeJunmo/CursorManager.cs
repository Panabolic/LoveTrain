using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Runtime.InteropServices; // DllImport 사용

public class CursorManager : MonoBehaviour
{
    [Header("커서 설정")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    private CursorLockMode currentLockMode = CursorLockMode.None;
    private bool osLocked = false;

    // --- Windows API 정의 (윈도우에서만 컴파일되도록 처리) ---
#if UNITY_STANDALONE_WIN
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(ref RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(IntPtr rect);

    // ✨ [추가] 현재 활성화된 윈도우(게임창)의 ID를 가져옴
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    // ✨ [추가] 해당 윈도우의 실제 모니터 좌표를 가져옴
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
#endif
    // -------------------------------------------------------

    void Start()
    {
        if (cursorTexture != null)
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);

        ConfineCursor();
    }

    void Update()
    {
        // 입력 시스템 체크
        if (Keyboard.current == null || Mouse.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentLockMode != CursorLockMode.None)
                FreeCursor();
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentLockMode != CursorLockMode.Confined)
                ConfineCursor();
        }

        // ✨ [보완] 창 모드에서 창을 드래그해서 옮기거나 크기를 바꿀 때를 대비해
        // 갇힌 상태라면 지속적으로 영역을 갱신해주는 것이 안전합니다.
        if (osLocked)
        {
            // LockOSCursor(); // 너무 자주 호출하면 성능에 영향이 있을 수 있으니 필요시 주석 해제
        }
    }

    void ConfineCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        currentLockMode = CursorLockMode.Confined;

        LockOSCursor();
    }

    void FreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        currentLockMode = CursorLockMode.None;

        UnlockOSCursor();
    }

    void LockOSCursor()
    {
#if UNITY_STANDALONE_WIN
        // 1. 현재 게임 창의 핸들(ID)을 가져옴
        IntPtr hwnd = GetActiveWindow();

        // 2. 그 창의 실제 스크린 좌표(RECT)를 가져옴
        RECT rect;
        if (GetWindowRect(hwnd, out rect))
        {
            // 3. 해당 좌표로 커서를 가둠
            ClipCursor(ref rect);
            osLocked = true;
            Debug.Log($"Cursor LOCKED to System Rect: L{rect.left} T{rect.top} R{rect.right} B{rect.bottom}");
        }
        else
        {
            Debug.LogError("Failed to get window rect!");
        }
#endif
    }

    void UnlockOSCursor()
    {
#if UNITY_STANDALONE_WIN
        // NULL(IntPtr.Zero)을 보내면 잠금 해제됨
        ClipCursor(IntPtr.Zero);
        osLocked = false;
        Debug.Log("Cursor UNLOCKED from System");
#endif
    }

    // 알트탭(Alt+Tab) 등으로 포커스를 잃었다가 돌아왔을 때 다시 잠금
    void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (currentLockMode == CursorLockMode.Confined)
                LockOSCursor();
        }
        else
        {
            // 게임이 백그라운드로 가면 무조건 OS 잠금 해제 (안 그러면 윈도우 못 씀)
            UnlockOSCursor();
        }
    }
}