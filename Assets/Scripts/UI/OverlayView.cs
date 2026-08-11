using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CCQ.UI
{
    /// <summary>
    /// Modal overlay (settings / game over). Up to 5 pre-placed buttons — callers pass
    /// label + action pairs; unused buttons hide. While open, GameManager pauses battle.
    /// </summary>
    public class OverlayView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _body;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private TextMeshProUGUI[] _buttonLabels;

        private readonly Action[] _actions = new Action[5];

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                _buttons[i].onClick.AddListener(() => _actions[index]?.Invoke());
            }
        }

        public void Show(string title, string body,
            string label0, Action action0,
            string label1 = null, Action action1 = null,
            string label2 = null, Action action2 = null,
            string label3 = null, Action action3 = null,
            string label4 = null, Action action4 = null)
        {
            _title.text = title;
            _body.text = body;
            Bind(0, label0, action0);
            Bind(1, label1, action1);
            Bind(2, label2, action2);
            Bind(3, label3, action3);
            Bind(4, label4, action4);
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
    }
}
