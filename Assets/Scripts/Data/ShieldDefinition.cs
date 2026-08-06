using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Grants the wall a shield at the start of each wave (spec 9.5).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Shield", fileName = "Shield")]
    public class ShieldDefinition : ItemDefinition
    {
        [SerializeField] private int _shieldPerRarity = 15;

        public int ShieldFor(ItemRarity rarity)
        {
            return _shieldPerRarity * ((int)rarity + 1);
        }

        public override string DescribeStats(ItemRarity rarity, CombatContext ctx)
        {
            return string.Format("Wave-start shield: {0}", ShieldFor(rarity));
        }

#if UNITY_EDITOR
        public void EditorSetupShield(int shieldPerRarity)
        {
            _shieldPerRarity = shieldPerRarity;
        }
#endif
    }
}
