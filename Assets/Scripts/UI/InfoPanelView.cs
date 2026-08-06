using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>Bottom-left info panel: selected item details or default hints, plus the sell button.</summary>
    public class InfoPanelView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private Button _sellButton;
        [SerializeField] private TMP_Text _sellLabel;

        public Button SellButton => _sellButton;

        public void Init(PaletteConfig palette)
        {
            _background.sprite = SpriteFactory.UiRounded;
            _background.type = Image.Type.Sliced;
            _background.color = palette.Panel;
        }

        public void ShowText(string richText, bool sellVisible, string sellLabel)
        {
            _body.text = richText;
            _sellButton.gameObject.SetActive(sellVisible);
            if (sellVisible)
            {
                _sellLabel.text = sellLabel;
            }
        }
    }
}
