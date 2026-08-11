using CCQ.Data;

namespace CCQ.Events
{
    /// <summary>
    /// Result of a resolved peaceful event, consumed by GameManager/UI.
    /// Pure data — the resolver never touches presentation directly.
    /// </summary>
    public class EventOutcome
    {
        public string Text;

        // Banner (fortune / new sidekick), null icon = no banner
        public UnityEngine.Sprite BannerIcon;
        public string BannerTitle;
        public string BannerTag;

        // Floater over the hero: damage (negative) or heal (positive), 0 = none
        public int HeroHpDelta;

        // Level-ups triggered by treasure XP
        public int LevelUps;

        // Sidekick offer that requires a swap choice (pod full)
        public Sidekick PendingSidekick;
    }
}
