using UnityEngine;

namespace CCQ.Data
{
    /// <summary>
    /// Localization term keys for all flavor text (TEXT in the spec), critter/boss names
    /// and the glyph charset. The sentences themselves live in
    /// Assets/Localization/CCQ_Localization.csv — this asset only decides which key
    /// belongs to which pool. Markup inside the translations: *emphasis*,
    /// &lt;b&gt;bold&lt;/b&gt;, tokens {e} {xp} {heal} {dmg} {s} {d} {r} {p} {lv}.
    /// </summary>
    [CreateAssetMenu(menuName = "CCQ/Narrative Config", fileName = "NarrativeConfig")]
    public class NarrativeConfig : ScriptableObject
    {
        [SerializeField] private string[] _battleIntroKeys;
        [SerializeField] private string[] _eliteIntroKeys;
        [SerializeField] private string[] _bossIntroKeys;
        [SerializeField] private string[] _winKeys;
        [SerializeField] private string[] _fortuneKeys;
        [SerializeField] private string[] _choiceKeys;
        [SerializeField] private string[] _springKeys;
        [SerializeField] private string[] _sidekickKeys;
        [SerializeField] private string[] _sidekickFullKeys;
        [SerializeField] private string[] _trapKeys;
        [SerializeField] private string[] _treasureKeys;
        [SerializeField] private string[] _planetClearKeys;
        [SerializeField] private string _levelUpSuffixKey;
        [SerializeField] private string _introNewRunKey;
        [SerializeField] private string _introResumeKey;
        [SerializeField] private string _deathKey;
        [SerializeField] private string[] _critterNameKeys;
        [SerializeField] private string[] _bossNameKeys;
        [SerializeField] private string _glyphChars;

        public string[] BattleIntroKeys => _battleIntroKeys;
        public string[] EliteIntroKeys => _eliteIntroKeys;
        public string[] BossIntroKeys => _bossIntroKeys;
        public string[] WinKeys => _winKeys;
        public string[] FortuneKeys => _fortuneKeys;
        public string[] ChoiceKeys => _choiceKeys;
        public string[] SpringKeys => _springKeys;
        public string[] SidekickKeys => _sidekickKeys;
        public string[] SidekickFullKeys => _sidekickFullKeys;
        public string[] TrapKeys => _trapKeys;
        public string[] TreasureKeys => _treasureKeys;
        public string[] PlanetClearKeys => _planetClearKeys;
        public string LevelUpSuffixKey => _levelUpSuffixKey;
        public string IntroNewRunKey => _introNewRunKey;
        public string IntroResumeKey => _introResumeKey;
        public string DeathKey => _deathKey;
        public string[] CritterNameKeys => _critterNameKeys;
        public string[] BossNameKeys => _bossNameKeys;
        public string GlyphChars => _glyphChars;

        /// <summary>One random key out of a pool — resolve it through Loc / LocLine.</summary>
        public static string PickKey(string[] keys) =>
            keys == null || keys.Length == 0 ? string.Empty : keys[Random.Range(0, keys.Length)];
    }
}
