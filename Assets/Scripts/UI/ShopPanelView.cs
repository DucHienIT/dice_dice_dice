using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>Shop panel; visible only during the shopping phase (spec 6.4). Hidden via CanvasGroup, no hierarchy toggling.</summary>
    public class ShopPanelView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private ShopItemView[] _items;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TMP_Text _rerollLabel;
        [SerializeField] private Button _lockButton;
        [SerializeField] private TMP_Text _lockLabel;
        [SerializeField] private Image _lockBackground;
        [SerializeField] private Button _startWaveButton;
        [SerializeField] private TMP_Text _startWaveLabel;
        [SerializeField] private Button _sellButton;
        [SerializeField] private TMP_Text _sellLabel;

        public ShopItemView[] Items => _items;
        public Button RerollButton => _rerollButton;
        public Button LockButton => _lockButton;
        public Button StartWaveButton => _startWaveButton;
        public Button SellButton => _sellButton;

        /// <summary>Sell lives outside the panel body (next to the board) so it stays reachable by thumb.</summary>
        public void SetSell(bool visible, string label)
        {
            _sellButton.gameObject.SetActive(visible);
            if (visible)
            {
                _sellLabel.text = label;
            }
        }

        public void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        public void RefreshButtons(int rerollCost, bool locked, int nextWave, int totalWaves, PaletteConfig palette)
        {
            _rerollLabel.text = "Reroll (" + rerollCost + "g)";
            _lockLabel.text = locked ? "Locked" : "Lock";
            _lockBackground.color = locked ? new Color(1f, 0.8f, 0.35f) : Color.white;
            _startWaveButton.interactable = nextWave <= totalWaves;
            _startWaveLabel.text = "Start Wave " + Mathf.Min(nextWave, totalWaves);
        }
    }
}
