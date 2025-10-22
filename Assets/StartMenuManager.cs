using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class StartMenuManager : MonoBehaviour
{
    [Header("로드할 씬 설정")]
    [Tooltip("빌드 설정(Build Settings)에 등록된 플레이 씬의 이름을 정확히 입력하세요.")]
    [SerializeField] private string playSceneName = "PlayScene"; // 여기에 실제 게임 씬 이름을 입력

    /// <summary>
    /// 게임 시작 버튼을 눌렀을 때 호출될 함수입니다.
    /// </summary>
    public void LoadPlayScene()
    {
        // Debug.Log($"플레이 씬 '{playSceneName}'을 로드합니다...");
        SceneManager.LoadScene(playSceneName);
    }

    /// <summary>
    /// (보너스) 게임 종료 버튼을 만들 경우 사용할 함수입니다.
    /// </summary>
    public void QuitGame()
    {
        // Debug.Log("게임을 종료합니다...");
        Application.Quit();

        // (에디터에서는 작동 안 함, 빌드된 게임에서만 작동)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}