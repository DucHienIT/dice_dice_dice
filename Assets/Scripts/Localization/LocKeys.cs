namespace CCQ.Localization
{
    /// <summary>
    /// Term keys referenced directly from code. Keys that live on a ScriptableObject
    /// (narrative pools, planet/sidekick/upgrade/fortune names) are not listed here —
    /// the builder writes those onto the assets. Every key must exist in
    /// Assets/Localization/CCQ_Localization.csv.
    /// </summary>
    public static class LocKeys
    {
        // ---- narrative composed in code ----
        public const string ElitePrefix = "Narrative/ElitePrefix";
        public const string SidekickJoin = "Narrative/SidekickJoin";
        public const string SidekickPodFull = "Narrative/SidekickPodFull";
        public const string SidekickSwap = "Narrative/SidekickSwap";

        // ---- engage button ----
        public const string EngageEngage = "UI/Engage/Engage";
        public const string EngageTraveling = "UI/Engage/Traveling";
        public const string EngageBattling = "UI/Engage/Battling";
        public const string EngageChoosing = "UI/Engage/Choosing";
        public const string EngageNewVoyage = "UI/Engage/NewVoyage";

        // ---- HUD ----
        public const string HudRound = "UI/Hud/Round";
        public const string HudLevel = "UI/Hud/Level";
        public const string HudSpeed = "UI/Hud/Speed";
        public static readonly string[] HudStatTitles =
        {
            "UI/Hud/StatXp", "UI/Hud/StatHp", "UI/Hud/StatAtk", "UI/Hud/StatDef"
        };

        public const string ConsoleStarCycle = "UI/Console/StarCycle";
        public const string StageBossPrefix = "UI/Stage/BossPrefix";

        // ---- banners ----
        public const string BannerAcquired = "UI/Banner/Acquired";
        public const string BannerSmallFortune = "UI/Banner/SmallFortune";
        public const string BannerNewSidekick = "UI/Banner/NewSidekick";
        public const string BannerSidekickSwap = "UI/Banner/SidekickSwap";
        public const string BannerPlanetCleared = "UI/Banner/PlanetCleared";
        public const string BannerSidekickJoins = "UI/Banner/SidekickJoins";
        public const string BannerWarpingTo = "UI/Banner/WarpingTo";

        // ---- choice cards ----
        public const string ChoiceRelease = "UI/Choice/Release";
        public const string ChoiceAdoptDesc = "UI/Choice/AdoptDesc";
        public const string ChoiceWaveGoodbye = "UI/Choice/WaveGoodbye";
        public const string ChoiceSnackDesc = "UI/Choice/SnackDesc";

        // ---- floaters ----
        public const string FloaterEnraged = "UI/Floater/Enraged";
        public const string FloaterLevelUp = "UI/Floater/LevelUp";

        // ---- settings overlay ----
        public const string OverlaySettings = "UI/Overlay/Settings";
        public const string OverlayResume = "UI/Overlay/Resume";
        public const string OverlayMusicOn = "UI/Overlay/MusicOn";
        public const string OverlayMusicOff = "UI/Overlay/MusicOff";
        public const string OverlaySfxOn = "UI/Overlay/SfxOn";
        public const string OverlaySfxOff = "UI/Overlay/SfxOff";
        public const string OverlayRestart = "UI/Overlay/Restart";
        public const string OverlayResetAll = "UI/Overlay/ResetAll";
        public const string OverlayLanguage = "UI/Overlay/Language";
        public const string OverlayAbout = "UI/Overlay/About";
        public const string OverlayBestShort = "UI/Overlay/BestShort";

        // ---- death overlay ----
        public const string DeathTitle = "UI/Death/Title";
        public const string DeathFellOn = "UI/Death/FellOn";
        public const string DeathProgress = "UI/Death/Progress";
        public const string DeathLevelCycle = "UI/Death/LevelCycle";
        public const string DeathHitLine = "UI/Death/HitLine";
        public const string DeathDamageLine = "UI/Death/DamageLine";
        public const string DeathSlainLine = "UI/Death/SlainLine";
        public const string DeathNewBest = "UI/Death/NewBest";
        public const string DeathBestVoyage = "UI/Death/BestVoyage";

        /// <summary>"UI/Language/" + language code, e.g. UI/Language/vi.</summary>
        public const string LanguagePrefix = "UI/Language/";
    }
}
