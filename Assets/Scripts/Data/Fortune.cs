using Game.Localization;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Fortune", fileName = "Fortune")]
    public class Fortune : ScriptableObject
    {
        [Tooltip("Localization term key — the text lives in CCQ_Localization.csv.")]
        [SerializeField] private string _nameKey;
        [SerializeField] private Sprite _icon;
        [SerializeField] private StatMod[] _mods;

        public string DisplayName => Loc.Get(_nameKey);
        public Sprite Icon => _icon;
        public StatMod[] Mods => _mods;
    }
}
