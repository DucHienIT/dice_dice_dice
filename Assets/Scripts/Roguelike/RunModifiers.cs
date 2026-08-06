namespace DiceDiceDice
{
    /// <summary>Run-wide stat modifiers accumulated from roguelike upgrades (spec section 18).</summary>
    public class RunModifiers
    {
        public float DiceSpeed = 1f;
        public int DiceMinFace;
        public int SixBonusGold;
        public float DoubleRollChance;
        public int MergeDiceGold;
        public float Interest;
        public int WaveEndGold;
        public int FreeRerollPerWave;
        public float SellRate = 0.5f;

        public float PhysicalDamage = 1f;
        public float AttackSpeed = 1f;
        public float CritChance;
        public int Pierce;
        public bool CritExplode;

        public float MagicDamage = 1f;
        public float MagicCooldown = 1f;
        public float DotDuration = 1f;
        public float DoubleCastChance;

        public float SupportPower = 1f;

        public int HealPerWave;
        public int WaveShield;
    }
}
