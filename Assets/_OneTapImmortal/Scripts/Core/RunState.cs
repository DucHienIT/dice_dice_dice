using Game.Data;
using Game.Progression;

namespace Game.Core
{
    /// <summary>Whole-run state: hero + progress counters. Cycle = one ENGAGE tap.</summary>
    public class RunState
    {
        public PlayerState Player;
        public int Cycle;
        public int Round;
        public int WorldIndex;
        public int NonBattleStreak;
        public int SpeedIndex;
        public RunStats Stats = new RunStats();

        /// <summary>
        /// A fresh run. Star MetaPath ranks are baked into the hero here — the only place
        /// meta progression touches a run, so a loaded save is never boosted twice.
        /// </summary>
        public static RunState CreateNew(GameConfig config, MetaState meta, int keepSpeedIndex = 0)
        {
            PlayerState player = PlayerState.CreateNew(config);
            meta?.ApplyTo(config, player);
            return new RunState
            {
                Player = player,
                Cycle = 0,
                Round = 0,
                WorldIndex = 0,
                NonBattleStreak = 0,
                SpeedIndex = keepSpeedIndex,
                Stats = new RunStats()
            };
        }

        /// <summary>Global difficulty g = world×roundsPerWorld + round.</summary>
        public int GlobalRound(GameConfig config, int round) =>
            WorldIndex * config.RoundsPerWorld + round;

        public int Score(GameConfig config) => GlobalRound(config, Round);
    }
}
