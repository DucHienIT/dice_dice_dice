using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>One selectable card inside ChoicePanel.</summary>
    public class ChoiceCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;

        public Button Button => _button;
        public RectTransform Rect => (RectTransform)transform;

        public void SetContent(Sprite icon, string name, string description)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _name.text = name;
            _description.text = description;
        }

        public void SetVisible(bool on)
        {
            if (gameObject.activeSelf != on) gameObject.SetActive(on);
        }
    }
}
