using UnityEngine;
using TMPro; // TextMeshPro ���
using UnityEngine.UI; // Image ���

public class LevelUI : MonoBehaviour
{
    [Header("����")]
    [Tooltip("������ TrainLevelManager�� ����")]
    [SerializeField] private TrainLevelManager levelManager;

    [Header("UI ���")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private Slider xpBarSlider;

    void Start()
    {
        if (levelManager == null)
        {
            Debug.LogError("LevelManager�� ������� �ʾҽ��ϴ�!", this);
            this.enabled = false;
            return;
        }

        // �̺�Ʈ ����: ����ġ�� ��ų� �������� ������ UpdateUI �Լ��� �ڵ� ȣ���
        levelManager.OnExperienceGained += UpdateUI;
        levelManager.OnLevelUp += UpdateUI;

        // ���� ���� �� UI �ʱ�ȭ
        UpdateUI();
    }

    void OnDestroy()
    {
        // ������Ʈ �ı� �� �̺�Ʈ ���� ���� (�޸� ���� ����)
        if (levelManager != null)
        {
            levelManager.OnExperienceGained -= UpdateUI;
            levelManager.OnLevelUp -= UpdateUI;
        }
    }

    /// <summary>
    /// UI�� �ֽ� ������ ������Ʈ�մϴ�.
    /// </summary>
    private void UpdateUI()
    {
        // ���� �ؽ�Ʈ ������Ʈ (��: "LV 2")
        levelText.text = $"LV {levelManager.CurrentLevel}";

        // ����ġ �ؽ�Ʈ ������Ʈ (��: "30 / 150")
        xpText.text = $"{levelManager.CurrentLevelDisplayXp} / {levelManager.RequiredLevelDisplayXp}";

        // ����ġ ��(Image Fill) ������Ʈ
        xpBarSlider.value = levelManager.CurrentLevelProgress;
    }
}