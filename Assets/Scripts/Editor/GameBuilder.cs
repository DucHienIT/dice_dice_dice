using Game.Audio;
using Game.Combat;
using Game.Core;
using Game.Data;
using Game.Enemies;
using Game.Localization;
using Game.Worlds;
using Game.Sidekicks;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using static Game.EditorTools.BuilderUtil;

namespace Game.EditorTools
{
    /// <summary>
    /// One-shot builder: creates config assets, prefabs and the playable scene with every
    /// reference wired. Idempotent — rerunning overwrites the scene and re-wires content
    /// (numeric balance on existing config assets is left untouched).
    /// </summary>
    public static class GameBuilder
    {
        private const string DataDir = "Assets/Data";
        private const string UiDataDir = "Assets/Data/UI";
        private const string PrefabDir = "Assets/Prefabs";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string FrameName = "Frame";

        private const string ItemIcons =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Icon_ItemIcons(x2)/128/";
        private const string PictoIcons =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Icon_PictoIcons(x2)/128/";
        // rune icons come in six rarity tiers, which map 1:1 onto a metaPath node's 0..5 ranks
        private const string RuneIcons =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Icon_RuneIcons(x2)/128/";
        private const string SoftGlow =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Popup/Popup_00_Glow_white.png";
        private const string ShardIcon =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Icon_ItemIcons(x2)/128/Icon_Star.png";
        private const string ButtonsDir =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Button/";
        private const string FramesDir =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Frame/";
        private const string PopupsDir =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Popup/";
        private const string FontTtf =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular.ttf";
        // LilitaOne carries no Vietnamese glyphs, so localized languages get their own font
        private const string WideCharsetTtf = "Assets/Data/UI/Fonts/Roboto-Bold.ttf";
        private const string WideCharsetFallbackTtf = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

        private class Content
        {
            public GameConfig Config;
            public NarrativeConfig Narrative;
        }

        /// <summary>Display font plus the wide-charset font used by languages it cannot render.</summary>
        private class Fonts
        {
            public TMP_FontAsset Default;
            public TMP_FontAsset WideCharset;
            public Material DefaultWorld;
            public Material WideCharsetWorld;
        }

        private class Prefabs
        {
            public GameObject Hero;
            public GameObject Enemy;
            public GameObject Floater;
            public GameObject BurstStar;
            public GameObject Orb;
            public GameObject HpBar;
        }

        [MenuItem("Tools/Game/Build Game (Full)")]
        public static void BuildFull()
        {
            EnsureFolder(DataDir);
            EnsureFolder(UiDataDir);
            EnsureFolder(DataDir + "/Upgrades");
            EnsureFolder(DataDir + "/Fortunes");
            EnsureFolder(DataDir + "/Meta");
            EnsureFolder(DataDir + "/Sidekicks");
            EnsureFolder(DataDir + "/Worlds");
            EnsureFolder(PrefabDir);
            EnsureFolder(UiDataDir + "/Fonts");
            EnsureFolder("Assets/Scenes");

            // translations first: the configs below only store term keys, and the scene is
            // authored with the strings this asset resolves them to
            LocalizationImporter.Import();

            Fonts fonts = BuildFonts();
            Content content = BuildConfigs();
            // Bake every procedural sprite to a .png asset first — prefabs and the scene only
            // ever reference these, nothing paints at runtime.
            SpriteBaker.Sprites art = SpriteBaker.BakeAll(content.Config);
            Prefabs prefabs = BuildPrefabs(content, art, fonts.Default, fonts.DefaultWorld);
            BuildScene(content, art, prefabs, fonts);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Builder] One Tap Immortal build complete → " + ScenePath);
        }

