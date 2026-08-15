using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Pure hero run-state. No Unity scene dependency — testable, serializable by hand.</summary>
    public class PlayerState
    {
        public int Level = 1;
        public int Xp;
        public int MaxHp;
        public int Hp;
        public float Atk;
        public int Def;
        public float Crit;
        public float CritMult;
        public float Lifesteal;
        public float Thorns;
        public readonly List<Sidekick> Sidekicks = new List<Sidekick>(4);

        public static PlayerState CreateNew(GameConfig config)
        {
            return new PlayerState
            {
                Level = 1,
                Xp = 0,
                MaxHp = config.PlayerHp,
                Hp = config.PlayerHp,
                Atk = config.PlayerAtk,
                Def = config.PlayerDef,
                Crit = config.PlayerCrit,
                CritMult = config.PlayerCritMult,
                Lifesteal = 0f,
                Thorns = 0f
            };
        }

        public bool IsDead => Hp <= 0;

        /// <summary>Returns actual amount healed.</summary>
        public int Heal(int amount)
        {
            int before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + Mathf.Max(0, amount));
            return Hp - before;
        }

        public int HealPct(float pct) => Heal(Mathf.RoundToInt(MaxHp * pct));

        public void TakeDamage(int amount, int floorHp = 0)
        {
            Hp = Mathf.Max(floorHp, Hp - Mathf.Max(0, amount));
        }
    }
}
