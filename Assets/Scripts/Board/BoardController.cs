using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Scene-facing board API: placing purchases, drag move/merge, selling. Phase rules enforced here.</summary>
    public class BoardController : MonoBehaviour
    {
        [SerializeField] private GameManager _game;
        [SerializeField] private EconomyController _economy;
        [SerializeField] private AudioManager _audio;

        private readonly BoardModel _model = new BoardModel();
        private RunStats _stats;
        private RunModifiers _mods;

        public BoardModel Model => _model;

        public void Init(RunStats stats, RunModifiers mods)
        {
            _stats = stats;
            _mods = mods;
        }

        public bool TryPlaceNew(ItemDefinition definition)
        {
            int slot = _model.FirstEmpty();
            if (slot < 0)
            {
                return false;
            }
            _model.Place(slot, new ItemInstance(definition, ItemRarity.Common));
            return true;
        }

        public BoardMoveResult RequestMove(int from, int to)
        {
            if (_game.Phase == GamePhase.GameOver)
            {
                return BoardMoveResult.None;
            }

            ItemInstance source = _model.Get(from);
            BoardMoveResult result = _model.MoveOrMerge(from, to);
            if (result == BoardMoveResult.Merged)
            {
                _stats.Merges++;
                _audio.Play(Sfx.Merge);
                if (source.Definition is DiceDefinition && _mods.MergeDiceGold > 0)
                {
                    _economy.AddGold(_mods.MergeDiceGold);
                    _game.NotifyGoldPopup(to, _mods.MergeDiceGold);
                }
            }
            return result;
        }

        /// <summary>Selling is a shop action: only during the shopping phase (spec section 13).</summary>
        public bool TrySell(int index)
        {
            if (_game.Phase != GamePhase.Shopping)
            {
                return false;
            }
            ItemInstance item = _model.Get(index);
            if (item == null)
            {
                return false;
            }
            _economy.AddGold(item.Definition.SellPrice(item.Rarity, _mods));
            _model.RemoveAt(index);
            _audio.Play(Sfx.Coin);
            return true;
        }

        /// <summary>Total wave-start shield from Shield items on the board (spec 9.5).</summary>
        public int TotalShieldItems()
        {
            int total = 0;
            for (int i = 0; i < BoardModel.SlotCount; i++)
            {
                ItemInstance item = _model.Get(i);
                if (item == null)
                {
                    continue;
                }
                var shield = item.Definition as ShieldDefinition;
                if (shield != null)
                {
                    total += shield.ShieldFor(item.Rarity);
                }
            }
            return total;
        }
    }
}
