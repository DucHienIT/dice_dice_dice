using System;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Buy / reroll / lock logic. Only operates during the shopping phase (spec sections 12, 32.21).</summary>
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private ItemDefinition[] _itemPool;
        [SerializeField] private GameManager _game;
        [SerializeField] private EconomyController _economy;
        [SerializeField] private BoardController _board;
        [SerializeField] private AudioManager _audio;

        public class ShopOffer
        {
            public ItemDefinition Definition;
            public bool Sold;
        }

        private ShopOffer[] _offers;
        private RunStats _stats;
        private RunModifiers _mods;

        public int RerollCost { get; private set; }
        public int FreeRerollsLeft { get; private set; }
        public bool Locked { get; private set; }

        public event Action OffersChanged;
        public event Action<string> ShopMessage;

        public int OfferCount => _offers.Length;

        public ShopOffer GetOffer(int index)
        {
            return _offers[index];
        }

        public int CurrentRerollCost => FreeRerollsLeft > 0 ? 0 : RerollCost;

        public void Init(RunStats stats, RunModifiers mods)
        {
            _stats = stats;
            _mods = mods;
            _offers = new ShopOffer[_config.ShopSlotCount];
            for (int i = 0; i < _offers.Length; i++)
            {
                _offers[i] = new ShopOffer();
            }
            RerollCost = _config.RerollBaseCost;
        }

        /// <summary>The first shop always contains at least one Dice (spec 12.5).</summary>
        public void Roll(bool guaranteeDice)
        {
            for (int i = 0; i < _offers.Length; i++)
            {
                _offers[i].Definition = PickWeighted();
                _offers[i].Sold = false;
            }
            if (guaranteeDice && !HasDiceOffer())
            {
                _offers[0].Definition = FindDice();
            }
            OffersChanged?.Invoke();
        }

        public void Buy(int index)
        {
            if (_game.Phase != GamePhase.Shopping)
            {
                Reject("Chỉ mua được giữa các wave!");
                return;
            }
            ShopOffer offer = _offers[index];
            if (offer.Sold || offer.Definition == null)
            {
                return;
            }
            if (_economy.Gold < offer.Definition.Price)
            {
                Reject("Không đủ vàng!");
                return;
            }
            if (_board.Model.IsFull)
            {
                Reject("Bảng đã đầy 8 ô! Hãy merge hoặc bán bớt item.");
                return;
            }

            _economy.TrySpend(offer.Definition.Price);
            _board.TryPlaceNew(offer.Definition);
            offer.Sold = true;
            _stats.ItemsBought++;
            _audio.Play(Sfx.Buy);
            OffersChanged?.Invoke();
        }

        public void Reroll()
        {
            if (_game.Phase != GamePhase.Shopping)
            {
                return;
            }
            int cost = CurrentRerollCost;
            if (_economy.Gold < cost)
            {
                Reject("Không đủ vàng để reroll!");
                return;
            }
            if (FreeRerollsLeft > 0)
            {
                FreeRerollsLeft--;
            }
            else
            {
                _economy.TrySpend(cost);
                RerollCost += _config.RerollCostIncrement;
            }
            Locked = false;
            _audio.Play(Sfx.Buy);
            Roll(false);
        }

        public void ToggleLock()
        {
            Locked = !Locked;
            OffersChanged?.Invoke();
        }

        /// <summary>Reroll price resets each wave; free rerolls granted by upgrades (spec 12.3, 18.5).</summary>
        public void OnWaveStarted()
        {
            RerollCost = _config.RerollBaseCost;
            FreeRerollsLeft = _mods.FreeRerollPerWave;
        }

        /// <summary>Lock keeps the current list through one refresh (spec 12.4).</summary>
        public void OnWaveEnded()
        {
            if (Locked)
            {
                Locked = false;
                OffersChanged?.Invoke();
            }
            else
            {
                Roll(false);
            }
        }

        private void Reject(string message)
        {
            _audio.Play(Sfx.Error);
            ShopMessage?.Invoke(message);
        }

        private bool HasDiceOffer()
        {
            for (int i = 0; i < _offers.Length; i++)
            {
                if (_offers[i].Definition is DiceDefinition)
                {
                    return true;
                }
            }
            return false;
        }

        private ItemDefinition FindDice()
        {
            for (int i = 0; i < _itemPool.Length; i++)
            {
                if (_itemPool[i] is DiceDefinition)
                {
                    return _itemPool[i];
                }
            }
            return _itemPool[0];
        }

        private float WeightFor(ItemDefinition definition)
        {
            if (definition is DiceDefinition)
            {
                return _game.Wave < _config.EarlyDiceWaveThreshold
                    ? _config.DiceWeightEarly
                    : _config.DiceWeightLate;
            }
            return definition.ShopWeight;
        }

        private ItemDefinition PickWeighted()
        {
            float total = 0f;
            for (int i = 0; i < _itemPool.Length; i++)
            {
                total += WeightFor(_itemPool[i]);
            }
            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < _itemPool.Length; i++)
            {
                roll -= WeightFor(_itemPool[i]);
                if (roll <= 0f)
                {
                    return _itemPool[i];
                }
            }
            return _itemPool[_itemPool.Length - 1];
        }
    }
}
