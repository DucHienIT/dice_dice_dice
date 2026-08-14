using System;
using Game.Data;
using Game.Localization;
using Game.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The Star MetaPath screen: one continuous path of rune steps climbing the left rail.
    /// Every step and rail segment is pre-placed by the builder; this class only re-colours
    /// them from a <see cref="MetaState"/>. While it is open GameManager pauses the run.
    /// </summary>
    public class MetaPathView : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private MetaNodeView[] _nodes;
        [Tooltip("Rail segment below each step, index i joining step i to step i+1.")]
        [SerializeField] private RectTransform[] _railFills;
        [Tooltip("Full height of each rail segment, so the fill can show partial progress.")]
        [SerializeField] private float[] _railLengths;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _shards;
        [SerializeField] private TextMeshProUGUI _hint;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _closeLabel;
        [SerializeField] private Button _resetButton;
        [SerializeField] private TextMeshProUGUI _resetLabel;

        [Header("Palette")]
        // opaque on purpose: a translucent strip lets the rail behind it tint its edge
        [SerializeField] private Color _panelIdle = new Color(0.10f, 0.07f, 0.24f, 1f);
        [Tooltip("The step the player is working on — the screen's single focal point.")]
        [SerializeField] private Color _panelCurrent = new Color(0.24f, 0.18f, 0.52f, 1f);
        [SerializeField] private Color _glow = new Color(1f, 0.83f, 0.36f, 0.42f);
        [SerializeField] private Color _pipOn = new Color(1f, 0.83f, 0.36f, 1f);
        [SerializeField] private Color _pipOff = new Color(1f, 1f, 1f, 0.16f);
        [SerializeField] private Color _textNormal = new Color(0.91f, 0.92f, 0.96f, 1f);
        [SerializeField] private Color _textLocked = new Color(0.50f, 0.51f, 0.63f, 1f);
        [SerializeField] private Color _costAffordable = new Color(1f, 0.83f, 0.36f, 1f);
        [SerializeField] private Color _costTooDear = new Color(0.60f, 0.61f, 0.73f, 1f);
        [SerializeField] private Color _costMaxed = new Color(0.42f, 0.94f, 0.60f, 1f);

        [Header("Feel")]
        [SerializeField] private float _pulseAmplitude = 0.05f;
        [SerializeField] private float _pulseSpeed = 3.4f;

        public event Action<int> NodePicked;
        public event Action CloseRequested;
        public event Action ResetRequested;

        public bool IsOpen => gameObject.activeSelf;
        public int NodeCount => _nodes.Length;

        /// <summary>The one step that is unlocked, unfinished and affordable, else -1.</summary>
        private int _pulsing = -1;

        public void Init()
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                int index = i; // captured once at init, not per refresh
                _nodes[i].Button.onClick.AddListener(() => NodePicked?.Invoke(index));
            }
            _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            _resetButton.onClick.AddListener(() => ResetRequested?.Invoke());
            RefreshStaticText();
            Hide();
        }

        /// <summary>Captions that only change with the language, not with the meta state.</summary>
        public void RefreshStaticText()
        {
            _title.text = Loc.Get(LocKeys.MetaPathTitle);
            _hint.text = Loc.Get(LocKeys.MetaPathIntro);
            _closeLabel.text = Loc.Get(LocKeys.MetaPathBack);
            _resetLabel.text = Loc.Get(LocKeys.OverlayResetAll);
            MetaUpgrade[] steps = _config.MetaUpgrades;
            for (int i = 0; i < _nodes.Length && i < steps.Length; i++)
            {
                _nodes[i].SetText(steps[i].DisplayName, steps[i].Description);
            }
        }

        public void Refresh(MetaState meta)
        {
            MetaUpgrade[] steps = _config.MetaUpgrades;
            _shards.text = Loc.Format(LocKeys.MetaPathShards, meta.Shards);
            _pulsing = -1;

            for (int i = 0; i < _nodes.Length && i < steps.Length; i++)
            {
                MetaUpgrade step = steps[i];
                int rank = meta.Ranks[i];
                bool unlocked = meta.IsUnlocked(_config, i);
                int cost = meta.CostOf(_config, i);
                bool affordable = unlocked && cost >= 0 && meta.Shards >= cost;
                // on a single chain exactly one step is unlocked but unfinished: the
                // one being worked on. It carries the highlight, and pulses when payable.
                bool current = unlocked && cost >= 0;
                if (current && affordable) _pulsing = i;

                string label;
                Color color;
                bool priced = false;
                if (!unlocked)
                {
                    label = Loc.Get(LocKeys.MetaPathLocked);
                    color = _textLocked;
                }
                else if (cost < 0)
                {
                    label = Loc.Get(LocKeys.MetaPathMaxed);
                    color = _costMaxed;
                }
                else
                {
                    label = Loc.Format(LocKeys.MetaPathCost, cost);
                    color = affordable ? _costAffordable : _costTooDear;
                    priced = true;
                }

                _nodes[i].SetState(step.RankIcon(rank), rank, step.MaxRank, label, color,
                    priced, !unlocked, current, _panelIdle, _panelCurrent, _glow,
                    _pipOn, _pipOff, _textNormal, _textLocked);
            }

            // each rail segment fills as the step below it is ranked up towards the gate
            for (int i = 0; i < _railFills.Length && i + 1 < steps.Length; i++)
            {
                int gate = Mathf.Max(1, steps[i + 1].RequiredRank);
                float progress = Mathf.Clamp01((float)meta.Ranks[i] / gate);
                Vector2 size = _railFills[i].sizeDelta;
                _railFills[i].sizeDelta = new Vector2(size.x, _railLengths[i] * progress);
            }
        }

        /// <summary>Ticked by UIController only while the screen is up.</summary>
        public void Tick(float time)
        {
            if (_pulsing < 0) return;
            _nodes[_pulsing].SetPulse(1f + _pulseAmplitude * Mathf.Sin(time * _pulseSpeed));
        }

        public void Show()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
}
