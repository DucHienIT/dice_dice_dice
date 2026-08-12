using CCQ.Core;
using CCQ.Data;
using CCQ.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CCQ.UI
{
    /// <summary>
    /// Frequently-changing HUD: round/hits/planet over the battle viewport plus the
    /// stats bar (Lv/XP, HP, ATK, DEF). Lives on its own Canvas; every setter is
    /// change-guarded so battle beats never trigger a rebuild without need.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private TextMeshProUGUI _roundLabel;
        [SerializeField] private TextMeshProUGUI _hitsLabel;
        [SerializeField] private TextMeshProUGUI _planetLabel;
        [SerializeField] private TextMeshProUGUI _speedLabel;
        [SerializeField] private Button _speedButton;
        [SerializeField] private Button _gearButton;
        [Tooltip("Back to the front screen — the tab bar only exists there.")]
        [SerializeField] private Button _homeButton;
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Image _xpFill;
        [Tooltip("Fill ratios below this are hidden — too narrow for the pill's rounded caps.")]
        [SerializeField, Range(0f, 0.2f)] private float _minVisibleXp = 0.055f;
        [SerializeField] private TextMeshProUGUI _hpLabel;
        [SerializeField] private TextMeshProUGUI _atkLabel;
        [SerializeField] private TextMeshProUGUI _defLabel;
        [Tooltip("Static stats-bar captions, in the LocKeys.HudStatTitles order (XP/HP/ATK/DEF).")]
        [SerializeField] private TextMeshProUGUI[] _statTitles;

        private static readonly Color HpOk = new Color(0.55f, 0.94f, 0.48f);
        private static readonly Color HpLow = new Color(1f, 0.36f, 0.48f);

        private int _lastRound = -1, _lastHits = -1, _lastLevel = -1, _lastHp = -1,
            _lastMaxHp = -1, _lastAtk = -1, _lastDef = -1, _lastXp = -1, _lastSpeed = -1;

        // format strings pulled once per language so battle-rate setters stay allocation-free
        private string _roundFormat = "{0}/{1}", _levelFormat = "{0}", _speedFormat = "x{0}";

        public Button SpeedButton => _speedButton;
        public Button GearButton => _gearButton;
        public Button HomeButton => _homeButton;

        /// <summary>
        /// Re-pulls every translated caption and drops the change guards so the next
        /// RefreshRun re-writes all values. Called at start-up and on a language switch.
        /// </summary>
        public void RefreshStaticText()
        {
            _roundFormat = Loc.Get(LocKeys.HudRound);
            _levelFormat = Loc.Get(LocKeys.HudLevel);
            _speedFormat = Loc.Get(LocKeys.HudSpeed);
            for (int i = 0; i < _statTitles.Length && i < LocKeys.HudStatTitles.Length; i++)
            {
                _statTitles[i].text = Loc.Get(LocKeys.HudStatTitles[i]);
            }
            _lastRound = _lastHits = _lastLevel = _lastHp = _lastMaxHp = -1;
            _lastAtk = _lastDef = _lastXp = _lastSpeed = -1;
        }

        public void SetRound(int round, int total)
        {
            if (round == _lastRound) return;
            _lastRound = round;
            _roundLabel.SetText(_roundFormat, Mathf.Max(1, round), total);
        }

        public void SetHits(int hits)
        {
            if (hits == _lastHits) return;
            _lastHits = hits;
            _hitsLabel.SetText("{0}", hits);
        }

        public void SetPlanet(string name)
        {
            _planetLabel.text = name;
        }

        public void SetSpeed(int mult)
        {
            if (mult == _lastSpeed) return;
            _lastSpeed = mult;
            _speedLabel.SetText(_speedFormat, mult);
        }

        public void SetStats(PlayerState p)
        {
            bool levelChanged = p.Level != _lastLevel;
            if (levelChanged)
            {
                _lastLevel = p.Level;
                _levelLabel.SetText(_levelFormat, p.Level);
            }
            if (p.Xp != _lastXp || levelChanged)
            {
                _lastXp = p.Xp;
                float ratio = Mathf.Clamp01(p.Xp / (float)_config.XpNeed(p.Level));
                // width via anchorMax so the 9-sliced pill keeps its rounded caps at any fill
                RectTransform rt = _xpFill.rectTransform;
                rt.anchorMax = new Vector2(ratio, 1f);
                // below two cap widths the slice would squash — hide the sliver instead
                _xpFill.enabled = ratio > _minVisibleXp;
            }
            if (p.Hp != _lastHp || p.MaxHp != _lastMaxHp)
            {
                _lastHp = p.Hp;
                _lastMaxHp = p.MaxHp;
                _hpLabel.SetText("{0}/{1}", p.Hp, p.MaxHp);
                _hpLabel.color = p.Hp <= p.MaxHp * 0.3f ? HpLow : HpOk;
            }
            int atk = Mathf.RoundToInt(p.Atk);
            if (atk != _lastAtk)
            {
                _lastAtk = atk;
                _atkLabel.SetText("{0}", atk);
            }
            if (p.Def != _lastDef)
            {
                _lastDef = p.Def;
                _defLabel.SetText("{0}", p.Def);
            }
        }
    }
}
