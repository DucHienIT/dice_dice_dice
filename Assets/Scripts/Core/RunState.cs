using CCQ.Data;
using CCQ.Progression;

namespace CCQ.Core
{
    /// <summary>Whole-run state: hero + progress counters. StarCycle = one ENGAGE tap.</summary>
    public class RunState
    {
        public PlayerState Player;
        public int StarCycle;
        public int Round;
        public int PlanetIndex;
        public int NonBattleStreak;
        public int SpeedIndex;
        public RunStats Stats = new RunStats();

        /// <summary>
        /// A fresh voyage. Star Forge ranks are baked into the hero here — the only place
        /// meta progression touches a run, so a loaded save is never boosted twice.
        /// </summary>
        public static RunState CreateNew(GameConfig config, MetaState meta, int keepSpeedIndex = 0)
        {
            PlayerState player = PlayerState.CreateNew(config);
            meta?.ApplyTo(config, player);
            return new RunState
            {
                Player = player,
                StarCycle = 0,
                Round = 0,
                PlanetIndex = 0,
                NonBattleStreak = 0,
                SpeedIndex = keepSpeedIndex,
                Stats = new RunStats()
            };
        }

        /// <summary>Global difficulty g = planet×roundsPerPlanet + round.</summary>
        public int GlobalRound(GameConfig config, int round) =>
            PlanetIndex * config.RoundsPerPlanet + round;

        public int Score(GameConfig config) => GlobalRound(config, Round);
    }
}
