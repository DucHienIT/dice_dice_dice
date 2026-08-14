using Game.Localization;
using Game.Sidekicks;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Sidekick", fileName = "Sidekick")]
    public class Sidekick : ScriptableObject
    {
        [SerializeField] private string _id;
        [Tooltip("Localization term keys — the text lives in CCQ_Localization.csv.")]
        [SerializeField] private string _nameKey;
        [SerializeField] private string _descriptionKey;
        [SerializeField] private Sprite _icon;
        [Tooltip("Battle orb baked from the color below by Tools > Game > Build Game (Full).")]
        [SerializeField] private Sprite _orbSprite;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private SidekickType _type;
        [SerializeField] private float _value;

        public string Id => _id;
        public string DisplayName => Loc.Get(_nameKey);
        public string Description => Loc.Get(_descriptionKey);
        public Sprite Icon => _icon;
        public Sprite OrbSprite => _orbSprite;
        public Color Color => _color;
        public SidekickType Type => _type;
        public float Value => _value;
    }
}
