using CCQ.Localization;
using UnityEngine;

namespace CCQ.Data
{
    /// <summary>
    /// One step of the Star Forge path, bought rank by rank with star shards between runs.
    /// The path is a single chain: array order is climb order, and each step is gated by the
    /// one below it. Pure data — a new step is a new .asset plus two CSV rows.
    /// </summary>
    [CreateAssetMenu(menuName = "CCQ/Meta Upgrade", fileName = "MetaUpgrade")]
    public class MetaUpgrade : ScriptableObject
    {
        [Tooltip("Stable save id — ranks are stored against it, so reordering is safe.")]
        [SerializeField] private string _id;
        [Tooltip("Localization term keys — the text lives in CCQ_Localization.csv.")]
        [SerializeField] private string _nameKey;
        [SerializeField] private string _descriptionKey;
        [Tooltip("Rune tiers from dullest to finest; the rune shown scales with the rank.")]
        [SerializeField] private Sprite[] _rankIcons;
        [SerializeField] private int _maxRank = 3;
        [Tooltip("Shard price of the first rank; every later rank costs +CostStep more.")]
        [SerializeField] private int _costBase = 20;
        [SerializeField] private int _costStep = 12;
        [Tooltip("Applied once per owned rank.")]
        [SerializeField] private StatMod[] _modsPerRank;

        [Header("Path")]
        [Tooltip("Step below this one on the path. Empty = the first step.")]
        [SerializeField] private MetaUpgrade _requires;
        [SerializeField] private int _requiredRank = 3;

        public string Id => _id;
        public string DisplayName => Loc.Get(_nameKey);
        public string Description => Loc.Get(_descriptionKey);
        public int MaxRank => _maxRank;
        public StatMod[] ModsPerRank => _modsPerRank;
        public MetaUpgrade Requires => _requires;
        public int RequiredRank => _requiredRank;

        /// <summary>Price of the next rank when <paramref name="rank"/> are already owned.</summary>
        public int CostAt(int rank) => _costBase + _costStep * rank;

        /// <summary>
        /// The rune that stands for this many ranks. Ranks are spread across whatever tiers
        /// the sprite set has, so MaxRank and the tier count need not match.
        /// </summary>
        public Sprite RankIcon(int rank)
        {
            if (_rankIcons == null || _rankIcons.Length == 0) return null;
            if (_maxRank <= 0) return _rankIcons[0];
            int top = _rankIcons.Length - 1;
            int tier = Mathf.RoundToInt(Mathf.Clamp01((float)rank / _maxRank) * top);
            return _rankIcons[Mathf.Clamp(tier, 0, top)];
        }
    }
}
