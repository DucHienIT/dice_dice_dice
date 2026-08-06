using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>The economy item: rolls automatically during waves only, generates gold equal to the face (spec section 8).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Dice", fileName = "Dice")]
    public class DiceDefinition : ItemDefinition
    {
        [SerializeField] private float _baseInterval = 5f;
        [SerializeField] private float _intervalReductionPerRarity = 0.7f;
        [SerializeField] private float _minInterval = 1.2f;

        public float RollInterval(ItemRarity rarity, RunModifiers mods, float speedAura)
        {
            float raw = _baseInterval - (int)rarity * _intervalReductionPerRarity;
            return Mathf.Max(_minInterval, raw / (mods.DiceSpeed * speedAura));
        }

        public int MinFace(ItemRarity rarity, RunModifiers mods)
        {
            return Mathf.Clamp(1 + (int)rarity + mods.DiceMinFace, 1, 6);
        }

        public int GoldFor(int face, ItemRarity rarity, RunModifiers mods)
        {
            int gold = face * ((int)rarity + 1);
            if (face == 6)
            {
                gold += mods.SixBonusGold;
            }
            return gold;
        }

        public override string DescribeStats(ItemRarity rarity, CombatContext ctx)
        {
            float interval = RollInterval(rarity, ctx.Mods, ctx.Auras.SpeedMultiplier);
            int min = MinFace(rarity, ctx.Mods);
            return string.Format("Rolls every {0:0.0}s | Faces {1}-6 | Gold = face x {2}", interval, min, (int)rarity + 1);
        }

#if UNITY_EDITOR
        public void EditorSetupDice(float baseInterval, float intervalReductionPerRarity, float minInterval)
        {
            _baseInterval = baseInterval;
            _intervalReductionPerRarity = intervalReductionPerRarity;
            _minInterval = minInterval;
        }
#endif
    }
}
