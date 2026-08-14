using Game.Localization;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Upgrade Card", fileName = "UpgradeCard")]
    public class UpgradeCard : ScriptableObject
    {
        [Tooltip("Localization term keys — the text lives in CCQ_Localization.csv.")]
        [SerializeField] private string _nameKey;
        [SerializeField] private string _descriptionKey;
        [SerializeField] private Sprite _icon;
        [SerializeField] private StatMod[] _mods;

        public string DisplayName => Loc.Get(_nameKey);
        public string Description => Loc.Get(_descriptionKey);
        public Sprite Icon => _icon;
        public StatMod[] Mods => _mods;
    }
}
