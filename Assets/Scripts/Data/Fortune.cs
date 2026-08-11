using UnityEngine;

namespace CCQ.Data
{
    [CreateAssetMenu(menuName = "CCQ/Fortune", fileName = "Fortune")]
    public class Fortune : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private StatMod[] _mods;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public StatMod[] Mods => _mods;
    }
}
