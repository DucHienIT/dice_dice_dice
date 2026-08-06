using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>All run-level balancing knobs. Balance data lives here, never hardcoded in logic.</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Run")]
        [SerializeField] private int _startGold = 10;
        [SerializeField] private int _wallMaxHp = 100;
        [SerializeField] private int _waveCount = 10;

        [Header("XP / Level")]
        [SerializeField] private int _xpBase = 10;
        [SerializeField] private int _xpPerLevel = 10;

        [Header("Shop")]
        [SerializeField] private int _shopSlotCount = 3;
        [SerializeField] private int _rerollBaseCost = 2;
        [SerializeField] private int _rerollCostIncrement = 1;
        [SerializeField] private int _earlyDiceWaveThreshold = 3;
        [SerializeField] private float _diceWeightEarly = 3f;
        [SerializeField] private float _diceWeightLate = 1.2f;
        [SerializeField] private int _waveEndGold = 5;
        [SerializeField] private int _interestCap = 15;

        [Header("Dice feel")]
        [SerializeField] private float _rollAnimDuration = 0.55f;

        [Header("Enemy scaling")]
        [SerializeField] private float _hpGrowthPerWave = 0.28f;
        [SerializeField] private float _eliteHpGrowthPerWave = 0.15f;
        [SerializeField] private float _speedGrowthPerWave = 0.03f;
        [SerializeField] private float _slowSpeedFactor = 0.55f;
        [SerializeField] private float _healerPulseInterval = 1f;
        [SerializeField] private float _healerRadius = 1.1f;
        [SerializeField] private float _healerHpPerPulse = 3f;

        [Header("Layout (world units, 1u = 100px at 1920x1080)")]
        [SerializeField] private Vector2 _boardOrigin = new Vector2(-8.86f, 1.68f);
        [SerializeField] private float _slotStep = 1.12f;
        [SerializeField] private float _wallCenterX = -7f;
        [SerializeField] private float _wallWidth = 0.3f;
        [SerializeField] private float _wallStopX = -6.85f;
        [SerializeField] private float _spawnX = 10.3f;
        [SerializeField] private float _enemyYMin = -4.1f;
        [SerializeField] private float _enemyYMax = 3.5f;
        [SerializeField] private float _fieldRightX = 9.9f;
        [SerializeField] private float _skySpawnY = 6f;

        [Header("Performance")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private int _enemyPoolSize = 48;
        [SerializeField] private int _projectilePoolSize = 128;
        [SerializeField] private int _effectPoolSize = 48;
        [SerializeField] private int _lightningPoolSize = 8;
        [SerializeField] private int _floatingTextPoolSize = 32;

        public int StartGold => _startGold;
        public int WallMaxHp => _wallMaxHp;
        public int WaveCount => _waveCount;
        public int XpBase => _xpBase;
        public int XpPerLevel => _xpPerLevel;
        public int ShopSlotCount => _shopSlotCount;
        public int RerollBaseCost => _rerollBaseCost;
        public int RerollCostIncrement => _rerollCostIncrement;
        public int EarlyDiceWaveThreshold => _earlyDiceWaveThreshold;
        public float DiceWeightEarly => _diceWeightEarly;
        public float DiceWeightLate => _diceWeightLate;
        public int WaveEndGold => _waveEndGold;
        public int InterestCap => _interestCap;
        public float RollAnimDuration => _rollAnimDuration;
        public float SlowSpeedFactor => _slowSpeedFactor;
        public float HealerPulseInterval => _healerPulseInterval;
        public float HealerRadius => _healerRadius;
        public float HealerHpPerPulse => _healerHpPerPulse;
        public float WallCenterX => _wallCenterX;
        public float WallWidth => _wallWidth;
        public float WallStopX => _wallStopX;
        public float SpawnX => _spawnX;
        public float EnemyYMin => _enemyYMin;
        public float EnemyYMax => _enemyYMax;
        public float FieldRightX => _fieldRightX;
        public float SkySpawnY => _skySpawnY;
        public int TargetFrameRate => _targetFrameRate;
        public int EnemyPoolSize => _enemyPoolSize;
        public int ProjectilePoolSize => _projectilePoolSize;
        public int EffectPoolSize => _effectPoolSize;
        public int LightningPoolSize => _lightningPoolSize;
        public int FloatingTextPoolSize => _floatingTextPoolSize;

        public int XpNeededFor(int level)
        {
            return _xpBase + level * _xpPerLevel;
        }

        /// <summary>HP multiplier for a wave; elites and bosses grow slower (they start big).</summary>
        public float HpMultiplier(int wave, bool eliteOrBoss)
        {
            float growth = eliteOrBoss ? _eliteHpGrowthPerWave : _hpGrowthPerWave;
            return 1f + growth * (wave - 1);
        }

        public float SpeedMultiplier(int wave)
        {
            return 1f + _speedGrowthPerWave * (wave - 1);
        }

        /// <summary>World-space center of board slot 0..7 (2 columns x 4 rows, spec section 6.2).</summary>
        public Vector2 SlotWorldPosition(int index)
        {
            int col = index % BoardModel.Columns;
            int row = index / BoardModel.Columns;
            return new Vector2(_boardOrigin.x + col * _slotStep, _boardOrigin.y - row * _slotStep);
        }
    }
}
