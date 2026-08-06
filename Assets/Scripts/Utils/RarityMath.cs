namespace DiceDiceDice
{
    public static class RarityMath
    {
        /// <summary>Stat multiplier per rarity step: Common x1, Rare x2, Epic x4, Legendary x8.</summary>
        public static float Multiplier(ItemRarity rarity)
        {
            return 1 << (int)rarity;
        }
    }
}
