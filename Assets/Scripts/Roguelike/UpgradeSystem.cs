using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Rolls three distinct level-up choices and applies the picked one to RunModifiers (spec section 18).</summary>
    public class UpgradeSystem : MonoBehaviour
    {
        [SerializeField] private UpgradeDefinition[] _pool;
        [SerializeField] private BaseWall _wall;
        [SerializeField] private AuraService _auras;

        private readonly HashSet<UpgradeDefinition> _takenOnce = new HashSet<UpgradeDefinition>();
        private readonly List<UpgradeDefinition> _rollBuffer = new List<UpgradeDefinition>(32);
        private RunModifiers _mods;
        private RunStats _stats;

        public void Init(RunModifiers mods, RunStats stats)
        {
            _mods = mods;
            _stats = stats;
        }

        public void RollChoices(List<UpgradeDefinition> results)
        {
            results.Clear();
            _rollBuffer.Clear();
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_takenOnce.Contains(_pool[i]))
                {
                    _rollBuffer.Add(_pool[i]);
                }
            }
            int want = Mathf.Min(3, _rollBuffer.Count);
            for (int i = 0; i < want; i++)
            {
                int pick = Random.Range(0, _rollBuffer.Count);
                results.Add(_rollBuffer[pick]);
                _rollBuffer[pick] = _rollBuffer[_rollBuffer.Count - 1];
                _rollBuffer.RemoveAt(_rollBuffer.Count - 1);
            }
        }

        public void Apply(UpgradeDefinition upgrade)
        {
            _stats.ChosenUpgrades.Add(upgrade.DisplayName);
            if (upgrade.Once)
            {
                _takenOnce.Add(upgrade);
            }

            float value = upgrade.Value;
            switch (upgrade.Stat)
            {
                case UpgradeStat.DiceSpeed: _mods.DiceSpeed = Op(_mods.DiceSpeed, upgrade); break;
                case UpgradeStat.DiceMinFace: _mods.DiceMinFace = (int)Op(_mods.DiceMinFace, upgrade); break;
                case UpgradeStat.SixBonusGold: _mods.SixBonusGold = (int)Op(_mods.SixBonusGold, upgrade); break;
                case UpgradeStat.DoubleRollChance: _mods.DoubleRollChance = Op(_mods.DoubleRollChance, upgrade); break;
                case UpgradeStat.MergeDiceGold: _mods.MergeDiceGold = (int)Op(_mods.MergeDiceGold, upgrade); break;
                case UpgradeStat.Interest: _mods.Interest = Op(_mods.Interest, upgrade); break;
                case UpgradeStat.WaveEndGold: _mods.WaveEndGold = (int)Op(_mods.WaveEndGold, upgrade); break;
                case UpgradeStat.FreeRerollPerWave: _mods.FreeRerollPerWave = (int)Op(_mods.FreeRerollPerWave, upgrade); break;
                case UpgradeStat.SellRate: _mods.SellRate = Op(_mods.SellRate, upgrade); break;
                case UpgradeStat.PhysicalDamage: _mods.PhysicalDamage = Op(_mods.PhysicalDamage, upgrade); break;
                case UpgradeStat.AttackSpeed: _mods.AttackSpeed = Op(_mods.AttackSpeed, upgrade); break;
                case UpgradeStat.CritChance: _mods.CritChance = Op(_mods.CritChance, upgrade); break;
                case UpgradeStat.Pierce: _mods.Pierce = (int)Op(_mods.Pierce, upgrade); break;
                case UpgradeStat.CritExplode: _mods.CritExplode = true; break;
                case UpgradeStat.MagicDamage: _mods.MagicDamage = Op(_mods.MagicDamage, upgrade); break;
                case UpgradeStat.MagicCooldown: _mods.MagicCooldown = Op(_mods.MagicCooldown, upgrade); break;
                case UpgradeStat.DotDuration: _mods.DotDuration = Op(_mods.DotDuration, upgrade); break;
                case UpgradeStat.DoubleCastChance: _mods.DoubleCastChance = Op(_mods.DoubleCastChance, upgrade); break;
                case UpgradeStat.SupportPower: _mods.SupportPower = Op(_mods.SupportPower, upgrade); break;
                case UpgradeStat.WallMaxHp: _wall.AddMaxHp((int)value); break;
                case UpgradeStat.HealPerWave: _mods.HealPerWave = (int)Op(_mods.HealPerWave, upgrade); break;
                case UpgradeStat.WaveShield: _mods.WaveShield = (int)Op(_mods.WaveShield, upgrade); break;
            }

            _auras.Recompute();
        }

        private static float Op(float current, UpgradeDefinition upgrade)
        {
            switch (upgrade.Operation)
            {
                case UpgradeOperation.Multiply: return current * upgrade.Value;
                case UpgradeOperation.Set: return upgrade.Value;
                default: return current + upgrade.Value;
            }
        }
    }
}
