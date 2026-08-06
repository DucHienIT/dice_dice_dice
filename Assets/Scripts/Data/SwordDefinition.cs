using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Sword wave slashing the enemies closest to the wall; hits more targets per rarity (spec 9.2).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Items/Sword", fileName = "Sword")]
    public class SwordDefinition : CombatItemDefinition
    {
        [SerializeField] private int _baseTargets = 1;
        [SerializeField] private float _baseCritChance = 0.05f;
        [SerializeField] private float _critMultiplier = 2f;
        [SerializeField] private float _critExplodeRadius = 0.5f;
        [SerializeField] private float _critExplodeDamageFactor = 0.5f;

        private static readonly List<Enemy> TargetBuffer = new List<Enemy>(8);

        public override bool Fire(CombatContext ctx, ItemRarity rarity, Vector2 origin)
        {
            int count = _baseTargets + (int)rarity;
            ctx.Enemies.CollectFrontmost(count, TargetBuffer);
            if (TargetBuffer.Count == 0)
            {
                return false;
            }

            float damage = EffectiveDamage(rarity, ctx);
            for (int i = 0; i < TargetBuffer.Count; i++)
            {
                Enemy enemy = TargetBuffer[i];
                bool crit = RollCrit(ctx, _baseCritChance);
                Vector2 pos = enemy.Position;
                ctx.Enemies.DamageEnemy(enemy, damage * (crit ? _critMultiplier : 1f), DamageType, DisplayName);
                ctx.Effects.SpawnSlash(pos);
                if (crit)
                {
                    ctx.Effects.SpawnCritLabel(pos);
                    if (ctx.Mods.CritExplode)
                    {
                        ctx.Enemies.AoeDamage(pos, _critExplodeRadius, damage * _critExplodeDamageFactor, DamageType, DisplayName, 0f, 0f);
                    }
                }
            }

            ctx.Audio.Play(Sfx.Shoot);
            return true;
        }
    }
}
