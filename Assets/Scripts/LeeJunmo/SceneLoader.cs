using UnityEngine;
using UnityEngine.SceneManagement; // �� ������ ���� �ʼ�!

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("�� ����")]
    [Tooltip("�ε��� ���� ���� �̸��� ��Ȯ�� �Է��ϼ���.")]
    [SerializeField] private string startSceneName = "StartScene"; // ���⿡ ���� ���� �� �̸��� ��������.

    private void Awake()
    {
        // ������ �̱��� ����
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �ٸ� ������ �Ѿ�� �ı����� ����
        }
    }

    /// <summary>
    /// ������ ���� ���� �ε��մϴ�.
    /// </summary>
    public void LoadStartScene()
    {
        // ���� �ð��� �������� �ǵ����� �� �ε� (�߿�!)
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate; // ������ ����ȭ

        SceneManager.LoadScene(startSceneName);
    }

    /// <summary>
    /// (���ʽ�) �̸����� Ư�� ���� �ε��ϴ� �Լ�
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        SceneManager.LoadScene(sceneName);
    }
}