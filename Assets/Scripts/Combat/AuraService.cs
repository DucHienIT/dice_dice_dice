using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Caches the combined Support-item auras (Anvil/Hourglass); recomputed when the board changes.</summary>
    public class AuraService : MonoBehaviour
    {
        [SerializeField] private BoardController _board;

        private RunModifiers _mods;

        public float PhysicalMultiplier { get; private set; } = 1f;
        public float SpeedMultiplier { get; private set; } = 1f;

        public void Init(RunModifiers mods)
        {
            _mods = mods;
            _board.Model.BoardChanged += Recompute;
            Recompute();
        }

        public void Recompute()
        {
            float physical = 0f;
            float speed = 0f;
            BoardModel model = _board.Model;
            for (int i = 0; i < BoardModel.SlotCount; i++)
            {
                ItemInstance item = model.Get(i);
                if (item == null)
                {
                    continue;
                }
                var support = item.Definition as SupportDefinition;
                if (support == null)
                {
                    continue;
                }
                physical += support.PhysicalAura(item.Rarity, _mods);
                speed += support.SpeedAura(item.Rarity, _mods);
            }
            PhysicalMultiplier = 1f + physical;
            SpeedMultiplier = 1f + speed;
        }
    }
}
