using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Low damage, slows enemies; Epic+ can freeze briefly (spec 9.3).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Frost Stone", fileName = "FrostStone")]
    public class FrostStoneDefinition : CombatItemDefinition
    {
        [SerializeField] private float _projectileSpeed = 4.6f;
        [SerializeField] private float _slowDuration = 2f;
        [SerializeField] private float _freezeChance = 0.2f;
        [SerializeField] private float _freezeDuration = 1f;
        [SerializeField] private ItemRarity _freezeMinRarity = ItemRarity.Epic;

        public override bool Fire(CombatContext ctx, ItemRarity rarity, Vector2 origin)
        {
            Enemy target = ctx.Enemies.NearestToWall();
            if (target == null)
            {
                return false;
            }

            bool freeze = rarity >= _freezeMinRarity && Random.value < _freezeChance;

            ProjectileSpec spec = default;
            spec.Visual = ProjectileVisual.Frost;
            spec.Origin = origin;
            spec.Target = target;
            spec.Speed = _projectileSpeed;
            spec.Damage = EffectiveDamage(rarity, ctx);
            spec.DamageType = DamageType;
            spec.SourceName = DisplayName;
            spec.LaunchDelay = 0.1f;
            spec.SlowDuration = _slowDuration * ctx.Mods.DotDuration;
            spec.FreezeDuration = freeze ? _freezeDuration * ctx.Mods.DotDuration : 0f;
            ctx.Projectiles.Spawn(spec);
            return true;
        }
    }
}
