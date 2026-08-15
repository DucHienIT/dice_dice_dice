using Game.Localization;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Swaps every text in the game over to a diacritic-capable font for languages the
    /// display font cannot render — LilitaOne carries no Vietnamese glyphs, and letting
    /// TMP fall back per character would mix two typefaces inside a single word.
    /// Applied at start-up and on a language switch only, never per frame.
    /// </summary>
    public class LocalizedFontView : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _defaultFont;
        [SerializeField] private TMP_FontAsset _wideCharsetFont;
        [Tooltip("Language codes whose glyphs the default font lacks.")]
        [SerializeField] private string[] _wideCharsetCodes = { Loc.VietnameseCode };
        [Header("Targets — wired by the game builder")]
        [SerializeField] private TextMeshProUGUI[] _uiTexts;
        [SerializeField] private TextMeshPro[] _worldTexts;
        [Tooltip("Outlined materials the world texts use; one per font.")]
        [SerializeField] private Material _defaultWorldMaterial;
        [SerializeField] private Material _wideCharsetWorldMaterial;

        public void Apply()
        {
            if (_defaultFont == null || _wideCharsetFont == null) return;
            bool wide = UsesWideCharset(Loc.CurrentCode);
            TMP_FontAsset font = wide ? _wideCharsetFont : _defaultFont;
            Material worldMaterial = wide ? _wideCharsetWorldMaterial : _defaultWorldMaterial;

            for (int i = 0; i < _uiTexts.Length; i++)
            {
                TextMeshProUGUI text = _uiTexts[i];
                if (text != null && text.font != font) text.font = font;
            }
            for (int i = 0; i < _worldTexts.Length; i++)
            {
                TextMeshPro text = _worldTexts[i];
                if (text == null || text.font == font) continue;
                text.font = font;
                if (worldMaterial != null) text.fontSharedMaterial = worldMaterial;
            }
        }

        private bool UsesWideCharset(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            for (int i = 0; i < _wideCharsetCodes.Length; i++)
            {
                if (_wideCharsetCodes[i] == code) return true;
            }
            return false;
        }
    }
}
