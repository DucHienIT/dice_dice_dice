namespace Game.Utils
{
    /// <summary>
    /// Cached int→string lookup so damage floaters and counters avoid per-beat allocations.
    /// Values above the cache range fall back to ToString (rare, late game only).
    /// </summary>
    public static class NumberStrings
    {
        private const int CacheSize = 4096;
        private static readonly string[] Cache = new string[CacheSize];

        public static string Get(int value)
        {
            if (value < 0 || value >= CacheSize) return value.ToString();
            return Cache[value] ??= value.ToString();
        }
    }
}
