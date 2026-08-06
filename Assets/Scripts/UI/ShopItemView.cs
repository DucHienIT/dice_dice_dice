using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>One shop row: icon, name, one-line description, price. Click buys.</summary>
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private TMP_Text _priceLabel;
        [SerializeField] private CanvasGroup _group;

        private UIController _controller;
        private int _index;

        public void Init(int index, UIController controller)
        {
            _index = index;
            _controller = controller;
            _button.onClick.AddListener(OnClicked);
        }

        public void Render(ShopController.ShopOffer offer, PaletteConfig palette)
        {
            ItemDefinition definition = offer.Definition;
            _icon.sprite = definition.IconSprite;
            _icon.color = Color.white;
            _nameLabel.text = definition.DisplayName;
            _nameLabel.color = palette.GroupColor(definition.Group);
            _descriptionLabel.text = definition.Description;
            _priceLabel.text = definition.Price.ToString();
            _group.alpha = offer.Sold ? 0.35f : 1f;
            _group.interactable = !offer.Sold;
            _group.blocksRaycasts = !offer.Sold;
        }

        private void OnClicked()
        {
            _controller.OnShopItemClicked(_index);
        }
    }
}
