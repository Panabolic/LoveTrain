using UnityEngine;
using UnityEngine.SceneManagement; // �� ������ ���� �ʼ�!

public class StartMenuManager : MonoBehaviour
{
    [Header("�ε��� �� ����")]
    [Tooltip("���� ����(Build Settings)�� ��ϵ� �÷��� ���� �̸��� ��Ȯ�� �Է��ϼ���.")]
    [SerializeField] private string playSceneName = "PlayScene"; // ���⿡ ���� ���� �� �̸��� �Է�

    /// <summary>
    /// ���� ���� ��ư�� ������ �� ȣ��� �Լ��Դϴ�.
    /// </summary>
    public void LoadPlayScene()
    {
        // Debug.Log($"�÷��� �� '{playSceneName}'�� �ε��մϴ�...");
        SceneManager.LoadScene(playSceneName);
    }

    /// <summary>
    /// (���ʽ�) ���� ���� ��ư�� ���� ��� ����� �Լ��Դϴ�.
    /// </summary>
    public void QuitGame()
    {
        // Debug.Log("������ �����մϴ�...");
        Application.Quit();

        // (�����Ϳ����� �۵� �� ��, ����� ���ӿ����� �۵�)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}