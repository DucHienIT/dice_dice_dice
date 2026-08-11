using CCQ.Data;
using UnityEngine;

namespace CCQ.Enemies
{
    /// <summary>Builds enemies from the spec scaling formulas and rolls their procedural look.</summary>
    public class EnemyFactory
    {
        private readonly GameConfig _config;
        private readonly NarrativeConfig _narrative;

        public EnemyFactory(GameConfig config, NarrativeConfig narrative)
        {
            _config = config;
            _narrative = narrative;
        }

        public EnemyState Create(int g, EnemyKind kind, int planetIndex)
        {
            int hp = _config.EnemyHp(g);
            int atk = _config.EnemyAtk(g);
            int def = _config.EnemyDef(g);
            int xp = _config.EnemyXp(g);
            string name;

            if (kind == EnemyKind.Boss)
            {
                Vector3 m = _config.BossMult;
                hp = Mathf.RoundToInt(hp * m.x);
                atk = Mathf.RoundToInt(atk * m.y);
                xp = Mathf.RoundToInt(xp * m.z);
                string[] bosses = _narrative.BossNames;
                name = bosses[planetIndex % bosses.Length];
            }
            else if (kind == EnemyKind.Elite)
            {
                Vector3 m = _config.EliteMult;
                hp = Mathf.RoundToInt(hp * m.x);
                atk = Mathf.RoundToInt(atk * m.y);
                xp = Mathf.RoundToInt(xp * m.z);
                name = "Irradiated " + NarrativeConfig.Pick(_narrative.CritterNames);
            }
            else
            {
                name = NarrativeConfig.Pick(_narrative.CritterNames);
            }

            return new EnemyState
            {
                Name = name,
                Kind = kind,
                MaxHp = hp,
                Hp = hp,
                Atk = atk,
                Def = def,
                Xp = xp,
                Look = RollLook(kind)
            };
        }

        private CritterLook RollLook(EnemyKind kind)
        {
            return new CritterLook
            {
                ColorIndex = Random.Range(0, _config.CritterColors.Length),
                Eyes = kind == EnemyKind.Boss ? 3 : Random.Range(1, 4),
                Horns = Random.value < 0.5f,
                Spots = Random.value < 0.6f,
                Size = kind == EnemyKind.Boss ? 1.45f :
                       kind == EnemyKind.Elite ? 1.2f : Random.Range(0.85f, 1.05f)
            };
        }
    }
}
