using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Slow, heavy bolt with high crit chance — the Elite/Boss killer (spec 9.2).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Crossbow", fileName = "Crossbow")]
    public class CrossbowDefinition : CombatItemDefinition
    {
        [SerializeField] private float _projectileSpeed = 6.8f;
        [SerializeField] private float _baseCritChance = 0.3f;
        [SerializeField] private float _critChancePerRarity = 0.05f;
        [SerializeField] private float _critMultiplier = 2.5f;
        [SerializeField] private int _legendaryPierce = 2;

        public override bool Fire(CombatContext ctx, ItemRarity rarity, Vector2 origin)
        {
            Enemy target = ctx.Enemies.NearestToWall();
            if (target == null)
            {
                return false;
            }

            bool crit = RollCrit(ctx, _baseCritChance + _critChancePerRarity * (int)rarity);
            float damage = EffectiveDamage(rarity, ctx) * (crit ? _critMultiplier : 1f);

            ProjectileSpec spec = default;
            spec.Visual = ProjectileVisual.Bolt;
            spec.Origin = origin;
            spec.Target = target;
            spec.Speed = _projectileSpeed;
            spec.Damage = damage;
            spec.DamageType = DamageType;
            spec.SourceName = DisplayName;
            spec.Crit = crit;
            spec.Pierce = (rarity >= ItemRarity.Legendary ? _legendaryPierce : 0) + ctx.Mods.Pierce;
            ctx.Projectiles.Spawn(spec);

            ctx.Audio.Play(Sfx.Shoot);
            return true;
        }
    }
}
