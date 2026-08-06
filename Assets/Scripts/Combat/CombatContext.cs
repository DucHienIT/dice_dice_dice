using System;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Everything an item's Fire needs, assembled once by GameManager. Keeps definitions free of scene references.</summary>
    public class CombatContext
    {
        public GameConfig Config;
        public EnemyManager Enemies;
        public ProjectileManager Projectiles;
        public EffectManager Effects;
        public AudioManager Audio;
        public RunModifiers Mods;
        public AuraService Auras;
        public RunStats Stats;
        public Action<string> ShowBanner;
    }
}
