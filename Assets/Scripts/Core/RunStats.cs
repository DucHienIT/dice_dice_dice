using System.Collections.Generic;

namespace DiceDiceDice
{
    /// <summary>End-of-run statistics (spec section 28).</summary>
    public class RunStats
    {
        public int Kills;
        public int DiceGold;
        public int Rolls;
        public int Sixes;
        public int ItemsBought;
        public int Merges;

        public readonly Dictionary<string, float> DamageBySource = new Dictionary<string, float>(16);
        public readonly List<string> ChosenUpgrades = new List<string>(16);

        public void TrackDamage(string source, float amount)
        {
            float current;
            DamageBySource.TryGetValue(source, out current);
            DamageBySource[source] = current + amount;
        }

        public string TopDamageSource(out float amount)
        {
            string best = null;
            amount = 0f;
            foreach (KeyValuePair<string, float> pair in DamageBySource)
            {
                if (pair.Value > amount)
                {
                    amount = pair.Value;
                    best = pair.Key;
                }
            }
            return best;
        }
    }
}
