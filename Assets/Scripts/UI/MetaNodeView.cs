using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CCQ.UI
{
    /// <summary>
    /// One step on the Star Forge path: a rune medallion on the rail plus its name, effect,
    /// rank pips and price. Presentation only — StarForgeView decides the state.
    /// </summary>
    public class MetaNodeView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _panel;
        [Tooltip("Scaled to make the step the player can afford breathe.")]
        [SerializeField] private RectTransform _medallion;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _glow;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private TextMeshProUGUI _cost;
        [Tooltip("Shard glyph in front of a price — hidden when the step is locked or maxed.")]
        [SerializeField] private Image _costIcon;
        [SerializeField] private Image[] _pips;

        public Button Button => _button;

        /// <summary>Static per language — re-pushed on a language switch, not every refresh.</summary>
        public void SetText(string displayName, string description)
        {
            _name.text = displayName;
            _description.text = description;
        }

        public void SetState(Sprite icon, int rank, int maxRank, string costLabel,
            Color costColor, bool priced, bool locked, bool glowing,
            Color panelIdle, Color panelCurrent, Color glowColor,
            Color pipOn, Color pipOff, Color textNormal, Color textLocked)
        {
            _icon.sprite = icon;
            _icon.color = locked ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
            _panel.color = glowing ? panelCurrent : panelIdle;
            _name.color = locked ? textLocked : textNormal;
            _description.color = locked ? textLocked : textNormal;
            _cost.text = costLabel;
            _cost.color = costColor;
            if (_costIcon.enabled != priced) _costIcon.enabled = priced;
            if (priced) _costIcon.color = costColor;
            if (_glow.enabled != glowing) _glow.enabled = glowing;
            if (glowing) _glow.color = glowColor;
            if (!glowing) _medallion.localScale = Vector3.one;

            for (int i = 0; i < _pips.Length; i++)
            {
                bool used = i < maxRank;
                if (_pips[i].gameObject.activeSelf != used) _pips[i].gameObject.SetActive(used);
                if (used) _pips[i].color = i < rank ? pipOn : pipOff;
            }
        }

        public void SetPulse(float scale)
        {
            _medallion.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
