namespace DiceDiceDice
{
    /// <summary>A concrete item occupying one board slot: definition + rarity + running timers.</summary>
    public class ItemInstance
    {
        public readonly ItemDefinition Definition;
        public ItemRarity Rarity;

        /// <summary>Cooldown / roll progress in seconds. Dice progress carries over between waves (spec 8.1).</summary>
        public float Timer;

        /// <summary>Remaining duration of the dice roll animation; a roll resolves when this reaches 0.</summary>
        public float RollAnimTimer;

        public ItemInstance(ItemDefinition definition, ItemRarity rarity)
        {
            Definition = definition;
            Rarity = rarity;
        }

        public int RarityIndex => (int)Rarity;
    }
}
