using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Slow shell with area explosion — crowd control (spec 9.2).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Cannon", fileName = "Cannon")]
    public class CannonDefinition : CombatItemDefinition
    {
        [SerializeField] private float _projectileSpeed = 3.8f;
        [SerializeField] private float _baseAoeRadius = 0.7f;
        [SerializeField] private float _aoeRadiusPerRarity = 0.12f;

        public override bool Fire(CombatContext ctx, ItemRarity rarity, Vector2 origin)
        {
            Enemy target = ctx.Enemies.NearestToWall();
            if (target == null)
            {
                return false;
            }

            ProjectileSpec spec = default;
            spec.Visual = ProjectileVisual.Shell;
            spec.Origin = origin;
            spec.Target = target;
            spec.Speed = _projectileSpeed;
            spec.Damage = EffectiveDamage(rarity, ctx);
            spec.DamageType = DamageType;
            spec.SourceName = DisplayName;
            spec.AoeRadius = _baseAoeRadius + _aoeRadiusPerRarity * (int)rarity;
            ctx.Projectiles.Spawn(spec);

            ctx.Audio.Play(Sfx.Shoot);
            return true;
        }
    }
}
