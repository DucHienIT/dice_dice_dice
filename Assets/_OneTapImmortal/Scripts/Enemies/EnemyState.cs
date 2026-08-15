using System;

namespace Game.Enemies
{
    public enum EnemyKind
    {
        Normal,
        Elite,
        Boss
    }

    /// <summary>Procedural look parameters — same data, different face.</summary>
    [Serializable]
    public struct EnemyLook
    {
        public int ColorIndex;
        public int Eyes;
        public bool Horns;
        public bool Spots;
        public float Size;
    }

    /// <summary>Pure enemy state for one battle.</summary>
    public class EnemyState
    {
        public string Name;
        public EnemyKind Kind;
        public int MaxHp;
        public int Hp;
        public int Atk;
        public int Def;
        public int Xp;
        public EnemyLook Look;

        public bool IsDead => Hp <= 0;

        public void TakeDamage(int amount)
        {
            Hp -= amount;
            if (Hp < 0) Hp = 0;
        }
    }
}
