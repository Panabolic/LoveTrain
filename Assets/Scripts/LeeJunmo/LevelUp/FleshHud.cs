using TMPro;
using UnityEngine;

// Renders authored scene/prefab references. The wallet retains all resource and creation ownership.
[DisallowMultipleComponent]
public sealed class FleshHud : MonoBehaviour
{
    [SerializeField] private TrainLevelManager wallet;
    [SerializeField] private TMP_Text fleshAmount;
    [SerializeField] private TMP_Text soulAmount;
    [SerializeField] private TMP_Text creationLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private UnityEngine.UI.Image completedFill;
    [SerializeField] private UnityEngine.UI.Image progressFill;
    [SerializeField] private GameObject creationHint;

    private static readonly Color32[] Palette =
    {
        new Color32(237, 105, 105, 255),
        new Color32(245, 149, 73, 255),
        new Color32(242, 211, 103, 255),
        new Color32(108, 206, 144, 255),
        new Color32(98, 155, 233, 255),
        new Color32(138, 131, 222, 255),
        new Color32(186, 133, 231, 255),
        new Color32(241, 152, 204, 255),
        new Color32(103, 211, 209, 255),
        new Color32(244, 238, 223, 255)
    };

    private TrainLevelManager subscribedWallet;
    private int affordableCount;
    private bool hintVisible;

    private void OnEnable()
    {
        subscribedWallet = wallet;
        if (subscribedWallet != null) subscribedWallet.OnResourcesChanged += RefreshResources;
        EnglishLocalization.LanguageChanged += RefreshResources;
        RefreshResources();
        RefreshCreationHint(true);
    }

    // All Awake calls have completed here, including the wallet's persisted soul initialization.
    private void Start() => RefreshResources();

    private void OnDisable()
    {
        if (subscribedWallet != null) subscribedWallet.OnResourcesChanged -= RefreshResources;
        subscribedWallet = null;
        EnglishLocalization.LanguageChanged -= RefreshResources;
        hintVisible = false;
        if (creationHint != null) creationHint.SetActive(false);
    }

    private void LateUpdate() => RefreshCreationHint();

    private void RefreshResources()
    {
        if (wallet == null)
        {
            affordableCount = 0;
            RefreshCreationHint();
            return;
        }

        FleshCreationProgress progress = FleshCreationProgress.Calculate(wallet.Flesh, wallet.CreationCost, wallet.CreationCostIncrease);
        affordableCount = progress.AffordableCount;
        if (fleshAmount != null) fleshAmount.text = EnglishLocalization.Format("hud.flesh_amount", "살점 {0}", wallet.Flesh);
        if (soulAmount != null) soulAmount.text = EnglishLocalization.Format("hud.soul_amount", "영혼 {0}", wallet.Souls);
        if (creationLabel != null)
            creationLabel.text = affordableCount >= 2
                ? EnglishLocalization.Format("hud.creation_multiple", "조직 창조 X {0}", affordableCount)
                : EnglishLocalization.Get("hud.creation", "조직 창조");
        if (costLabel != null) costLabel.text = EnglishLocalization.Format("hud.creation_cost", "비용 {0}", wallet.CreationCost);

        if (completedFill != null)
        {
            completedFill.enabled = affordableCount > 0;
            completedFill.fillAmount = 1f;
            if (affordableCount > 0) completedFill.color = Palette[(affordableCount - 1) % Palette.Length];
        }
        if (progressFill != null)
        {
            progressFill.color = Palette[affordableCount % Palette.Length];
            progressFill.fillAmount = progress.Fraction;
            // Authored sprite-free Simple images express progress through their anchored width.
            RectTransform fillRect = progressFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(progress.Fraction, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }
        RefreshCreationHint();
    }

    private void RefreshCreationHint(bool force = false)
    {
        bool visible = wallet != null && affordableCount > 0 && wallet.CanRequestCreation;
        if (!force && visible == hintVisible) return;
        hintVisible = visible;
        if (creationHint != null) creationHint.SetActive(visible);
    }
}
