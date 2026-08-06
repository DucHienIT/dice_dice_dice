using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Base for auto-attacking items. Each concrete type implements its own Fire (Open/Closed: new item = new class + asset).</summary>
    public abstract class CombatItemDefinition : ItemDefinition
    {
        [SerializeField] private float _baseDamage;
        [SerializeField] private float _baseCooldown;
        [SerializeField] private DamageType _damageType;

        public DamageType DamageType => _damageType;

        public float EffectiveDamage(ItemRarity rarity, CombatContext ctx)
        {
            float damage = _baseDamage * RarityMath.Multiplier(rarity);
            if (_damageType == DamageType.Physical)
            {
                return damage * ctx.Mods.PhysicalDamage * ctx.Auras.PhysicalMultiplier;
            }
            if (_damageType == DamageType.Magic)
            {
                return damage * ctx.Mods.MagicDamage;
            }
            return damage;
        }

        public float EffectiveCooldown(ItemRarity rarity, CombatContext ctx)
        {
            if (_damageType == DamageType.Magic)
            {
                return _baseCooldown * ctx.Mods.MagicCooldown / ctx.Auras.SpeedMultiplier;
            }
            return _baseCooldown / (ctx.Mods.AttackSpeed * ctx.Auras.SpeedMultiplier);
        }

        /// <summary>Attempt one attack. Returns false when no valid target exists (cooldown is not consumed).</summary>
        public abstract bool Fire(CombatContext ctx, ItemRarity rarity, Vector2 origin);

        public override string DescribeStats(ItemRarity rarity, CombatContext ctx)
        {
            return string.Format("Damage {0:0} | Cooldown {1:0.00}s",
                EffectiveDamage(rarity, ctx), EffectiveCooldown(rarity, ctx));
        }

        protected bool RollCrit(CombatContext ctx, float baseChance)
        {
            return Random.value < baseChance + ctx.Mods.CritChance;
        }

#if UNITY_EDITOR
        public void EditorSetupCombat(float baseDamage, float baseCooldown, DamageType damageType)
        {
            _baseDamage = baseDamage;
            _baseCooldown = baseCooldown;
            _damageType = damageType;
        }
#endif
    }
}
