using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Summons a meteor onto the target: area damage plus burn over time (spec 9.3).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Fire Book", fileName = "FireBook")]
    public class FireBookDefinition : CombatItemDefinition
    {
        [SerializeField] private float _fallSpeed = 4.6f;
        [SerializeField] private float _baseAoeRadius = 0.75f;
        [SerializeField] private float _aoeRadiusPerRarity = 0.12f;
        [SerializeField] private float _burnDpsPerRarity = 3f;
        [SerializeField] private float _burnDuration = 3f;

        public override bool Fire(CombatContext ctx, ItemRarity rarity, Vector2 origin)
        {
            Enemy target = ctx.Enemies.NearestToWall();
            if (target == null)
            {
                return false;
            }

            ProjectileSpec spec = default;
            spec.Visual = ProjectileVisual.Meteor;
            spec.MeteorFall = true;
            spec.Origin = new Vector2(target.Position.x + Random.Range(-0.1f, 0.1f), ctx.Config.SkySpawnY);
            spec.Target = target;
            spec.Speed = _fallSpeed;
            spec.Damage = EffectiveDamage(rarity, ctx);
            spec.DamageType = DamageType;
            spec.SourceName = DisplayName;
            spec.LaunchDelay = 0.12f;
            spec.AoeRadius = _baseAoeRadius + _aoeRadiusPerRarity * (int)rarity;
            spec.BurnDps = _burnDpsPerRarity * ((int)rarity + 1);
            spec.BurnDuration = _burnDuration * ctx.Mods.DotDuration;
            ctx.Projectiles.Spawn(spec);
            return true;
        }
    }
}
