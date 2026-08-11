using System;
using CCQ.Data;
using UnityEngine;

namespace CCQ.UI
{
    /// <summary>
    /// The decision panel: 3 upgrade cards, or 4 options for a sidekick swap
    /// (3 × "release X" + decline). Layout is set by code — no LayoutGroup rebuilds.
    /// </summary>
    public class ChoicePanel : MonoBehaviour
    {
        [SerializeField] private ChoiceCardView[] _cards;
        [SerializeField] private float _cardSpacingX = 340f;
        [SerializeField] private float _swapSpacingX = 258f;
        [SerializeField] private float _swapCardScale = 0.75f;

        public event Action<int> Picked;

        private void Awake()
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                int index = i; // captured once at init, not per show
                _cards[i].Button.onClick.AddListener(() => Picked?.Invoke(index));
            }
        }

        public void ShowUpgrades(UpgradeCard[] options)
        {
            for (int i = 0; i < 3; i++)
            {
                _cards[i].SetContent(options[i].Icon, options[i].DisplayName, options[i].Description);
                _cards[i].SetVisible(true);
                _cards[i].Rect.anchoredPosition = new Vector2((i - 1) * _cardSpacingX, 0f);
                _cards[i].Rect.localScale = Vector3.one;
            }
            _cards[3].SetVisible(false);
            Open();
        }

        public void ShowSidekickSwap(System.Collections.Generic.List<Sidekick> owned,
            Sidekick incoming, float snackHealPct)
        {
            var swapScale = new Vector3(_swapCardScale, _swapCardScale, 1f);
            for (int i = 0; i < 3; i++)
            {
                Sidekick s = owned[i];
                _cards[i].SetContent(s.Icon, "Release " + s.DisplayName,
                    "Adopt <b>" + incoming.DisplayName + "</b> — " + incoming.Description);
                _cards[i].SetVisible(true);
                _cards[i].Rect.anchoredPosition = new Vector2((i - 1.5f) * _swapSpacingX, 0f);
                _cards[i].Rect.localScale = swapScale;
            }
            _cards[3].SetContent(incoming.Icon, "Wave goodbye",
                "Keep your pod — it gifts a snack (+" + Mathf.RoundToInt(snackHealPct * 100f) + "% HP)");
            _cards[3].SetVisible(true);
            _cards[3].Rect.anchoredPosition = new Vector2(1.5f * _swapSpacingX, 0f);
            _cards[3].Rect.localScale = swapScale;
            Open();
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void Open()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }
    }
}
