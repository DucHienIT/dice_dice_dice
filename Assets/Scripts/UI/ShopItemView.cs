using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>One shop row: icon, name, description, price. Click buys, hover previews in the info panel.</summary>
    public class ShopItemView : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _tagLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private TMP_Text _priceLabel;
        [SerializeField] private CanvasGroup _group;

        private UIController _controller;
        private int _index;

        public void Init(int index, UIController controller, PaletteConfig palette)
        {
            _index = index;
            _controller = controller;
            _background.sprite = SpriteFactory.UiRounded;
            _background.type = Image.Type.Sliced;
            _background.color = palette.PanelLight;
            _button.onClick.AddListener(OnClicked);
        }

        public void Render(ShopController.ShopOffer offer, PaletteConfig palette, string groupName)
        {
            ItemDefinition definition = offer.Definition;
            _icon.sprite = SpriteFactory.Icon(definition.Icon);
            _icon.color = palette.GroupColor(definition.Group);
            _nameLabel.text = definition.DisplayName;
            _nameLabel.color = palette.GroupColor(definition.Group);
            _tagLabel.text = "Common · " + groupName;
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            _controller.OnShopItemHovered(_index);
        }
    }
}
