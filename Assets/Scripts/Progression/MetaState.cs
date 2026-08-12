using CCQ.Core;
using CCQ.Data;
using UnityEngine;

namespace CCQ.Progression
{
    /// <summary>
    /// Progress that outlives a run: star shards earned per voyage and the Star Forge ranks
    /// bought with them. Ranks are re-applied to every new hero, so a lost run still moves
    /// the player forward. Pure logic — persisted by SaveSystem under ccq_meta.
    /// </summary>
    public class MetaState
    {
        public int Shards;
        public int LifetimeShards;

        /// <summary>Owned ranks, index-aligned with GameConfig.MetaUpgrades.</summary>
        public readonly int[] Ranks;

        public MetaState(int trackCount)
        {
            Ranks = new int[Mathf.Max(0, trackCount)];
        }

        public void Grant(int amount)
        {
            if (amount <= 0) return;
            Shards += amount;
            LifetimeShards += amount;
        }

        public bool IsMaxed(GameConfig config, int index) =>
            Ranks[index] >= config.MetaUpgrades[index].MaxRank;

        /// <summary>False while the node's prerequisite is not raised far enough.</summary>
        public bool IsUnlocked(GameConfig config, int index)
        {
            MetaUpgrade required = config.MetaUpgrades[index].Requires;
            if (required == null) return true;
            int parent = IndexOf(config, required);
            return parent >= 0 &&
                   Ranks[parent] >= config.MetaUpgrades[index].RequiredRank;
        }

        public static int IndexOf(GameConfig config, MetaUpgrade upgrade)
        {
            MetaUpgrade[] tracks = config.MetaUpgrades;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i] == upgrade) return i;
            }
            return -1;
        }

        /// <summary>Price of the next rank, or -1 when the track is maxed.</summary>
        public int CostOf(GameConfig config, int index) =>
            IsMaxed(config, index) ? -1 : config.MetaUpgrades[index].CostAt(Ranks[index]);

        public bool CanAfford(GameConfig config, int index)
        {
            int cost = CostOf(config, index);
            return cost >= 0 && Shards >= cost && IsUnlocked(config, index);
        }

        /// <summary>Spends the shards and adds one rank. False when locked, maxed or too poor.</summary>
        public bool Buy(GameConfig config, int index)
        {
            if (!CanAfford(config, index)) return false;
            Shards -= CostOf(config, index);
            Ranks[index]++;
            return true;
        }

        /// <summary>Drives the nav bar's Forge badge: is anything buyable right now?</summary>
        public bool HasAffordableStep(GameConfig config)
        {
            for (int i = 0; i < Ranks.Length && i < config.MetaUpgrades.Length; i++)
            {
                if (CanAfford(config, i)) return true;
            }
            return false;
        }

        /// <summary>Applies every owned rank to a freshly created hero, then tops HP up.</summary>
        public void ApplyTo(GameConfig config, PlayerState player)
        {
            MetaUpgrade[] tracks = config.MetaUpgrades;
            if (tracks == null) return;
            for (int i = 0; i < tracks.Length && i < Ranks.Length; i++)
            {
                for (int rank = 0; rank < Ranks[i]; rank++)
                {
                    StatModApplier.Apply(player, tracks[i].ModsPerRank);
                }
            }
            player.Hp = player.MaxHp;
        }

        /// <summary>Shards cashed out when a voyage ends — depth plus elite/boss kills.</summary>
        public static int ShardsForRun(GameConfig config, RunState run) =>
            Mathf.FloorToInt(run.Score(config) * config.ShardsPerRound) +
            run.Stats.ElitesSlain * config.ShardsPerElite +
            run.Stats.BossesSlain * config.ShardsPerBoss;

        /// <summary>Immediate reward for clearing a planet, scaled by its 1-based number.</summary>
        public static int ShardsForPlanetClear(GameConfig config, int planetNumber) =>
            config.ShardsPerPlanetClear * Mathf.Max(1, planetNumber);
    }
}
