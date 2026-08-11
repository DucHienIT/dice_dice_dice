using CCQ.Data;
using CCQ.Localization;

namespace CCQ.Events
{
    /// <summary>
    /// Result of a resolved peaceful event, consumed by GameManager/UI.
    /// Pure data — the resolver never touches presentation directly.
    /// </summary>
    public class EventOutcome
    {
        // Console text, kept unresolved so it survives a language switch. Suffix is an
        // optional second sentence appended to Line (level-up, "pod is full", ...).
        public LocLine Line;
        public LocLine Suffix;

        // Banner (fortune / new sidekick), null icon = no banner. Banners live ~3s, so
        // their text is resolved up front instead of being re-rendered on a language switch.
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
