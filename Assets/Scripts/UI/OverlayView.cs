using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CCQ.UI
{
    /// <summary>
    /// The one modal panel shell every screen-over-the-game uses (settings, profile, records,
    /// game over). Mobile-game convention: the card carries the title and up to 6 action
    /// buttons, and a round ✕ hangs *below* it — within thumb reach and never covering
    /// content. The card grows to fit its text instead of leaving a hole under short panels.
    /// While it is open GameManager pauses the run.
    /// </summary>
    public class OverlayView : MonoBehaviour
    {
        public const int MaxButtons = 6;

        private const float ButtonPitch = 112f;
        private const float ButtonHeight = 104f;
        private const float ButtonBottom = 96f;
        /// <summary>Gap the body text keeps above the topmost button.</summary>
        private const float ButtonGap = 26f;
        /// <summary>Card top down to where the builder anchors the body text.</summary>
        private const float TitleBlock = 150f;
        private const float MinCardHeight = 480f;

        [SerializeField] private RectTransform _card;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _body;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private TextMeshProUGUI[] _buttonLabels;
        [SerializeField] private Button _closeButton;
        [SerializeField] private float _bodyWidth = 760f;
        [SerializeField] private float _maxCardHeight = 1440f;

        private readonly Action[] _actions = new Action[MaxButtons];

        /// <summary>Raised by the ✕. The caller decides what "closed" means for its screen.</summary>
        public event Action CloseRequested;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                _buttons[i].onClick.AddListener(() => _actions[index]?.Invoke());
            }
            _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        public void Show(string title, string body,
            string label0 = null, Action action0 = null,
            string label1 = null, Action action1 = null,
            string label2 = null, Action action2 = null,
            string label3 = null, Action action3 = null,
            string label4 = null, Action action4 = null,
            string label5 = null, Action action5 = null)
        {
            _title.text = title;
            _body.text = body;
            Bind(0, label0, action0);
            Bind(1, label1, action1);
            Bind(2, label2, action2);
            Bind(3, label3, action3);
            Bind(4, label4, action4);
            Bind(5, label5, action5);
            Layout();
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void Bind(int i, string label, Action action)
        {
            if (i >= _buttons.Length) return;
            bool used = !string.IsNullOrEmpty(label);
            _buttons[i].gameObject.SetActive(used);
            if (!used) return;
            _buttonLabels[i].text = label;
            _actions[i] = action;
        }

        /// <summary>
        /// Sizes the card to title + text + however many buttons are actually in use, then
        /// stacks those buttons up from the card's bottom edge so they stay put as it grows.
        /// </summary>
        private void Layout()
        {
            int used = 0;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i].gameObject.activeSelf) used++;
            }

            float textHeight = Mathf.Max(120f, _body.GetPreferredValues(_body.text,
                _bodyWidth, 0f).y);
            // measured from the card's bottom edge to the top of the topmost button — the
            // stack is (used-1) pitches plus one whole button, not `used` pitches
            float buttonBlock = used > 0
                ? ButtonBottom + (used - 1) * ButtonPitch + ButtonHeight + ButtonGap
                : 90f;
            float height = Mathf.Clamp(TitleBlock + textHeight + buttonBlock,
                MinCardHeight, _maxCardHeight);
            _card.sizeDelta = new Vector2(_card.sizeDelta.x, height);
            _body.rectTransform.sizeDelta = new Vector2(_bodyWidth,
                Mathf.Min(textHeight, height - TitleBlock - buttonBlock));

            int slot = 0;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (!_buttons[i].gameObject.activeSelf) continue;
                var rt = (RectTransform)_buttons[i].transform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x,
                    ButtonBottom + (used - 1 - slot) * ButtonPitch);
                slot++;
            }
        }
    }
}
