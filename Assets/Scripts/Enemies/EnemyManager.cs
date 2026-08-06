using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Owns the list of active enemies and ticks them in one Update path (CODE_RULES 5.2). Source of truth for targeting.</summary>
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private Enemy _enemyPrefab;
        [SerializeField] private Transform _enemyParent;
        [SerializeField] private EffectManager _effects;
        [SerializeField] private AudioManager _audio;

        private ObjectPool<Enemy> _pool;
        private readonly List<Enemy> _active = new List<Enemy>(64);
        private readonly HashSet<Enemy> _chained = new HashSet<Enemy>();
        private RunStats _stats;
        private BaseWall _wall;

        /// <summary>Raised with the XP reward when an enemy dies from damage.</summary>
        public event Action<Enemy> EnemyKilled;
        public event Action BossArmorBroken;

        public int ActiveCount => _active.Count;

        public void Init(RunStats stats, BaseWall wall)
        {
            _stats = stats;
            _wall = wall;
            _pool = new ObjectPool<Enemy>(_enemyPrefab, _enemyParent, _config.EnemyPoolSize);
        }

        public void Spawn(EnemyDefinition definition, int wave)
        {
            Enemy enemy = _pool.Get();
            float hpMul = _config.HpMultiplier(wave, definition.IsElite || definition.IsBoss);
            float speedMul = _config.SpeedMultiplier(wave);
            var position = new Vector2(_config.SpawnX, UnityEngine.Random.Range(_config.EnemyYMin, _config.EnemyYMax));
            enemy.Setup(definition, hpMul, speedMul, position);
            _active.Add(enemy);
        }

        public void Tick(float deltaTime)
        {
            float slowFactor = _config.SlowSpeedFactor;
            float stopX = _config.WallStopX;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _active[i];
                if (enemy.Dead)
                {
                    RemoveAt(i);
                    continue;
                }

                float dotDamage;
                enemy.TickTimers(deltaTime, out dotDamage);
                if (dotDamage > 0f)
                {
                    enemy.ApplyDirectDamage(dotDamage);
                    _stats.TrackDamage("Thiêu đốt", dotDamage);
                    if (enemy.Hp <= 0f)
                    {
                        Kill(enemy);
                        RemoveAt(i);
                        continue;
                    }
                }

                enemy.Move(enemy.CurrentSpeed(slowFactor) * deltaTime);

                if (enemy.Definition.IsHealer)
                {
                    TickHealer(enemy, deltaTime);
                }

                if (enemy.Position.x <= stopX + enemy.Radius)
                {
                    _wall.TakeDamage(enemy.Definition.ContactDamage);
                    _effects.SpawnWallHit(enemy.Position.y);
                    _audio.Play(Sfx.Boom);
                    enemy.MarkDead();
                    RemoveAt(i);
                }
            }
        }

        public void DespawnAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _pool.Release(_active[i]);
            }
            _active.Clear();
        }

        // ---------- Damage ----------

        public void DamageEnemy(Enemy enemy, float amount, DamageType type, string sourceName)
        {
            if (enemy.Dead)
            {
                return;
            }

            float final = amount;
            if (type == DamageType.Physical)
            {
                final *= 1f - enemy.Definition.PhysicalResist;
            }
            else if (type == DamageType.Magic)
            {
                final *= 1f - enemy.Definition.MagicResist;
            }

            float dealt;
            bool armorBroke = enemy.ApplyDamage(final, out dealt);
            if (armorBroke)
            {
                BossArmorBroken?.Invoke();
            }
            _stats.TrackDamage(sourceName, dealt);
            _audio.Play(Sfx.Hit);
            if (enemy.Hp <= 0f)
            {
                Kill(enemy);
            }
        }

        public void AoeDamage(Vector2 center, float radius, float damage, DamageType type, string sourceName, float burnDps, float burnDuration)
        {
            _effects.SpawnExplosion(center, radius);
            _audio.Play(Sfx.Boom);
            for (int i = 0; i < _active.Count; i++)
            {
                Enemy enemy = _active[i];
                if (enemy.Dead)
                {
                    continue;
                }
                float reach = radius + enemy.Radius;
                if ((enemy.Position - center).sqrMagnitude <= reach * reach)
                {
                    DamageEnemy(enemy, damage, type, sourceName);
                    if (burnDps > 0f && !enemy.Dead)
                    {
                        enemy.AddDot(burnDps, burnDuration);
                    }
                }
            }
        }

        private void Kill(Enemy enemy)
        {
            if (enemy.Dead)
            {
                return;
            }
            enemy.MarkDead();
            _stats.Kills++;
            _effects.SpawnDeathPop(enemy.Position, enemy.Radius, enemy.Definition.BodyColor);
            _audio.Play(Sfx.Die);
            EnemyKilled?.Invoke(enemy);
        }

        private void RemoveAt(int index)
        {
            Enemy enemy = _active[index];
            _active[index] = _active[_active.Count - 1];
            _active.RemoveAt(_active.Count - 1);
            _pool.Release(enemy);
        }

        private void TickHealer(Enemy healer, float deltaTime)
        {
            healer.HealTimer += deltaTime;
            if (healer.HealTimer < _config.HealerPulseInterval)
            {
                return;
            }
            healer.HealTimer = 0f;
            float radius = _config.HealerRadius;
            float healAmount = _config.HealerHpPerPulse * _config.HpMultiplier(WaveContext, false);
            for (int i = 0; i < _active.Count; i++)
            {
                Enemy other = _active[i];
                if (other.Dead || ReferenceEquals(other, healer))
                {
                    continue;
                }
                if ((other.Position - healer.Position).sqrMagnitude <= radius * radius)
                {
                    other.HealBy(healAmount);
                }
            }
            _effects.SpawnHeal(healer.Position);
        }

        /// <summary>Current wave number, provided by GameManager so heal scaling matches spawn scaling.</summary>
        public int WaveContext { get; set; } = 1;

        // ---------- Targeting (list-based, no physics scans) ----------

        public Enemy NearestToWall()
        {
            Enemy best = null;
            float bestX = float.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                Enemy enemy = _active[i];
                if (!enemy.Dead && enemy.Position.x < bestX)
                {
                    bestX = enemy.Position.x;
                    best = enemy;
                }
            }
            return best;
        }

        public Enemy RandomAlive()
        {
            int aliveCount = 0;
            for (int i = 0; i < _active.Count; i++)
            {
                if (!_active[i].Dead)
                {
                    aliveCount++;
                }
            }
            if (aliveCount == 0)
            {
                return null;
            }
            int pick = UnityEngine.Random.Range(0, aliveCount);
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Dead)
                {
                    continue;
                }
                if (pick-- == 0)
                {
                    return _active[i];
                }
            }
            return null;
        }

        public void CollectFrontmost(int count, List<Enemy> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < _active.Count; i++)
            {
                Enemy enemy = _active[i];
                if (enemy.Dead)
                {
                    continue;
                }
                int insertAt = buffer.Count;
                for (int j = 0; j < buffer.Count; j++)
                {
                    if (enemy.Position.x < buffer[j].Position.x)
                    {
                        insertAt = j;
                        break;
                    }
                }
                if (insertAt < count)
                {
                    buffer.Insert(insertAt, enemy);
                    if (buffer.Count > count)
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                    }
                }
            }
        }

        /// <summary>First alive enemy overlapping a point (projectile hit test), skipping already-hit ones.</summary>
        public Enemy FindOverlapping(Vector2 point, float padding, HashSet<Enemy> exclude)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                Enemy enemy = _active[i];
                if (enemy.Dead || exclude.Contains(enemy))
                {
                    continue;
                }
                float reach = enemy.Radius + padding;
                if ((enemy.Position - point).sqrMagnitude <= reach * reach)
                {
                    return enemy;
                }
            }
            return null;
        }

        public void BeginChain()
        {
            _chained.Clear();
        }

        public void MarkChained(Enemy enemy)
        {
            _chained.Add(enemy);
        }

        public Enemy NextChainTarget(Vector2 from, float radius)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                Enemy enemy = _active[i];
                if (enemy.Dead || _chained.Contains(enemy))
                {
                    continue;
                }
                if ((enemy.Position - from).sqrMagnitude <= radius * radius)
                {
                    return enemy;
                }
            }
            return null;
        }
    }
}
