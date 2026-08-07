using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>
    /// Single tick loop for every board item: dice roll timers (waves only, progress carries over — spec 8.1)
    /// and combat cooldowns. Called from GameManager.Update; items own no Update.
    /// </summary>
    public class ItemTicker : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private GameManager _game;
        [SerializeField] private BoardController _board;
        [SerializeField] private EconomyController _economy;
        [SerializeField] private AudioManager _audio;

        private struct PendingCast
        {
            public CombatItemDefinition Definition;
            public ItemRarity Rarity;
            public Vector2 Origin;
            public float Delay;
        }

        private const float DoubleCastDelay = 0.2f;

        private readonly List<PendingCast> _pendingCasts = new List<PendingCast>(4);
        private CombatContext _ctx;
        private RunStats _stats;
        private RunModifiers _mods;

        public event Action<int> DiceRollStarted;
        public event Action<int, int, int> DiceRolled;
        public event Action<int> ItemFired;

        public void Init(CombatContext ctx, RunStats stats, RunModifiers mods)
        {
            _ctx = ctx;
            _stats = stats;
            _mods = mods;
        }

        public void Tick(float deltaTime)
        {
            bool waveActive = _game.Phase == GamePhase.Wave;
            BoardModel model = _board.Model;

            for (int slot = 0; slot < BoardModel.SlotCount; slot++)
            {
                ItemInstance item = model.Get(slot);
                if (item == null)
                {
                    continue;
                }

                var dice = item.Definition as DiceDefinition;
                if (dice != null)
                {
                    TickDice(slot, item, dice, deltaTime, waveActive);
                    continue;
                }

                var combat = item.Definition as CombatItemDefinition;
                if (combat != null)
                {
                    TickCombatItem(slot, item, combat, deltaTime, waveActive);
                }
            }

            TickPendingCasts(deltaTime, waveActive);
        }

        public float GetProgress(int slot)
        {
            ItemInstance item = _board.Model.Get(slot);
            if (item == null)
            {
                return 0f;
            }
            var dice = item.Definition as DiceDefinition;
            if (dice != null)
            {
                float interval = dice.RollInterval(item.Rarity, _mods, _ctx.Auras.SpeedMultiplier);
                return Mathf.Clamp01(item.Timer / interval);
            }
            var combat = item.Definition as CombatItemDefinition;
            if (combat != null)
            {
                return Mathf.Clamp01(item.Timer / combat.EffectiveCooldown(item.Rarity, _ctx));
            }
            return 0f;
        }

        private void TickDice(int slot, ItemInstance item, DiceDefinition dice, float deltaTime, bool waveActive)
        {
            // A roll already in motion finishes even if the wave just ended.
            if (item.RollAnimTimer > 0f)
            {
                item.RollAnimTimer -= deltaTime;
                if (item.RollAnimTimer <= 0f)
                {
                    ResolveRoll(slot, item, dice);
                }
                return;
            }

            if (!waveActive)
            {
                return;
            }

            item.Timer += deltaTime;
            float interval = dice.RollInterval(item.Rarity, _mods, _ctx.Auras.SpeedMultiplier);
            if (item.Timer >= interval)
            {
                item.RollAnimTimer = _config.RollAnimDuration;
                _audio.Play(Sfx.Roll);
                DiceRollStarted?.Invoke(slot);
            }
        }

private void ResolveRoll(int slot, ItemInstance item, DiceDefinition dice)
        {
            item.Timer = 0f;
            int totalGold = RollOnce(item, dice, out int face);
            item.LastDiceFace = face;
            if (_mods.DoubleRollChance > 0f && UnityEngine.Random.value < _mods.DoubleRollChance)
            {
                totalGold += RollOnce(item, dice, out _);
            }
            _stats.DiceGold += totalGold;
            _economy.AddGold(totalGold);
            _audio.Play(Sfx.Thud);
            DiceRolled?.Invoke(slot, face, totalGold);
        }

        private int RollOnce(ItemInstance item, DiceDefinition dice, out int face)
        {
            face = UnityEngine.Random.Range(dice.MinFace(item.Rarity, _mods), 7);
            _stats.Rolls++;
            if (face == 6)
            {
                _stats.Sixes++;
            }
            return dice.GoldFor(face, item.Rarity, _mods);
        }

        private void TickCombatItem(int slot, ItemInstance item, CombatItemDefinition combat, float deltaTime, bool waveActive)
        {
            item.Timer += deltaTime;
            if (!waveActive)
            {
                return;
            }
            float cooldown = combat.EffectiveCooldown(item.Rarity, _ctx);
            if (item.Timer < cooldown)
            {
                return;
            }
            Vector2 origin = _config.SlotWorldPosition(slot);
            if (!combat.Fire(_ctx, item.Rarity, origin))
            {
                return;
            }
            item.Timer = 0f;
            ItemFired?.Invoke(slot);
            if (combat.Group == ItemGroup.Magic && UnityEngine.Random.value < _mods.DoubleCastChance)
            {
                PendingCast cast;
                cast.Definition = combat;
                cast.Rarity = item.Rarity;
                cast.Origin = origin;
                cast.Delay = DoubleCastDelay;
                _pendingCasts.Add(cast);
            }
        }

        private void TickPendingCasts(float deltaTime, bool waveActive)
        {
            for (int i = _pendingCasts.Count - 1; i >= 0; i--)
            {
                PendingCast cast = _pendingCasts[i];
                cast.Delay -= deltaTime;
                if (cast.Delay > 0f)
                {
                    _pendingCasts[i] = cast;
                    continue;
                }
                _pendingCasts[i] = _pendingCasts[_pendingCasts.Count - 1];
                _pendingCasts.RemoveAt(_pendingCasts.Count - 1);
                if (waveActive)
                {
                    cast.Definition.Fire(_ctx, cast.Rarity, cast.Origin);
                }
            }
        }
    }
}
