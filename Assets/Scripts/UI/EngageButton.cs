using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CCQ.UI
{
    /// <summary>The one button. Mode drives label, art and interactability; idle pulse via Tick.</summary>
    public class EngageButton : MonoBehaviour
    {
        public enum Mode
        {
            Engage,
            Traveling,
            Battling,
            Choosing,
            Dead
        }

        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Sprite _readySprite;
        [SerializeField] private Sprite _lockedSprite;
        [SerializeField] private Sprite _deadSprite;
        [SerializeField] private float _pulseAmount = 0.035f;
        [SerializeField] private float _pulseSpeed = 3.2f;

        private Mode _mode = Mode.Engage;

        public Button Button => _button;
        public Mode CurrentMode => _mode;

        public void SetMode(Mode mode)
        {
            _mode = mode;
            switch (mode)
            {
                case Mode.Engage:
                    _label.text = "ENGAGE";
                    _background.sprite = _readySprite;
                    _button.interactable = true;
                    break;
                case Mode.Traveling:
                    // looks locked, but stays tappable so mashing ENGAGE cuts the walk short
                    _label.text = "TRAVELING...";
                    _background.sprite = _lockedSprite;
                    _button.interactable = true;
                    break;
                case Mode.Battling:
                    _label.text = "BATTLING...";
                    _background.sprite = _lockedSprite;
                    _button.interactable = false;
                    break;
                case Mode.Choosing:
                    _label.text = "CHOOSE...";
                    _background.sprite = _lockedSprite;
                    _button.interactable = false;
                    break;
                case Mode.Dead:
                    _label.text = "NEW VOYAGE";
                    _background.sprite = _deadSprite;
                    _button.interactable = true;
                    break;
            }
            if (mode != Mode.Engage) transform.localScale = Vector3.one;
        }

        public void Tick(float time)
        {
            if (_mode != Mode.Engage && _mode != Mode.Dead) return;
            float s = 1f + Mathf.Sin(time * _pulseSpeed) * _pulseAmount;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
