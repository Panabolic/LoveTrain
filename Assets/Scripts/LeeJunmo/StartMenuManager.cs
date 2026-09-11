using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class StartMenuManager : MonoBehaviour
{
    [Header("로드할 씬 설정")]
    [Tooltip("빌드 설정(Build Settings)에 등록된 플레이 씬의 이름을 정확히 입력하세요.")]
    [SerializeField] private string playSceneName = "PlayScene"; // 여기에 실제 게임 씬 이름을 입력

    private void Start()
    {
        Option.SetOptionInputBlocked(false);
    }

    /// <summary>
    /// 게임 시작 버튼을 눌렀을 때 호출될 함수입니다.
    /// </summary>
    public void LoadPlayScene()
    {
        Option.SetOptionInputBlocked(true);
        // Debug.Log($"플레이 씬 '{playSceneName}'을 로드합니다...");
        SceneManager.LoadScene(playSceneName);
    }

    // ✨ [추가] 타이틀 화면에서 '게임 시작' 버튼을 누르면 호출할 함수
    // 이 함수가 호출되면 Start 상태가 되고 -> BGM이 꺼집니다.
    public void EnterStartState()
    {
        // 씬 전환이 필요하다면 여기서 SceneManager.LoadScene("MainGame") 등을 호출
        // 씬 전환 후 상태 변경
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
