using System.Text;
using Game.Audio;
using Game.Combat;
using Game.Data;
using Game.Enemies;
using Game.Events;
using Game.Localization;
using Game.Worlds;
using Game.Progression;
using Game.Save;
using Game.UI;
using Game.Utils;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Central run state machine (Ready / Traveling / Battling / Choosing / Dead) and tick hub.
    /// Coordinates systems only — combat, events, progression and presentation each
    /// own their logic. Everything is wired via the Inspector; no runtime lookup.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig _config;
        [SerializeField] private NarrativeConfig _narrative;
        [Header("Presentation")]
        [SerializeField] private Camera _camera;
        [SerializeField] private CameraShaker _shaker;
        [SerializeField] private BattleStageView _stage;
        [SerializeField] private WorldBackgroundRenderer _background;
        [SerializeField] private FloaterManager _floaters;
        [SerializeField] private BurstManager _bursts;
        [SerializeField] private TribulationFxView _tribulationFx;
        [SerializeField] private AudioManager _audio;
        [SerializeField] private UIController _ui;
        [SerializeField] private ScreenLockView _screenLock;
        [SerializeField] private Sprite _warpBannerIcon;
        [Header("Camera framing")]
        [SerializeField] private float _cameraTopWorldY = 9.6f;
        [SerializeField] private float _designHalfWidth = 5.4f;
        [SerializeField] private float _minOrthoSize = 9.6f;

        /// <summary>Which modal is up, so a language switch can re-render it in place.</summary>
        private enum OverlayKind
        {
            None,
            Settings,
            Death,
            MetaPath,
            Menu,
            Profile,
            Records
        }

        private RunState _run;
        private GameState _state;
        private BattleEngine _engine;
        private EventRoller _roller;
        private PeacefulEventResolver _resolver;
        private EnemyFactory _enemyFactory;
        private readonly UpgradeCard[] _choiceBuffer = new UpgradeCard[3];
        private readonly StringBuilder _sb = new StringBuilder(512);
        private readonly StringBuilder _crew = new StringBuilder(96);
        private Sidekick _pendingSidekick;
        private EnemyState _currentEnemy;
        private float _time;
        private float _travelT;
        // the console line is kept unresolved (key + tokens) so a language switch can
        // re-render the very same sentence instead of rolling a new one
        private readonly LocLine[] _eventLines = new LocLine[3];
        private OverlayKind _openOverlay;
        private bool _deathWasNewBest;
        // meta progression: shards + Star MetaPath ranks, reloaded once and kept across runs
        private MetaState _meta;
        private int _deathShards;
        // which modal the metaPath was opened from, so closing it puts that screen back
        private OverlayKind _metaPathReturn;
        private bool _menuCanContinue;

        private string WorldName => _config.WorldAt(_run.WorldIndex).DisplayName;

        private void Awake()
        {
            ValidateReferences();
            Application.targetFrameRate = _config.TargetFrameRate;
            // aspect lock first: it decides camera.aspect, which FitCamera reads
            _screenLock.Apply();
            FitCamera();

            _engine = new BattleEngine(_config);
            _engine.HitApplied += OnHitApplied;
            _engine.HeroHealed += OnHeroHealed;
            _engine.ThornsReflected += OnThornsReflected;
            _engine.EnrageStarted += OnEnrageStarted;
            _engine.Won += OnBattleWon;
            _engine.Lost += OnBattleLost;

            _roller = new EventRoller(_config);
            _resolver = new PeacefulEventResolver(_config, _narrative);
            _enemyFactory = new EnemyFactory(_config, _narrative);
        }

        private void Start()
        {
            _ui.Init();
            _ui.EngagePressed += OnEngagePressed;
            _ui.SpeedPressed += OnSpeedPressed;
            _ui.GearPressed += OnGearPressed;
            _ui.ChoicePicked += OnChoicePicked;
            _ui.MetaPathNodePicked += BuyMetaPathRank;
            _ui.MetaPathClosed += CloseMetaPath;
            _ui.MetaPathResetRequested += ResetAllData;
            _ui.HomePressed += () => { _audio.PlayClick(); ShowMenu(canContinue: true); };
            _ui.MenuStartPressed += OnMenuStart;
            _ui.MenuNewRunPressed += StartRunFromMenu;
            _ui.MenuProfilePressed += () => { _audio.PlayClick(); ShowProfile(); };
            _ui.NavPicked += OnNavPicked;
            _ui.OverlayClosed += () => { _audio.PlayClick(); HideOverlay(); };
            Loc.Changed += OnLanguageChanged;

            // the metaPath must exist before any run is created — CreateNew bakes its ranks in
            _meta = SaveSystem.LoadMeta(_config);
            _run = SaveSystem.TryLoadRun(_config);
            bool resumed = _run != null;
            if (!resumed) _run = RunState.CreateNew(_config, _meta);

            EnterWorld();
            _stage.HideEnemy();
            _state = GameState.Ready;
            _ui.SetEngageMode(EngageButton.Mode.Engage);
            SetEvent(LocLine.Of(resumed ? _narrative.IntroResumeKey : _narrative.IntroNewRunKey)
                .With("{p}", WorldName));
            _ui.ScrambleGlyphs();
            RefreshAll();
            if (!resumed) SaveSystem.SaveRun(_run);
            // the game opens on the front screen; the run behind it is already set up and
            // simply unpauses when the player picks continue or a new run
            ShowMenu(resumed);
        }

        private void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
        }

        /// <summary>
        /// Re-renders everything that was written in the old language: static captions,
        /// run values, the current console line and whichever modal is open.
        /// </summary>
        private void OnLanguageChanged()
        {
            _ui.RefreshStaticText();
            RefreshAll();
            RenderEvent();
            // settings, the profile and the records panel all open on top of the front
            // screen, so it needs re-rendering even when it is not the modal on top
            if (_ui.Menu.IsOpen) RefreshMenu();
            if (_openOverlay == OverlayKind.Settings) ShowSettings();
            else if (_openOverlay == OverlayKind.Death) ShowDeathOverlay(_deathWasNewBest);
            else if (_openOverlay == OverlayKind.MetaPath) _ui.RefreshMetaPath(_meta);
            else if (_openOverlay == OverlayKind.Profile) ShowProfile();
            else if (_openOverlay == OverlayKind.Records) ShowRecords();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;
            if (_screenLock.Tick()) FitCamera();
            bool paused = _ui.IsModalOpen;
            float scaledDt = dt * _config.Speeds[_run.SpeedIndex];

            if (_state == GameState.Battling && !paused) _engine.Tick(scaledDt);
            else if (_state == GameState.Traveling && !paused) TickTravel(scaledDt);
            _stage.Tick(_time, paused ? 0f : scaledDt, _engine, _run.Player);
            _floaters.Tick(dt);
            _bursts.Tick(dt);
            _tribulationFx.Tick(dt);
            _shaker.Tick(dt);
            _background.Tick(_time);
            _ui.Tick(_time, dt);
        }

        // ---------------- input ----------------

        private void OnEngagePressed()
        {
            if (_state == GameState.Ready)
            {
                _audio.PlayEngage();
                BeginTravel();
            }
            else if (_state == GameState.Traveling)
            {
                // tapping again cuts the walk short instead of queueing up
                _audio.PlayClick();
                ArriveFromTravel();
            }
            else if (_state == GameState.Dead)
            {
                _audio.PlayEngage();
                NewRun(keepSpeed: true);
            }
        }

        private void OnSpeedPressed()
        {
            _audio.PlayClick();
            _run.SpeedIndex = (_run.SpeedIndex + 1) % _config.Speeds.Length;
            _ui.SetSpeed(_config.Speeds[_run.SpeedIndex]);
            // never persist mid-fight — the speed change rides along with the next event save
            if (_state != GameState.Battling) SaveSystem.SaveRun(_run);
        }

        private void OnGearPressed()
        {
            _audio.PlayClick();
            ShowSettings();
        }

        // ---------------- run lifecycle ----------------

        private void NewRun(bool keepSpeed)
        {
            int speedIdx = keepSpeed ? _run.SpeedIndex : 0;
            _run = RunState.CreateNew(_config, _meta, speedIdx);
            _pendingSidekick = null;
            _currentEnemy = null;
            _travelT = 1f;
            _engine.Abort();
            _stage.SetWalking(false);
            _stage.HideEnemy();
            _floaters.Clear();
            // menu and metaPath first: HideOverlay reads whether the menu is still up
            _stage.SetBarsVisible(true);
            _ui.HideMenu();
            _ui.HideMetaPath();
            HideOverlay();
            _ui.HideChoices();
            EnterWorld();
            _state = GameState.Ready;
            _ui.SetEngageMode(EngageButton.Mode.Engage);
            SetEvent(LocLine.Of(_narrative.IntroNewRunKey).With("{p}", WorldName));
            _ui.ScrambleGlyphs();
            RefreshAll();
            SaveSystem.SaveRun(_run);
        }

        private void EnterWorld()
        {
            World world = _config.WorldAt(_run.WorldIndex);
            _background.Build(_run.WorldIndex, world);
            _audio.PlayWorldMusic(world);
        }

        // ---------------- travel ----------------

        /// <summary>One walk leg: the hero hops in place while the ground scrolls past him.</summary>
        private void BeginTravel()
        {
            _travelT = 0f;
            _state = GameState.Traveling;
            _stage.SetWalking(true);
            _ui.SetEngageMode(EngageButton.Mode.Traveling);
        }

        private void TickTravel(float scaledDt)
        {
            float step = Mathf.Min(scaledDt / Mathf.Max(0.01f, _config.TravelDuration),
                1f - _travelT);
            _travelT += step;
            _background.Scroll(step * _config.TravelDistance);
            if (_travelT >= 1f) ArriveFromTravel();
        }

        private void ArriveFromTravel()
        {
            _travelT = 1f;
            _stage.SetWalking(false);
            _state = GameState.Ready;
            NextEvent();
        }

        // ---------------- star cycle events ----------------

        private void NextEvent()
        {
            _run.Cycle++;
            _ui.ScrambleGlyphs();
            Events.EventType kind = _roller.Roll(_run.NonBattleStreak);
            if (kind == Events.EventType.Battle)
            {
                _run.NonBattleStreak = 0;
                StartBattle();
            }
            else
            {
                _run.NonBattleStreak++;
                ResolvePeaceful(kind);
            }
        }

        private void ResolvePeaceful(Events.EventType kind)
        {
            EventOutcome outcome = null;
            switch (kind)
            {
                case Events.EventType.Fortune:
                    outcome = _resolver.ResolveFortune(_run);
                    _audio.PlayChime();
                    break;
                case Events.EventType.Spring:
                    outcome = _resolver.ResolveSpring(_run);
                    _audio.PlayHeal();
                    break;
                case Events.EventType.Trap:
                    outcome = _resolver.ResolveTrap(_run);
                    _audio.PlayTrap();
                    _shaker.Shake(_config.ShakeHit);
                    break;
                case Events.EventType.Treasure:
                    outcome = _resolver.ResolveTreasure(_run);
                    _audio.PlayChime();
                    if (outcome.LevelUps > 0) CelebrateLevelUp();
                    break;
                case Events.EventType.Sidekick:
                    outcome = _resolver.ResolveSidekick(_run);
                    if (outcome.PendingSidekick != null)
                    {
                        _pendingSidekick = outcome.PendingSidekick;
                        SetEvent(outcome.Line, outcome.Suffix);
                        _ui.ShowSidekickSwap(_run.Player.Sidekicks, _pendingSidekick);
                        EnterChoosing();
                        return; // saved after the swap decision
                    }
                    _audio.PlayChime();
                    break;
                case Events.EventType.Choice:
                    _resolver.RollChoices(_choiceBuffer);
                    SetEvent(LocLine.Of(NarrativeConfig.PickKey(_narrative.ChoiceKeys)));
                    _ui.ShowUpgradeChoices(_choiceBuffer);
                    EnterChoosing();
                    return; // saved after the pick
            }

            ApplyOutcome(outcome);
            _state = GameState.Ready;
            _ui.SetEngageMode(EngageButton.Mode.Engage);
            RefreshAll();
            SaveSystem.SaveRun(_run);
        }

        private void EnterChoosing()
        {
            _state = GameState.Choosing;
            _ui.SetEngageMode(EngageButton.Mode.Choosing);
            RefreshAll();
        }

        private void OnChoicePicked(int index)
        {
            if (_state != GameState.Choosing) return;
            _audio.PlayClick();
            _ui.HideChoices();

            if (_pendingSidekick != null)
            {
                Sidekick incoming = _pendingSidekick;
                _pendingSidekick = null;
                EventOutcome outcome = index < _config.MaxSidekicks
                    ? _resolver.ResolveSwap(_run, incoming, index)
                    : _resolver.ResolveSnack(_run);
                _audio.PlayChime();
                ApplyOutcome(outcome);
            }
            else
            {
                UpgradeCard card = _choiceBuffer[index];
                StatModApplier.Apply(_run.Player, card.Mods);
                _ui.ShowBanner(card.Icon, card.DisplayName, Loc.Get(LocKeys.BannerAcquired));
                _audio.PlayChime();
            }

            _state = GameState.Ready;
            _ui.SetEngageMode(EngageButton.Mode.Engage);
            RefreshAll();
            SaveSystem.SaveRun(_run);
        }

        private void ApplyOutcome(EventOutcome outcome)
        {
            if (outcome == null) return;
            if (!outcome.Line.IsEmpty) SetEvent(outcome.Line, outcome.Suffix);
            if (outcome.BannerIcon != null || !string.IsNullOrEmpty(outcome.BannerTitle))
            {
                _ui.ShowBanner(outcome.BannerIcon, outcome.BannerTitle, outcome.BannerTag);
            }
            if (outcome.HeroHpDelta > 0)
            {
                _floaters.SpawnAmount(_stage.HeroFloaterPos, outcome.HeroHpDelta, false,
                    FloaterManager.HealColor, true);
            }
            else if (outcome.HeroHpDelta < 0)
            {
                _floaters.SpawnAmount(_stage.HeroFloaterPos, -outcome.HeroHpDelta, false,
                    FloaterManager.HeroHurtColor, false);
            }
        }

        // ---------------- battle ----------------

        private void StartBattle()
        {
            _run.Round++;
            int g = _run.GlobalRound(_config, _run.Round);
            EnemyKind kind = EnemyKind.Normal;
            if (_run.Round % _config.BossEvery == 0) kind = EnemyKind.Boss;
            else if (Random.value < _config.EliteChance) kind = EnemyKind.Elite;

            _currentEnemy = _enemyFactory.Create(g, kind, _run.WorldIndex);
            _stage.ShowEnemy(_currentEnemy);

            string[] pool = kind == EnemyKind.Boss ? _narrative.BossIntroKeys :
                kind == EnemyKind.Elite ? _narrative.EliteIntroKeys : _narrative.BattleIntroKeys;
            SetEvent(LocLine.Of(NarrativeConfig.PickKey(pool)).With("{e}", _currentEnemy.Name));

            if (kind == EnemyKind.Boss)
            {
                _audio.PlayBossSting();
                _shaker.Shake(_config.ShakeBoss);
                // the tribulation announces a realm lord
                _tribulationFx.PlayStrike(
                    new Vector3(_stage.EnemyX, _stage.BaseY + 0.6f, 0f), 1f);
            }

            _engine.StartBattle(_run.Player, _currentEnemy, _run.Stats);
            _state = GameState.Battling;
            _ui.SetEngageMode(EngageButton.Mode.Battling);
            RefreshAll();
        }

        private void OnHitApplied(HitInfo hit)
        {
            if (hit.Target == BattleActor.Enemy)
            {
                _floaters.SpawnAmount(_stage.EnemyFloaterPos, hit.Damage, hit.Crit,
                    hit.Crit ? FloaterManager.CritColor : FloaterManager.DamageColor, false);
                _audio.PlayHeroHit(hit.Crit);
                Color burstColor = _config.EnemyColors[
                    _currentEnemy.Look.ColorIndex % _config.EnemyColors.Length];
                Vector3 impact = new Vector3(_stage.EnemyX, _stage.BaseY + 0.7f, 0f);
                _bursts.Burst(impact, burstColor, hit.Crit ? 8 : 4, hit.Crit ? 3.4f : 2.2f);
                if (hit.Crit) _shaker.Shake(_config.ShakeHit);
                _ui.SetHits(_run.Stats.Hits);
                if (hit.TargetDied)
                {
                    _stage.BeginDissolve();
                    _bursts.Burst(impact, burstColor, 14, 4f);
                    if (_currentEnemy.Kind == EnemyKind.Elite) _run.Stats.ElitesSlain++;
                    else if (_currentEnemy.Kind == EnemyKind.Boss) _run.Stats.BossesSlain++;
                }
            }
            else
            {
                _floaters.SpawnAmount(_stage.HeroFloaterPos, hit.Damage, hit.Crit,
                    hit.Crit ? FloaterManager.HeroCritColor : FloaterManager.HeroHurtColor, false);
                _audio.PlayHeroHurt(hit.Crit);
                _shaker.Shake(hit.Crit ? _config.ShakeHit * 1.8f : _config.ShakeHit);
                _ui.SetStats(_run.Player);
            }
        }

        private void OnHeroHealed(int amount)
        {
            _floaters.SpawnAmount(_stage.HeroFloaterPos, amount, false,
                FloaterManager.HealColor, true);
            _ui.SetStats(_run.Player);
        }

        private void OnThornsReflected(int damage)
        {
            _floaters.SpawnAmount(_stage.EnemyFloaterPos, damage, false,
                FloaterManager.ThornsColor, false);
        }

        private void OnEnrageStarted()
        {
            _floaters.SpawnNotice(_stage.EnemyFloaterPos + Vector3.up * 0.4f,
                Loc.Get(LocKeys.FloaterEnraged), FloaterManager.HeroCritColor);
            _shaker.Shake(_config.ShakeHit * 1.5f);
        }

        private void OnBattleWon()
        {
            LocLine win = LocLine.Of(NarrativeConfig.PickKey(_narrative.WinKeys))
                .With("{e}", _currentEnemy.Name)
                .With("{xp}", NumberStrings.Get(_currentEnemy.Xp));
            LocLine levelUp = default;
            LocLine cleared = default;

            int ups = XpSystem.GrantXp(_config, _run.Player, _currentEnemy.Xp);
            if (ups > 0)
            {
                levelUp = LocLine.Of(_narrative.LevelUpSuffixKey)
                    .With("{lv}", NumberStrings.Get(_run.Player.Level));
                CelebrateLevelUp();
            }
            _run.Player.HealPct(_config.RegenPct);
            _audio.PlayWin();
            _stage.HideEnemy();
            _currentEnemy = null;

            if (_run.Round >= _config.RoundsPerWorld)
            {
                // clearing a world pays star shards on the spot, so progress banked here
                // survives the death that ends the run
                int shards = MetaState.ShardsForWorldClear(_config, _run.WorldIndex + 1);
                _meta.Grant(shards);
                SaveSystem.SaveMeta(_config, _meta);
                // the farewell line names the world just cleared, the banner the next one
                cleared = LocLine.Of(NarrativeConfig.PickKey(_narrative.WorldClearKeys))
                    .With("{p}", WorldName)
                    .With("{n}", NumberStrings.Get(shards))
                    .NextLine();
                _run.WorldIndex++;
                _run.Round = 0;
                _run.Player.HealPct(_config.WorldClearHeal);
                EnterWorld();
                _audio.PlayWarp();
                _ui.ShowBanner(_warpBannerIcon,
                    Loc.Get(LocKeys.BannerWarpingTo).Replace("{p}", WorldName),
                    Loc.Get(LocKeys.BannerWorldCleared));
            }

            SetEvent(win, levelUp, cleared);
            _state = GameState.Ready;
            _ui.SetEngageMode(EngageButton.Mode.Engage);
            RefreshAll();
            SaveSystem.SaveRun(_run);
        }

        private void OnBattleLost()
        {
            _state = GameState.Dead;
            _audio.PlayDeath();
            bool newBest = SaveSystem.SaveBestIfHigher(_config, _run);
            // cash the run out once, here — ShowDeathOverlay only displays the amount
            // and is re-run on a language switch or on returning from the metaPath
            _deathShards = MetaState.ShardsForRun(_config, _run);
            _meta.Grant(_deathShards);
            SaveSystem.SaveMeta(_config, _meta);
            _ui.SetMetaPathBadge(_meta.HasAffordableStep(_config));
            SaveSystem.ClearRun();
            SetEvent(LocLine.Of(_narrative.DeathKey).With("{e}", _currentEnemy.Name));
            _ui.SetEngageMode(EngageButton.Mode.Dead);
            ShowDeathOverlay(newBest);
        }

        private void CelebrateLevelUp()
        {
            _floaters.SpawnNotice(_stage.HeroFloaterPos + Vector3.up * 0.35f,
                Loc.Get(LocKeys.FloaterLevelUp), FloaterManager.NoticeColor);
            _audio.PlayLevelUp();
            _bursts.Burst(new Vector3(_stage.HeroX, _stage.BaseY + 0.8f, 0f),
                FloaterManager.NoticeColor, 10, 3f);
            // a softer bolt marks the breakthrough
            _tribulationFx.PlayStrike(
                new Vector3(_stage.HeroX, _stage.BaseY + 0.7f, 0f), 0.7f);
        }

        // ---------------- overlays ----------------

        private void ShowDeathOverlay(bool newBest)
        {
            BestSaveData best = SaveSystem.LoadBest();
            RunStats s = _run.Stats;
            _openOverlay = OverlayKind.Death;
            _deathWasNewBest = newBest;
            _sb.Clear();
            _sb.Append(Loc.Format(LocKeys.DeathFellOn, WorldName)).Append('\n');
            _sb.Append(Loc.Format(LocKeys.DeathProgress, _run.WorldIndex + 1, _run.Round))
                .Append('\n');
            _sb.Append(Loc.Format(LocKeys.DeathLevelCycle, _run.Player.Level, _run.Cycle))
                .Append("\n\n");
            _sb.Append(Loc.Format(LocKeys.DeathHitLine, s.Hits, s.Crits, s.MaxHit)).Append('\n');
            _sb.Append(Loc.Format(LocKeys.DeathDamageLine, s.DamageDealt)).Append('\n');
            _sb.Append(Loc.Format(LocKeys.DeathSlainLine, s.ElitesSlain, s.BossesSlain))
                .Append('\n');
            _sb.Append(Loc.Format(LocKeys.DeathShards, _deathShards, _meta.Shards));
            if (newBest)
            {
                _sb.Append("\n\n").Append(Loc.Get(LocKeys.DeathNewBest));
            }
            else if (best != null)
            {
                _sb.Append("\n\n")
                    .Append(Loc.Format(LocKeys.DeathBestRun, best.world, best.round, best.lv));
            }
            _ui.Overlay.Show(Loc.Get(LocKeys.DeathTitle), _sb.ToString(),
                Loc.Get(LocKeys.EngageNewRun), () =>
                {
                    _audio.PlayEngage();
                    NewRun(keepSpeed: true);
                },
                Loc.Get(LocKeys.MetaPathEntry), () =>
                {
                    _audio.PlayClick();
                    ShowMetaPath(OverlayKind.Death);
                },
                Loc.Get(LocKeys.MenuEntry), () =>
                {
                    _audio.PlayClick();
                    ShowMenu(canContinue: false);
                });
        }

        // ---------------- bottom nav ----------------

        private void OnNavPicked(int index)
        {
            _audio.PlayClick();
            switch (index)
            {
                // the tab bar only exists on the front screen, so that is always the way back
                case 0: ShowMetaPath(OverlayKind.Menu); break;
                case 1: ShowProfile(); break;
                case 2: ShowRecords(); break;
                case 3: ShowSettings(); break;
            }
        }

        /// <summary>The hero sheet — everything the HUD deliberately keeps off screen.</summary>
        private void ShowProfile()
        {
            PlayerState p = _run.Player;
            _openOverlay = OverlayKind.Profile;
            _sb.Clear();
            _sb.Append(Loc.Format(LocKeys.ProfileLevel, p.Level, p.Xp, _config.XpNeed(p.Level)))
                .Append('\n');
            _sb.Append(Loc.Format(LocKeys.ProfileVitals, p.Hp, p.MaxHp, p.Atk, p.Def))
                .Append('\n');
            _sb.Append(Loc.Format(LocKeys.ProfileEdge, Percent(p.Crit), Percent(p.Lifesteal),
                Percent(p.Thorns))).Append("\n\n");
            _sb.Append(Loc.Format(LocKeys.ProfileCrew, CrewLine(p))).Append('\n');
            _sb.Append(Loc.Format(LocKeys.ProfileWhere, WorldName, _run.Round,
                _config.RoundsPerWorld, _run.Cycle)).Append('\n');
            _sb.Append(Loc.Format(LocKeys.OverlayShards, _meta.Shards));
            _ui.Overlay.Show(Loc.Get(LocKeys.ProfileTitle), _sb.ToString());
        }

        private string CrewLine(PlayerState p)
        {
            if (p.Sidekicks.Count == 0) return Loc.Get(LocKeys.ProfileCrewNone);
            _crew.Clear();
            for (int i = 0; i < p.Sidekicks.Count; i++)
            {
                if (i > 0) _crew.Append(", ");
                _crew.Append(p.Sidekicks[i].DisplayName);
            }
            return _crew.ToString();
        }

        private static int Percent(float value) => Mathf.RoundToInt(value * 100f);

        /// <summary>Lifetime numbers, kept out of the run HUD on purpose.</summary>
        private void ShowRecords()
        {
            BestSaveData best = SaveSystem.LoadBest();
            RunStats s = _run.Stats;
            _openOverlay = OverlayKind.Records;
            _sb.Clear();
            _sb.Append(best != null
                    ? Loc.Format(LocKeys.DeathBestRun, best.world, best.round, best.lv)
                    : Loc.Get(LocKeys.RecordsNoBest))
                .Append('\n');
            _sb.Append(Loc.Format(LocKeys.RecordsLifetime, _meta.LifetimeShards)).Append("\n\n");
            _sb.Append(Loc.Get(LocKeys.RecordsThisRun)).Append('\n');
            _sb.Append(Loc.Format(LocKeys.DeathHitLine, s.Hits, s.Crits, s.MaxHit)).Append('\n');
            _sb.Append(Loc.Format(LocKeys.DeathDamageLine, s.DamageDealt)).Append('\n');
            _sb.Append(Loc.Format(LocKeys.DeathSlainLine, s.ElitesSlain, s.BossesSlain));
            _ui.Overlay.Show(Loc.Get(LocKeys.RecordsTitle), _sb.ToString());
        }

        // ---------------- main menu ----------------

        private void ShowMenu(bool canContinue)
        {
            _menuCanContinue = canContinue;
            _openOverlay = OverlayKind.Menu;
            _ui.Overlay.Hide();
            _ui.HideMetaPath();
            // a paused battle keeps its enemy — it is still that run's stage. A finished
            // one does not: the menu would show the monster that just killed the player.
            if (_state == GameState.Dead) _stage.HideEnemy();
            _stage.SetBarsVisible(false);
            RefreshMenu();
        }

        /// <summary>
        /// Re-sends the front screen's values without disturbing whatever is open on top of
        /// it — the menu's own text comes from here, not from RefreshStaticText.
        /// </summary>
        private void RefreshMenu()
        {
            var status = new MenuStatus
            {
                Level = _run.Player.Level,
                Shards = _meta.Shards,
                WorldName = WorldName,
                Round = _run.Round,
                RoundsPerWorld = _config.RoundsPerWorld
            };
            _ui.ShowMenu(_menuCanContinue, status, SaveSystem.LoadBest());
            // the tab bar only exists here, so this is the only place its badge can matter
            _ui.SetMetaPathBadge(_meta.HasAffordableStep(_config));
        }

        /// <summary>The one big CTA: resume the waiting run, or open a fresh one.</summary>
        private void OnMenuStart()
        {
            if (_menuCanContinue) CloseMenu();
            else StartRunFromMenu();
        }

        private void CloseMenu()
        {
            _audio.PlayEngage();
            _openOverlay = OverlayKind.None;
            _stage.SetBarsVisible(true);
            _ui.HideMenu();
        }

        private void StartRunFromMenu()
        {
            _audio.PlayEngage();
            _openOverlay = OverlayKind.None;
            _stage.SetBarsVisible(true);
            _ui.HideMenu();
            NewRun(keepSpeed: true);
        }

        private void ShowSettings()
        {
            BestSaveData best = SaveSystem.LoadBest();
            _openOverlay = OverlayKind.Settings;
            _sb.Clear();
            _sb.Append(Loc.Get(LocKeys.OverlayAbout));
            if (best != null)
            {
                _sb.Append("\n\n").Append(Loc.Format(LocKeys.OverlayBestShort, best.world, best.round));
            }
            _sb.Append('\n').Append(Loc.Format(LocKeys.OverlayShards, _meta.Shards));
            // no "resume" row: the ✕ closes the panel, per the mobile-panel convention
            _ui.Overlay.Show(Loc.Get(LocKeys.OverlaySettings), _sb.ToString(),
                Loc.Get(_audio.MusicOn ? LocKeys.OverlayMusicOn : LocKeys.OverlayMusicOff), () =>
                {
                    _audio.SetMusicOn(!_audio.MusicOn);
                    _audio.PlayClick();
                    ShowSettings();
                },
                Loc.Get(_audio.SfxOn ? LocKeys.OverlaySfxOn : LocKeys.OverlaySfxOff), () =>
                {
                    _audio.SetSfxOn(!_audio.SfxOn);
                    _audio.PlayClick();
                    ShowSettings();
                },
                Loc.Format(LocKeys.OverlayLanguage, Loc.LanguageLabel(Loc.CurrentCode)), () =>
                {
                    _audio.PlayClick();
                    // I2 batches the switch by a frame; OnLanguageChanged re-shows this panel
                    Loc.CycleLanguage();
                },
                // the front screen owns the Star MetaPath, so settings only offers the way back
                // to it — and not at all when it is already the screen underneath
                _ui.Menu.IsOpen ? null : Loc.Get(LocKeys.MenuEntry), () =>
                {
                    _audio.PlayClick();
                    ShowMenu(canContinue: true);
                },
                Loc.Get(LocKeys.OverlayRestart), () =>
                {
                    _audio.PlayClick();
                    NewRun(keepSpeed: true);
                });
        }

        /// <summary>
        /// Opens the Star MetaPath skill tree. It replaces whichever modal asked for it and
        /// puts that one back on close, so the pause never lifts in between.
        /// </summary>
        private void ShowMetaPath(OverlayKind returnTo)
        {
            _metaPathReturn = returnTo;
            _openOverlay = OverlayKind.MetaPath;
            _ui.Overlay.Hide();
            _ui.Menu.Hide(); // the play HUD stays parked; the metaPath covers the screen anyway
            _ui.ShowMetaPath(_meta);
        }

        private void CloseMetaPath()
        {
            _audio.PlayClick();
            _ui.HideMetaPath();
            if (_metaPathReturn == OverlayKind.Death) ShowDeathOverlay(_deathWasNewBest);
            else if (_metaPathReturn == OverlayKind.Menu) ShowMenu(canContinue: true);
            else HideOverlay();
        }

        private void BuyMetaPathRank(int index)
        {
            if (index >= _config.MetaUpgrades.Length) return;
            if (_meta.Buy(_config, index))
            {
                SaveSystem.SaveMeta(_config, _meta);
                _audio.PlayLevelUp();
            }
            else
            {
                _audio.PlayTrap(); // locked, maxed out, or not enough shards yet
            }
            _ui.RefreshMetaPath(_meta);
            _ui.SetMetaPathBadge(_meta.HasAffordableStep(_config));
        }

        private void ResetAllData()
        {
            _audio.PlayClick();
            SaveSystem.ClearRun();
            SaveSystem.ClearBest();
            SaveSystem.ClearMeta();
            _meta = SaveSystem.LoadMeta(_config);
            _ui.HideMetaPath();
            NewRun(keepSpeed: false);
            ShowMenu(canContinue: false); // wiped: back to the front screen, not mid-run
        }

        /// <summary>
        /// Closes the OverlayView. Settings can be opened from the front screen, so what is
        /// left behind may still be the menu rather than the run.
        /// </summary>
        private void HideOverlay()
        {
            _ui.Overlay.Hide();
            _openOverlay = _ui.Menu.IsOpen ? OverlayKind.Menu : OverlayKind.None;
        }

        // ---------------- helpers ----------------

        private void SetEvent(LocLine line) => SetEvent(line, default, default);

        private void SetEvent(LocLine line, LocLine suffix) => SetEvent(line, suffix, default);

        /// <summary>
        /// Stores the console line unresolved (so a language switch can re-render it)
        /// and pushes the formatted text to the view.
        /// </summary>
        private void SetEvent(LocLine line, LocLine suffix, LocLine tail)
        {
            _eventLines[0] = line;
            _eventLines[1] = suffix;
            _eventLines[2] = tail;
            RenderEvent();
        }

        private void RenderEvent()
        {
            _sb.Clear();
            for (int i = 0; i < _eventLines.Length; i++)
            {
                LocLine line = _eventLines[i];
                if (line.IsEmpty) continue;
                if (line.OnNewLine && _sb.Length > 0) _sb.Append('\n');
                _sb.Append(line.Resolve());
            }
            _ui.SetEventText(RichText.Format(_sb.ToString()));
        }

        private void RefreshAll()
        {
            _ui.RefreshRun(_run, WorldName);
            _ui.SetMetaPathBadge(_meta.HasAffordableStep(_config));
            _stage.UpdateSidekicks(_run.Player);
        }

        private void FitCamera()
        {
            // lock the visible world width; the top edge stays at _cameraTopWorldY on any aspect
            float size = Mathf.Max(_minOrthoSize, _designHalfWidth / _camera.aspect);
            _camera.orthographicSize = size;
            Transform t = _camera.transform;
            Vector3 pos = t.position;
            pos.x = 0f;
            pos.y = _cameraTopWorldY - size;
            t.position = pos;
        }

        private void ValidateReferences()
        {
            if (_config == null) Debug.LogError("[Core] GameManager: GameConfig missing");
            else if (_ui != null && _config.MetaUpgrades != null &&
                     _config.MetaUpgrades.Length != _ui.MetaPath.NodeCount)
            {
                // the tree's nodes are pre-placed by the builder — a mismatch would silently
                // hide a track the player paid for
                Debug.LogError("[Core] GameManager: GameConfig has " +
                               _config.MetaUpgrades.Length + " meta upgrades but the " +
                               "cultivation path has " + _ui.MetaPath.NodeCount + " nodes");
            }
            if (_narrative == null) Debug.LogError("[Core] GameManager: NarrativeConfig missing");
            if (_camera == null) Debug.LogError("[Core] GameManager: Camera missing");
            if (_shaker == null) Debug.LogError("[Core] GameManager: CameraShaker missing");
            if (_stage == null) Debug.LogError("[Core] GameManager: BattleStageView missing");
            if (_background == null) Debug.LogError("[Core] GameManager: Background missing");
            if (_floaters == null) Debug.LogError("[Core] GameManager: FloaterManager missing");
            if (_bursts == null) Debug.LogError("[Core] GameManager: BurstManager missing");
            if (_tribulationFx == null) Debug.LogError("[Core] GameManager: TribulationFxView missing");
            if (_audio == null) Debug.LogError("[Core] GameManager: AudioManager missing");
            if (_ui == null) Debug.LogError("[Core] GameManager: UIController missing");
            if (_screenLock == null) Debug.LogError("[Core] GameManager: ScreenLockView missing");
        }
    }
}
