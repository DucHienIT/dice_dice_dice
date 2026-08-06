using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Base data for every purchasable item (spec section 27.2). Behavior lives in subclasses.</summary>
    public abstract class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private ItemGroup _group;
        [SerializeField] private int _price;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private ItemIcon _icon;
        [SerializeField] private float _shopWeight = 1f;

        public string DisplayName => _displayName;
        public ItemGroup Group => _group;
        public int Price => _price;
        public string Description => _description;
        public ItemIcon Icon => _icon;
        public float ShopWeight => _shopWeight;

        public int SellPrice(ItemRarity rarity, RunModifiers mods)
        {
            return Mathf.Max(1, Mathf.FloorToInt(_price * RarityMath.Multiplier(rarity) * mods.SellRate));
        }

        /// <summary>Extra stats line for the info panel, formatted per item type.</summary>
        public abstract string DescribeStats(ItemRarity rarity, CombatContext ctx);

#if UNITY_EDITOR
        public void EditorSetup(string displayName, ItemGroup group, int price, string description, ItemIcon icon, float shopWeight)
        {
            _displayName = displayName;
            _group = group;
            _price = price;
            _description = description;
            _icon = icon;
            _shopWeight = shopWeight;
        }
#endif
    }
}
