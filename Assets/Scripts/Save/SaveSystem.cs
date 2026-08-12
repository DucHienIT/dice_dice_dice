using CCQ.Core;
using CCQ.Data;
using CCQ.Progression;
using UnityEngine;

namespace CCQ.Save
{
    /// <summary>PlayerPrefs persistence with the ccq_ namespace prefix.</summary>
    public static class SaveSystem
    {
        private const string RunKey = "ccq_run";
        private const string BestKey = "ccq_best";
        private const string MetaKey = "ccq_meta";
        private const string MusicKey = "ccq_music";
        private const string SfxKey = "ccq_sfx";

        public static void SaveRun(RunState run)
        {
            PlayerState p = run.Player;
            var data = new RunSaveData
            {
                level = p.Level,
                xp = p.Xp,
                maxHp = p.MaxHp,
                hp = p.Hp,
                atk = p.Atk,
                def = p.Def,
                crit = p.Crit,
                critMult = p.CritMult,
                lifesteal = p.Lifesteal,
                thorns = p.Thorns,
                sidekickIds = new string[p.Sidekicks.Count],
                starCycle = run.StarCycle,
                round = run.Round,
                planet = run.PlanetIndex,
                nonBattleStreak = run.NonBattleStreak,
                speedIdx = run.SpeedIndex,
                hits = run.Stats.Hits,
                crits = run.Stats.Crits,
                damageDealt = run.Stats.DamageDealt,
                maxHit = run.Stats.MaxHit,
                elitesSlain = run.Stats.ElitesSlain,
                bossesSlain = run.Stats.BossesSlain
            };
            for (int i = 0; i < p.Sidekicks.Count; i++)
            {
                data.sidekickIds[i] = p.Sidekicks[i].Id;
            }
            PlayerPrefs.SetString(RunKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static RunState TryLoadRun(GameConfig config)
        {
            string raw = PlayerPrefs.GetString(RunKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return null;

            RunSaveData data;
            try
            {
                data = JsonUtility.FromJson<RunSaveData>(raw);
            }
            catch
            {
                return null;
            }
            if (data == null || data.maxHp <= 0) return null;

            var run = new RunState
            {
                Player = new PlayerState
                {
                    Level = Mathf.Max(1, data.level),
                    Xp = data.xp,
                    MaxHp = data.maxHp,
                    Hp = Mathf.Clamp(data.hp, 1, data.maxHp),
                    Atk = data.atk,
                    Def = data.def,
                    Crit = data.crit,
                    CritMult = data.critMult,
                    Lifesteal = data.lifesteal,
                    Thorns = data.thorns
                },
                StarCycle = data.starCycle,
                Round = data.round,
                PlanetIndex = data.planet,
                NonBattleStreak = data.nonBattleStreak,
                SpeedIndex = Mathf.Clamp(data.speedIdx, 0, config.Speeds.Length - 1),
                Stats = new RunStats
                {
                    Hits = data.hits,
                    Crits = data.crits,
                    DamageDealt = data.damageDealt,
                    MaxHit = data.maxHit,
                    ElitesSlain = data.elitesSlain,
                    BossesSlain = data.bossesSlain
                }
            };

            if (data.sidekickIds != null)
            {
                for (int i = 0; i < data.sidekickIds.Length; i++)
                {
                    Sidekick found = FindSidekick(config, data.sidekickIds[i]);
                    if (found != null) run.Player.Sidekicks.Add(found);
                }
            }
            return run;
        }

        private static Sidekick FindSidekick(GameConfig config, string id)
        {
            Sidekick[] all = config.Sidekicks;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Id == id) return all[i];
            }
            return null;
        }

        public static void ClearRun()
        {
            PlayerPrefs.DeleteKey(RunKey);
            PlayerPrefs.Save();
        }

        public static BestSaveData LoadBest()
        {
            string raw = PlayerPrefs.GetString(BestKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return null;
            try
            {
                BestSaveData best = JsonUtility.FromJson<BestSaveData>(raw);
                return best != null && best.score > 0 ? best : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Overwrites best only when the score is higher. Returns true if it was a new best.</summary>
        public static bool SaveBestIfHigher(GameConfig config, RunState run)
        {
            BestSaveData best = LoadBest();
            int score = run.Score(config);
            if (best != null && score <= best.score) return false;
            var data = new BestSaveData
            {
                score = score,
                planet = run.PlanetIndex + 1,
                round = run.Round,
                lv = run.Player.Level
            };
            PlayerPrefs.SetString(BestKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            return true;
        }

        public static void ClearBest()
        {
            PlayerPrefs.DeleteKey(BestKey);
            PlayerPrefs.Save();
        }

        // ---------------- meta progression (ccq_meta) ----------------

        public static void SaveMeta(GameConfig config, MetaState meta)
        {
            MetaUpgrade[] tracks = config.MetaUpgrades;
            var data = new MetaSaveData
            {
                shards = meta.Shards,
                lifetime = meta.LifetimeShards,
                upgradeIds = new string[tracks.Length],
                ranks = new int[tracks.Length]
            };
            for (int i = 0; i < tracks.Length && i < meta.Ranks.Length; i++)
            {
                data.upgradeIds[i] = tracks[i].Id;
                data.ranks[i] = meta.Ranks[i];
            }
            PlayerPrefs.SetString(MetaKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        /// <summary>Never null — an empty forge when nothing has been saved yet.</summary>
        public static MetaState LoadMeta(GameConfig config)
        {
            var meta = new MetaState(config.MetaUpgrades.Length);
            string raw = PlayerPrefs.GetString(MetaKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return meta;

            MetaSaveData data;
            try
            {
                data = JsonUtility.FromJson<MetaSaveData>(raw);
            }
            catch
            {
                return meta;
            }
            if (data == null) return meta;

            meta.Shards = Mathf.Max(0, data.shards);
            meta.LifetimeShards = Mathf.Max(meta.Shards, data.lifetime);
            if (data.upgradeIds == null || data.ranks == null) return meta;
            // matched by id, so reordering or inserting a track never re-assigns ranks
            for (int i = 0; i < data.upgradeIds.Length && i < data.ranks.Length; i++)
            {
                int index = IndexOfMetaUpgrade(config, data.upgradeIds[i]);
                if (index < 0) continue;
                meta.Ranks[index] =
                    Mathf.Clamp(data.ranks[i], 0, config.MetaUpgrades[index].MaxRank);
            }
            return meta;
        }

        public static void ClearMeta()
        {
            PlayerPrefs.DeleteKey(MetaKey);
            PlayerPrefs.Save();
        }

        private static int IndexOfMetaUpgrade(GameConfig config, string id)
        {
            MetaUpgrade[] tracks = config.MetaUpgrades;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].Id == id) return i;
            }
            return -1;
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
            set { PlayerPrefs.SetInt(SfxKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