        [MenuItem("Tools/Game/Rebuild Main Menu UI")]
        public static void RebuildMainMenuUi()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Builder] Stop Play Mode before rebuilding the main menu UI.");
                return;
            }

            GameObject uiRoot = GameObject.Find("UI");
            if (uiRoot == null)
            {
                Debug.LogError("[Builder] Cannot rebuild main menu: scene has no UI root.");
                return;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                                      UiDataDir + "/LilitaOne SDF.asset")
                                  ?? TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Debug.LogError("[Builder] Cannot rebuild main menu: no TMP font is available.");
                return;
            }

            Transform existing = uiRoot.transform.Find("Canvas_Menu");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            MainMenuView menu = BuildMenuCanvas(uiRoot.transform, font, out NavBarView nav);
            UIController controller = uiRoot.GetComponent<UIController>();
            if (controller == null)
            {
                Debug.LogError("[Builder] Cannot wire main menu: UI root has no UIController.");
                Object.DestroyImmediate(menu.transform.parent.parent.gameObject);
                return;
            }

            SetPrivate(controller, "_menu", menu);
            SetPrivate(controller, "_nav", nav);

            LocalizedFontView localizedFonts = uiRoot.GetComponent<LocalizedFontView>();
            if (localizedFonts != null)
            {
                SetPrivate(localizedFonts, "_uiTexts",
                    uiRoot.GetComponentsInChildren<TextMeshProUGUI>(true));
            }

            EditorSceneManager.MarkSceneDirty(uiRoot.scene);
            EditorSceneManager.SaveScene(uiRoot.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Builder] Main menu UI rebuilt from Layer Lab assets.");
        }


        // ================= font =================

        private static Fonts BuildFonts()
        {
            var fonts = new Fonts
            {
                Default = LoadOrCreateFont(FontTtf, UiDataDir + "/LilitaOne SDF.asset",
                    "LilitaOne SDF")
            };
            if (fonts.Default == null)
            {
                Debug.LogWarning("[Builder] LilitaOne unavailable — falling back to TMP default font");
                fonts.Default = TMP_Settings.defaultFontAsset;
            }

            fonts.WideCharset = LoadOrCreateFont(WideCharsetTtf, UiDataDir + "/RobotoVN SDF.asset",
                                   "RobotoVN SDF")
                               ?? LoadOrCreateFont(WideCharsetFallbackTtf,
                                   UiDataDir + "/LiberationVN SDF.asset", "LiberationVN SDF");
            if (fonts.WideCharset == null)
            {
                Debug.LogWarning("[Builder] No diacritic-capable font found — " +
                                 "Vietnamese text will show as missing glyphs");
                fonts.WideCharset = fonts.Default;
            }

            fonts.DefaultWorld = LoadOrCreateWorldMaterial(fonts.Default,
                UiDataDir + "/WorldText.mat");
            fonts.WideCharsetWorld = fonts.WideCharset == fonts.Default
                ? fonts.DefaultWorld
                : LoadOrCreateWorldMaterial(fonts.WideCharset, UiDataDir + "/WorldTextVN.mat");
            return fonts;
        }

        /// <summary>Dynamic SDF font asset for a TTF, created once and reused afterwards.</summary>
        private static TMP_FontAsset LoadOrCreateFont(string ttfPath, string assetPath, string name)
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fa != null) return fa;

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (ttf == null) return null;
            fa = TMP_FontAsset.CreateFontAsset(ttf, 70, 7, GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa == null) return null;

            fa.name = name;
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.material.name = name + " Material";
            fa.atlasTexture.name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        /// <summary>Outlined variant of a font's material, used by all world-space text.</summary>
        private static Material LoadOrCreateWorldMaterial(TMP_FontAsset font, string path)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(font.material);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.85f));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ================= configs =================

        private static Content BuildConfigs()
        {
            // ---- worlds (realms) ----
            // Palettes follow the theme doc: jade/ivory base, vermillion + gold-black accents,
            // one realm per elemental mood. Music roots sit on the G pentatonic (G A B D E).
            var worlds = new World[5];
            worlds[0] = MakeWorld("AzureCloud", "#0e2e3a", "#1d5c55", "#35c9a3", "#1d8a74",
                "#2a6b52", "#3a5c6b", "#f5ead6", new[] { "#7ef0c0", "#5cb8ff", "#f5ead6" }, 146.83f, false);
            worlds[1] = MakeWorld("Emberfall", "#3a0f12", "#7a2410", "#e86a28", "#a83c0f",
                "#5c2a14", "#4a2020", "#ffd9a0", new[] { "#ffd35c", "#ff7a3d", "#e8506b" }, 110f, true);
            worlds[2] = MakeWorld("Frostmoon", "#10204a", "#2e5a8f", "#9fdcff", "#4a9ad4",
                "#4a6ea8", "#3a4a7a", "#eaf6ff", new[] { "#cfeaff", "#9bb8ff", "#e8d8ff" }, 164.81f, false);
            worlds[3] = MakeWorld("Gloomfen", "#1c0f2e", "#3a1f4a", "#8a5cd4", "#5c2a90",
                "#3a2a52", "#2e2244", "#d3ffc9", new[] { "#7aff9b", "#c9ff5c", "#c78aff" }, 123.47f, true);
            worlds[4] = MakeWorld("HollowDeep", "#0a0a0f", "#1c1428", "#d4a53d", "#8a6420",
                "#241c30", "#1a1424", "#f0e0b0", new[] { "#ffd35c", "#c04a3a", "#8f5cff" }, 98f, true);

            // ---- sidekicks ----
            var sidekicks = new Sidekick[4];
            sidekicks[0] = MakeSidekick("blob", "FlyingSword",
                PictoIcons + "Pictoicon_Fist.Png", "#9be8d8", SidekickType.Damage, 0.15f);
            sidekicks[1] = MakeSidekick("medic", "Lingzhi",
                PictoIcons + "Pictoicon_Mushroom.Png", "#8bf07a", SidekickType.Heal, 0.02f);
            sidekicks[2] = MakeSidekick("shield", "ShellWard",
                PictoIcons + "Pictoicon_Magic_Ball.Png", "#d4b86b", SidekickType.Block, 0.12f);
            sidekicks[3] = MakeSidekick("spark", "ThunderPearl",
                PictoIcons + "Pictoicon_Thunder.Png", "#ffd35c", SidekickType.Crit, 0.06f);

            // ---- fortunes ----
            var fortunes = new Fortune[5];
            fortunes[0] = MakeFortune("MaxHp", ItemIcons + "Icon_Heart.png",
                new StatMod(StatModType.MaxHpPct, 0.07f));
            fortunes[1] = MakeFortune("Atk", ItemIcons + "Icon_Sword.png",
                new StatMod(StatModType.AtkPct, 0.05f));
            fortunes[2] = MakeFortune("Def", ItemIcons + "Icon_Shield.png",
                new StatMod(StatModType.DefFlat, 2f));
            fortunes[3] = MakeFortune("Crit", ItemIcons + "Icon_Clover.png",
                new StatMod(StatModType.CritChance, 0.03f));
            fortunes[4] = MakeFortune("Heal", ItemIcons + "Icon_Potion02_Green.png",
                new StatMod(StatModType.HealNowPct, 0.20f));

            // ---- upgrades ----
            var upgrades = new UpgradeCard[8];
            upgrades[0] = MakeUpgrade("FlameArt",
                ItemIcons + "Icon_Energy_Green.png", new StatMod(StatModType.AtkPct, 0.18f));
            upgrades[1] = MakeUpgrade("UndyingBody",
                PictoIcons + "Pictoicon_Life.Png", new StatMod(StatModType.MaxHpPct, 0.22f));
            upgrades[2] = MakeUpgrade("GoldenBell",
                ItemIcons + "Icon_Shield.png", new StatMod(StatModType.DefFlat, 4f));
            upgrades[3] = MakeUpgrade("EssenceDrain",
                ItemIcons + "Icon_Tooth.png", new StatMod(StatModType.Lifesteal, 0.08f));
            upgrades[4] = MakeUpgrade("SpiritEye",
                ItemIcons + "Icon_Target.png", new StatMod(StatModType.CritChance, 0.08f));
            upgrades[5] = MakeUpgrade("ReboundForce",
                PictoIcons + "Pictoicon_Cactus.Png", new StatMod(StatModType.Thorns, 0.20f));
            upgrades[6] = MakeUpgrade("QiDeviation",
                PictoIcons + "Pictoicon_Boom.Png", new StatMod(StatModType.AtkPct, 0.30f),
                new StatMod(StatModType.MaxHpMult, 0.9f));
            upgrades[7] = MakeUpgrade("RejuvenationPill",
                ItemIcons + "Icon_Potion01_Red.png", new StatMod(StatModType.HealNowPct, 0.45f));

            // ---- cultivation path (permanent, bought with shards between runs) ----
            // One chain, climbed bottom to top: array order is climb order and each step
            // costs more than the last. ChainSteps below gates every step on the one under it.
            var metaUpgrades = new MetaUpgrade[6];
            metaUpgrades[0] = MakeMetaUpgrade("BodyTempering", "Ball_Health", 20, 12,
                new StatMod(StatModType.MaxHpPct, 0.08f));
            metaUpgrades[1] = MakeMetaUpgrade("QiRefining", "Damage", 28, 16,
                new StatMod(StatModType.AtkPct, 0.07f));
            metaUpgrades[2] = MakeMetaUpgrade("Foundation", "Buff", 36, 20,
                new StatMod(StatModType.DefFlat, 2f));
            metaUpgrades[3] = MakeMetaUpgrade("GoldenCore", "Passive", 44, 24,
                new StatMod(StatModType.Lifesteal, 0.02f));
            metaUpgrades[4] = MakeMetaUpgrade("NascentSoul", "Debuff", 52, 28,
                new StatMod(StatModType.Thorns, 0.06f));
            metaUpgrades[5] = MakeMetaUpgrade("SpiritSevering", "Critical_Chance", 60, 32,
                new StatMod(StatModType.CritChance, 0.03f));
            ChainSteps(metaUpgrades);

            // ---- narrative (term keys only — the sentences live in Localization.csv) ----
            var narrative = LoadOrCreateAsset<NarrativeConfig>(DataDir + "/NarrativeConfig.asset");
            SetPrivate(narrative, "_battleIntroKeys", Keys("Narrative/BattleIntro/", 5));
            SetPrivate(narrative, "_eliteIntroKeys", Keys("Narrative/EliteIntro/", 2));
            SetPrivate(narrative, "_bossIntroKeys", Keys("Narrative/BossIntro/", 2));
            SetPrivate(narrative, "_winKeys", Keys("Narrative/Win/", 3));
            SetPrivate(narrative, "_fortuneKeys", Keys("Narrative/Fortune/", 3));
            SetPrivate(narrative, "_choiceKeys", Keys("Narrative/Choice/", 3));
            SetPrivate(narrative, "_springKeys", Keys("Narrative/Spring/", 2));
            SetPrivate(narrative, "_sidekickKeys", Keys("Narrative/Sidekick/", 2));
            SetPrivate(narrative, "_sidekickFullKeys", Keys("Narrative/SidekickFull/", 1));
            SetPrivate(narrative, "_trapKeys", Keys("Narrative/Trap/", 2));
            SetPrivate(narrative, "_treasureKeys", Keys("Narrative/Treasure/", 2));
            SetPrivate(narrative, "_worldClearKeys", Keys("Narrative/WorldClear/", 1));
            SetPrivate(narrative, "_levelUpSuffixKey", "Narrative/LevelUpSuffix");
            SetPrivate(narrative, "_introNewRunKey", "Narrative/IntroNewRun");
            SetPrivate(narrative, "_introResumeKey", "Narrative/IntroResume");
            SetPrivate(narrative, "_deathKey", "Narrative/Death");
            SetPrivate(narrative, "_enemyNameKeys", Keys("Narrative/BeastName/", 8));
            SetPrivate(narrative, "_bossNameKeys", Keys("Narrative/BossName/", 5));
            SetPrivate(narrative, "_glyphChars", "<>/|+=*#%&?!~^");

            // ---- game config (content arrays only; numbers keep asset values) ----
            var config = LoadOrCreateAsset<GameConfig>(DataDir + "/GameConfig.asset");
            SetPrivate(config, "_upgrades", upgrades);
            SetPrivate(config, "_metaUpgrades", metaUpgrades);
            SetPrivate(config, "_fortunes", fortunes);
            SetPrivate(config, "_sidekicks", sidekicks);
            SetPrivate(config, "_worlds", worlds);
            SetPrivate(config, "_enemyColors", new[]
            {
                Hex("#e8b84f"), Hex("#c04a3a"), Hex("#5cb8a0"), Hex("#8f6bff"),
                Hex("#d45c8a"), Hex("#4a90d4"), Hex("#9bd45c")
            });

            return new Content { Config = config, Narrative = narrative };
        }

        /// <summary>Numbered pool of term keys, e.g. "Narrative/Win/1".."Narrative/Win/3".</summary>
        private static string[] Keys(string prefix, int count)
        {
            var keys = new string[count];
            for (int i = 0; i < count; i++) keys[i] = prefix + (i + 1);
            return keys;
        }

        private static World MakeWorld(string name, string sky1, string sky2, string lake,
            string lakeDeep, string ground, string rock, string moon, string[] flora,
            float rootHz, bool minor)
        {
            var p = LoadOrCreateAsset<World>(DataDir + "/Worlds/World_" + name + ".asset");
            SetPrivate(p, "_nameKey", "World/" + name);
            SetPrivate(p, "_skyTop", Hex(sky1));
            SetPrivate(p, "_skyBottom", Hex(sky2));
            SetPrivate(p, "_lake", Hex(lake));
            SetPrivate(p, "_lakeDeep", Hex(lakeDeep));
            SetPrivate(p, "_ground", Hex(ground));
            SetPrivate(p, "_rock", Hex(rock));
            SetPrivate(p, "_moon", Hex(moon));
            var floraColors = new Color[flora.Length];
            for (int i = 0; i < flora.Length; i++) floraColors[i] = Hex(flora[i]);
            SetPrivate(p, "_flora", floraColors);
            SetPrivate(p, "_musicRootHz", rootHz);
            SetPrivate(p, "_minorMood", minor);
            return p;
        }

        private static Sidekick MakeSidekick(string id, string name, string iconPath,
            string colorHex, SidekickType type, float value)
        {
            var s = LoadOrCreateAsset<Sidekick>(DataDir + "/Sidekicks/Sidekick_" + name + ".asset");
            SetPrivate(s, "_id", id);
            SetPrivate(s, "_nameKey", "Sidekick/" + name + "/Name");
            SetPrivate(s, "_descriptionKey", "Sidekick/" + name + "/Desc");
            SetPrivate(s, "_icon", LoadSprite(iconPath));
            SetPrivate(s, "_color", Hex(colorHex));
            SetPrivate(s, "_type", type);
            SetPrivate(s, "_value", value);
            return s;
        }

        private static Fortune MakeFortune(string id, string iconPath, params StatMod[] mods)
        {
            var f = LoadOrCreateAsset<Fortune>(DataDir + "/Fortunes/Fortune_" + id + ".asset");
            SetPrivate(f, "_nameKey", "Fortune/" + id);
            SetPrivate(f, "_icon", LoadSprite(iconPath));
            SetPrivate(f, "_mods", mods);
            return f;
        }

        private static UpgradeCard MakeUpgrade(string id, string iconPath, params StatMod[] mods)
        {
            var u = LoadOrCreateAsset<UpgradeCard>(
                DataDir + "/Upgrades/Upgrade_" + id + ".asset");
            SetPrivate(u, "_nameKey", "Upgrade/" + id + "/Name");
            SetPrivate(u, "_descriptionKey", "Upgrade/" + id + "/Desc");
            SetPrivate(u, "_icon", LoadSprite(iconPath));
            SetPrivate(u, "_mods", mods);
            return u;
        }

        private const int MetaRanks = 3;
        private const int RuneTiers = 6;

        /// <summary>
        /// A metaPath step. Its rank icons are the six rarity tiers of one rune motif, so the
        /// rune visibly levels up: the dullest stone is rank 0, the finest is maxed.
        /// </summary>
        private static MetaUpgrade MakeMetaUpgrade(string id, string runeMotif, int costBase,
            int costStep, params StatMod[] modsPerRank)
        {
            var icons = new Sprite[RuneTiers];
            for (int tier = 0; tier < RuneTiers; tier++)
            {
                icons[tier] = LoadSprite(RuneIcons + "RuneIcon" + tier + "_" + runeMotif + ".Png");
            }
            var m = LoadOrCreateAsset<MetaUpgrade>(DataDir + "/Meta/Meta_" + id + ".asset");
            SetPrivate(m, "_id", id);
            SetPrivate(m, "_nameKey", "Meta/" + id + "/Name");
            SetPrivate(m, "_descriptionKey", "Meta/" + id + "/Desc");
            SetPrivate(m, "_rankIcons", icons);
            SetPrivate(m, "_maxRank", MetaRanks);
            SetPrivate(m, "_costBase", costBase);
            SetPrivate(m, "_costStep", costStep);
            SetPrivate(m, "_modsPerRank", modsPerRank);
            return m;
        }

        /// <summary>Gates every step on the one below it being finished — the single path.</summary>
        private static void ChainSteps(MetaUpgrade[] steps)
        {
            SetPrivate(steps[0], "_requires", null);
            SetPrivate(steps[0], "_requiredRank", 0);
            for (int i = 1; i < steps.Length; i++)
            {
                SetPrivate(steps[i], "_requires", steps[i - 1]);
                SetPrivate(steps[i], "_requiredRank", MetaRanks);
            }
        }

        // ================= prefabs =================

        private static Prefabs BuildPrefabs(Content content, SpriteBaker.Sprites art,
            TMP_FontAsset font, Material worldMat)
        {
            var prefabs = new Prefabs();

            // Hero
            {
                var go = new GameObject("Hero");
                var view = go.AddComponent<HeroView>();
                // shadow stays on the ground; everything else hops with the Rig while walking
                SpriteRenderer shadow = NewSprite(go, "Shadow", 3, art.HeroShadow,
                    new Vector3(0f, 0.02f, 0f));
                GameObject rig = NewChild(go, "Rig");
                NewSprite(rig, "Body", 6, art.HeroBody);
                SpriteRenderer sword = NewSprite(rig, "Sword", 5, art.HeroSword,
                    new Vector3(0.34f, 0.5f, 0f));
                sword.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
                SpriteRenderer flash = NewSprite(rig, "Flash", 9, art.HeroFlash,
                    new Vector3(0f, 0.85f, 0f));
                flash.enabled = false;
                SetPrivate(view, "_rig", rig.transform);
                SetPrivate(view, "_shadow", shadow);
                SetPrivate(view, "_sword", sword);
                SetPrivate(view, "_flash", flash);
                prefabs.Hero = SavePrefab(go, PrefabDir + "/Hero.prefab");
            }
            // Enemy — one renderer per baked layer, stacked in draw order
            {
                var go = new GameObject("Enemy");
                var view = go.AddComponent<EnemyView>();
                SpriteRenderer shadow = NewSprite(go, "Shadow", 3, art.EnemyShadow,
                    new Vector3(0f, 0.02f, 0f));
                SpriteRenderer glow = NewSprite(go, "Glow", 4, art.EnemyGlow);
                SpriteRenderer horns = NewSprite(go, "Horns", 5, art.EnemyHorns);
                SpriteRenderer spikes = NewSprite(go, "Spikes", 6, art.EnemySpikes);
                SpriteRenderer body = NewSprite(go, "Body", 7, art.EnemyBodies[0]);
                SpriteRenderer spots = NewSprite(go, "Spots", 8, art.EnemySpots);
                SpriteRenderer eyes = NewSprite(go, "Eyes", 9, art.EnemyEyes[0]);
                SpriteRenderer mouth = NewSprite(go, "Mouth", 10, art.EnemyMouth);
                SpriteRenderer flash = NewSprite(go, "Flash", 11, art.EnemyFlash,
                    new Vector3(0f, 0.9f, 0f));
                flash.enabled = false;
                SetPrivate(view, "_glow", glow);
                SetPrivate(view, "_horns", horns);
                SetPrivate(view, "_spikes", spikes);
                SetPrivate(view, "_body", body);
                SetPrivate(view, "_spots", spots);
                SetPrivate(view, "_eyes", eyes);
                SetPrivate(view, "_mouth", mouth);
                SetPrivate(view, "_shadow", shadow);
                SetPrivate(view, "_flash", flash);
                SetPrivate(view, "_bodySprites", art.EnemyBodies);
                SetPrivate(view, "_eyeSprites", art.EnemyEyes);
                SetPrivate(view, "_tintLayers",
                    new[] { glow, horns, spikes, body, spots, eyes, mouth });
                prefabs.Enemy = SavePrefab(go, PrefabDir + "/Enemy.prefab");
            }
            // Floater
            {
                var go = new GameObject("Floater");
                var tmp = go.AddComponent<TextMeshPro>();
                tmp.font = font;
                tmp.fontSharedMaterial = worldMat;
                tmp.fontSize = 3.6f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                ((RectTransform)go.transform).sizeDelta = new Vector2(5f, 1f);
                go.GetComponent<MeshRenderer>().sortingOrder = 20;
                var floater = go.AddComponent<Floater>();
                SetPrivate(floater, "_text", tmp);
                prefabs.Floater = SavePrefab(go, PrefabDir + "/Floater.prefab");
            }
            // Burst star
            {
                var go = new GameObject("BurstStar");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 16;
                sr.sprite = art.BurstStar;
                prefabs.BurstStar = SavePrefab(go, PrefabDir + "/BurstStar.prefab");
            }
            // Sidekick orb — sprite comes from the owned Sidekick asset at assign time
            {
                var go = new GameObject("SidekickOrb");
                var view = go.AddComponent<SidekickOrbView>();
                SpriteRenderer body = NewSprite(go, "Body", 4,
                    content.Config.Sidekicks[0].OrbSprite);
                SetPrivate(view, "_body", body);
                prefabs.Orb = SavePrefab(go, PrefabDir + "/SidekickOrb.prefab");
            }
            // HP bar
            {
                var go = new GameObject("HpBar");
                var view = go.AddComponent<HpBarView>();
                NewSprite(go, "Frame", 12, art.HpFrame);
                SpriteRenderer fill = NewSprite(go, "Fill", 13, art.HpFill,
                    new Vector3(-0.56f, 0f, 0f));
                TextMeshPro value = NewWorldText(go, "Value", 2.8f, Color.white, font, worldMat, 14,
                    new Vector3(0f, 0.33f, 0f));
                SetPrivate(view, "_fill", fill);
                SetPrivate(view, "_value", value);
                prefabs.HpBar = SavePrefab(go, PrefabDir + "/HpBar.prefab");
            }
            return prefabs;
        }

        /// <summary>Sprite child with its baked sprite already assigned.</summary>
        private static SpriteRenderer NewSprite(GameObject parent, string name, int order,
            Sprite sprite, Vector3 localPos = default)
        {
            SpriteRenderer sr = NewSpriteChild(parent, name, order, localPos);
            sr.sprite = sprite;
            return sr;
        }

        private static GameObject SavePrefab(GameObject temp, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            if (prefab == null) Debug.LogError("[Builder] Failed to save prefab " + path);
            return prefab;
        }

        private static GameObject Spawn(GameObject prefab, Transform parent, Vector3 pos,
            string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            if (name != null) go.name = name;
            return go;
        }

        private static void WireInstance(Component c, string field, object value)
        {
            SetPrivate(c, field, value);
            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }

        // ================= scene =================

        private static void BuildScene(Content content, SpriteBaker.Sprites art,
            Prefabs prefabs, Fonts fonts)
        {
            TMP_FontAsset font = fonts.Default;
            Material worldMat = fonts.DefaultWorld;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- camera ----
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9.6f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#05030f");
            cam.GetUniversalAdditionalCameraData();
            var shaker = camGo.AddComponent<CameraShaker>();
            SetPrivate(shaker, "_target", camGo.transform);

            // ---- background ----
            var bgGo = new GameObject("WorldBackground");
            var background = bgGo.AddComponent<WorldBackgroundRenderer>();
            World firstWorld = content.Config.Worlds[0];
            SpriteRenderer sky = NewSprite(bgGo, "Sky", -12, firstWorld.SkyLayer);
            var twinkles = new SpriteRenderer[8];
            GameObject twinkleRoot = NewChild(bgGo, "Twinkles");
            for (int i = 0; i < twinkles.Length; i++)
            {
                twinkles[i] = NewSprite(twinkleRoot, "Twinkle" + i, -11, art.Twinkle);
            }
            // two copies of the tiling ground strip leap-frog each other as it scrolls
            var groundCopies = new SpriteRenderer[2];
            var lakeGlows = new SpriteRenderer[2];
            for (int i = 0; i < groundCopies.Length; i++)
            {
                groundCopies[i] = NewSprite(bgGo, "Ground" + i, -10, firstWorld.GroundLayer);
                lakeGlows[i] = NewSprite(groundCopies[i].gameObject, "LakeGlow", -9, art.LakeGlow,
                    new Vector3(0f, SpriteBaker.BgLakeLocalY, 0f));
            }
            SetPrivate(background, "_sky", sky);
            SetPrivate(background, "_groundCopies", groundCopies);
            SetPrivate(background, "_lakeGlows", lakeGlows);
            SetPrivate(background, "_twinkles", twinkles);
            // world metrics must match the pixels the baker produced
            SetPrivate(background, "_worldWidth", SpriteBaker.BgWorldWidth);
            SetPrivate(background, "_worldHeight", SpriteBaker.BgWorldHeight);
            SetPrivate(background, "_bottomWorldY", SpriteBaker.BgBottomY);
            SetPrivate(background, "_horizonWorldY", SpriteBaker.BgHorizonY);

            // ---- battle stage ----
            var stageGo = new GameObject("BattleStage");
            var stage = stageGo.AddComponent<BattleStageView>();
            GameObject heroGo = Spawn(prefabs.Hero, stageGo.transform, new Vector3(-1.85f, 3.1f, 0f));
            GameObject enemyGo = Spawn(prefabs.Enemy, stageGo.transform, new Vector3(1.85f, 3.1f, 0f));
            enemyGo.SetActive(false);
            GameObject heroBarGo = Spawn(prefabs.HpBar, stageGo.transform,
                new Vector3(-1.85f, 2.5f, 0f), "HpBar_Hero");
            GameObject enemyBarGo = Spawn(prefabs.HpBar, stageGo.transform,
                new Vector3(1.85f, 2.5f, 0f), "HpBar_Enemy");
            var heroBar = heroBarGo.GetComponent<HpBarView>();
            var enemyBar = enemyBarGo.GetComponent<HpBarView>();
            WireInstance(heroBar, "_fillTint", Hex("#a8e05f"));
            WireInstance(enemyBar, "_fillTint", Hex("#ff5c6b"));
            TextMeshPro enemyName = NewWorldText(stageGo, "EnemyName", 3f, Color.white, font,
                worldMat, 15, new Vector3(1.85f, 5.3f, 0f));
            var orbs = new SidekickOrbView[3];
            GameObject orbRoot = NewChild(stageGo, "Orbs");
            for (int i = 0; i < orbs.Length; i++)
            {
                GameObject orbGo = Spawn(prefabs.Orb, orbRoot.transform,
                    new Vector3(-2.6f - i * 0.4f, 3.5f, 0f), "Orb" + i);
                orbs[i] = orbGo.GetComponent<SidekickOrbView>();
                orbGo.SetActive(false);
            }
            SetPrivate(stage, "_config", content.Config);
            SetPrivate(stage, "_hero", heroGo.GetComponent<HeroView>());
            SetPrivate(stage, "_enemyView", enemyGo.GetComponent<EnemyView>());
            SetPrivate(stage, "_heroBar", heroBar);
            SetPrivate(stage, "_enemyBar", enemyBar);
            SetPrivate(stage, "_enemyName", enemyName);
            SetPrivate(stage, "_orbs", orbs);

            // ---- floaters ----
            var floatersGo = new GameObject("FloaterManager");
            var floaters = floatersGo.AddComponent<FloaterManager>();
            var floaterPool = new Floater[12];
            for (int i = 0; i < floaterPool.Length; i++)
            {
                GameObject f = Spawn(prefabs.Floater, floatersGo.transform, Vector3.zero,
                    "Floater" + i);
                floaterPool[i] = f.GetComponent<Floater>();
                f.SetActive(false);
            }
            SetPrivate(floaters, "_config", content.Config);
            SetPrivate(floaters, "_pool", floaterPool);

            // ---- bursts ----
            var burstsGo = new GameObject("BurstManager");
            var bursts = burstsGo.AddComponent<BurstManager>();
            var starPool = new SpriteRenderer[16];
            for (int i = 0; i < starPool.Length; i++)
            {
                GameObject s = Spawn(prefabs.BurstStar, burstsGo.transform, Vector3.zero,
                    "Star" + i);
                starPool[i] = s.GetComponent<SpriteRenderer>();
            }
            SetPrivate(bursts, "_pool", starPool);

            // ---- audio ----
            var audioGo = new GameObject("AudioManager");
            var audio = audioGo.AddComponent<AudioManager>();
            var musicSrc = NewChild(audioGo, "Music").AddComponent<AudioSource>();
            musicSrc.playOnAwake = false;
            var sfxSources = new AudioSource[4];
            for (int i = 0; i < sfxSources.Length; i++)
            {
                sfxSources[i] = NewChild(audioGo, "Sfx" + i).AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
            }
            SetPrivate(audio, "_musicSource", musicSrc);
            SetPrivate(audio, "_sfxSources", sfxSources);

            // ---- event system ----
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            var inputModule = esGo.AddComponent<InputSystemUIInputModule>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (actions != null) inputModule.actionsAsset = actions;

            // ---- UI ----
            var uiRoot = new GameObject("UI");
            var ui = uiRoot.AddComponent<UIController>();
            HudView hud = BuildHudCanvas(uiRoot.transform, content, art, font);
            BuildConsoleCanvas(uiRoot.transform, content, font,
                out ConsoleView console, out FortuneBanner banner, out ChoicePanel choices,
                out SidekickChipsView chips, out EngageButton engage);
            OverlayView overlay = BuildOverlayCanvas(uiRoot.transform, font);
            MetaPathView metaPath = BuildMetaPathCanvas(uiRoot.transform, content, font);
            MainMenuView menu = BuildMenuCanvas(uiRoot.transform, font, out NavBarView nav);
            SetPrivate(ui, "_config", content.Config);
            SetPrivate(ui, "_hud", hud);
            SetPrivate(ui, "_console", console);
            SetPrivate(ui, "_banner", banner);
            SetPrivate(ui, "_choices", choices);
            SetPrivate(ui, "_chips", chips);
            SetPrivate(ui, "_engage", engage);
            SetPrivate(ui, "_overlay", overlay);
            SetPrivate(ui, "_metaPath", metaPath);
            SetPrivate(ui, "_menu", menu);
            SetPrivate(ui, "_nav", nav);

            // ---- screen lock (portrait pillarbox) ----
            var lockGo = new GameObject("ScreenLock");
            var screenLock = lockGo.AddComponent<ScreenLockView>();
            CanvasScaler[] scalers = uiRoot.GetComponentsInChildren<CanvasScaler>(true);
            var frames = new RectTransform[scalers.Length];
            for (int i = 0; i < scalers.Length; i++)
            {
                frames[i] = (RectTransform)scalers[i].transform.Find(FrameName);
            }
            SetPrivate(screenLock, "_camera", cam);
            SetPrivate(screenLock, "_scalers", scalers);
            SetPrivate(screenLock, "_frames", frames);
            SetPrivate(screenLock, "_portraitLock", true);
            SetPrivate(screenLock, "_designResolution", new Vector2(1080f, 1920f));

            // ---- game manager ----
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            SetPrivate(gm, "_config", content.Config);
            SetPrivate(gm, "_narrative", content.Narrative);
            SetPrivate(gm, "_camera", cam);
            SetPrivate(gm, "_shaker", shaker);
            SetPrivate(gm, "_stage", stage);
            SetPrivate(gm, "_background", background);
            SetPrivate(gm, "_floaters", floaters);
            SetPrivate(gm, "_bursts", bursts);
            SetPrivate(gm, "_audio", audio);
            SetPrivate(gm, "_ui", ui);
            SetPrivate(gm, "_screenLock", screenLock);
            SetPrivate(gm, "_warpBannerIcon", LoadSprite(PictoIcons + "Pictoicon_Planet.Png"));

            // ---- per-language font swap (must run last: it collects every text in the scene) ----
            var fontView = uiRoot.AddComponent<LocalizedFontView>();
            SetPrivate(fontView, "_defaultFont", fonts.Default);
            SetPrivate(fontView, "_wideCharsetFont", fonts.WideCharset);
            SetPrivate(fontView, "_wideCharsetCodes", new[] { Loc.VietnameseCode });
            SetPrivate(fontView, "_defaultWorldMaterial", fonts.DefaultWorld);
            SetPrivate(fontView, "_wideCharsetWorldMaterial", fonts.WideCharsetWorld);
            SetPrivate(fontView, "_uiTexts", uiRoot.GetComponentsInChildren<TextMeshProUGUI>(true));
            SetPrivate(fontView, "_worldTexts", Object.FindObjectsByType<TextMeshPro>(
                FindObjectsInactive.Include, FindObjectsSortMode.None));
            SetPrivate(ui, "_fonts", fontView);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // ================= canvases =================

        /// <summary>
        /// Every canvas gets one stretched "Frame" child; all screens parent into it so
        /// ScreenLockView can shrink the whole UI into the pillarboxed viewport without
        /// any screen knowing the Frame exists.
        /// </summary>
        private static Canvas NewCanvas(Transform parent, string name, int sortingOrder,
            out RectTransform frame)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f; // portrait: lock width
            go.AddComponent<GraphicRaycaster>();
            frame = Stretch(NewUiChild(go.transform, FrameName));
            return canvas;
        }

        private static HudView BuildHudCanvas(Transform uiRoot, Content content,
            SpriteBaker.Sprites art, TMP_FontAsset font)
        {
            Canvas canvas = NewCanvas(uiRoot, "Canvas_HUD", 10, out RectTransform frame);
            var hud = canvas.gameObject.AddComponent<HudView>();
            Sprite uiSprite = BuiltinUiSprite();
            Color pillColor = Hex("#1a1240");
            pillColor.a = 0.92f;

            // round banner (top center)
            RectTransform round = Place(NewUiChild(frame, "RoundBanner"),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f),
                new Vector2(340f, 64f));
            AddImage(round, uiSprite, pillColor);
            var roundLabel = AddTmp(Stretch(NewUiChild(round, "Label")), "Round 1/30", 32f,
                Color.white, font, TextAlignmentOptions.Center);

            // hits (top left)
            RectTransform hits = Place(NewUiChild(frame, "HitsBox"),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f),
                new Vector2(190f, 64f));
            AddImage(hits, uiSprite, pillColor);
            RectTransform hitsIcon = Place(NewUiChild(hits, "Icon"), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f));
            AddImage(hitsIcon, LoadSprite(PictoIcons + "Pictoicon_Attack.Png"), Color.white,
                false, false);
            var hitsLabel = AddTmp(Place(NewUiChild(hits, "Value"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(110f, 50f)),
                "0", 32f, Color.white, font, TextAlignmentOptions.MidlineLeft);

            // gear button (top right)
            RectTransform gear = Place(NewUiChild(frame, "GearBtn"),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -14f),
                new Vector2(76f, 76f));
            Image gearImg = AddImage(gear, LoadSprite(ButtonsDir + "Btn_OtherButton_Circle01_n.png"),
                Color.white, true, false);
            Button gearBtn = AddButton(gear, gearImg);
            RectTransform gearIcon = Place(NewUiChild(gear, "Icon"), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(42f, 42f));
            AddImage(gearIcon, LoadSprite(PictoIcons + "Pictoicon_Setting.Png"), Color.white,
                false, false);

            // home button: the tab bar lives on the front screen, so this is the way back to it
            RectTransform home = Place(NewUiChild(frame, "HomeBtn"),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-108f, -14f),
                new Vector2(76f, 76f));
            Image homeImg = AddImage(home, LoadSprite(ButtonsDir + "Btn_OtherButton_Circle01_n.png"),
                Color.white, true, false);
            Button homeBtn = AddButton(home, homeImg);
            AddImage(Place(NewUiChild(home, "Icon"), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(42f, 42f)),
                LoadSprite(PictoIcons + "Pictoicon_Home_0.Png"), Color.white, false, false);

            // world tag (bottom-left of viewport, above stats bar)
            RectTransform world = Place(NewUiChild(frame, "WorldTag"),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 1126f),
                new Vector2(300f, 58f));
            AddImage(world, uiSprite, pillColor);
            RectTransform worldIcon = Place(NewUiChild(world, "Icon"), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(36f, 36f));
            AddImage(worldIcon, LoadSprite(PictoIcons + "Pictoicon_Planet.Png"), Color.white,
                false, false);
            var worldLabel = AddTmp(Place(NewUiChild(world, "Name"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(230f, 48f)),
                "Verdania", 28f, Hex("#ffd98a"), font, TextAlignmentOptions.MidlineLeft);

            // speed button (bottom-right of viewport)
            RectTransform speed = Place(NewUiChild(frame, "SpeedBtn"),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 1126f),
                new Vector2(120f, 64f));
            Image speedImg = AddImage(speed, LoadSprite(ButtonsDir + "Btn_OtherButton_Square02.png"),
                Color.white, true);
            Button speedBtn = AddButton(speed, speedImg);
            var speedLabel = AddTmp(Stretch(NewUiChild(speed, "Label")), "x1", 34f,
                Color.white, font, TextAlignmentOptions.Center);

            // stats bar
            RectTransform bar = NewUiChild(frame, "StatsBar");
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 940f);
            bar.sizeDelta = new Vector2(0f, 176f);
            Color barBg = Hex("#14102e");
            barBg.a = 0.96f;
            AddImage(bar, null, barBg);

            TextMeshProUGUI levelLabel = null, hpLabel = null, atkLabel = null, defLabel = null;
            Image xpFill = null;
            var statTitles = new TextMeshProUGUI[4];
            string[] iconPaths =
            {
                ItemIcons + "Icon_Star.png", ItemIcons + "Icon_Heart.png",
                ItemIcons + "Icon_Sword.png", ItemIcons + "Icon_Shield.png"
            };
            for (int i = 0; i < 4; i++)
            {
                float x = -405f + i * 270f;
                statTitles[i] = AddTmp(Place(NewUiChild(bar, "Title" + i), new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), new Vector2(x, 58f), new Vector2(250f, 30f)),
                    Loc.Get(LocKeys.HudStatTitles[i]), 20f, Hex("#8f9bd4"), font,
                    TextAlignmentOptions.Center);
                RectTransform pill = Place(NewUiChild(bar, "Pill" + i), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(x, -14f), new Vector2(252f, 86f));
                AddImage(pill, BuiltinUiSprite(), Hex("#241b4d"));
                // every pill reads [icon] [value]; the XP one adds a slim bar along the bottom
                float rowY = i == 0 ? 9f : 0f;
                RectTransform icon = Place(NewUiChild(pill, "Icon"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(14f, rowY), new Vector2(46f, 46f));
                AddImage(icon, LoadSprite(iconPaths[i]), Color.white, false, false);
                var value = AddTmp(Place(NewUiChild(pill, "Value"), new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f), new Vector2(70f, rowY), new Vector2(175f, 60f)),
                    "0", 32f, Color.white, font, TextAlignmentOptions.MidlineLeft);

                if (i == 0)
                {
                    levelLabel = value;
                    value.text = "Lv.1";
                    RectTransform track = Place(NewUiChild(pill, "XpTrack"),
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 15f),
                        new Vector2(224f, SpriteBaker.CapsuleHeight));
                    AddImage(track, art.UiCapsule, Hex("#3b2f78"));
                    // width is driven by anchorMax.x in HudView, so the fill keeps round caps
                    RectTransform fillRt = NewUiChild(track, "Fill");
                    fillRt.anchorMin = Vector2.zero;
                    fillRt.anchorMax = new Vector2(0f, 1f);
                    fillRt.pivot = new Vector2(0f, 0.5f);
                    fillRt.offsetMin = Vector2.zero;
                    fillRt.offsetMax = Vector2.zero;
                    xpFill = AddImage(fillRt, art.UiCapsule, Hex("#a98bff"));
                }
                else if (i == 1) hpLabel = value;
                else if (i == 2) atkLabel = value;
                else defLabel = value;
            }

            SetPrivate(hud, "_config", content.Config);
            SetPrivate(hud, "_roundLabel", roundLabel);
            SetPrivate(hud, "_hitsLabel", hitsLabel);
            SetPrivate(hud, "_worldLabel", worldLabel);
            SetPrivate(hud, "_speedLabel", speedLabel);
            SetPrivate(hud, "_speedButton", speedBtn);
            SetPrivate(hud, "_gearButton", gearBtn);
            SetPrivate(hud, "_homeButton", homeBtn);
            SetPrivate(hud, "_levelLabel", levelLabel);
            SetPrivate(hud, "_xpFill", xpFill);
            SetPrivate(hud, "_hpLabel", hpLabel);
            SetPrivate(hud, "_atkLabel", atkLabel);
            SetPrivate(hud, "_defLabel", defLabel);
            SetPrivate(hud, "_statTitles", statTitles);
            return hud;
        }

        private static void BuildConsoleCanvas(Transform uiRoot, Content content,
            TMP_FontAsset font, out ConsoleView console, out FortuneBanner banner,
            out ChoicePanel choices, out SidekickChipsView chips, out EngageButton engage)
        {
            Canvas canvas = NewCanvas(uiRoot, "Canvas_Console", 20, out RectTransform frame);
            console = canvas.gameObject.AddComponent<ConsoleView>();
            Sprite uiSprite = BuiltinUiSprite();

            // opaque console panel pinned to the bottom
            RectTransform panel = NewUiChild(frame, "Panel");
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(0f, 940f);
            Color panelBg = Hex("#120c28");
            panelBg.a = 0.97f;
            AddImage(panel, null, panelBg);
            RectTransform topLine = NewUiChild(panel, "TopLine");
            topLine.anchorMin = new Vector2(0f, 1f);
            topLine.anchorMax = new Vector2(1f, 1f);
            topLine.pivot = new Vector2(0.5f, 1f);
            topLine.anchoredPosition = Vector2.zero;
            topLine.sizeDelta = new Vector2(0f, 3f);
            AddImage(topLine, null, new Color(0.43f, 0.9f, 1f, 0.3f));

            // glyph noise
            var glyphLines = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                glyphLines[i] = AddTmp(Place(NewUiChild(panel, "Glyph" + i),
                        new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(40f, -18f - i * 33f), new Vector2(1000f, 30f)),
                    "", 21f, new Color(0.56f, 0.61f, 0.83f, 0.32f), font,
                    TextAlignmentOptions.MidlineLeft);
                glyphLines[i].characterSpacing = 6f;
            }

            // star cycle header
            var cycleLabel = AddTmp(Place(NewUiChild(panel, "CycleLabel"), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(40f, -160f), new Vector2(420f, 44f)),
                "Star Cycle 1", 34f, Hex("#6ee7ff"), font, TextAlignmentOptions.MidlineLeft);
            RectTransform rule = Place(NewUiChild(panel, "Rule"), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(470f, -180f), new Vector2(570f, 2f));
            AddImage(rule, null, new Color(0.43f, 0.9f, 1f, 0.18f));

            // event text box
            RectTransform eventBox = NewUiChild(panel, "EventBox");
            eventBox.anchorMin = new Vector2(0f, 1f);
            eventBox.anchorMax = new Vector2(1f, 1f);
            eventBox.pivot = new Vector2(0.5f, 1f);
            eventBox.anchoredPosition = new Vector2(0f, -214f);
            eventBox.sizeDelta = new Vector2(-60f, 252f);
            Color boxBg = Hex("#1a1240");
            boxBg.a = 0.85f;
            AddImage(eventBox, uiSprite, boxBg);
            var eventText = AddTmp(Stretch(NewUiChild(eventBox, "Text"), 28f, 28f, 22f, 22f),
                "", 30f, Hex("#e8eaf6"), font, TextAlignmentOptions.TopLeft, true);
            eventText.richText = true;
            eventText.lineSpacing = 8f;

            // sidekick chips
            var chipsGo = NewUiChild(panel, "SidekickChips");
            Place(chipsGo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -486f), new Vector2(1040f, 66f));
            chips = chipsGo.gameObject.AddComponent<SidekickChipsView>();
            var chipGos = new GameObject[3];
            var chipIcons = new Image[3];
            var chipNames = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform chip = Place(NewUiChild(chipsGo, "Chip" + i),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(10f + i * 345f, 0f), new Vector2(330f, 64f));
                AddImage(chip, uiSprite, Hex("#241b4d"));
                RectTransform ic = Place(NewUiChild(chip, "Icon"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(40f, 40f));
                chipIcons[i] = AddImage(ic, null, Color.white, false, false);
                chipNames[i] = AddTmp(Place(NewUiChild(chip, "Name"), new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(255f, 50f)),
                    "", 26f, Hex("#c9cff2"), font, TextAlignmentOptions.MidlineLeft);
                chipGos[i] = chip.gameObject;
                chip.gameObject.SetActive(false);
            }
            SetPrivate(chips, "_chips", chipGos);
            SetPrivate(chips, "_icons", chipIcons);
            SetPrivate(chips, "_names", chipNames);

            // ENGAGE
            RectTransform engageRt = Place(NewUiChild(panel, "Engage"), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -580f), new Vector2(640f, 176f));
            engage = engageRt.gameObject.AddComponent<EngageButton>();
            Image engageImg = AddImage(engageRt, LoadSprite(ButtonsDir + "Btn_MainButton_Green.Png"),
                Color.white, true);
            Button engageBtn = AddButton(engageRt, engageImg);
            var engageLabel = AddTmp(Stretch(NewUiChild(engageRt, "Label"), 0f, 0f, 0f, 14f),
                Loc.Get(LocKeys.EngageEngage), 62f, Color.white, font, TextAlignmentOptions.Center);
            SetPrivate(engage, "_button", engageBtn);
            SetPrivate(engage, "_background", engageImg);
            SetPrivate(engage, "_label", engageLabel);
            SetPrivate(engage, "_readySprite", LoadSprite(ButtonsDir + "Btn_MainButton_Green.Png"));
            SetPrivate(engage, "_lockedSprite", LoadSprite(ButtonsDir + "Btn_MainButton_Gray.Png"));
            SetPrivate(engage, "_deadSprite", LoadSprite(ButtonsDir + "Btn_MainButton_Orange.Png"));

            // fortune banner (over glyph area)
            RectTransform bannerRt = Place(NewUiChild(panel, "FortuneBanner"),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f),
                new Vector2(920f, 122f));
            banner = bannerRt.gameObject.AddComponent<FortuneBanner>();
            var group = bannerRt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            AddImage(bannerRt, uiSprite, Hex("#2a1b5e"));
            RectTransform bIcon = Place(NewUiChild(bannerRt, "Icon"), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(76f, 76f));
            Image bannerIcon = AddImage(bIcon, null, Color.white, false, false);
            var bannerTitle = AddTmp(Place(NewUiChild(bannerRt, "Title"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(116f, 12f), new Vector2(700f, 48f)),
                "", 36f, Color.white, font, TextAlignmentOptions.MidlineLeft);
            var bannerTag = AddTmp(Place(NewUiChild(bannerRt, "Tag"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(116f, -30f), new Vector2(700f, 36f)),
                Loc.Get(LocKeys.BannerSmallFortune), 24f, Hex("#ffd35c"), font,
                TextAlignmentOptions.MidlineLeft);
            SetPrivate(banner, "_group", group);
            SetPrivate(banner, "_root", bannerRt);
            SetPrivate(banner, "_icon", bannerIcon);
            SetPrivate(banner, "_title", bannerTitle);
            SetPrivate(banner, "_tag", bannerTag);
            bannerRt.gameObject.SetActive(false);

            // choice panel
            RectTransform choiceRt = Place(NewUiChild(panel, "ChoicePanel"),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f),
                new Vector2(1060f, 550f));
            choices = choiceRt.gameObject.AddComponent<ChoicePanel>();
            Color choiceBg = Hex("#0a0620");
            choiceBg.a = 0.92f;
            AddImage(choiceRt, uiSprite, choiceBg, true); // blocks clicks behind
            var cards = new ChoiceCardView[4];
            for (int i = 0; i < 4; i++)
            {
                cards[i] = BuildChoiceCard(choiceRt, font, i);
            }
            SetPrivate(choices, "_cards", cards);
            choiceRt.gameObject.SetActive(false);

            SetPrivate(console, "_narrative", content.Narrative);
            SetPrivate(console, "_cycleLabel", cycleLabel);
            SetPrivate(console, "_eventText", eventText);
            SetPrivate(console, "_glyphLines", glyphLines);
        }

        private static ChoiceCardView BuildChoiceCard(RectTransform parent, TMP_FontAsset font,
            int index)
        {
            RectTransform card = Place(NewUiChild(parent, "Card" + index),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(320f, 500f));
            var view = card.gameObject.AddComponent<ChoiceCardView>();
            Image bg = AddImage(card, BuiltinUiSprite(), Hex("#2a2258"), true);
            Button btn = AddButton(card, bg);
            RectTransform icon = Place(NewUiChild(card, "Icon"), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(120f, 120f));
            Image iconImg = AddImage(icon, null, Color.white, false, false);
            var name = AddTmp(Place(NewUiChild(card, "Name"), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -168f), new Vector2(290f, 90f)),
                "", 32f, Color.white, font, TextAlignmentOptions.Top, true);
            var desc = AddTmp(Place(NewUiChild(card, "Desc"), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(290f, 210f)),
                "", 26f, Hex("#b8bfe8"), font, TextAlignmentOptions.Top, true);
            SetPrivate(view, "_button", btn);
            SetPrivate(view, "_icon", iconImg);
            SetPrivate(view, "_name", name);
            SetPrivate(view, "_description", desc);
            return view;
        }

        /// <summary>
        /// The bottom tab strip, parented into the console panel so it hides with the rest of
        /// the play HUD. 150 px tall is ~50 dp — above the 48 dp minimum touch target — and
        /// the whole tab is the button, not just its icon.
        /// </summary>
        private static NavBarView BuildNavBar(RectTransform panel, TMP_FontAsset font)
        {
            RectTransform bar = NewUiChild(panel, "NavBar");
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, 154f);
            var nav = bar.gameObject.AddComponent<NavBarView>();

            Color navColor = Hex("#0b1734");
            navColor.a = 0.97f;
            AddImage(bar, BuiltinUiSprite(), navColor);
            var navShadow = bar.gameObject.AddComponent<Shadow>();
            navShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            navShadow.effectDistance = new Vector2(0f, 8f);
            navShadow.useGraphicAlpha = true;

            RectTransform topLine = NewUiChild(bar, "TopLine");
            topLine.anchorMin = new Vector2(0f, 1f);
            topLine.anchorMax = new Vector2(1f, 1f);
            topLine.pivot = new Vector2(0.5f, 1f);
            topLine.anchoredPosition = Vector2.zero;
            topLine.sizeDelta = new Vector2(0f, 3f);
            AddImage(topLine, null, new Color(0.36f, 0.89f, 1f, 0.28f));

            string[] icons =
            {
                PictoIcons + "Pictoicon_Anvil.Png",
                PictoIcons + "Pictoicon_Profile.Png",
                PictoIcons + "Pictoicon_Trophy_0.Png",
                PictoIcons + "Pictoicon_Setting.Png"
            };
            var buttons = new Button[NavBarView.TabCount];
            var labels = new TextMeshProUGUI[NavBarView.TabCount];
            GameObject badge = null;
            for (int i = 0; i < NavBarView.TabCount; i++)
            {
                RectTransform tab = Place(NewUiChild(bar, "Tab" + i), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2((i - 1.5f) * 258f, 10f),
                    new Vector2(236f, 130f));
                Image tabImg = AddImage(tab, BuiltinUiSprite(),
                    new Color(1f, 1f, 1f, 0.001f), true);
                buttons[i] = AddButton(tab, tabImg);

                RectTransform iconPlate = Place(NewUiChild(tab, "IconPlate"),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 20f), new Vector2(64f, 64f));
                AddImage(iconPlate, BuiltinUiSprite(), Hex("#182a52"));
                RectTransform icon = Place(NewUiChild(iconPlate, "Icon"),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(40f, 40f));
                AddImage(icon, LoadSprite(icons[i]), Hex("#dce8ff"));

                labels[i] = AddTmp(Place(NewUiChild(tab, "Label"),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -36f), new Vector2(220f, 30f)),
                    "", 22f, Hex("#aebbdc"), font, TextAlignmentOptions.Center);

                if (i != 0) continue;
                RectTransform dot = Place(NewUiChild(tab, "Badge"),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(31f, 48f), new Vector2(22f, 22f));
                AddImage(dot, BuiltinUiSprite(), Hex("#ffd35c"));
                badge = dot.gameObject;
            }

            SetPrivate(nav, "_buttons", buttons);
            SetPrivate(nav, "_labels", labels);
            SetPrivate(nav, "_metaPathBadge", badge);
            return nav;
        }

        private static OverlayView BuildOverlayCanvas(Transform uiRoot, TMP_FontAsset font)
        {
            Canvas canvas = NewCanvas(uiRoot, "Canvas_Overlay", 30, out RectTransform frame);
            RectTransform root = Stretch(NewUiChild(frame, "Overlay"));
            var overlay = root.gameObject.AddComponent<OverlayView>();
            AddImage(root, null, new Color(0f, 0f, 0f, 0.72f), true); // dim + click blocker

            RectTransform card = Place(NewUiChild(root, "Card"), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 1440f));
            Color cardBg = Hex("#1a1240");
            cardBg.a = 0.98f;
            AddImage(card, BuiltinUiSprite(), cardBg);

            var title = AddTmp(Place(NewUiChild(card, "Title"), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(780f, 80f)),
                Loc.Get(LocKeys.OverlaySettings), 58f, Hex("#ffd35c"), font,
                TextAlignmentOptions.Center);
            var body = AddTmp(Place(NewUiChild(card, "Body"), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 560f)),
                "", 33f, Hex("#e8eaf6"), font, TextAlignmentOptions.Top, true);
            body.richText = true;
            body.lineSpacing = 10f;
            RectTransform titleRule = Place(NewUiChild(card, "TitleRule"), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(700f, 2f));
            AddImage(titleRule, null, new Color(0.43f, 0.9f, 1f, 0.22f));

            // Settings: Resume / Music / SFX / Language / Star MetaPath / Restart —
            // see GameManager.ShowSettings. "Reset all data" lives on the metaPath screen.
            string[] btnSprites =
            {
                ButtonsDir + "Btn_MainButton_Green.Png",
                ButtonsDir + "Btn_MainButton_Sky.Png",
                ButtonsDir + "Btn_MainButton_Sky.Png",
                ButtonsDir + "Btn_MainButton_Sky.Png",
                ButtonsDir + "Btn_MainButton_Orange.Png",
                ButtonsDir + "Btn_MainButton_Red.Png"
            };
            var buttons = new Button[OverlayView.MaxButtons];
            var labels = new TextMeshProUGUI[OverlayView.MaxButtons];
            for (int i = 0; i < OverlayView.MaxButtons; i++)
            {
                RectTransform b = Place(NewUiChild(card, "Button" + i), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 620f - i * 112f), new Vector2(600f, 104f));
                Image img = AddImage(b, LoadSprite(btnSprites[i]), Color.white, true);
                buttons[i] = AddButton(b, img);
                labels[i] = AddTmp(Stretch(NewUiChild(b, "Label"), 0f, 0f, 0f, 10f), "Button",
                    36f, Color.white, font, TextAlignmentOptions.Center);
            }

            // round ✕ hanging below the card: thumb-reachable and never over the content
            RectTransform close = Place(NewUiChild(card, "Close"), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(112f, 112f));
            Image closeImg = AddImage(close,
                LoadSprite(ButtonsDir + "Btn_OtherButton_Circle01_n.png"), Color.white, true);
            Button closeBtn = AddButton(close, closeImg);
            AddImage(Place(NewUiChild(close, "X"), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(52f, 52f)),
                LoadSprite(PictoIcons + "Pictoicon_Close.Png"), Color.white);

            SetPrivate(overlay, "_card", card);
            SetPrivate(overlay, "_title", title);
            SetPrivate(overlay, "_body", body);
            SetPrivate(overlay, "_buttons", buttons);
            SetPrivate(overlay, "_buttonLabels", labels);
            SetPrivate(overlay, "_closeButton", closeBtn);
            root.gameObject.SetActive(false);
            return overlay;
        }

        // ================= main menu =================

        /// <summary>
        /// The front screen. Unlike the metaPath it is only a scrim, because the world backdrop
        /// and the idling hero are the art — UIController parks the play HUD while it is up.
        /// </summary>
        private static MainMenuView BuildMenuCanvas(Transform uiRoot, TMP_FontAsset font,
            out NavBarView nav)
        {
            Canvas canvas = NewCanvas(uiRoot, "Canvas_Menu", 28, out RectTransform frame);
            RectTransform root = Stretch(NewUiChild(frame, "Menu"));
            var menu = root.gameObject.AddComponent<MainMenuView>();
            AddImage(root, null, new Color(0.01f, 0.005f, 0.05f, 0.10f), true);

            Sprite purplePanel = LoadSprite(PopupsDir + "Popup_Frame01_Purple.png");
            Sprite shard = LoadSprite(ShardIcon);
            Sprite uiSprite = BuiltinUiSprite();

            // Keep the character and world as the hero art. A restrained glow adds focus
            // without covering the live world with another decorative card.
            RectTransform heroAura = Place(NewUiChild(root, "HeroAura"),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 380f), new Vector2(620f, 620f));
            Color auraColor = Hex("#58e7ff");
            auraColor.a = 0.10f;
            AddImage(heroAura, LoadSprite(SoftGlow), auraColor, false, false);

            Color hudColor = Hex("#101d42");
            hudColor.a = 0.94f;

            RectTransform chip = Place(NewUiChild(root, "ProfileChip"), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(268f, 84f));
            Image chipImg = AddImage(chip, uiSprite, hudColor, true);
            Button profileBtn = AddButton(chip, chipImg);
            var chipShadow = chip.gameObject.AddComponent<Shadow>();
            chipShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            chipShadow.effectDistance = new Vector2(0f, -6f);
            chipShadow.useGraphicAlpha = true;

            RectTransform avatarBase = Place(NewUiChild(chip, "AvatarBase"),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(10f, 0f), new Vector2(66f, 66f));
            AddImage(avatarBase, purplePanel, Color.white);
            AddImage(Place(NewUiChild(avatarBase, "Avatar"), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f)),
                LoadSprite(PictoIcons + "Pictoicon_Profile.Png"), Color.white);
            var levelLabel = AddTmp(Place(NewUiChild(chip, "Level"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(158f, 48f)),
                "Lv.1", 30f, Color.white, font, TextAlignmentOptions.MidlineLeft);

            RectTransform shardChip = Place(NewUiChild(root, "ShardChip"),
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(200f, 84f));
            AddImage(shardChip, uiSprite, hudColor);
            var shardShadow = shardChip.gameObject.AddComponent<Shadow>();
            shardShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shardShadow.effectDistance = new Vector2(0f, -6f);
            shardShadow.useGraphicAlpha = true;
            RectTransform shardGlow = Place(NewUiChild(shardChip, "IconGlow"),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(8f, 0f), new Vector2(70f, 70f));
            Color shardGlowColor = Hex("#ffd35c");
            shardGlowColor.a = 0.25f;
            AddImage(shardGlow, LoadSprite(SoftGlow), shardGlowColor, false, false);
            AddImage(Place(NewUiChild(shardChip, "Icon"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(17f, 0f), new Vector2(48f, 48f)),
                shard, Color.white);
            var shardLabel = AddTmp(Place(NewUiChild(shardChip, "Count"),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(78f, 0f), new Vector2(104f, 48f)),
                "0", 32f, Hex("#ffe27a"), font, TextAlignmentOptions.MidlineLeft);

            var title = AddTmp(Place(NewUiChild(root, "Title"), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -140f),
                    new Vector2(600f, 48f)),
                Loc.Get(LocKeys.MenuTitle), 34f, Hex("#d8e2ff"), font,
                TextAlignmentOptions.Center);
            title.characterSpacing = 2f;

            RectTransform mission = Place(NewUiChild(root, "MissionCard"),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 575f), new Vector2(870f, 190f));
            Color missionColor = Hex("#111d40");
            missionColor.a = 0.96f;
            AddImage(mission, uiSprite, missionColor);
            var missionShadow = mission.gameObject.AddComponent<Shadow>();
            missionShadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
            missionShadow.effectDistance = new Vector2(0f, -10f);
            missionShadow.useGraphicAlpha = true;

            RectTransform accent = Place(NewUiChild(mission, "Accent"),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(8f, 128f));
            AddImage(accent, uiSprite, Hex("#61e6ff"));

            RectTransform worldBase = Place(NewUiChild(mission, "WorldBase"),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(28f, 0f), new Vector2(104f, 104f));
            Color worldBaseColor = Hex("#29366a");
            worldBaseColor.a = 0.95f;
            AddImage(worldBase, uiSprite, worldBaseColor);
            AddImage(Place(NewUiChild(worldBase, "WorldIcon"),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(60f, 60f)),
                LoadSprite(PictoIcons + "Pictoicon_Planet.Png"), Hex("#7deaff"));

            var progress = AddTmp(Place(NewUiChild(mission, "Progress"),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(156f, 35f), new Vector2(665f, 48f)),
                "", 32f, Color.white, font, TextAlignmentOptions.MidlineLeft);
            var best = AddTmp(Place(NewUiChild(mission, "Best"),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(156f, -18f), new Vector2(665f, 34f)),
                "", 23f, Hex("#8fe8ff"), font, TextAlignmentOptions.MidlineLeft);

            RectTransform progressTrack = Place(NewUiChild(mission, "ProgressTrack"),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(156f, -62f), new Vector2(664f, 14f));
            AddImage(progressTrack, uiSprite, Hex("#26345b"));
            RectTransform fill = Stretch(NewUiChild(progressTrack, "Fill"));
            Image progressFill = AddImage(fill, uiSprite, Hex("#56e3dc"));
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            progressFill.fillAmount = 0.2f;

            RectTransform ctaGlow = Place(NewUiChild(root, "CtaGlow"),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 370f), new Vector2(900f, 300f));
            Color ctaGlowColor = Hex("#ffb84f");
            ctaGlowColor.a = 0.16f;
            AddImage(ctaGlow, LoadSprite(SoftGlow), ctaGlowColor, false, false);

            RectTransform start = Place(NewUiChild(root, "Start"), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 370f),
                new Vector2(790f, 154f));
            Image startImg = AddImage(start,
                LoadSprite(ButtonsDir + "Btn_MainButton_Orange.Png"), Color.white, true);
            Button startBtn = AddButton(start, startImg);
            AddImage(Place(NewUiChild(start, "PlayIcon"), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(-270f, 5f),
                    new Vector2(60f, 60f)),
                LoadSprite(PictoIcons + "Pictoicon_Control_Play.Png"), Hex("#28173f"));
            var startLabel = AddTmp(Stretch(NewUiChild(start, "Label"), 112f, 36f, 0f, 12f),
                Loc.Get(LocKeys.MenuContinue), 54f, Hex("#28173f"), font,
                TextAlignmentOptions.Center);

            RectTransform newRun = Place(NewUiChild(root, "NewRun"), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 215f),
                new Vector2(420f, 76f));
            Color secondaryColor = Hex("#182952");
            secondaryColor.a = 0.96f;
            Image newRunImg = AddImage(newRun, uiSprite, secondaryColor, true);
            Button newRunBtn = AddButton(newRun, newRunImg);
            var newRunLabel = AddTmp(Stretch(NewUiChild(newRun, "Label"), 0f, 0f, 0f, 4f),
                Loc.Get(LocKeys.MenuNewRun), 27f, Hex("#c6d3f2"), font,
                TextAlignmentOptions.Center);

            nav = BuildNavBar(root, font);

            SetPrivate(menu, "_heroPresentation",
                Object.FindFirstObjectByType<HeroView>()?.transform);
            SetPrivate(menu, "_profileChip", profileBtn);
            SetPrivate(menu, "_levelLabel", levelLabel);
            SetPrivate(menu, "_shardLabel", shardLabel);
            SetPrivate(menu, "_title", title);
            SetPrivate(menu, "_progress", progress);
            SetPrivate(menu, "_best", best);
            SetPrivate(menu, "_progressFill", progressFill);
            SetPrivate(menu, "_startButton", startBtn);
            SetPrivate(menu, "_startLabel", startLabel);
            SetPrivate(menu, "_newRunButton", newRunBtn);
            SetPrivate(menu, "_newRunLabel", newRunLabel);
            root.gameObject.SetActive(false);
            return menu;
        }

        // ================= star metaPath path =================

        private const float StepSpacing = 195f;   // centre-to-centre between two steps
        private const float MedallionSize = 124f;
        private const float RailX = -382f;        // the climb runs up the left edge
        private const float RailWidth = 14f;
        private const float BottomStepY = -470f;

        private static float StepY(int index) => BottomStepY + index * StepSpacing;

        /// <summary>
        /// The Star MetaPath screen: one rail climbing the left side with a rune medallion per
        /// step and its detail strip beside it. Step count and order come from
        /// GameConfig.MetaUpgrades, so re-shaping the path means editing that data, not this.
        /// </summary>
        private static MetaPathView BuildMetaPathCanvas(Transform uiRoot, Content content,
            TMP_FontAsset font)
        {
            Canvas canvas = NewCanvas(uiRoot, "Canvas_MetaPath", 25, out RectTransform frame);
            RectTransform root = Stretch(NewUiChild(frame, "MetaPath"));
            var metaPath = root.gameObject.AddComponent<MetaPathView>();
            // a full screen, not a popup: opaque, so nothing of the run shows through
            AddImage(root, null, Hex("#0a0620"), true);

            Sprite uiSprite = BuiltinUiSprite();
            Sprite shard = LoadSprite(ShardIcon);
            Sprite glowSprite = LoadSprite(SoftGlow);
            Color pillColor = Hex("#1a1240");
            pillColor.a = 0.92f;

            var title = AddTmp(Place(NewUiChild(root, "Title"), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 80f)),
                Loc.Get(LocKeys.MetaPathTitle), 62f, Hex("#ffd35c"), font,
                TextAlignmentOptions.Center);

            // shard balance pill: star glyph + count
            RectTransform pill = Place(NewUiChild(root, "ShardPill"), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(320f, 76f));
            AddImage(pill, uiSprite, pillColor);
            AddImage(Place(NewUiChild(pill, "Icon"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(18f, 0f), new Vector2(46f, 46f)), shard, Color.white);
            var shards = AddTmp(Place(NewUiChild(pill, "Count"), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(220f, 56f)),
                "0", 42f, Hex("#ffd35c"), font, TextAlignmentOptions.Left);

            var hint = AddTmp(Place(NewUiChild(root, "Hint"), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -216f), new Vector2(880f, 90f)),
                Loc.Get(LocKeys.MetaPathIntro), 25f, Hex("#a9aed0"), font,
                TextAlignmentOptions.Top, true);

            MetaUpgrade[] steps = content.Config.MetaUpgrades;
            RectTransform path = Place(NewUiChild(root, "Path"), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1080f, 1500f));

            // rail segments first, so the medallions sit on top of them
            float railLength = StepSpacing - MedallionSize + 12f;
            var railFills = new RectTransform[steps.Length - 1];
            var railLengths = new float[steps.Length - 1];
            for (int i = 0; i + 1 < steps.Length; i++)
            {
                float bottom = StepY(i) + MedallionSize * 0.5f - 6f;
                RectTransform track = Place(NewUiChild(path, "Rail" + i),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(RailX, bottom),
                    new Vector2(RailWidth, railLength));
                AddImage(track, null, Hex("#3b2f6b")); // must stay readable against #0a0620
                RectTransform fill = Place(NewUiChild(track, "Fill"), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), Vector2.zero, new Vector2(RailWidth, 0f));
                AddImage(fill, null, Hex("#ffd35c"));
                railFills[i] = fill;
                railLengths[i] = railLength;
            }

            var nodes = new MetaNodeView[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                nodes[i] = BuildMetaPathStep(path, steps[i], StepY(i), uiSprite, shard, glowSprite,
                    font);
            }

            RectTransform close = Place(NewUiChild(root, "Close"), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 176f), new Vector2(600f, 104f));
            Image closeImg = AddImage(close, LoadSprite(ButtonsDir + "Btn_MainButton_Green.Png"),
                Color.white, true);
            Button closeBtn = AddButton(close, closeImg);
            var closeLabel = AddTmp(Stretch(NewUiChild(close, "Label"), 0f, 0f, 0f, 10f),
                Loc.Get(LocKeys.MetaPathBack), 36f, Color.white, font, TextAlignmentOptions.Center);

            RectTransform reset = Place(NewUiChild(root, "Reset"), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 74f), new Vector2(420f, 72f));
            Image resetImg = AddImage(reset, LoadSprite(ButtonsDir + "Btn_MainButton_Red.Png"),
                Color.white, true);
            Button resetBtn = AddButton(reset, resetImg);
            var resetLabel = AddTmp(Stretch(NewUiChild(reset, "Label"), 0f, 0f, 0f, 8f),
                Loc.Get(LocKeys.OverlayResetAll), 26f, Color.white, font,
                TextAlignmentOptions.Center);

            SetPrivate(metaPath, "_config", content.Config);
            SetPrivate(metaPath, "_nodes", nodes);
            SetPrivate(metaPath, "_railFills", railFills);
            SetPrivate(metaPath, "_railLengths", railLengths);
            SetPrivate(metaPath, "_title", title);
            SetPrivate(metaPath, "_shards", shards);
            SetPrivate(metaPath, "_hint", hint);
            SetPrivate(metaPath, "_closeButton", closeBtn);
            SetPrivate(metaPath, "_closeLabel", closeLabel);
            SetPrivate(metaPath, "_resetButton", resetBtn);
            SetPrivate(metaPath, "_resetLabel", resetLabel);
            root.gameObject.SetActive(false);
            return metaPath;
        }

        /// <summary>
        /// One step: the detail strip is the button (a comfortable tap target that spans the
        /// row), with the medallion riding the rail on its left.
        /// </summary>
        private static MetaNodeView BuildMetaPathStep(RectTransform path, MetaUpgrade step,
            float y, Sprite panelSprite, Sprite shard, Sprite glowSprite, TMP_FontAsset font)
        {
            RectTransform rt = Place(NewUiChild(path, "Step_" + step.Id),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, y),
                new Vector2(800f, 152f));
            var view = rt.gameObject.AddComponent<MetaNodeView>();
            Image panel = AddImage(rt, panelSprite, Hex("#1a1240"), true);
            Button btn = AddButton(rt, panel);

            // the medallion lives outside the strip, centred on the rail
            RectTransform medallion = Place(NewUiChild(path, "Rune_" + step.Id),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(RailX, y),
                new Vector2(MedallionSize, MedallionSize));
            // wide enough to read as an aura, tight enough not to spill off the screen edge
            float glowSize = MedallionSize * 1.78f;
            Image glow = AddImage(Place(NewUiChild(medallion, "Glow"), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(glowSize, glowSize)),
                glowSprite, Color.white);
            glow.enabled = false;
            Image icon = AddImage(Stretch(NewUiChild(medallion, "Rune")), step.RankIcon(0),
                Color.white);

            var name = AddTmp(Place(NewUiChild(rt, "Name"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(34f, 36f), new Vector2(470f, 38f)),
                step.DisplayName, 31f, Color.white, font, TextAlignmentOptions.Left);
            var desc = AddTmp(Place(NewUiChild(rt, "Desc"), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(34f, -2f), new Vector2(470f, 34f)),
                step.Description, 24f, Hex("#b8bfe8"), font, TextAlignmentOptions.Left);

            // one pip per rank, lit as ranks are bought
            var pips = new Image[step.MaxRank];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = AddImage(Place(NewUiChild(rt, "Pip" + i), new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f), new Vector2(36f + i * 30f, -44f),
                        new Vector2(20f, 20f)),
                    null, Color.white);
            }

            Image costIcon = AddImage(Place(NewUiChild(rt, "CostIcon"), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-186f, 0f), new Vector2(34f, 34f)),
                shard, Color.white);
            var cost = AddTmp(Place(NewUiChild(rt, "Cost"), new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(150f, 44f)),
                "", 30f, Color.white, font, TextAlignmentOptions.Right);

            SetPrivate(view, "_button", btn);
            SetPrivate(view, "_panel", panel);
            SetPrivate(view, "_medallion", medallion);
            SetPrivate(view, "_icon", icon);
            SetPrivate(view, "_glow", glow);
            SetPrivate(view, "_name", name);
            SetPrivate(view, "_description", desc);
            SetPrivate(view, "_cost", cost);
            SetPrivate(view, "_costIcon", costIcon);
            SetPrivate(view, "_pips", pips);
            return view;
        }
    }
}
