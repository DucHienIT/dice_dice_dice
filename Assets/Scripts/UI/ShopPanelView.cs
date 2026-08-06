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

        public ShopItemView[] Items => _items;
        public Button RerollButton => _rerollButton;
        public Button LockButton => _lockButton;
        public Button StartWaveButton => _startWaveButton;

        public void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        public void RefreshButtons(int rerollCost, bool locked, int nextWave, int totalWaves, PaletteConfig palette)
        {
            _rerollLabel.text = "Reroll (" + rerollCost + " vàng)";
            _lockLabel.text = locked ? "Đã khóa" : "Khóa";
            _lockBackground.color = locked ? new Color(1f, 0.8f, 0.35f) : Color.white;
            _startWaveButton.interactable = nextWave <= totalWaves;
            _startWaveLabel.text = "Bắt đầu Wave " + Mathf.Min(nextWave, totalWaves);
        }
    }
}
