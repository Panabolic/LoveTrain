using UnityEngine;
using Steamworks;

public class SteamIntegration : MonoBehaviour
{
    protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;
    private bool bInitialized = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        try
        {
            bInitialized = SteamAPI.Init();
            if (!bInitialized)
            {
                Debug.LogError("[SteamIntegration] 스팀 초기화 실패!");
                return;
            }
            Debug.Log($"[SteamIntegration] 스팀 연결 성공! 환영합니다, {SteamFriends.GetPersonaName()}님.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SteamIntegration] 초기화 중 예외 발생: {e.Message}");
        }
    }

    private void OnEnable()
    {
        if (!bInitialized) return;
        m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnSteamOverlayActivated);
    }

    private void Update()
    {
        if (!bInitialized) return;
        SteamAPI.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (!bInitialized) return;
        SteamAPI.Shutdown();
        Debug.Log("[SteamIntegration] 스팀 연결 종료");
    }

    // ✨ 핵심 수정: 오버레이 상태 변경 감지 로직
    private void OnSteamOverlayActivated(GameOverlayActivated_t pCallback)
    {
        if (pCallback.m_bActive != 0) // 스팀 오버레이가 '열렸을 때'
        {
            Debug.Log("[SteamIntegration] 스팀 오버레이 켜짐 -> 강제 일시정지 UI 호출");

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Pause)
            {
                // 현재 활성화된 씬에서 Option 스크립트를 찾습니다.
                // (오버레이는 어쩌다 한 번 열리므로 FindObjectOfType을 써도 성능에 전혀 지장이 없습니다)
                Option optionUI = FindAnyObjectByType<Option>();

                if (optionUI != null)
                {
                    // Option 패널을 여는 메서드(내부적으로 PauseGame 호출)를 실행합니다.
                    optionUI.ToggleOptionPanel();
                }
                else
                {
                    // 만약 씬에 Option UI가 없다면 최소한 게임 시간이라도 멈춰줍니다.
                    GameManager.Instance.PauseGame();
                }
            }
        }
        else // 스팀 오버레이가 '닫혔을 때'
        {
            // 요구사항: 오버레이를 닫았을 때는 아무 동작도 하지 않음.
            // (플레이어가 직접 Option 창의 X버튼이나 ESC를 눌러야 ResumeGame이 호출됨)
            Debug.Log("[SteamIntegration] 스팀 오버레이 닫힘 (게임은 계속 일시정지 상태 유지)");
        }
    }
}