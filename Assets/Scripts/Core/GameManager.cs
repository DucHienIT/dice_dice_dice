using System.Text;
using CCQ.Audio;
using CCQ.Combat;
using CCQ.Data;
using CCQ.Enemies;
using CCQ.Events;
using CCQ.Planets;
using CCQ.Progression;
using CCQ.Save;
using CCQ.UI;
using CCQ.Utils;
using UnityEngine;

namespace CCQ.Core
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
        [SerializeField] private PlanetBackgroundRenderer _background;
        [SerializeField] private FloaterManager _floaters;
        [SerializeField] private BurstManager _bursts;
        [SerializeField] private AudioManager _audio;
        [SerializeField] private UIController _ui;
        [SerializeField] private ScreenLockView _screenLock;
        [SerializeField] private Sprite _warpBannerIcon;
        [Header("Camera framing")]
        [SerializeField] private float _cameraTopWorldY = 9.6f;
        [SerializeField] private float _designHalfWidth = 5.4f;
        [SerializeField] private float _minOrthoSize = 9.6f;

        private RunState _run;
        private GameState _state;
        private BattleEngine _engine;
        private EventRoller _roller;
        private PeacefulEventResolver _resolver;
        private EnemyFactory _enemyFactory;
        private readonly UpgradeCard[] _choiceBuffer = new UpgradeCard[3];
        private readonly StringBuilder _sb = new StringBuilder(512);
        private Sidekick _pendingSidekick;
        private EnemyState _currentEnemy;
        private float _time;
        private float _travelT;

        private string PlanetName => _config.PlanetAt(_run.PlanetIndex).DisplayName;

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

            _run = SaveSystem.TryLoadRun(_config);
            bool resumed = _run != null;
            if (!resumed) _run = RunState.CreateNew(_config);

            EnterPlanet();
            _stage.HideEnemy();
            _state = GameState.Ready;
            _ui.SetEngageMode(EngageButton.Mode.Engage);
            SetEvent((resumed ? _narrative.IntroResume : _narrative.IntroNewRun)
                .Replace("{p}", PlanetName));
            _ui.ScrambleGlyphs();
            RefreshAll();
            if (!resumed) SaveSystem.SaveRun(_run);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;
            if (_screenLock.Tick()) FitCamera();
            bool paused = _ui.Overlay.IsOpen;
            float scaledDt = dt * _config.Speeds[_run.SpeedIndex];

            if (_state == GameState.Battling && !paused) _engine.Tick(scaledDt);
            else if (_state == GameState.Traveling && !paused) TickTravel(scaledDt);
            _stage.Tick(_time, paused ? 0f : scaledDt, _engine, _run.Player);
            _floaters.Tick(dt);
            _bursts.Tick(dt);
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
            _run = RunState.CreateNew(_config, speedIdx);
            _pendingSidekick = null;
            _currentEnemy = null;
            _travelT = 1f;
            _engine.Abort();
            _stage.SetWalking(false);
            _stage.HideEnemy();
            _floaters.Clear();
            _ui.Overlay.Hide();
            _ui.HideChoices();
            EnterPlanet();
            _state = GameState.Ready;
            _ui.SetEngageMode(EngageButton.Mode.Engage);
            SetEvent(_narrative.IntroNewRun.Replace("{p}", PlanetName));
            _ui.ScrambleGlyphs();
            RefreshAll();
            SaveSystem.SaveRun(_run);
        }

        private void EnterPlanet()
        {
            Planet planet = _config.PlanetAt(_run.PlanetIndex);
            _background.Build(_run.PlanetIndex, planet);
            _audio.PlayPlanetMusic(planet);
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
            _run.StarCycle++;
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
                        SetEvent(outcome.Text);
                        _ui.ShowSidekickSwap(_run.Player.Sidekicks, _pendingSidekick);
                        EnterChoosing();
                        return; // saved after the swap decision
                    }
                    _audio.PlayChime();
                    break;
                case Events.EventType.Choice:
                    _resolver.RollChoices(_choiceBuffer);
                    SetEvent(NarrativeConfig.Pick(_narrative.ChoiceTexts));
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
                _ui.ShowBanner(card.Icon, card.DisplayName, "Acquired");
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
            if (!string.IsNullOrEmpty(outcome.Text)) SetEvent(outcome.Text);
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

            _currentEnemy = _enemyFactory.Create(g, kind, _run.PlanetIndex);
            _stage.ShowEnemy(_currentEnemy);

            string[] pool = kind == EnemyKind.Boss ? _narrative.BossIntros :
                kind == EnemyKind.Elite ? _narrative.EliteIntros : _narrative.BattleIntros;
            SetEvent(NarrativeConfig.Pick(pool).Replace("{e}", _currentEnemy.Name));

            if (kind == EnemyKind.Boss)
            {
                _audio.PlayBossSting();
                _shaker.Shake(_config.ShakeBoss);
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
                Color burstColor = _config.CritterColors[
                    _currentEnemy.Look.ColorIndex % _config.CritterColors.Length];
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
            _floaters.SpawnNotice(_stage.EnemyFloaterPos + Vector3.up * 0.4f, "ENRAGED!",
                FloaterManager.HeroCritColor);
            _shaker.Shake(_config.ShakeHit * 1.5f);
        }

        private void OnBattleWon()
        {
            _sb.Clear();
            _sb.Append(NarrativeConfig.Pick(_narrative.WinTexts)
                .Replace("{e}", _currentEnemy.Name)
                .Replace("{xp}", NumberStrings.Get(_currentEnemy.Xp)));

            int ups = XpSystem.GrantXp(_config, _run.Player, _currentEnemy.Xp);
            if (ups > 0)
            {
                _sb.Append(_narrative.LevelUpSuffix.Replace("{lv}",
                    NumberStrings.Get(_run.Player.Level)));
                CelebrateLevelUp();
            }
            _run.Player.HealPct(_config.RegenPct);
            _audio.PlayWin();
            _stage.HideEnemy();
            _currentEnemy = null;

            if (_run.Round >= _config.RoundsPerPlanet)
            {
                _sb.Append('\n').Append(NarrativeConfig.Pick(_narrative.PlanetClearTexts)
                    .Replace("{p}", PlanetName));
                _run.PlanetIndex++;
                _run.Round = 0;
                _run.Player.HealPct(_config.PlanetClearHeal);
                EnterPlanet();
                _audio.PlayWarp();
                _ui.ShowBanner(_warpBannerIcon, "Warping to " + PlanetName + "!", "Planet cleared");
            }

            SetEvent(_sb.ToString());
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
            SaveSystem.ClearRun();
            SetEvent(_narrative.DeathText.Replace("{e}", _currentEnemy.Name));
            _ui.SetEngageMode(EngageButton.Mode.Dead);
            ShowDeathOverlay(newBest);
        }

        private void CelebrateLevelUp()
        {
            _floaters.SpawnNotice(_stage.HeroFloaterPos + Vector3.up * 0.35f, "LEVEL UP!",
                FloaterManager.NoticeColor);
            _audio.PlayLevelUp();
            _bursts.Burst(new Vector3(_stage.HeroX, _stage.BaseY + 0.8f, 0f),
                FloaterManager.NoticeColor, 10, 3f);
        }

        // ---------------- overlays ----------------

        private void ShowDeathOverlay(bool newBest)
        {
            BestSaveData best = SaveSystem.LoadBest();
            RunStats s = _run.Stats;
            _sb.Clear();
            _sb.Append("You fell on <b>").Append(PlanetName).Append("</b>\n");
            _sb.Append("<size=140%>Planet ").Append(_run.PlanetIndex + 1)
                .Append(" - Round ").Append(_run.Round).Append("</size>\n");
            _sb.Append("Level ").Append(_run.Player.Level)
                .Append(" - Star Cycle ").Append(_run.StarCycle).Append("\n\n");
            _sb.Append("Hits ").Append(s.Hits)
                .Append(" - Crits ").Append(s.Crits)
                .Append(" - Max hit ").Append(s.MaxHit).Append('\n');
            _sb.Append("Damage dealt ").Append(s.DamageDealt).Append('\n');
            _sb.Append("Elites ").Append(s.ElitesSlain)
                .Append(" - Bosses ").Append(s.BossesSlain);
            if (newBest)
            {
                _sb.Append("\n\n<color=#FFD35C><b>NEW BEST VOYAGE!</b></color>");
            }
            else if (best != null)
            {
                _sb.Append("\n\nBest voyage: Planet ").Append(best.planet)
                    .Append(" - Round ").Append(best.round)
                    .Append(" (Lv.").Append(best.lv).Append(')');
            }
            _ui.Overlay.Show("Voyage Ended", _sb.ToString(),
                "NEW VOYAGE", () =>
                {
                    _audio.PlayEngage();
                    NewRun(keepSpeed: true);
                });
        }

        private void ShowSettings()
        {
            BestSaveData best = SaveSystem.LoadBest();
            _sb.Clear();
            _sb.Append("Cosmic Critter Quest — a tap-to-advance\nauto-battle voyage.");
            if (best != null)
            {
                _sb.Append("\n\nBest voyage: Planet ").Append(best.planet)
                    .Append(" - Round ").Append(best.round);
            }
            _ui.Overlay.Show("Settings", _sb.ToString(),
                "Resume", () =>
                {
                    _audio.PlayClick();
                    _ui.Overlay.Hide();
                },
                _audio.MusicOn ? "Music: ON" : "Music: OFF", () =>
                {
                    _audio.SetMusicOn(!_audio.MusicOn);
                    _audio.PlayClick();
                    ShowSettings();
                },
                _audio.SfxOn ? "SFX: ON" : "SFX: OFF", () =>
                {
                    _audio.SetSfxOn(!_audio.SfxOn);
                    _audio.PlayClick();
                    ShowSettings();
                },
                "Restart voyage", () =>
                {
                    _audio.PlayClick();
                    NewRun(keepSpeed: true);
                },
                "Reset all data", () =>
                {
                    _audio.PlayClick();
                    SaveSystem.ClearRun();
                    SaveSystem.ClearBest();
                    NewRun(keepSpeed: false);
                });
        }

        // ---------------- helpers ----------------

        private void SetEvent(string template)
        {
            _ui.SetEventText(RichText.Format(template));
        }

        private void RefreshAll()
        {
            _ui.RefreshRun(_run, PlanetName);
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
            if (_narrative == null) Debug.LogError("[Core] GameManager: NarrativeConfig missing");
            if (_camera == null) Debug.LogError("[Core] GameManager: Camera missing");
            if (_shaker == null) Debug.LogError("[Core] GameManager: CameraShaker missing");
            if (_stage == null) Debug.LogError("[Core] GameManager: BattleStageView missing");
            if (_background == null) Debug.LogError("[Core] GameManager: Background missing");
            if (_floaters == null) Debug.LogError("[Core] GameManager: FloaterManager missing");
            if (_bursts == null) Debug.LogError("[Core] GameManager: BurstManager missing");
            if (_audio == null) Debug.LogError("[Core] GameManager: AudioManager missing");
            if (_ui == null) Debug.LogError("[Core] GameManager: UIController missing");
            if (_screenLock == null) Debug.LogError("[Core] GameManager: ScreenLockView missing");
        }
    }
}
