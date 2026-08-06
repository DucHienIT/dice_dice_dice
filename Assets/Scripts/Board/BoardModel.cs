using System;

namespace DiceDiceDice
{
    /// <summary>
    /// Pure-logic 8-slot board (2 columns x 4 rows). The slot limit is a hard spec rule (section 32.3/32.19)
    /// and is deliberately a constant, not config.
    /// </summary>
    public class BoardModel
    {
        public const int Columns = 2;
        public const int Rows = 4;
        public const int SlotCount = Columns * Rows;

        private readonly ItemInstance[] _slots = new ItemInstance[SlotCount];

        public event Action BoardChanged;

        /// <summary>Raised with the target slot after a successful merge (spec section 11).</summary>
        public event Action<int, ItemInstance> MergedAt;

        public ItemInstance Get(int index)
        {
            return _slots[index];
        }

        public bool IsFull
        {
            get
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if (_slots[i] == null)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public int FirstEmpty()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Place(int index, ItemInstance item)
        {
            _slots[index] = item;
            BoardChanged?.Invoke();
        }

        public void RemoveAt(int index)
        {
            _slots[index] = null;
            BoardChanged?.Invoke();
        }

        /// <summary>
        /// Drag one slot onto another: move to empty, merge same type + same rarity (never random results),
        /// otherwise swap. Rearranging is allowed during waves (spec 5.1 step 8).
        /// </summary>
        public BoardMoveResult MoveOrMerge(int from, int to)
        {
            if (from == to || _slots[from] == null)
            {
                return BoardMoveResult.None;
            }

            ItemInstance source = _slots[from];
            ItemInstance target = _slots[to];

            if (target == null)
            {
                _slots[to] = source;
                _slots[from] = null;
                BoardChanged?.Invoke();
                return BoardMoveResult.Moved;
            }

            bool sameKind = ReferenceEquals(target.Definition, source.Definition) && target.Rarity == source.Rarity;
            if (sameKind && source.Rarity == ItemRarity.Legendary)
            {
                return BoardMoveResult.MaxRarity;
            }

            if (sameKind)
            {
                var merged = new ItemInstance(source.Definition, source.Rarity + 1);
                _slots[to] = merged;
                _slots[from] = null;
                BoardChanged?.Invoke();
                MergedAt?.Invoke(to, merged);
                return BoardMoveResult.Merged;
            }

            _slots[to] = source;
            _slots[from] = target;
            BoardChanged?.Invoke();
            return BoardMoveResult.Swapped;
        }
    }
}
