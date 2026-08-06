using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceDiceDice
{
    /// <summary>
    /// Central phase state machine (Shopping ↔ Wave → GameOver) and the single Update entry that ticks
    /// every system in a fixed order (CODE_RULES 3, 5.2). Coordinates only — no domain logic.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig _config;
        [SerializeField] private PaletteConfig _palette;
        [SerializeField] private UiSkin _uiSkin;

        [Header("Systems")]
        [SerializeField] private BoardController _board;
        [SerializeField] private EconomyController _economy;
        [SerializeField] private ShopController _shop;
        [SerializeField] private EnemyManager _enemies;
        [SerializeField] private ProjectileManager _projectiles;
        [SerializeField] private EffectManager _effects;
        [SerializeField] private WaveSpawner _spawner;
        [SerializeField] private ItemTicker _ticker;
        [SerializeField] private AuraService _auras;
        [SerializeField] private UpgradeSystem _upgrades;
        [SerializeField] private BaseWall _wall;
        [SerializeField] private AudioManager _audio;
        [SerializeField] private WallView _wallView;
        [SerializeField] private UIController _ui;

        [Header("Run start (spec 14.1)")]
        [SerializeField] private ItemDefinition[] _startingItems;

        private readonly List<UpgradeDefinition> _choiceBuffer = new List<UpgradeDefinition>(3);

        public GamePhase Phase { get; private set; } = GamePhase.Shopping;
        public int Wave { get; private set; }
        public bool Paused { get; private set; }
        public RunStats Stats { get; } = new RunStats();
        public RunModifiers Mods { get; } = new RunModifiers();
        public CombatContext Ctx { get; private set; }

        public GameConfig Config => _config;
        public PaletteConfig Palette => _palette;
        public UiSkin Skin => _uiSkin;
        public BoardController Board => _board;
        public EconomyController Economy => _economy;
        public ShopController Shop => _shop;
        public ItemTicker Ticker => _ticker;
        public BaseWall Wall => _wall;
        public AudioManager Audio => _audio;
        public UpgradeSystem Upgrades => _upgrades;

        public event Action PhaseChanged;
        public event Action WaveChanged;
        public event Action<string> BannerRequested;
        public event Action<string> ToastRequested;
        public event Action<int, int> GoldPopupRequested;

        private void Awake()
        {
            Application.targetFrameRate = _config.TargetFrameRate;
            Ctx = new CombatContext
            {
                Config = _config,
                Enemies = _enemies,
                Projectiles = _projectiles,
                Effects = _effects,
                Audio = _audio,
                Mods = Mods,
                Auras = _auras,
                Stats = Stats,
                ShowBanner = RequestBanner
            };
        }

        private void Start()
        {
            _wall.Init();
            _economy.Init();
            _board.Init(Stats, Mods);
            _auras.Init(Mods);
            _enemies.Init(Stats, _wall);
            _projectiles.Init(Ctx);
            _effects.Init();
            _shop.Init(Stats, Mods);
            _ticker.Init(Ctx, Stats, Mods);
            _upgrades.Init(Mods, Stats);
            _wallView.Init();

            _enemies.EnemyKilled += OnEnemyKilled;
            _enemies.BossArmorBroken += OnBossArmorBroken;
            _wall.WallDestroyed += OnWallDestroyed;
            _economy.LevelUpQueued += MaybeShowLevelUp;

            for (int i = 0; i < _startingItems.Length; i++)
            {
                _board.TryPlaceNew(_startingItems[i]);
            }
            _shop.Roll(true);

            _ui.Init(this);
            Paused = true;
            _ui.ShowIntro(OnIntroClosed);
        }

        private void Update()
        {
            if (Paused || Phase == GamePhase.GameOver)
            {
                return;
            }
            float deltaTime = Time.deltaTime;

            _ticker.Tick(deltaTime);
            if (Phase == GamePhase.Wave)
            {
                _spawner.Tick(deltaTime);
                _enemies.Tick(deltaTime);
                _projectiles.Tick(deltaTime);
                if (Phase == GamePhase.Wave && _spawner.IsFinished && _enemies.ActiveCount == 0)
                {
                    EndWave();
                }
            }
            _effects.Tick(deltaTime);
        }

        public void StartWave()
        {
            if (Phase != GamePhase.Shopping || Paused || Wave >= _config.WaveCount)
            {
                return;
            }
            Wave++;
            _enemies.WaveContext = Wave;
            _wall.SetShield(Mods.WaveShield + _board.TotalShieldItems());
            _shop.OnWaveStarted();
            _spawner.Begin(Wave);
            Phase = GamePhase.Wave;
            WaveChanged?.Invoke();
            PhaseChanged?.Invoke();
            WaveDefinition wave = _spawner.CurrentWave;
            RequestBanner(string.IsNullOrEmpty(wave.Label) ? "Wave " + Wave : wave.Label);
            if (wave.BossAlarm)
            {
                _audio.Play(Sfx.Boss);
            }
        }

        private void EndWave()
        {
            int bonus = _config.WaveEndGold + Mods.WaveEndGold;
            if (Mods.Interest > 0f)
            {
                bonus += Mathf.Min(_config.InterestCap, Mathf.FloorToInt(_economy.Gold * Mods.Interest));
            }
            _economy.AddGold(bonus);
            if (Mods.HealPerWave > 0)
            {
                _wall.Heal(Mods.HealPerWave);
            }
            _shop.OnWaveEnded();

            if (Wave >= _config.WaveCount)
            {
                GameOver(true);
                return;
            }

            Phase = GamePhase.Shopping;
            PhaseChanged?.Invoke();
            RequestBanner("Hoàn thành Wave " + Wave + "! +" + bonus + " vàng");
        }

        public void NotifyGoldPopup(int slot, int amount)
        {
            GoldPopupRequested?.Invoke(slot, amount);
        }

        public void Restart()
        {
            DOTween.KillAll();
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        private void OnIntroClosed()
        {
            Paused = false;
            MaybeShowLevelUp();
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            _economy.GainXp(enemy.Definition.XpReward);
        }

        private void OnBossArmorBroken()
        {
            ToastRequested?.Invoke("Giáp của Boss đã vỡ!");
        }

        private void OnWallDestroyed()
        {
            GameOver(false);
        }

        private void MaybeShowLevelUp()
        {
            if (Paused || Phase == GamePhase.GameOver || _economy.PendingLevelUps <= 0)
            {
                return;
            }
            Paused = true;
            _audio.Play(Sfx.LevelUp);
            _upgrades.RollChoices(_choiceBuffer);
            _ui.ShowLevelUp(_economy.Level, _choiceBuffer, OnUpgradePicked);
        }

        private void OnUpgradePicked(UpgradeDefinition upgrade)
        {
            _upgrades.Apply(upgrade);
            _economy.ConsumePendingLevelUp();
            _audio.Play(Sfx.Buy);
            Paused = false;
            MaybeShowLevelUp();
        }

        private void GameOver(bool win)
        {
            if (Phase == GamePhase.GameOver)
            {
                return;
            }
            Phase = GamePhase.GameOver;
            Paused = true;
            SaveSystem.BestWave = Wave;
            _audio.Play(win ? Sfx.Win : Sfx.Lose);
            PhaseChanged?.Invoke();
            _ui.ShowGameOver(win);
        }

        private void RequestBanner(string text)
        {
            BannerRequested?.Invoke(text);
        }
    }
}
