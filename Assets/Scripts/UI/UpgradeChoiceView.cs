using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>One of the three roguelike choice cards in the level-up modal.</summary>
    public class UpgradeChoiceView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;
        [SerializeField] private TMP_Text _groupLabel;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;

        private UpgradeDefinition _upgrade;
        private Action<UpgradeDefinition> _onPicked;

        public void Init()
        {
            _button.onClick.AddListener(OnClicked);
        }

        public void Render(UpgradeDefinition upgrade, UiSkin skin, string groupName, Action<UpgradeDefinition> onPicked)
        {
            _upgrade = upgrade;
            _onPicked = onPicked;
            _background.sprite = skin.CardBg(upgrade.Group);
            _border.sprite = skin.CardBorder(upgrade.Group);
            _groupLabel.text = groupName;
            _nameLabel.text = upgrade.DisplayName;
            _descriptionLabel.text = upgrade.Description;
        }

        private void OnClicked()
        {
            _onPicked?.Invoke(_upgrade);
        }
    }
}
