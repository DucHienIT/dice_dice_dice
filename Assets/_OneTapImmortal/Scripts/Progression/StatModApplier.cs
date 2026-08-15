using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// The single interpreter for StatMod data — upgrades and fortunes are pure data,
    /// new cards need no code change. This switch exists in exactly one place.
    /// </summary>
    public static class StatModApplier
    {
        public static void Apply(PlayerState player, StatMod[] mods)
        {
            if (mods == null) return;
            for (int i = 0; i < mods.Length; i++) Apply(player, mods[i]);
        }

        public static void Apply(PlayerState player, StatMod mod)
        {
            switch (mod.Type)
            {
                case StatModType.MaxHpPct:
                    int add = Mathf.RoundToInt(player.MaxHp * mod.Value);
                    player.MaxHp += add;
                    player.Hp += add;
                    break;
                case StatModType.AtkPct:
                    player.Atk = Mathf.Round(player.Atk * (1f + mod.Value) * 10f) / 10f;
                    break;
                case StatModType.DefFlat:
                    player.Def += Mathf.RoundToInt(mod.Value);
                    break;
                case StatModType.CritChance:
                    player.Crit += mod.Value;
                    break;
                case StatModType.Lifesteal:
                    player.Lifesteal += mod.Value;
                    break;
                case StatModType.Thorns:
                    player.Thorns += mod.Value;
                    break;
                case StatModType.HealNowPct:
                    player.HealPct(mod.Value);
                    break;
                case StatModType.MaxHpMult:
                    player.MaxHp = Mathf.RoundToInt(player.MaxHp * mod.Value);
                    player.Hp = Mathf.Min(player.Hp, player.MaxHp);
                    break;
            }
        }
    }
}
