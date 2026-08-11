using System;
using System.Collections.Generic;
using CCQ.Core;
using CCQ.Data;
using UnityEngine;

namespace CCQ.UI
{
    /// <summary>
    /// Facade over all UI views. GameManager talks only to this class; button presses
    /// surface as C# events. No gameplay logic lives here.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private HudView _hud;
        [SerializeField] private ConsoleView _console;
        [SerializeField] private FortuneBanner _banner;
        [SerializeField] private ChoicePanel _choices;
        [SerializeField] private SidekickChipsView _chips;
        [SerializeField] private EngageButton _engage;
        [SerializeField] private OverlayView _overlay;

        public event Action EngagePressed;
        public event Action SpeedPressed;
        public event Action GearPressed;
        public event Action<int> ChoicePicked;

        public OverlayView Overlay => _overlay;

        public void Init()
        {
            _engage.Button.onClick.AddListener(() => EngagePressed?.Invoke());
            _hud.SpeedButton.onClick.AddListener(() => SpeedPressed?.Invoke());
            _hud.GearButton.onClick.AddListener(() => GearPressed?.Invoke());
            _choices.Picked += i => ChoicePicked?.Invoke(i);
            _choices.Hide();
            _banner.Hide();
            _overlay.Hide();
        }

        public void Tick(float time, float dt)
        {
            _banner.Tick(dt);
            _console.Tick(dt);
            _engage.Tick(time);
        }

        // ---- HUD ----
        public void RefreshRun(RunState run, string planetName)
        {
            _hud.SetStats(run.Player);
            _hud.SetRound(run.Round, _config.RoundsPerPlanet);
            _hud.SetHits(run.Stats.Hits);
            _hud.SetPlanet(planetName);
            _hud.SetSpeed(_config.Speeds[run.SpeedIndex]);
            _console.SetStarCycle(run.StarCycle);
            _chips.SetSidekicks(run.Player.Sidekicks);
        }

        public void SetStats(PlayerState player) => _hud.SetStats(player);
        public void SetHits(int hits) => _hud.SetHits(hits);
        public void SetSpeed(int mult) => _hud.SetSpeed(mult);

        // ---- console ----
        public void SetEventText(string formatted) => _console.SetEventText(formatted);
        public void ScrambleGlyphs() => _console.ScrambleGlyphs();

        // ---- banner ----
        public void ShowBanner(Sprite icon, string title, string tag) =>
            _banner.Show(icon, title, tag);

        // ---- choices ----
        public void ShowUpgradeChoices(UpgradeCard[] options) => _choices.ShowUpgrades(options);

        public void ShowSidekickSwap(List<Sidekick> owned, Sidekick incoming) =>
            _choices.ShowSidekickSwap(owned, incoming, _config.SnackHeal);

        public void HideChoices() => _choices.Hide();

        // ---- engage ----
        public void SetEngageMode(EngageButton.Mode mode) => _engage.SetMode(mode);
    }
}
