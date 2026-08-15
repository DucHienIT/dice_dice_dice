using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Data
{
    [Serializable]
    public class EventWeights
    {
        public float Battle = 46f;
        public float Fortune = 13f;
        public float Choice = 12f;
        public float Spring = 8f;
        public float Sidekick = 8f;
        public float Trap = 6f;
        public float Treasure = 7f;
    }

    /// <summary>
    /// Central balance config — every tunable from the spec (EVENT_WEIGHTS, PLAYER, ENEMY,
    /// ENRAGE...) lives here. Tune in the Inspector, never in code.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Run structure")]
        [SerializeField, FormerlySerializedAs("_roundsPerPlanet")] private int _roundsPerWorld = 30;
        [SerializeField] private int _bossEvery = 10;
        [SerializeField, Range(0f, 1f)] private float _eliteChance = 0.12f;
        [SerializeField] private int[] _speeds = { 1, 2, 4 };
        [SerializeField] private float _beatMs = 550f;

        [Header("Player base (PLAYER)")]
        [SerializeField] private int _playerHp = 160;
        [SerializeField] private float _playerAtk = 15f;
        [SerializeField] private int _playerDef = 3;
        [SerializeField, Range(0f, 1f)] private float _playerCrit = 0.08f;
        [SerializeField] private float _playerCritMult = 1.6f;
        [SerializeField, Range(0f, 1f)] private float _regenPct = 0.12f;

        [Header("Level curve (LEVEL): xpNeed = base × lv^pow")]
        [SerializeField] private float _xpNeedBase = 16f;
        [SerializeField] private float _xpNeedPow = 1.55f;
        [SerializeField] private float _levelHpPct = 0.14f;
        [SerializeField] private float _levelAtkPct = 0.11f;
        [SerializeField] private int _levelDefFlat = 1;
        [SerializeField] private float _levelHealPct = 0.35f;

        [Header("Enemy scaling (ENEMY), g = world×30 + round")]
        [SerializeField] private float _enemyHpBase = 26f;
        [SerializeField] private float _enemyHpLinear = 9f;
        [SerializeField] private float _enemyHpQuad = 0.15f;
        [SerializeField] private float _enemyHpExp = 1.015f;
        [SerializeField] private float _enemyAtkBase = 6f;
        [SerializeField] private float _enemyAtkLinear = 0.9f;
        [SerializeField] private float _enemyAtkExp = 1.013f;
        [SerializeField] private float _enemyDefBase = 1f;
        [SerializeField] private float _enemyDefLinear = 0.35f;
        [SerializeField] private float _enemyXpBase = 8f;
        [SerializeField] private float _enemyXpLinear = 3f;
        [SerializeField] private Vector3 _bossMult = new Vector3(1.8f, 1.15f, 3f);   // hp, atk, xp
        [SerializeField] private Vector3 _eliteMult = new Vector3(1.7f, 1.25f, 1.8f); // hp, atk, xp

        [Header("Combat")]
        [SerializeField, Range(0f, 1f)] private float _dmgVariance = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _enemyCritChance = 0.05f;
        [SerializeField] private float _enemyCritMult = 1.5f;
        [SerializeField] private int _enrageAfterBeats = 40;
        [SerializeField] private float _enrageRamp = 0.05f;

        [Header("Events")]
        [SerializeField] private EventWeights _eventWeights = new EventWeights();
        [SerializeField] private int _maxNonBattleStreak = 2;
        [SerializeField] private Vector2 _springHeal = new Vector2(0.30f, 0.50f);
        [SerializeField] private Vector2 _trapDmg = new Vector2(0.08f, 0.15f);
        [SerializeField] private Vector2 _treasureXp = new Vector2(0.5f, 1.2f);
        [SerializeField] private int _maxSidekicks = 3;
        [SerializeField, Range(0f, 1f)] private float _snackHeal = 0.15f;
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("_planetClearHeal")] private float _worldClearHeal = 0.5f;

        [Header("Meta progression (STAR FORGE) — survives death")]
        [SerializeField] private MetaUpgrade[] _metaUpgrades;
        [Tooltip("Shards per global round reached when a run ends.")]
        [SerializeField] private float _shardsPerRound = 0.5f;
        [SerializeField] private int _shardsPerElite = 2;
        [SerializeField] private int _shardsPerBoss = 6;
        [Tooltip("Paid the moment a world is cleared, multiplied by the world number.")]
        [SerializeField, FormerlySerializedAs("_shardsPerPlanetClear")] private int _shardsPerWorldClear = 15;

        [Header("Content")]
        [SerializeField] private UpgradeCard[] _upgrades;
        [SerializeField] private Fortune[] _fortunes;
        [SerializeField] private Sidekick[] _sidekicks;
        [SerializeField, FormerlySerializedAs("_planets")] private World[] _worlds;
        [SerializeField, FormerlySerializedAs("_critterColors")] private Color[] _enemyColors;

        [Header("Feel (timings in seconds unless noted)")]
        [SerializeField] private float _lungeDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _hitPoint = 0.45f;
        [SerializeField] private float _winDelay = 0.45f;
        [SerializeField] private float _floaterLife = 1.1f;
        [SerializeField] private float _floaterRise = 0.75f;
        [SerializeField] private float _bobAmplitude = 0.05f;
        [SerializeField] private float _bobFrequency = 2.2f;
        [SerializeField] private float _lungeDistance = 0.95f;
        [SerializeField] private float _shakeHit = 0.06f;
        [SerializeField] private float _shakeBoss = 0.22f;
        [SerializeField] private int _targetFrameRate = 60;

        [Header("Travel (one walk leg per ENGAGE tap)")]
        [SerializeField] private float _travelDuration = 0.7f;
        [Tooltip("World units the ground scrolls during one travel leg.")]
        [SerializeField] private float _travelDistance = 3.4f;
        [SerializeField] private float _walkHopsPerSecond = 2.6f;
        [SerializeField] private float _enemyEnterDuration = 0.32f;
        [Tooltip("How far off-screen right the enemy starts before sliding in.")]
        [SerializeField] private float _enemyEnterOffset = 5.2f;

        public int RoundsPerWorld => _roundsPerWorld;
        public int BossEvery => _bossEvery;
        public float EliteChance => _eliteChance;
        public int[] Speeds => _speeds;
        public float BeatMs => _beatMs;

        public int PlayerHp => _playerHp;
        public float PlayerAtk => _playerAtk;
        public int PlayerDef => _playerDef;
        public float PlayerCrit => _playerCrit;
        public float PlayerCritMult => _playerCritMult;
        public float RegenPct => _regenPct;

        public float LevelHpPct => _levelHpPct;
        public float LevelAtkPct => _levelAtkPct;
        public int LevelDefFlat => _levelDefFlat;
        public float LevelHealPct => _levelHealPct;

        public float DmgVariance => _dmgVariance;
        public float EnemyCritChance => _enemyCritChance;
        public float EnemyCritMult => _enemyCritMult;
        public int EnrageAfterBeats => _enrageAfterBeats;
        public float EnrageRamp => _enrageRamp;

        public EventWeights Weights => _eventWeights;
        public int MaxNonBattleStreak => _maxNonBattleStreak;
        public Vector2 SpringHeal => _springHeal;
        public Vector2 TrapDmg => _trapDmg;
        public Vector2 TreasureXp => _treasureXp;
        public int MaxSidekicks => _maxSidekicks;
        public float SnackHeal => _snackHeal;
        public float WorldClearHeal => _worldClearHeal;

        public MetaUpgrade[] MetaUpgrades => _metaUpgrades;
        public float ShardsPerRound => _shardsPerRound;
        public int ShardsPerElite => _shardsPerElite;
        public int ShardsPerBoss => _shardsPerBoss;
        public int ShardsPerWorldClear => _shardsPerWorldClear;

        public UpgradeCard[] Upgrades => _upgrades;
        public Fortune[] Fortunes => _fortunes;
        public Sidekick[] Sidekicks => _sidekicks;
        public World[] Worlds => _worlds;
        public Color[] EnemyColors => _enemyColors;

        public float LungeDuration => _lungeDuration;
        public float HitPoint => _hitPoint;
        public float WinDelay => _winDelay;
        public float FloaterLife => _floaterLife;
        public float FloaterRise => _floaterRise;
        public float BobAmplitude => _bobAmplitude;
        public float BobFrequency => _bobFrequency;
        public float LungeDistance => _lungeDistance;
        public float ShakeHit => _shakeHit;
        public float ShakeBoss => _shakeBoss;
        public int TargetFrameRate => _targetFrameRate;

        public float TravelDuration => _travelDuration;
        public float TravelDistance => _travelDistance;
        public float WalkHopsPerSecond => _walkHopsPerSecond;
        public float EnemyEnterDuration => _enemyEnterDuration;
        public float EnemyEnterOffset => _enemyEnterOffset;

        public int XpNeed(int level) =>
            Mathf.RoundToInt(_xpNeedBase * Mathf.Pow(level, _xpNeedPow));

        public int EnemyHp(int g) =>
            Mathf.RoundToInt((_enemyHpBase + _enemyHpLinear * g + _enemyHpQuad * g * g) *
                             Mathf.Pow(_enemyHpExp, g));

        public int EnemyAtk(int g) =>
            Mathf.RoundToInt((_enemyAtkBase + _enemyAtkLinear * g) * Mathf.Pow(_enemyAtkExp, g));

        public int EnemyDef(int g) =>
            Mathf.RoundToInt(_enemyDefBase + _enemyDefLinear * g);

        public int EnemyXp(int g) =>
            Mathf.RoundToInt(_enemyXpBase + _enemyXpLinear * g);

        public Vector3 BossMult => _bossMult;
        public Vector3 EliteMult => _eliteMult;

        public World WorldAt(int worldIndex) =>
            _worlds[worldIndex % _worlds.Length];
    }
}
