using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>One roguelike level-up choice (spec section 18). Effect is data: stat + operation + value.</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Upgrade", fileName = "Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private ItemGroup _group;
        [SerializeField] private UpgradeStat _stat;
        [SerializeField] private UpgradeOperation _operation;
        [SerializeField] private float _value;
        [SerializeField] private bool _once;

        public string DisplayName => _displayName;
        public string Description => _description;
        public ItemGroup Group => _group;
        public UpgradeStat Stat => _stat;
        public UpgradeOperation Operation => _operation;
        public float Value => _value;
        public bool Once => _once;

#if UNITY_EDITOR
        public void EditorSetup(string displayName, string description, ItemGroup group,
            UpgradeStat stat, UpgradeOperation operation, float value, bool once)
        {
            _displayName = displayName;
            _description = description;
            _group = group;
            _stat = stat;
            _operation = operation;
            _value = value;
            _once = once;
        }
#endif
    }
}
