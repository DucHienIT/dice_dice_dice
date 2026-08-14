using System;
using Game.Core;
using Game.Data;
using Game.Enemies;
using Game.Sidekicks;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Beat-based auto-battle per the spec: hero first, alternating turns on BEAT_MS,
    /// hit applied at HitPoint of the lunge, enrage ramp past EnrageAfterBeats.
    /// Pure logic — ticked by GameManager with speed-scaled dt; views listen to events.
    /// </summary>
    public class BattleEngine
    {
        private readonly GameConfig _config;

        private PlayerState _player;
        private EnemyState _enemy;
        private RunStats _stats;

        private float _beatTimer;
        private BattleActor _turn;
        private bool _animActive;
        private BattleActor _animWho;
        private float _animT;
        private bool _hitApplied;
        private int _pendingDamage;
        private bool _pendingCrit;
        private float _winTimer = -1f;
        private int _battleBeats;
        private bool _enrageAnnounced;
        private bool _running;

        public event Action<BattleActor> BeatStarted;
        public event Action<HitInfo> HitApplied;
        public event Action<int> HeroHealed;
        public event Action<int> ThornsReflected;
        public event Action EnrageStarted;
        public event Action Won;
        public event Action Lost;

        public BattleEngine(GameConfig config)
        {
            _config = config;
        }

        public bool IsRunning => _running;
        public bool AnimActive => _animActive;
        public BattleActor AnimWho => _animWho;
        public float AnimT => _animT;
        public bool IsEnraged => _battleBeats > _config.EnrageAfterBeats;
        public EnemyState Enemy => _enemy;

        public void StartBattle(PlayerState player, EnemyState enemy, RunStats stats)
        {
            _player = player;
            _enemy = enemy;
            _stats = stats;
            _beatTimer = 0f;
            _turn = BattleActor.Hero;
            _animActive = false;
            _animT = 0f;
            _hitApplied = false;
            _winTimer = -1f;
            _battleBeats = 0;
            _enrageAnnounced = false;
            _running = true;
        }

        public void Abort()
        {
            _running = false;
            _enemy = null;
            _animActive = false;
            _winTimer = -1f;
        }

        /// <summary>dt already multiplied by the speed setting.</summary>
        public void Tick(float dt)
        {
            if (!_running) return;

            if (_winTimer >= 0f)
            {
                _winTimer -= dt;
                if (_animActive)
                {
                    _animT = Mathf.Min(1f, _animT + dt / _config.LungeDuration);
                    if (_animT >= 1f) _animActive = false;
                }
                if (_winTimer <= 0f)
                {
                    _animActive = false;
                    _running = false;
                    Won?.Invoke();
                }
                return;
            }

            if (_animActive)
            {
                _animT += dt / _config.LungeDuration;
                if (!_hitApplied && _animT >= _config.HitPoint) ApplyHit();
                if (!_running) return; // hero died inside ApplyHit
                if (_animT >= 1f)
                {
                    _animT = 1f;
                    _animActive = false;
                }
                return;
            }

            _beatTimer += dt * 1000f;
            if (_beatTimer >= _config.BeatMs)
            {
                _beatTimer = 0f;
                StartBeat();
            }
        }

        private void StartBeat()
        {
            _hitApplied = false;
            _animT = 0f;
            _animActive = true;
            _battleBeats++;

            if (_turn == BattleActor.Hero)
            {
                _animWho = BattleActor.Hero;
                float effAtk = _player.Atk * (1f + SidekickRoster.BonusFor(_player, SidekickType.Damage));
                float critChance = _player.Crit + SidekickRoster.BonusFor(_player, SidekickType.Crit);
                _pendingDamage = DamageCalculator.Roll(effAtk, _enemy.Def, critChance,
                    _player.CritMult, _config.DmgVariance, out _pendingCrit);
            }
            else
            {
                _animWho = BattleActor.Enemy;
                int dmg = DamageCalculator.Roll(_enemy.Atk, _player.Def, _config.EnemyCritChance,
                    _config.EnemyCritMult, _config.DmgVariance, out _pendingCrit);
                // Enrage: past N beats enemy damage ramps up — infinite-sustain stalemates stay impossible
                float enrage = 1f + Mathf.Max(0, _battleBeats - _config.EnrageAfterBeats) * _config.EnrageRamp;
                float block = SidekickRoster.BonusFor(_player, SidekickType.Block);
                _pendingDamage = Mathf.Max(1, Mathf.RoundToInt(dmg * enrage * (1f - block)));

                if (!_enrageAnnounced && _battleBeats > _config.EnrageAfterBeats)
                {
                    _enrageAnnounced = true;
                    EnrageStarted?.Invoke();
                }
            }
            BeatStarted?.Invoke(_animWho);
        }

        private void ApplyHit()
        {
            _hitApplied = true;
            if (_animWho == BattleActor.Hero)
            {
                _enemy.TakeDamage(_pendingDamage);
                _stats.RegisterHit(_pendingDamage, _pendingCrit);
                bool died = _enemy.IsDead;
                HitApplied?.Invoke(new HitInfo(BattleActor.Enemy, _pendingDamage, _pendingCrit, died));

                float healBonus = SidekickRoster.BonusFor(_player, SidekickType.Heal);
                int healed = Mathf.RoundToInt(_pendingDamage * _player.Lifesteal +
                                              _player.MaxHp * healBonus);
                if (healed > 0 && _player.Hp < _player.MaxHp)
                {
                    HeroHealed?.Invoke(_player.Heal(healed));
                }
                if (died)
                {
                    _winTimer = _config.WinDelay;
                }
            }
            else
            {
                _player.TakeDamage(_pendingDamage);
                bool heroDied = _player.IsDead;
                HitApplied?.Invoke(new HitInfo(BattleActor.Hero, _pendingDamage, _pendingCrit, heroDied));

                if (_player.Thorns > 0f && !_enemy.IsDead)
                {
                    int reflected = Mathf.Max(1, Mathf.RoundToInt(_pendingDamage * _player.Thorns));
                    _enemy.TakeDamage(reflected);
                    ThornsReflected?.Invoke(reflected);
                    if (_enemy.IsDead) _winTimer = _config.WinDelay;
                }
                if (heroDied)
                {
                    _running = false;
                    Lost?.Invoke();
                    return;
                }
            }
            _turn = _turn == BattleActor.Hero ? BattleActor.Enemy : BattleActor.Hero;
        }
    }
}
