using System;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>The wall the enemies attack. Run ends when HP hits 0 (spec 6.1). Shield absorbs first (spec 9.5).</summary>
    public class BaseWall : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;

        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public int Shield { get; private set; }

        public event Action HpChanged;
        public event Action WallDestroyed;

        public void Init()
        {
            MaxHp = _config.WallMaxHp;
            Hp = MaxHp;
            Shield = 0;
            HpChanged?.Invoke();
        }

        public void TakeDamage(int amount)
        {
            if (Hp <= 0)
            {
                return;
            }
            if (Shield > 0)
            {
                int absorbed = Mathf.Min(Shield, amount);
                Shield -= absorbed;
                amount -= absorbed;
            }
            Hp = Mathf.Max(0, Hp - amount);
            HpChanged?.Invoke();
            if (Hp == 0)
            {
                WallDestroyed?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            Hp = Mathf.Min(MaxHp, Hp + amount);
            HpChanged?.Invoke();
        }

        public void AddMaxHp(int amount)
        {
            MaxHp += amount;
            Hp += amount;
            HpChanged?.Invoke();
        }

        public void SetShield(int amount)
        {
            Shield = amount;
            HpChanged?.Invoke();
        }
    }
}
