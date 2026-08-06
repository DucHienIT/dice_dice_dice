using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Non-attacking aura item (Anvil: physical damage, Hourglass: speed). Data-driven — both share this class (spec 9.4).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Support", fileName = "Support")]
    public class SupportDefinition : ItemDefinition
    {
        [SerializeField] private float _physicalAuraPerRarity;
        [SerializeField] private float _speedAuraPerRarity;

        public float PhysicalAura(ItemRarity rarity, RunModifiers mods)
        {
            return _physicalAuraPerRarity * ((int)rarity + 1) * mods.SupportPower;
        }

        public float SpeedAura(ItemRarity rarity, RunModifiers mods)
        {
            return _speedAuraPerRarity * ((int)rarity + 1) * mods.SupportPower;
        }

        public override string DescribeStats(ItemRarity rarity, CombatContext ctx)
        {
            float phys = PhysicalAura(rarity, ctx.Mods);
            float speed = SpeedAura(rarity, ctx.Mods);
            if (phys > 0f && speed > 0f)
            {
                return string.Format("+{0:0}% sát thương vật lý · +{1:0}% tốc độ toàn đội", phys * 100f, speed * 100f);
            }
            if (phys > 0f)
            {
                return string.Format("+{0:0}% sát thương vật lý toàn đội", phys * 100f);
            }
            return string.Format("+{0:0}% tốc độ toàn đội", speed * 100f);
        }

#if UNITY_EDITOR
        public void EditorSetupSupport(float physicalAuraPerRarity, float speedAuraPerRarity)
        {
            _physicalAuraPerRarity = physicalAuraPerRarity;
            _speedAuraPerRarity = speedAuraPerRarity;
        }
#endif
    }
}
