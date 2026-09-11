using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    [Header("커서 설정")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    [Header("포커스 안정화")]
    [SerializeField] private bool confineOnStart = true;
    [SerializeField] private float focusReconfineDelay = 0.25f;

    private CursorLockMode requestedLockMode = CursorLockMode.None;
    private Coroutine focusReconfineCoroutine;

    void Start()
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }

        if (confineOnStart)
        {
            ConfineCursor();
        }
        else
        {
            FreeCursor();
        }
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (requestedLockMode != CursorLockMode.None)
            {
                FreeCursor();
            }
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (requestedLockMode != CursorLockMode.Confined && Application.isFocused)
            {
                ConfineCursor();
            }
        }
    }

    void ConfineCursor()
    {
        requestedLockMode = CursorLockMode.Confined;
        ApplyCursorLock(CursorLockMode.Confined, "requested");
    }

    void FreeCursor()
    {
        requestedLockMode = CursorLockMode.None;
        StopFocusReconfine();
        ApplyCursorLock(CursorLockMode.None, "requested");
    }

    private void ApplyCursorLock(CursorLockMode lockMode, string reason)
    {
        Cursor.lockState = lockMode;
        Cursor.visible = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Cursor lock state set to {lockMode}. Reason: {reason}. Focused: {Application.isFocused}");
#endif
    }

    private void ReleaseCursorForFocusChange(string reason)
    {
        StopFocusReconfine();
        ApplyCursorLock(CursorLockMode.None, reason);
    }

    private void ScheduleFocusReconfine()
    {
        StopFocusReconfine();
        focusReconfineCoroutine = StartCoroutine(ReconfineAfterFocusDelay());
    }

    private IEnumerator ReconfineAfterFocusDelay()
    {
        float delay = Mathf.Max(0f, focusReconfineDelay);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        focusReconfineCoroutine = null;
        if (requestedLockMode == CursorLockMode.Confined && Application.isFocused)
        {
            ApplyCursorLock(CursorLockMode.Confined, "focus restored");
        }
    }

    private void StopFocusReconfine()
    {
        if (focusReconfineCoroutine == null)
        {
            return;
        }

        StopCoroutine(focusReconfineCoroutine);
        focusReconfineCoroutine = null;
    }

    void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (requestedLockMode == CursorLockMode.Confined)
            {
                ScheduleFocusReconfine();
            }
        }
        else
        {
            ReleaseCursorForFocusChange("focus lost");
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            ReleaseCursorForFocusChange("application paused");
        }
        else if (requestedLockMode == CursorLockMode.Confined)
        {
            ScheduleFocusReconfine();
        }
    }

    private void OnApplicationQuit()
    {
        requestedLockMode = CursorLockMode.None;
        ReleaseCursorForFocusChange("application quit");
    }

    private void OnDestroy()
    {
        if (requestedLockMode == CursorLockMode.None && focusReconfineCoroutine == null)
        {
            return;
        }

        requestedLockMode = CursorLockMode.None;
        ReleaseCursorForFocusChange("destroyed");
    }
}
