using Game.Data;
using Game.Localization;
using UnityEngine;

namespace Game.Enemies
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

        public EnemyState Create(int g, EnemyKind kind, int worldIndex)
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
                string[] bosses = _narrative.BossNameKeys;
                name = Loc.Get(bosses[worldIndex % bosses.Length]);
            }
            else if (kind == EnemyKind.Elite)
            {
                Vector3 m = _config.EliteMult;
                hp = Mathf.RoundToInt(hp * m.x);
                atk = Mathf.RoundToInt(atk * m.y);
                xp = Mathf.RoundToInt(xp * m.z);
                name = Loc.Get(LocKeys.ElitePrefix)
                    .Replace("{e}", Loc.Pick(_narrative.EnemyNameKeys));
            }
            else
            {
                name = Loc.Pick(_narrative.EnemyNameKeys);
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

        private EnemyLook RollLook(EnemyKind kind)
        {
            return new EnemyLook
            {
                ColorIndex = Random.Range(0, _config.EnemyColors.Length),
                Eyes = kind == EnemyKind.Boss ? 3 : Random.Range(1, 4),
                Horns = Random.value < 0.5f,
                Spots = Random.value < 0.6f,
                Size = kind == EnemyKind.Boss ? 1.45f :
                       kind == EnemyKind.Elite ? 1.2f : Random.Range(0.85f, 1.05f)
            };
        }
    }
}
