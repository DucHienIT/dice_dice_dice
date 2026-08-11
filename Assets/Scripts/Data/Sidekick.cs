using CCQ.Sidekicks;
using UnityEngine;

namespace CCQ.Data
{
    [CreateAssetMenu(menuName = "CCQ/Sidekick", fileName = "Sidekick")]
    public class Sidekick : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [Tooltip("Battle orb baked from the color below by Tools > CCQ > Build Game (Full).")]
        [SerializeField] private Sprite _orbSprite;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private SidekickType _type;
        [SerializeField] private float _value;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Sprite OrbSprite => _orbSprite;
        public Color Color => _color;
        public SidekickType Type => _type;
        public float Value => _value;
    }
}
