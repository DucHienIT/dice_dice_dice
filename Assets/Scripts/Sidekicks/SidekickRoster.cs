using Game.Core;
using Game.Data;

namespace Game.Sidekicks
{
    /// <summary>Aggregates passive sidekick bonuses. No LINQ — battle-beat safe.</summary>
    public static class SidekickRoster
    {
        public static float BonusFor(PlayerState player, SidekickType type)
        {
            float total = 0f;
            var list = player.Sidekicks;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Type == type) total += list[i].Value;
            }
            return total;
        }

        public static bool Owns(PlayerState player, Sidekick sidekick)
        {
            var list = player.Sidekicks;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == sidekick) return true;
            }
            return false;
        }

        /// <summary>Picks a random not-yet-owned sidekick; null if the player owns them all.</summary>
        public static Sidekick RollNew(GameConfig config, PlayerState player)
        {
            int available = 0;
            var all = config.Sidekicks;
            for (int i = 0; i < all.Length; i++)
            {
                if (!Owns(player, all[i])) available++;
            }
            if (available == 0) return null;
            int pick = UnityEngine.Random.Range(0, available);
            for (int i = 0; i < all.Length; i++)
            {
                if (Owns(player, all[i])) continue;
                if (pick == 0) return all[i];
                pick--;
            }
            return null;
        }
    }
}
