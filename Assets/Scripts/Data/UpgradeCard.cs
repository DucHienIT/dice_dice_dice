using UnityEngine;

namespace CCQ.Data
{
    [CreateAssetMenu(menuName = "CCQ/Upgrade Card", fileName = "UpgradeCard")]
    public class UpgradeCard : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private StatMod[] _mods;

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public StatMod[] Mods => _mods;
    }
}
