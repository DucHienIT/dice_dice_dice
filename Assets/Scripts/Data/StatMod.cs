using System;

namespace Game.Data
{
    public enum StatModType
    {
        MaxHpPct,      // maxHp += maxHp * value (hp grows with it)
        AtkPct,        // atk *= 1 + value
        DefFlat,       // def += value
        CritChance,    // crit += value
        Lifesteal,     // lifesteal += value
        Thorns,        // thorns += value
        HealNowPct,    // hp += maxHp * value (clamped)
        MaxHpMult      // maxHp *= value (Unstable Core trade-off)
    }

    [Serializable]
    public struct StatMod
    {
        public StatModType Type;
        public float Value;

        public StatMod(StatModType type, float value)
        {
            Type = type;
            Value = value;
        }
    }
}
