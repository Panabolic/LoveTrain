using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("씬 설정")]
    [Tooltip("로드할 시작 씬의 이름을 정확히 입력하세요.")]
    [SerializeField] private string startSceneName = "StartScene"; // 여기에 실제 시작 씬 이름을 넣으세요.

    private void Awake()
    {
        // 간단한 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 다른 씬으로 넘어가도 파괴되지 않음
        }
    }

    /// <summary>
    /// 지정된 시작 씬을 로드합니다.
    /// </summary>
    public void LoadStartScene()
    {
        // 게임 시간을 정상으로 되돌리고 씬 로드 (중요!)
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate; // 물리도 정상화

        SceneManager.LoadScene(startSceneName);
    }

    /// <summary>
    /// (보너스) 이름으로 특정 씬을 로드하는 함수
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        SceneManager.LoadScene(sceneName);
    }
}