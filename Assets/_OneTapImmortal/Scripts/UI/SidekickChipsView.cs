using System.Collections.Generic;
using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Row of chips showing the owned sidekicks (max 3).</summary>
    public class SidekickChipsView : MonoBehaviour
    {
        [SerializeField] private GameObject[] _chips;
        [SerializeField] private Image[] _icons;
        [SerializeField] private TextMeshProUGUI[] _names;

        public void SetSidekicks(List<Sidekick> owned)
        {
            for (int i = 0; i < _chips.Length; i++)
            {
                bool active = i < owned.Count;
                if (_chips[i].activeSelf != active) _chips[i].SetActive(active);
                if (!active) continue;
                _icons[i].sprite = owned[i].Icon;
                _names[i].text = owned[i].DisplayName;
            }
        }
    }
}
