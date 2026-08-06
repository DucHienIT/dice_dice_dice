using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Pools and ticks all projectiles in one loop; hit detection is list-distance based, no physics.</summary>
    public class ProjectileManager : MonoBehaviour
    {
        private const float HitPadding = 0.08f;

        [SerializeField] private GameConfig _config;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Transform _projectileParent;
        [SerializeField] private EffectManager _effects;

        private ObjectPool<Projectile> _pool;
        private readonly List<Projectile> _active = new List<Projectile>(64);
        private CombatContext _ctx;

        public void Init(CombatContext ctx)
        {
            _ctx = ctx;
            _pool = new ObjectPool<Projectile>(_projectilePrefab, _projectileParent, _config.ProjectilePoolSize);
        }

        public void Spawn(in ProjectileSpec spec)
        {
            Projectile projectile = _pool.Get();
            projectile.Setup(spec);
            _active.Add(projectile);
        }

        public void DespawnAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _pool.Release(_active[i]);
            }
            _active.Clear();
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Projectile projectile = _active[i];
                if (TickOne(projectile, deltaTime))
                {
                    _active[i] = _active[_active.Count - 1];
                    _active.RemoveAt(_active.Count - 1);
                    _pool.Release(projectile);
                }
            }
        }

        /// <summary>Returns true when the projectile is done.</summary>
        private bool TickOne(Projectile projectile, float deltaTime)
        {
            if (projectile.Spec.MeteorFall)
            {
                projectile.FallTo(deltaTime);
                float groundY = projectile.Target != null && !projectile.Target.Dead
                    ? projectile.Target.Position.y
                    : _config.EnemyYMin;
                if (projectile.Position.y <= groundY)
                {
                    _ctx.Enemies.AoeDamage(new Vector2(projectile.Position.x, groundY), projectile.Spec.AoeRadius,
                        projectile.Spec.Damage, projectile.Spec.DamageType, projectile.Spec.SourceName,
                        projectile.Spec.BurnDps, projectile.Spec.BurnDuration);
                    return true;
                }
                return false;
            }

            if (projectile.Target == null || projectile.Target.Dead)
            {
                Enemy retarget = _ctx.Enemies.NearestToWall();
                projectile.Target = retarget;
                if (retarget == null)
                {
                    projectile.MoveStraight(deltaTime);
                }
                else
                {
                    projectile.MoveTowards(retarget.Position, deltaTime);
                }
            }
            else
            {
                projectile.MoveTowards(projectile.Target.Position, deltaTime);
            }

            Vector2 position = projectile.Position;
            if (position.x > _config.FieldRightX + 0.6f || position.x < _config.ProjectileLeftX ||
                position.y > _config.SkySpawnY + 1f || position.y < _config.EnemyYMin - 1f)
            {
                return true;
            }

            return CheckCollision(projectile);
        }

        private bool CheckCollision(Projectile projectile)
        {
            Enemy hit = FindHitEnemy(projectile);
            if (hit == null)
            {
                return false;
            }

            ProjectileSpec spec = projectile.Spec;
            Vector2 hitPos = hit.Position;

            if (spec.AoeRadius > 0f)
            {
                _ctx.Enemies.AoeDamage(projectile.Position, spec.AoeRadius, spec.Damage, spec.DamageType, spec.SourceName, spec.BurnDps, spec.BurnDuration);
                return true;
            }

            if (spec.SlowDuration > 0f || spec.FreezeDuration > 0f)
            {
                _ctx.Enemies.DamageEnemy(hit, spec.Damage, spec.DamageType, spec.SourceName);
                if (!hit.Dead)
                {
                    if (spec.SlowDuration > 0f) hit.ApplySlow(spec.SlowDuration);
                    if (spec.FreezeDuration > 0f) hit.ApplyFreeze(spec.FreezeDuration);
                }
                _effects.SpawnFrostRing(hitPos);
                return true;
            }

            _ctx.Enemies.DamageEnemy(hit, spec.Damage, spec.DamageType, spec.SourceName);
            if (spec.Crit)
            {
                _effects.SpawnCritLabel(hitPos);
                if (_ctx.Mods.CritExplode)
                {
                    _ctx.Enemies.AoeDamage(hitPos, 0.5f, spec.Damage * 0.4f, spec.DamageType, spec.SourceName, 0f, 0f);
                }
            }

            projectile.HitEnemies.Add(hit);
            if (projectile.Spec.Pierce > 0)
            {
                ProjectileSpec updated = projectile.Spec;
                updated.Pierce--;
                projectile.Spec = updated;
                projectile.Target = null;
                return false;
            }
            return true;
        }

        private Enemy FindHitEnemy(Projectile projectile)
        {
            Enemy target = projectile.Target;
            Vector2 position = projectile.Position;
            // Preferred: current target; fallback: any enemy overlapping (covers pierce fly-through).
            if (target != null && !target.Dead && !projectile.HitEnemies.Contains(target))
            {
                float reach = target.Radius + HitPadding;
                if ((target.Position - position).sqrMagnitude <= reach * reach)
                {
                    return target;
                }
            }
            return _ctx.Enemies.FindOverlapping(position, HitPadding, projectile.HitEnemies);
        }
    }
}
