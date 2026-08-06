using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>One shop card - same shape as the roguelike choice cards. Click buys.</summary>
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;
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

        public void Render(ShopController.ShopOffer offer, UiSkin skin)
        {
            ItemDefinition definition = offer.Definition;
            _background.sprite = skin.CardBg(definition.Group);
            _border.sprite = skin.CardBorder(definition.Group);
            _icon.sprite = definition.IconSprite;
            _icon.color = Color.white;
            _nameLabel.text = definition.DisplayName;
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
