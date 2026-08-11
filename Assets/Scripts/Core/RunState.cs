using CCQ.Data;

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

        public static RunState CreateNew(GameConfig config, int keepSpeedIndex = 0)
        {
            return new RunState
            {
                Player = PlayerState.CreateNew(config),
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
