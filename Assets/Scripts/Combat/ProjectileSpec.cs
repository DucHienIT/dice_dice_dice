using UnityEngine;

namespace DiceDiceDice
{
    public enum ProjectileVisual
    {
        Arrow = 0,
        Bolt = 1,
        Shell = 2,
        Meteor = 3,
        Frost = 4
    }

    /// <summary>Spawn parameters for one projectile. Struct: no allocation per shot.</summary>
    public struct ProjectileSpec
    {
        public ProjectileVisual Visual;
        public Vector2 Origin;
        public Enemy Target;
        public float Speed;
        public float Damage;
        public DamageType DamageType;
        public string SourceName;
        public int Pierce;
        public bool Crit;
        public float AoeRadius;
        public float BurnDps;
        public float BurnDuration;
        public float SlowDuration;
        public float FreezeDuration;
                public float LaunchDelay;
public bool MeteorFall;
    }
}
