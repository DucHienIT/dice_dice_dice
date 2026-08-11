using UnityEngine;

namespace CCQ.Data
{
    [CreateAssetMenu(menuName = "CCQ/Planet", fileName = "Planet")]
    public class Planet : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Color _skyTop;
        [SerializeField] private Color _skyBottom;
        [SerializeField] private Color _lake;
        [SerializeField] private Color _lakeDeep;
        [SerializeField] private Color _ground;
        [SerializeField] private Color _rock;
        [SerializeField] private Color _moon;
        [SerializeField] private Color[] _flora;
        [Header("Art — baked from the palette above by Tools > CCQ > Build Game (Full)")]
        [Tooltip("Static far layer: sky gradient, nebula, stars, moon.")]
        [SerializeField] private Sprite _skyLayer;
        [Tooltip("Scrolling strip: ground, rocks, lake, flora. Tiles seamlessly.")]
        [SerializeField] private Sprite _groundLayer;
        [Header("Music")]
        [SerializeField] private float _musicRootHz = 110f;
        [SerializeField] private bool _minorMood;

        public string DisplayName => _displayName;
        public Color SkyTop => _skyTop;
        public Color SkyBottom => _skyBottom;
        public Color Lake => _lake;
        public Color LakeDeep => _lakeDeep;
        public Color Ground => _ground;
        public Color Rock => _rock;
        public Color Moon => _moon;
        public Color[] Flora => _flora;
        public Sprite SkyLayer => _skyLayer;
        public Sprite GroundLayer => _groundLayer;
        public float MusicRootHz => _musicRootHz;
        public bool MinorMood => _minorMood;
    }
}
