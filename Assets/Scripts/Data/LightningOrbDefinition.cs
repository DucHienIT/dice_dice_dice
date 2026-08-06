using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Instant chain lightning between nearby enemies (spec 9.3).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Lightning Orb", fileName = "LightningOrb")]
    public class LightningOrbDefinition : CombatItemDefinition
    {
        [SerializeField] private int _baseChainCount = 3;
        [SerializeField] private float _chainRadius = 1.3f;

        public override bool Fire(CombatContext ctx, ItemRarity rarity, Vector2 origin)
        {
            Enemy current = ctx.Enemies.NearestToWall();
            if (current == null)
            {
                return false;
            }

            float damage = EffectiveDamage(rarity, ctx);
            int remaining = _baseChainCount + (int)rarity;
            Vector2 from = origin;
            ctx.Enemies.BeginChain();
            while (current != null && remaining-- > 0)
            {
                ctx.Enemies.MarkChained(current);
                Vector2 pos = current.Position;
                ctx.Effects.SpawnLightning(from, pos);
                ctx.Enemies.DamageEnemy(current, damage, DamageType, DisplayName);
                from = pos;
                current = ctx.Enemies.NextChainTarget(from, _chainRadius);
            }

            ctx.Audio.Play(Sfx.Zap);
            return true;
        }
    }
}
