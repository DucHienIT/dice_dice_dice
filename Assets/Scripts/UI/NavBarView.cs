using System;
using CCQ.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CCQ.UI
{
    /// <summary>
    /// The bottom tab strip: the only always-on way into the non-gameplay screens. It lives
    /// at the very bottom because that is the reachable half of a tall phone, and every tab
    /// is a full-height touch target rather than just its icon.
    /// </summary>
    public class NavBarView : MonoBehaviour
    {
        /// <summary>Forge, Profile, Records, Settings — index-aligned with LocKeys.NavTitles.</summary>
        public const int TabCount = 4;

        [SerializeField] private Button[] _buttons;
        [SerializeField] private TextMeshProUGUI[] _labels;
        [Tooltip("Dot on the Forge tab, lit when a rank is affordable right now.")]
        [SerializeField] private GameObject _forgeBadge;

        public event Action<int> Picked;

        public void Init()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i; // captured once at init, not per press
                _buttons[i].onClick.AddListener(() => Picked?.Invoke(index));
            }
            RefreshStaticText();
            SetForgeBadge(false);
        }

        public void RefreshStaticText()
        {
            for (int i = 0; i < _labels.Length && i < LocKeys.NavTitles.Length; i++)
            {
                _labels[i].text = Loc.Get(LocKeys.NavTitles[i]);
            }
        }

        public void SetForgeBadge(bool on)
        {
            if (_forgeBadge.activeSelf != on) _forgeBadge.SetActive(on);
        }
    }
}
