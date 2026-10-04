using UnityEngine;
using TMPro; // TextMeshPro 사용
using UnityEngine.UI; // Slider 사용
using DG.Tweening; // ✨ DOTween 사용

public class LevelUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("기차의 TrainLevelManager를 연결")]
    [SerializeField] private TrainLevelManager levelManager;

    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider xpBarSlider;

    private Train train;
    void Start()
    {
        if (levelManager != null) train = levelManager.GetComponent<Train>();
    }
    void Update()
    {
        if (train == null) return;
        if (levelText != null) levelText.text = $"F · {levelManager.CreationCost}   FUEL {Mathf.CeilToInt(train.CurrentFuel)}";
        if (xpBarSlider != null) xpBarSlider.value = train.MaxFuel > 0f ? train.CurrentFuel / train.MaxFuel : 0f;
    }
}
