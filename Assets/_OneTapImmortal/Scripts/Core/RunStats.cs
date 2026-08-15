namespace Game.Core
{
    /// <summary>Per-run tallies surfaced on the death screen.</summary>
    public class RunStats
    {
        public int Hits;
        public int Crits;
        public long DamageDealt;
        public int MaxHit;
        public int ElitesSlain;
        public int BossesSlain;

        public void RegisterHit(int damage, bool crit)
        {
            Hits++;
            if (crit) Crits++;
            DamageDealt += damage;
            if (damage > MaxHit) MaxHit = damage;
        }
    }
}
