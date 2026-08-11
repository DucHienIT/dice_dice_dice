using CCQ.Data;
using UnityEngine;

namespace CCQ.Events
{
    /// <summary>
    /// Weighted event roll with the anti-boredom guard: past MAX_NONBATTLE_STREAK
    /// consecutive non-battle events, a battle is forced (effective battle rate ~55-60%).
    /// </summary>
    public class EventRoller
    {
        private readonly GameConfig _config;

        public EventRoller(GameConfig config)
        {
            _config = config;
        }

        public EventType Roll(int nonBattleStreak)
        {
            if (nonBattleStreak >= _config.MaxNonBattleStreak) return EventType.Battle;

            EventWeights w = _config.Weights;
            float total = w.Battle + w.Fortune + w.Choice + w.Spring + w.Sidekick + w.Trap + w.Treasure;
            float r = Random.value * total;

            if ((r -= w.Battle) < 0f) return EventType.Battle;
            if ((r -= w.Fortune) < 0f) return EventType.Fortune;
            if ((r -= w.Choice) < 0f) return EventType.Choice;
            if ((r -= w.Spring) < 0f) return EventType.Spring;
            if ((r -= w.Sidekick) < 0f) return EventType.Sidekick;
            if ((r -= w.Trap) < 0f) return EventType.Trap;
            if ((r - w.Treasure) < 0f) return EventType.Treasure;
            return EventType.Battle;
        }
    }
}
