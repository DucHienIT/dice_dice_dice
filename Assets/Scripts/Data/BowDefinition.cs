using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Fast, low-damage arrows. Epic pierces, Legendary occasionally rains arrows (spec 9.2 / 10).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Bow", fileName = "Bow")]
    public class BowDefinition : CombatItemDefinition
    {
        [SerializeField] private float _projectileSpeed = 5.2f;
        [SerializeField] private float _arrowRainChance = 0.15f;
        [SerializeField] private int _arrowRainCount = 5;
        [SerializeField] private float _arrowRainDamageFactor = 0.6f;
        [SerializeField] private float _arrowRainSpeed = 6f;

        public override bool Fire(CombatContext ctx, ItemRarity rarity, Vector2 origin)
        {
            Enemy target = ctx.Enemies.NearestToWall();
            if (target == null)
            {
                return false;
            }

            float damage = EffectiveDamage(rarity, ctx);
            int pierce = ctx.Mods.Pierce;
            if (rarity >= ItemRarity.Epic) pierce += 1;
            if (rarity >= ItemRarity.Legendary) pierce += 1;

            ProjectileSpec spec = default;
            spec.Visual = ProjectileVisual.Arrow;
            spec.Origin = origin;
            spec.Target = target;
            spec.Speed = _projectileSpeed;
            spec.Damage = damage;
            spec.DamageType = DamageType;
            spec.SourceName = DisplayName;
            spec.Pierce = pierce;
            ctx.Projectiles.Spawn(spec);

            if (rarity == ItemRarity.Legendary && Random.value < _arrowRainChance)
            {
                for (int i = 0; i < _arrowRainCount; i++)
                {
                    Enemy rainTarget = ctx.Enemies.RandomAlive();
                    if (rainTarget == null) break;
                    ProjectileSpec rain = default;
                    rain.Visual = ProjectileVisual.Arrow;
                    rain.Origin = new Vector2(Random.Range(ctx.Config.WallStopX + 1f, ctx.Config.FieldRightX - 1f), ctx.Config.SkySpawnY);
                    rain.Target = rainTarget;
                    rain.Speed = _arrowRainSpeed;
                    rain.Damage = damage * _arrowRainDamageFactor;
                    rain.DamageType = DamageType;
                    rain.SourceName = DisplayName;
                    ctx.Projectiles.Spawn(rain);
                }
                ctx.ShowBanner?.Invoke("MƯA TÊN!");
            }

            ctx.Audio.Play(Sfx.Shoot);
            return true;
        }
    }
}
