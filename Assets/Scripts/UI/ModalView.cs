using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>Full-screen modal: intro, level-up choices, end-of-run stats. Only one mode visible at a time.</summary>
    public class ModalView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private GameObject _choicesRoot;
        [SerializeField] private UpgradeChoiceView[] _choices;
        [SerializeField] private Image _statsBackground;
        [SerializeField] private TMP_Text _statsBody;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionLabel;

        private Action _onAction;

        public UpgradeChoiceView[] Choices => _choices;

        public void Init()
        {
            for (int i = 0; i < _choices.Length; i++)
            {
                _choices[i].Init();
            }
            _actionButton.onClick.AddListener(OnActionClicked);
            _root.SetActive(false);
        }

        public void ShowInfo(string title, string subtitle, string body, string actionLabel, Action onAction)
        {
            _title.text = title;
            _subtitle.text = subtitle;
            _choicesRoot.SetActive(false);
            _statsBackground.gameObject.SetActive(true);
            _statsBody.text = body;
            _actionButton.gameObject.SetActive(true);
            _actionLabel.text = actionLabel;
            _onAction = onAction;
            _root.SetActive(true);
        }

        public void ShowChoices(string title, string subtitle)
        {
            _title.text = title;
            _subtitle.text = subtitle;
            _choicesRoot.SetActive(true);
            _statsBackground.gameObject.SetActive(false);
            _actionButton.gameObject.SetActive(false);
            _onAction = null;
            _root.SetActive(true);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        private void OnActionClicked()
        {
            Action action = _onAction;
            _onAction = null;
            action?.Invoke();
        }
    }
}
