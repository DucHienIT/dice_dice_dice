using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>XP grants and level-ups per the spec curve (xpNeed = 16 × lv^1.55).</summary>
    public static class XpSystem
    {
        /// <summary>Grants XP, applies level-ups. Returns levels gained.</summary>
        public static int GrantXp(GameConfig config, PlayerState player, int amount)
        {
            player.Xp += amount;
            int ups = 0;
            while (player.Xp >= config.XpNeed(player.Level))
            {
                player.Xp -= config.XpNeed(player.Level);
                player.Level++;
                ups++;
                player.MaxHp = Mathf.RoundToInt(player.MaxHp * (1f + config.LevelHpPct));
                player.Atk = Mathf.Round(player.Atk * (1f + config.LevelAtkPct) * 10f) / 10f;
                player.Def += config.LevelDefFlat;
                player.HealPct(config.LevelHealPct);
            }
            return ups;
        }
    }
}
