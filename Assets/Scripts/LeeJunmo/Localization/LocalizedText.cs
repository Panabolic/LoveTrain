using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach only to fixed labels. Event and item text is resolved by its presenter.
[DisallowMultipleComponent]
public sealed class LocalizedText : MonoBehaviour
{
    public string localizationKey;
    [TextArea] public string fallbackText;

    private void OnEnable()
    {
        EnglishLocalization.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => EnglishLocalization.LanguageChanged -= Refresh;

    public void Refresh()
    {
        string value = EnglishLocalization.Get(localizationKey, fallbackText);
        TMP_Text tmp = GetComponent<TMP_Text>();
        if (tmp != null) { tmp.text = value; return; }
        Text legacy = GetComponent<Text>();
        if (legacy != null) legacy.text = value;
    }
}
