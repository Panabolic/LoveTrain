using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class StartMenuManager : MonoBehaviour
{
    [Header("로드할 씬 설정")]
    [Tooltip("빌드 설정(Build Settings)에 등록된 플레이 씬의 이름을 정확히 입력하세요.")]
    [SerializeField] private string playSceneName = "PlayScene"; // 여기에 실제 게임 씬 이름을 입력

    [Header("영구 강화 화면")]
    [SerializeField] private PermanentUpgradeCatalog upgradeCatalog;
    private PermanentUpgradeMenu upgradeMenu;
    private bool loadingPlayScene;

    private void Start()
    {
        Option.SetOptionInputBlocked(false);
    }

    /// <summary>
    /// 기존 Start 애니메이션이 끝나면 플레이 씬 대신 영구 강화 화면을 엽니다.
    /// </summary>
    public void LoadPlayScene()
    {
        if (loadingPlayScene) return;
        if (upgradeMenu == null) upgradeMenu = PermanentUpgradeMenu.Create(this, upgradeCatalog);
        if (upgradeMenu == null) return;
        upgradeMenu.Show();
        Option.SetOptionInputBlocked(true);
    }

    public void StartGameFromUpgrades()
    {
        if (loadingPlayScene) return;
        if (!Application.CanStreamedLevelBeLoaded(playSceneName))
        {
            Debug.LogError($"[StartMenuManager] 플레이 씬 '{playSceneName}'이 빌드 설정에 없습니다.", this);
            return;
        }
        loadingPlayScene = true;
        Option.SetOptionInputBlocked(true);
        EnterStartState();
        SceneManager.LoadScene(playSceneName);
    }

    // 강화 화면의 게임 시작 버튼에서만 Start 상태로 전환합니다.
    public void EnterStartState()
    {
        // 기존 Start 애니메이션의 두 번째 콜백은 강화 화면을 건너뛰면 안 됩니다.
        if (!loadingPlayScene) return;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameState.Start);
    }

    /// <summary>
    /// (보너스) 게임 종료 버튼을 만들 경우 사용할 함수입니다.
    /// </summary>
    public void QuitGame()
    {
        Option.SetOptionInputBlocked(true);
        // Debug.Log("게임을 종료합니다...");
        Application.Quit();

        // (에디터에서는 작동 안 함, 빌드된 게임에서만 작동)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
