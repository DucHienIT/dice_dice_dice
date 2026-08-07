using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Balance data for one enemy type (spec sections 16-17). Visuals come from the
    /// FantasyMonsters pack via the per-type Enemy prefab referenced here.</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Enemy", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private float _maxHp = 22f;
        [SerializeField] private float _speed = 0.55f;
        [SerializeField] private int _contactDamage = 10;
        [SerializeField] private int _xpReward = 3;
        [SerializeField] private float _radius = 0.18f;
        [SerializeField] private Color _bodyColor = Color.white;
        [SerializeField] private Enemy _prefab;
        [SerializeField, Min(1)] private int _poolSize = 8;
        [SerializeField, Range(0f, 1f)] private float _physicalResist;
        [SerializeField, Range(0f, 1f)] private float _magicResist;
        [SerializeField] private bool _isHealer;
        [SerializeField] private bool _isElite;
        [SerializeField] private bool _isBoss;
        [SerializeField] private float _armor;

        public string DisplayName => _displayName;
        public float MaxHp => _maxHp;
        public float Speed => _speed;
        public int ContactDamage => _contactDamage;
        public int XpReward => _xpReward;
        public float Radius => _radius;
        public Color BodyColor => _bodyColor;
        public Enemy Prefab => _prefab;
        public int PoolSize => _poolSize;
        public float PhysicalResist => _physicalResist;
        public float MagicResist => _magicResist;
        public bool IsHealer => _isHealer;
        public bool IsElite => _isElite;
        public bool IsBoss => _isBoss;
        public float Armor => _armor;

#if UNITY_EDITOR
        public void EditorSetup(string displayName, float maxHp, float speed, int contactDamage, int xpReward,
            float radius, Color bodyColor, float physicalResist, float magicResist, bool isHealer, bool isElite, bool isBoss, float armor)
        {
            _displayName = displayName;
            _maxHp = maxHp;
            _speed = speed;
            _contactDamage = contactDamage;
            _xpReward = xpReward;
            _radius = radius;
            _bodyColor = bodyColor;
            _physicalResist = physicalResist;
            _magicResist = magicResist;
            _isHealer = isHealer;
            _isElite = isElite;
            _isBoss = isBoss;
            _armor = armor;
        }

        public void EditorSetPrefab(Enemy prefab, int poolSize)
        {
            _prefab = prefab;
            _poolSize = Mathf.Max(1, poolSize);
        }
#endif
    }
}
