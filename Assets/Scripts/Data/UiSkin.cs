using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>
    /// All state-dependent UI sprites (Layer Lab GUI Pro skin), so views can swap sprites at runtime
    /// (rarity slot frames, group card frames, mute icon) without referencing asset paths in code.
    /// Static skin sprites are authored directly on the scene UI by the installer.
    /// </summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/UI Skin", fileName = "UiSkin")]
    public class UiSkin : ScriptableObject
    {
        [Header("Board slot frames (index = ItemRarity)")]
        [SerializeField] private Sprite[] _itemFrameByRarity = new Sprite[4];
        [SerializeField] private Sprite _itemFrameEmpty;
        [SerializeField] private Sprite _itemFrameFocus;

        [Header("Upgrade card frames (index = ItemGroup)")]
        [SerializeField] private Sprite[] _cardBgByGroup = new Sprite[5];
        [SerializeField] private Sprite[] _cardBorderByGroup = new Sprite[5];

        [Header("HUD")]
        [SerializeField] private Sprite _soundOnIcon;
        [SerializeField] private Sprite _soundOffIcon;
        [SerializeField] private Sprite _coinIcon;

        public Sprite ItemFrame(ItemRarity rarity) => _itemFrameByRarity[(int)rarity];
        public Sprite ItemFrameEmpty => _itemFrameEmpty;
        public Sprite ItemFrameFocus => _itemFrameFocus;
        public Sprite CardBg(ItemGroup group) => _cardBgByGroup[(int)group];
        public Sprite CardBorder(ItemGroup group) => _cardBorderByGroup[(int)group];
        public Sprite SoundOnIcon => _soundOnIcon;
        public Sprite SoundOffIcon => _soundOffIcon;
        public Sprite CoinIcon => _coinIcon;

#if UNITY_EDITOR
        public void EditorSetup(Sprite[] itemFrameByRarity, Sprite itemFrameEmpty, Sprite itemFrameFocus,
            Sprite[] cardBgByGroup, Sprite[] cardBorderByGroup, Sprite soundOnIcon, Sprite soundOffIcon, Sprite coinIcon)
        {
            _itemFrameByRarity = itemFrameByRarity;
            _itemFrameEmpty = itemFrameEmpty;
            _itemFrameFocus = itemFrameFocus;
            _cardBgByGroup = cardBgByGroup;
            _cardBorderByGroup = cardBorderByGroup;
            _soundOnIcon = soundOnIcon;
            _soundOffIcon = soundOffIcon;
            _coinIcon = coinIcon;
        }
#endif
    }
}
