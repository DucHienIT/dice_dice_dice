using System;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Gold, XP and level-up bookkeeping (spec section 7). UI listens via events.</summary>
    public class EconomyController : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;

        public int Gold { get; private set; }
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int PendingLevelUps { get; private set; }

        public event Action GoldChanged;
        public event Action XpChanged;
        public event Action LevelUpQueued;

        public int XpNeeded => _config.XpNeededFor(Level);

        public void Init()
        {
            Gold = _config.StartGold;
            GoldChanged?.Invoke();
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            GoldChanged?.Invoke();
        }

        public bool TrySpend(int amount)
        {
            if (Gold < amount)
            {
                return false;
            }
            Gold -= amount;
            GoldChanged?.Invoke();
            return true;
        }

        public void GainXp(int amount)
        {
            Xp += amount;
            bool leveled = false;
            while (Xp >= XpNeeded)
            {
                Xp -= XpNeeded;
                Level++;
                PendingLevelUps++;
                leveled = true;
            }
            XpChanged?.Invoke();
            if (leveled)
            {
                LevelUpQueued?.Invoke();
            }
        }

        public void ConsumePendingLevelUp()
        {
            PendingLevelUps = Mathf.Max(0, PendingLevelUps - 1);
        }
    }
}
