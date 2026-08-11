namespace CCQ.Combat
{
    public enum BattleActor
    {
        Hero,
        Enemy
    }

    /// <summary>One landed hit, passed by value to listeners — no boxing, no allocation.</summary>
    public readonly struct HitInfo
    {
        public readonly BattleActor Target;
        public readonly int Damage;
        public readonly bool Crit;
        public readonly bool TargetDied;

        public HitInfo(BattleActor target, int damage, bool crit, bool targetDied)
        {
            Target = target;
            Damage = damage;
            Crit = crit;
            TargetDied = targetDied;
        }
    }
}
