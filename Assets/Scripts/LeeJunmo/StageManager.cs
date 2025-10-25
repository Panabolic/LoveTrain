using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("����")]
    [SerializeField] private StageDatabase backgroundDatabase;
    [SerializeField] private Train train; // AutoScrollBackground�� ������ Train ����
    [SerializeField] private Transform backgroundParent; // ��� �������� ������ �θ�

    [Header("���� ����")]
    [SerializeField] private int startingBackgroundIndex = 0; // ���� �� �ε��� ��� �ε���

    // --- ���� ���� ---
    private GameObject currentBackgroundInstance; // ���� �ε�� ��� ������ �ν��Ͻ�
    private int currentBackgroundIndex;

    private void Awake()
    {
        // ������ �̱���
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }
    }

    private void Start()
    {
        if (backgroundDatabase == null || backgroundDatabase.stagePrefabs.Count == 0)
        {
            Debug.LogError("StageBackgroundDatabase�� ���ų� ����ֽ��ϴ�!", this);
            this.enabled = false;
            return;
        }

        // ���� ���� �� ù ��� �ε�
        currentBackgroundIndex = startingBackgroundIndex;
        LoadBackgroundByIndex(currentBackgroundIndex);

        // (����) GameManager �����Ͽ� Playing ������ ���� ��ũ�� Ȱ��ȭ ��...
        // if (GameManager.Instance != null) { ... }
    }

    /// <summary>
    /// �ε����� ����Ͽ� Ư�� ��� �������� �ε��մϴ�.
    /// </summary>
    public void LoadBackgroundByIndex(int index)
    {
        if (index < 0 || index >= backgroundDatabase.stagePrefabs.Count)
        {
            Debug.LogError($"�߸��� ��� �ε����Դϴ�: {index}");
            return;
        }

        LoadBackground(backgroundDatabase.stagePrefabs[index]);
        currentBackgroundIndex = index; // ���� �ε��� ���
    }

    /// <summary>
    /// ������ ��ü�� �޾� ����� �ε��ϴ� �ٽ� �Լ�
    /// </summary>
    private void LoadBackground(GameObject bgPrefab)
    {
        // 1. ���� ��� �ν��Ͻ��� ������ �ı�
        if (currentBackgroundInstance != null)
        {
            Destroy(currentBackgroundInstance);
        }

        // 2. �� ��� ������ �ν��Ͻ�ȭ
        if (bgPrefab != null)
        {
            currentBackgroundInstance = Instantiate(bgPrefab, backgroundParent);

            // 3. ������ �� AutoScrollBackground ��ũ��Ʈ�� Train ���� ����
            AutoScrollBackground bgScroll = currentBackgroundInstance.GetComponent<AutoScrollBackground>();
            if (bgScroll != null && train != null)
            {
                bgScroll.train = this.train;
                // (����) �ʱ� ��ũ�� ���� ���� (GameManager ���� ��)
                // bgScroll.enabled = (GameManager.Instance?.CurrentState == GameState.Playing);
            }
            else if (bgScroll == null)
            {
                Debug.LogError($"��� ������ '{bgPrefab.name}'�� AutoScrollBackground.cs�� �����ϴ�!", bgPrefab);
            }
        }
        else
        {
            Debug.LogError("�ε��� ��� �������� null�Դϴ�!");
        }
    }

    /// <summary>
    /// ���� ������ ����� �ε��մϴ�. (�������̸� ó������)
    /// </summary>
    public void LoadNextBackground()
    {
        int nextIndex = (currentBackgroundIndex + 1) % backgroundDatabase.stagePrefabs.Count;
        LoadBackgroundByIndex(nextIndex);
    }

    /// <summary>
    /// ���� ����� �ε��մϴ�. (���� ��� ����)
    /// </summary>
    public void LoadRandomBackground()
    {
        if (backgroundDatabase.stagePrefabs.Count <= 1)
        {
            LoadBackgroundByIndex(0); // ����� �ϳ�����
            return;
        }

        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, backgroundDatabase.stagePrefabs.Count);
        } while (randomIndex == currentBackgroundIndex); // ����� �ٸ� �ε����� ���� ������ �ݺ�

        LoadBackgroundByIndex(randomIndex);
    }
}