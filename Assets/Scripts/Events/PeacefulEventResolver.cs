using CCQ.Core;
using CCQ.Data;
using CCQ.Progression;
using CCQ.Sidekicks;
using CCQ.Utils;
using UnityEngine;

namespace CCQ.Events
{
    /// <summary>
    /// Resolves every non-battle event into an EventOutcome. Pure logic:
    /// mutates run state, returns display data, never touches UI.
    /// </summary>
    public class PeacefulEventResolver
    {
        private readonly GameConfig _config;
        private readonly NarrativeConfig _narrative;

        public PeacefulEventResolver(GameConfig config, NarrativeConfig narrative)
        {
            _config = config;
            _narrative = narrative;
        }

        public EventOutcome ResolveFortune(RunState run)
        {
            Fortune[] pool = _config.Fortunes;
            Fortune f = pool[Random.Range(0, pool.Length)];
            StatModApplier.Apply(run.Player, f.Mods);
            return new EventOutcome
            {
                Text = NarrativeConfig.Pick(_narrative.FortuneTexts),
                BannerIcon = f.Icon,
                BannerTitle = f.DisplayName,
                BannerTag = "Small fortune"
            };
        }

        public EventOutcome ResolveSpring(RunState run)
        {
            Vector2 range = _config.SpringHeal;
            int heal = run.Player.HealPct(Random.Range(range.x, range.y));
            string text = NarrativeConfig.Pick(_narrative.SpringTexts)
                .Replace("{heal}", NumberStrings.Get(heal));
            return new EventOutcome { Text = text, HeroHpDelta = heal };
        }

        public EventOutcome ResolveTrap(RunState run)
        {
            Vector2 range = _config.TrapDmg;
            int dmg = Mathf.RoundToInt(run.Player.MaxHp * Random.Range(range.x, range.y));
            run.Player.TakeDamage(dmg, 1); // traps never kill — floor at 1 HP
            string text = NarrativeConfig.Pick(_narrative.TrapTexts)
                .Replace("{dmg}", NumberStrings.Get(dmg));
            return new EventOutcome { Text = text, HeroHpDelta = -dmg };
        }

        public EventOutcome ResolveTreasure(RunState run)
        {
            Vector2 range = _config.TreasureXp;
            int g = run.GlobalRound(_config, run.Round + 1);
            int xp = Mathf.RoundToInt(_config.EnemyXp(g) * Random.Range(range.x, range.y));
            int ups = XpSystem.GrantXp(_config, run.Player, xp);
            string text = NarrativeConfig.Pick(_narrative.TreasureTexts)
                .Replace("{xp}", NumberStrings.Get(xp));
            if (ups > 0)
            {
                text += _narrative.LevelUpSuffix.Replace("{lv}", NumberStrings.Get(run.Player.Level));
            }
            return new EventOutcome { Text = text, LevelUps = ups };
        }

        /// <summary>
        /// Sidekick event. If the pod has room the critter joins immediately; when full,
        /// the outcome carries PendingSidekick — per the spec, adopting a 4th always
        /// means choosing one to release (or declining for a snack).
        /// </summary>
        public EventOutcome ResolveSidekick(RunState run)
        {
            Sidekick rolled = SidekickRoster.RollNew(_config, run.Player);
            if (rolled == null)
            {
                return ResolveSnack(run); // owns every kind already
            }

            if (run.Player.Sidekicks.Count >= _config.MaxSidekicks)
            {
                string offer = NarrativeConfig.Pick(_narrative.SidekickTexts)
                    .Replace("{s}", rolled.DisplayName);
                return new EventOutcome
                {
                    Text = offer + " But your pod is full — make room, or send it off with a snack?",
                    PendingSidekick = rolled
                };
            }

            run.Player.Sidekicks.Add(rolled);
            string text = NarrativeConfig.Pick(_narrative.SidekickTexts)
                    .Replace("{s}", rolled.DisplayName)
                + " <b>" + rolled.DisplayName + "</b> " + rolled.Description + ".";
            return new EventOutcome
            {
                Text = text,
                BannerIcon = rolled.Icon,
                BannerTitle = rolled.DisplayName + " joins!",
                BannerTag = "New sidekick"
            };
        }

        public EventOutcome ResolveSnack(RunState run)
        {
            int heal = run.Player.HealPct(_config.SnackHeal);
            string text = NarrativeConfig.Pick(_narrative.SidekickFullTexts)
                .Replace("{heal}", NumberStrings.Get(heal));
            return new EventOutcome { Text = text, HeroHpDelta = heal };
        }

        /// <summary>Swap: release owned sidekick at index, adopt the pending one.</summary>
        public EventOutcome ResolveSwap(RunState run, Sidekick incoming, int releaseIndex)
        {
            Sidekick released = run.Player.Sidekicks[releaseIndex];
            run.Player.Sidekicks[releaseIndex] = incoming;
            return new EventOutcome
            {
                Text = "*" + released.DisplayName + "* waves goodbye and drifts into the flora. <b>" +
                       incoming.DisplayName + "</b> " + incoming.Description + ".",
                BannerIcon = incoming.Icon,
                BannerTitle = incoming.DisplayName + " joins!",
                BannerTag = "Sidekick swap"
            };
        }

        private int[] _choiceIndices;

        /// <summary>Rolls 3 distinct upgrade cards for a choice event (partial Fisher-Yates).</summary>
        public void RollChoices(UpgradeCard[] buffer3)
        {
            UpgradeCard[] pool = _config.Upgrades;
            int n = pool.Length;
            if (_choiceIndices == null || _choiceIndices.Length < n) _choiceIndices = new int[n];
            for (int i = 0; i < n; i++) _choiceIndices[i] = i;
            for (int i = 0; i < 3; i++)
            {
                int j = Random.Range(i, n);
                (_choiceIndices[i], _choiceIndices[j]) = (_choiceIndices[j], _choiceIndices[i]);
                buffer3[i] = pool[_choiceIndices[i]];
            }
        }
    }
}
