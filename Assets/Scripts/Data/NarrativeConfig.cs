using UnityEngine;

namespace CCQ.Data
{
    /// <summary>
    /// All flavor text (TEXT in the spec), critter/boss names and glyph charset.
    /// Markup: *emphasis*, &lt;b&gt;bold&lt;/b&gt;, tokens {e} {xp} {heal} {dmg} {s} {p} {lv}.
    /// </summary>
    [CreateAssetMenu(menuName = "CCQ/Narrative Config", fileName = "NarrativeConfig")]
    public class NarrativeConfig : ScriptableObject
    {
        [SerializeField, TextArea] private string[] _battleIntros;
        [SerializeField, TextArea] private string[] _eliteIntros;
        [SerializeField, TextArea] private string[] _bossIntros;
        [SerializeField, TextArea] private string[] _winTexts;
        [SerializeField, TextArea] private string[] _fortuneTexts;
        [SerializeField, TextArea] private string[] _choiceTexts;
        [SerializeField, TextArea] private string[] _springTexts;
        [SerializeField, TextArea] private string[] _sidekickTexts;
        [SerializeField, TextArea] private string[] _sidekickFullTexts;
        [SerializeField, TextArea] private string[] _trapTexts;
        [SerializeField, TextArea] private string[] _treasureTexts;
        [SerializeField, TextArea] private string[] _planetClearTexts;
        [SerializeField, TextArea] private string _levelUpSuffix;
        [SerializeField, TextArea] private string _introNewRun;
        [SerializeField, TextArea] private string _introResume;
        [SerializeField, TextArea] private string _deathText;
        [SerializeField] private string[] _critterNames;
        [SerializeField] private string[] _bossNames;
        [SerializeField] private string _glyphChars;

        public string[] BattleIntros => _battleIntros;
        public string[] EliteIntros => _eliteIntros;
        public string[] BossIntros => _bossIntros;
        public string[] WinTexts => _winTexts;
        public string[] FortuneTexts => _fortuneTexts;
        public string[] ChoiceTexts => _choiceTexts;
        public string[] SpringTexts => _springTexts;
        public string[] SidekickTexts => _sidekickTexts;
        public string[] SidekickFullTexts => _sidekickFullTexts;
        public string[] TrapTexts => _trapTexts;
        public string[] TreasureTexts => _treasureTexts;
        public string[] PlanetClearTexts => _planetClearTexts;
        public string LevelUpSuffix => _levelUpSuffix;
        public string IntroNewRun => _introNewRun;
        public string IntroResume => _introResume;
        public string DeathText => _deathText;
        public string[] CritterNames => _critterNames;
        public string[] BossNames => _bossNames;
        public string GlyphChars => _glyphChars;

        public static string Pick(string[] pool) =>
            pool == null || pool.Length == 0 ? string.Empty : pool[Random.Range(0, pool.Length)];
    }
}
