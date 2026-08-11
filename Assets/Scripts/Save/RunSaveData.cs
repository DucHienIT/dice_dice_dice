using System;

namespace CCQ.Save
{
    /// <summary>
    /// JSON payload for ccq_run. Written only between events, never mid-fight —
    /// reloading mid-fight restores the pre-fight state by design.
    /// </summary>
    [Serializable]
    public class RunSaveData
    {
        public int level;
        public int xp;
        public int maxHp;
        public int hp;
        public float atk;
        public int def;
        public float crit;
        public float critMult;
        public float lifesteal;
        public float thorns;
        public string[] sidekickIds;

        public int starCycle;
        public int round;
        public int planet;
        public int nonBattleStreak;
        public int speedIdx;

        public int hits;
        public int crits;
        public long damageDealt;
        public int maxHit;
        public int elitesSlain;
        public int bossesSlain;
    }
}
