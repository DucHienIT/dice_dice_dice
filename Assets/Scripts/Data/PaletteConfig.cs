using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Every color used by the game, editable without touching code (mood: dark board-game table, bright toy pieces).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Palette Config", fileName = "PaletteConfig")]
    public class PaletteConfig : ScriptableObject
    {
        [Header("Item groups (index = ItemGroup)")]
        [SerializeField] private Color[] _groupColors =
        {
            new Color(0.96f, 0.77f, 0.26f), // Economy - gold
            new Color(1.00f, 0.42f, 0.29f), // Weapon - orange red
            new Color(0.56f, 0.42f, 1.00f), // Magic - violet
            new Color(0.30f, 0.69f, 0.31f), // Support - green
            new Color(0.20f, 0.79f, 0.84f)  // Defense - cyan
        };

        [Header("Rarities (index = ItemRarity)")]
        [SerializeField] private Color[] _rarityColors =
        {
            new Color(0.72f, 0.75f, 0.80f), // Common - grey white
            new Color(0.30f, 0.64f, 1.00f), // Rare - blue
            new Color(0.73f, 0.42f, 1.00f), // Epic - purple
            new Color(1.00f, 0.72f, 0.19f)  // Legendary - amber
        };

        [Header("HUD")]
        [SerializeField] private Color _gold = new Color(0.96f, 0.77f, 0.26f);
        [SerializeField] private Color _hp = new Color(1.00f, 0.35f, 0.35f);
        [SerializeField] private Color _shield = new Color(0.20f, 0.79f, 0.84f);
        [SerializeField] private Color _xp = new Color(0.48f, 0.36f, 1.00f);
        [SerializeField] private Color _crit = new Color(1.00f, 0.35f, 0.35f);

        [Header("World")]
        [SerializeField] private Color _wall = new Color(0.18f, 0.66f, 0.88f);
        [SerializeField] private Color _wallDark = new Color(0.11f, 0.50f, 0.69f);
        [SerializeField] private Color _ground = new Color(0.17f, 0.20f, 0.31f);
        [SerializeField] private Color _backgroundTop = new Color(0.09f, 0.11f, 0.18f);
        [SerializeField] private Color _backgroundBottom = new Color(0.15f, 0.19f, 0.31f);

        [Header("UI panels")]
        [SerializeField] private Color _panel = new Color(0.12f, 0.14f, 0.20f, 0.93f);
        [SerializeField] private Color _panelLight = new Color(0.15f, 0.17f, 0.25f, 1f);
        [SerializeField] private Color _panelBorder = new Color(0.20f, 0.24f, 0.33f, 1f);
        [SerializeField] private Color _textMain = new Color(0.91f, 0.93f, 0.96f);
        [SerializeField] private Color _textDim = new Color(0.60f, 0.64f, 0.74f);
        [SerializeField] private Color _emptySlot = new Color(0.09f, 0.10f, 0.16f, 0.80f);
        [SerializeField] private Color _emptySlotBorder = new Color(0.31f, 0.23f, 0.23f, 1f);
        [SerializeField] private Color _buttonPrimary = new Color(0.23f, 0.43f, 0.94f);

        public Color GroupColor(ItemGroup group) => _groupColors[(int)group];
        public Color RarityColor(ItemRarity rarity) => _rarityColors[(int)rarity];
        public Color Gold => _gold;
        public Color Hp => _hp;
        public Color Shield => _shield;
        public Color Xp => _xp;
        public Color Crit => _crit;
        public Color Wall => _wall;
        public Color WallDark => _wallDark;
        public Color Ground => _ground;
        public Color BackgroundTop => _backgroundTop;
        public Color BackgroundBottom => _backgroundBottom;
        public Color Panel => _panel;
        public Color PanelLight => _panelLight;
        public Color PanelBorder => _panelBorder;
        public Color TextMain => _textMain;
        public Color TextDim => _textDim;
        public Color EmptySlot => _emptySlot;
        public Color EmptySlotBorder => _emptySlotBorder;
        public Color ButtonPrimary => _buttonPrimary;

#if UNITY_EDITOR
        public void EditorSetTextColors(Color textMain, Color textDim)
        {
            _textMain = textMain;
            _textDim = textDim;
        }
#endif
    }
}
