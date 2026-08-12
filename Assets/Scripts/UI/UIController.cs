using System;
using System.Collections.Generic;
using CCQ.Core;
using CCQ.Data;
using CCQ.Progression;
using CCQ.Save;
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
        [SerializeField] private StarForgeView _forge;
        [SerializeField] private MainMenuView _menu;
        [SerializeField] private NavBarView _nav;
        [SerializeField] private LocalizedFontView _fonts;

        public event Action EngagePressed;
        public event Action SpeedPressed;
        public event Action GearPressed;
        public event Action<int> ChoicePicked;
        public event Action<int> ForgeNodePicked;
        public event Action ForgeClosed;
        public event Action ForgeResetRequested;
        public event Action HomePressed;
        public event Action MenuStartPressed;
        public event Action MenuNewRunPressed;
        public event Action MenuProfilePressed;
        public event Action<int> NavPicked;
        public event Action OverlayClosed;

        public OverlayView Overlay => _overlay;
        public StarForgeView Forge => _forge;
        public MainMenuView Menu => _menu;

        /// <summary>Any full-screen modal is up, so the run must hold still.</summary>
        public bool IsModalOpen => _overlay.IsOpen || _forge.IsOpen || _menu.IsOpen;

        public void Init()
        {
            _engage.Button.onClick.AddListener(() => EngagePressed?.Invoke());
            _hud.SpeedButton.onClick.AddListener(() => SpeedPressed?.Invoke());
            _hud.GearButton.onClick.AddListener(() => GearPressed?.Invoke());
            _choices.Picked += i => ChoicePicked?.Invoke(i);
            _forge.NodePicked += i => ForgeNodePicked?.Invoke(i);
            _forge.CloseRequested += () => ForgeClosed?.Invoke();
            _forge.ResetRequested += () => ForgeResetRequested?.Invoke();
            _forge.Init();
            _hud.HomeButton.onClick.AddListener(() => HomePressed?.Invoke());
            _menu.StartPressed += () => MenuStartPressed?.Invoke();
            _menu.NewRunPressed += () => MenuNewRunPressed?.Invoke();
            _menu.ProfilePressed += () => MenuProfilePressed?.Invoke();
            _menu.Init();
            _nav.Picked += i => NavPicked?.Invoke(i);
            _nav.Init();
            _overlay.CloseRequested += () => OverlayClosed?.Invoke();
            _choices.Hide();
            _banner.Hide();
            _overlay.Hide();
            RefreshStaticText();
        }

        /// <summary>
        /// Re-applies the per-language font and every translated caption that is not
        /// re-sent by RefreshRun. Called at start-up and after a language switch.
        /// </summary>
        public void RefreshStaticText()
        {
            _fonts.Apply();
            _hud.RefreshStaticText();
            _console.RefreshStaticText();
            _engage.RefreshLabel();
            _forge.RefreshStaticText();
            _menu.RefreshStaticText();
            _nav.RefreshStaticText();
        }

        public void SetForgeBadge(bool on) => _nav.SetForgeBadge(on);

        // ---- star forge ----
        public void ShowForge(MetaState meta)
        {
            _forge.Refresh(meta);
            _forge.Show();
        }

        public void RefreshForge(MetaState meta) => _forge.Refresh(meta);
        public void HideForge() => _forge.Hide();

        // ---- main menu ----
        /// <summary>
        /// Opening the menu also parks the play HUD, so the front screen shows the planet
        /// and the hero rather than a paused dashboard. The tab bar rides on the menu, so it
        /// appears and disappears with it.
        /// </summary>
        public void ShowMenu(bool canContinue, MenuStatus status, BestSaveData best)
        {
            SetPlayVisible(false);
            _menu.Show(canContinue, status, best);
        }

        public void HideMenu()
        {
            _menu.Hide();
            SetPlayVisible(true);
        }

        private void SetPlayVisible(bool on)
        {
            if (_hud.gameObject.activeSelf != on) _hud.gameObject.SetActive(on);
            if (_console.gameObject.activeSelf != on) _console.gameObject.SetActive(on);
        }

        public void Tick(float time, float dt)
        {
            _banner.Tick(dt);
            _console.Tick(dt);
            _engage.Tick(time);
            if (_forge.IsOpen) _forge.Tick(time);
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
