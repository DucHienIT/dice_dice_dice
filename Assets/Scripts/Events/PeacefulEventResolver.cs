using Game.Core;
using Game.Data;
using Game.Localization;
using Game.Progression;
using Game.Sidekicks;
using Game.Utils;
using UnityEngine;

namespace Game.Events
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
                Line = LocLine.Of(NarrativeConfig.PickKey(_narrative.FortuneKeys)),
                BannerIcon = f.Icon,
                BannerTitle = f.DisplayName,
                BannerTag = Loc.Get(LocKeys.BannerSmallFortune)
            };
        }

        public EventOutcome ResolveSpring(RunState run)
        {
            Vector2 range = _config.SpringHeal;
            int heal = run.Player.HealPct(Random.Range(range.x, range.y));
            LocLine line = LocLine.Of(NarrativeConfig.PickKey(_narrative.SpringKeys))
                .With("{heal}", NumberStrings.Get(heal));
            return new EventOutcome { Line = line, HeroHpDelta = heal };
        }

        public EventOutcome ResolveTrap(RunState run)
        {
            Vector2 range = _config.TrapDmg;
            int dmg = Mathf.RoundToInt(run.Player.MaxHp * Random.Range(range.x, range.y));
            run.Player.TakeDamage(dmg, 1); // traps never kill — floor at 1 HP
            LocLine line = LocLine.Of(NarrativeConfig.PickKey(_narrative.TrapKeys))
                .With("{dmg}", NumberStrings.Get(dmg));
            return new EventOutcome { Line = line, HeroHpDelta = -dmg };
        }

        public EventOutcome ResolveTreasure(RunState run)
        {
            Vector2 range = _config.TreasureXp;
            int g = run.GlobalRound(_config, run.Round + 1);
            int xp = Mathf.RoundToInt(_config.EnemyXp(g) * Random.Range(range.x, range.y));
            int ups = XpSystem.GrantXp(_config, run.Player, xp);
            var outcome = new EventOutcome
            {
                Line = LocLine.Of(NarrativeConfig.PickKey(_narrative.TreasureKeys))
                    .With("{xp}", NumberStrings.Get(xp)),
                LevelUps = ups
            };
            if (ups > 0)
            {
                outcome.Suffix = LocLine.Of(_narrative.LevelUpSuffixKey)
                    .With("{lv}", NumberStrings.Get(run.Player.Level))
                    .With("{realm}", Loc.Get(_narrative.RealmKey(run.Player.Level)));
            }
            return outcome;
        }

        /// <summary>
        /// Sidekick event. If the pod has room the enemy joins immediately; when full,
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

            LocLine intro = LocLine.Of(NarrativeConfig.PickKey(_narrative.SidekickKeys))
                .With("{s}", rolled.DisplayName);

            if (run.Player.Sidekicks.Count >= _config.MaxSidekicks)
            {
                return new EventOutcome
                {
                    Line = intro,
                    Suffix = LocLine.Of(LocKeys.SidekickPodFull),
                    PendingSidekick = rolled
                };
            }

            run.Player.Sidekicks.Add(rolled);
            return new EventOutcome
            {
                Line = intro,
                Suffix = LocLine.Of(LocKeys.SidekickJoin)
                    .With("{s}", rolled.DisplayName)
                    .With("{d}", rolled.Description),
                BannerIcon = rolled.Icon,
                BannerTitle = Loc.Get(LocKeys.BannerSidekickJoins).Replace("{s}", rolled.DisplayName),
                BannerTag = Loc.Get(LocKeys.BannerNewSidekick)
            };
        }

        public EventOutcome ResolveSnack(RunState run)
        {
            int heal = run.Player.HealPct(_config.SnackHeal);
            LocLine line = LocLine.Of(NarrativeConfig.PickKey(_narrative.SidekickFullKeys))
                .With("{heal}", NumberStrings.Get(heal));
            return new EventOutcome { Line = line, HeroHpDelta = heal };
        }

        /// <summary>Swap: release owned sidekick at index, adopt the pending one.</summary>
        public EventOutcome ResolveSwap(RunState run, Sidekick incoming, int releaseIndex)
        {
            Sidekick released = run.Player.Sidekicks[releaseIndex];
            run.Player.Sidekicks[releaseIndex] = incoming;
            return new EventOutcome
            {
                Line = LocLine.Of(LocKeys.SidekickSwap)
                    .With("{r}", released.DisplayName)
                    .With("{s}", incoming.DisplayName)
                    .With("{d}", incoming.Description),
                BannerIcon = incoming.Icon,
                BannerTitle = Loc.Get(LocKeys.BannerSidekickJoins).Replace("{s}", incoming.DisplayName),
                BannerTag = Loc.Get(LocKeys.BannerSidekickSwap)
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
