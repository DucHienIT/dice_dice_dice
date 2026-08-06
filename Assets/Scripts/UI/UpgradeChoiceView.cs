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
        [SerializeField] private TMP_Text _groupLabel;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;

        private UpgradeDefinition _upgrade;
        private Action<UpgradeDefinition> _onPicked;

        public void Init(PaletteConfig palette)
        {
            _background.sprite = SpriteFactory.UiRounded;
            _background.type = Image.Type.Sliced;
            _background.color = palette.Panel;
            _button.onClick.AddListener(OnClicked);
        }

        public void Render(UpgradeDefinition upgrade, PaletteConfig palette, string groupName, Action<UpgradeDefinition> onPicked)
        {
            _upgrade = upgrade;
            _onPicked = onPicked;
            _groupLabel.text = groupName;
            _groupLabel.color = palette.GroupColor(upgrade.Group);
            _nameLabel.text = upgrade.DisplayName;
            _descriptionLabel.text = upgrade.Description;
        }

        private void OnClicked()
        {
            _onPicked?.Invoke(_upgrade);
        }
    }
}
