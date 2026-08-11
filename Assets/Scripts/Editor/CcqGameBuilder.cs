using CCQ.Audio;
using CCQ.Combat;
using CCQ.Core;
using CCQ.Data;
using CCQ.Enemies;
using CCQ.Planets;
using CCQ.Sidekicks;
using CCQ.UI;
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
using static CCQ.EditorTools.CcqBuilderUtil;

namespace CCQ.EditorTools
{
    /// <summary>
    /// One-shot builder: creates config assets, prefabs and the playable scene with every
    /// reference wired. Idempotent — rerunning overwrites the scene and re-wires content
    /// (numeric balance on existing config assets is left untouched).
    /// </summary>
    public static class CcqGameBuilder
    {
        private const string DataDir = "Assets/Data";
        private const string UiDataDir = "Assets/Data/UI";
        private const string PrefabDir = "Assets/Prefabs";
        private const string ScenePath = "Assets/Scenes/CosmicCritterQuest.unity";
        private const string FrameName = "Frame";

        private const string ItemIcons =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Icon_ItemIcons(x2)/128/";
        private const string PictoIcons =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Icon_PictoIcons(x2)/128/";
        private const string ButtonsDir =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Sprite/Component/Button/";
        private const string FontTtf =
            "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular.ttf";

        private class Content
        {
            public GameConfig Config;
            public NarrativeConfig Narrative;
        }

        private class Prefabs
        {
            public GameObject Hero;
            public GameObject Critter;
            public GameObject Floater;
            public GameObject BurstStar;
            public GameObject Orb;
            public GameObject HpBar;
        }

        [MenuItem("Tools/CCQ/Build Game (Full)")]
        public static void BuildFull()
        {
            EnsureFolder(DataDir);
            EnsureFolder(UiDataDir);
            EnsureFolder(DataDir + "/Upgrades");
            EnsureFolder(DataDir + "/Fortunes");
            EnsureFolder(DataDir + "/Sidekicks");
            EnsureFolder(DataDir + "/Planets");
            EnsureFolder(PrefabDir);
            EnsureFolder("Assets/Scenes");

            TMP_FontAsset font = BuildFont(out Material worldTextMat);
            Content content = BuildConfigs();
            // Bake every procedural sprite to a .png asset first — prefabs and the scene only
            // ever reference these, nothing paints at runtime.
            CcqSpriteBaker.Sprites art = CcqSpriteBaker.BakeAll(content.Config);
            Prefabs prefabs = BuildPrefabs(content, art, font, worldTextMat);
            BuildScene(content, art, prefabs, font, worldTextMat);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Builder] Cosmic Critter Quest build complete → " + ScenePath);
        }

        // ================= font =================

        private static TMP_FontAsset BuildFont(out Material worldTextMat)
        {
            string fontPath = UiDataDir + "/LilitaOne SDF.asset";
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (fa == null)
            {
                var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtf);
                if (ttf != null)
                {
                    fa = TMP_FontAsset.CreateFontAsset(ttf, 70, 7, GlyphRenderMode.SDFAA,
                        1024, 1024, AtlasPopulationMode.Dynamic, true);
                }
                if (fa != null)
                {
                    fa.name = "LilitaOne SDF";
                    AssetDatabase.CreateAsset(fa, fontPath);
                    fa.material.name = "LilitaOne SDF Material";
                    fa.atlasTexture.name = "LilitaOne SDF Atlas";
                    AssetDatabase.AddObjectToAsset(fa.material, fa);
                    AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
                    AssetDatabase.SaveAssets();
                }
            }
            if (fa == null)
            {
                Debug.LogWarning("[Builder] LilitaOne unavailable — falling back to TMP default font");
                fa = TMP_Settings.defaultFontAsset;
            }

            string matPath = UiDataDir + "/CcqWorldText.mat";
            worldTextMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (worldTextMat == null)
            {
                worldTextMat = new Material(fa.material);
                worldTextMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
                worldTextMat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.85f));
                AssetDatabase.CreateAsset(worldTextMat, matPath);
            }
            return fa;
        }

        // ================= configs =================

        private static Content BuildConfigs()
        {
            // ---- planets ----
            var planets = new Planet[5];
            planets[0] = MakePlanet("Verdania", "#2b1b5e", "#123a6b", "#1fa8a0", "#0f7c86",
                "#1b6d68", "#274b7a", "#ffd98a", new[] { "#ff5fae", "#c95fff", "#37e0b8" }, 110f, false);
            planets[1] = MakePlanet("Pyros", "#4a1035", "#7a2410", "#e86a28", "#a83c0f",
                "#6b2a14", "#5c2020", "#ffe9c9", new[] { "#ffd35c", "#ff7a3d", "#ff4a6b" }, 98f, true);
            planets[2] = MakePlanet("Glacius", "#101a4a", "#1f5a8f", "#6fd8ff", "#2e9ad4",
                "#3a6ea8", "#2a4a8a", "#eaf6ff", new[] { "#aef2ff", "#7a9bff", "#e2c9ff" }, 123.47f, false);
            planets[3] = MakePlanet("Fungaria", "#1c0f3a", "#3a1a5e", "#8a4ad4", "#5c24a0",
                "#4a2a7a", "#38205e", "#d3ffb8", new[] { "#5cff8f", "#c9ff5c", "#ff9bdd" }, 87.31f, true);
            planets[4] = MakePlanet("Voidreach", "#05030f", "#1a0a2e", "#e83d8f", "#8f1458",
                "#2a0f35", "#1c0f2e", "#ff9bce", new[] { "#ff2e7a", "#8f2eff", "#2effd8" }, 73.42f, true);

            // ---- sidekicks ----
            var sidekicks = new Sidekick[4];
            sidekicks[0] = MakeSidekick("blob", "Gloop", "adds +15% of your ATK each strike",
                PictoIcons + "Pictoicon_Fist.Png", "#6ee7ff", SidekickType.Damage, 0.15f);
            sidekicks[1] = MakeSidekick("medic", "Sporeling", "heals 2% Max HP each beat",
                PictoIcons + "Pictoicon_Mushroom.Png", "#8bf07a", SidekickType.Heal, 0.02f);
            sidekicks[2] = MakeSidekick("shield", "Orbit", "blocks 12% incoming damage",
                PictoIcons + "Pictoicon_Magic_Ball.Png", "#c79bff", SidekickType.Block, 0.12f);
            sidekicks[3] = MakeSidekick("spark", "Zappy", "crit chance +6%",
                PictoIcons + "Pictoicon_Thunder.Png", "#ffd35c", SidekickType.Crit, 0.06f);

            // ---- fortunes ----
            var fortunes = new Fortune[5];
            fortunes[0] = MakeFortune("Max HP +7%", ItemIcons + "Icon_Heart.png",
                new StatMod(StatModType.MaxHpPct, 0.07f));
            fortunes[1] = MakeFortune("ATK +5%", ItemIcons + "Icon_Sword.png",
                new StatMod(StatModType.AtkPct, 0.05f));
            fortunes[2] = MakeFortune("DEF +2", ItemIcons + "Icon_Shield.png",
                new StatMod(StatModType.DefFlat, 2f));
            fortunes[3] = MakeFortune("Crit +3%", ItemIcons + "Icon_Clover.png",
                new StatMod(StatModType.CritChance, 0.03f));
            fortunes[4] = MakeFortune("Heal 20% HP", ItemIcons + "Icon_Potion02_Green.png",
                new StatMod(StatModType.HealNowPct, 0.20f));

            // ---- upgrades ----
            var upgrades = new UpgradeCard[8];
            upgrades[0] = MakeUpgrade("Plasma Cell", "Cosmic ATK +18%",
                ItemIcons + "Icon_Energy_Green.png", new StatMod(StatModType.AtkPct, 0.18f));
            upgrades[1] = MakeUpgrade("Gene Splice", "Max HP +22%",
                PictoIcons + "Pictoicon_Life.Png", new StatMod(StatModType.MaxHpPct, 0.22f));
            upgrades[2] = MakeUpgrade("Orbital Plating", "Shield DEF +4",
                ItemIcons + "Icon_Shield.png", new StatMod(StatModType.DefFlat, 4f));
            upgrades[3] = MakeUpgrade("Symbiote Fangs", "Lifesteal 8% of damage",
                ItemIcons + "Icon_Tooth.png", new StatMod(StatModType.Lifesteal, 0.08f));
            upgrades[4] = MakeUpgrade("Targeting Visor", "Crit chance +8%",
                ItemIcons + "Icon_Target.png", new StatMod(StatModType.CritChance, 0.08f));
            upgrades[5] = MakeUpgrade("Spike Membrane", "Thorns: reflect 20% dmg",
                PictoIcons + "Pictoicon_Cactus.Png", new StatMod(StatModType.Thorns, 0.20f));
            upgrades[6] = MakeUpgrade("Unstable Core", "ATK +30% but Max HP -10%",
                PictoIcons + "Pictoicon_Boom.Png", new StatMod(StatModType.AtkPct, 0.30f),
                new StatMod(StatModType.MaxHpMult, 0.9f));
            upgrades[7] = MakeUpgrade("Nano Serum", "Heal 45% HP instantly",
                ItemIcons + "Icon_Potion01_Red.png", new StatMod(StatModType.HealNowPct, 0.45f));

            // ---- narrative ----
            var narrative = LoadOrCreateAsset<NarrativeConfig>(DataDir + "/NarrativeConfig.asset");
            SetPrivate(narrative, "_battleIntros", new[]
            {
                "*Cosmic Critters* emerge from the bio-luminescent flora; engage defensive protocols!",
                "A wild *{e}* blocks the trail, hissing static; you have no choice but to strike first.",
                "Sensors ping - *{e}* burrows out of the glowshroom bed, fangs first!",
                "The lake bubbles... a hungry *{e}* surfaces and charges!",
                "Your translator crackles: \"*{e}* claims this territory. Prepare!\""
            });
            SetPrivate(narrative, "_eliteIntros", new[]
            {
                "An *irradiated {e}* looms ahead, crackling with unstable energy!",
                "This *{e}* has feasted on starlight - larger, meaner, hungrier."
            });
            SetPrivate(narrative, "_bossIntros", new[]
            {
                "The ground trembles. *{e}*, tyrant of this world, descends!",
                "All flora dims. *{e}* has found you."
            });
            SetPrivate(narrative, "_winTexts", new[]
            {
                "The *{e}* dissolves into stardust. You absorb <b>+{xp} XP</b>.",
                "*{e}* flees into the flora, defeated. <b>+{xp} XP</b> gathered.",
                "Threat neutralized. Cosmic residue grants <b>+{xp} XP</b>."
            });
            SetPrivate(narrative, "_fortuneTexts", new[]
            {
                "A drifting *spore of luck* settles on your antenna.",
                "You sip glowing dew from a crystal leaf. Refreshing!",
                "A tiny *star fragment* fuses with your suit."
            });
            SetPrivate(narrative, "_choiceTexts", new[]
            {
                "A derelict *supply pod* cracks open, offering strange tech...",
                "The flora whispers of *mutation*. Choose your evolution:",
                "An ancient vending machine hums to life. *Pick one:*"
            });
            SetPrivate(narrative, "_springTexts", new[]
            {
                "You discover a *bio-luminescent spring* and soak your weary tentacles. <b>+{heal} HP</b>.",
                "Friendly micro-critters knit your wounds. <b>+{heal} HP</b>."
            });
            SetPrivate(narrative, "_sidekickTexts", new[]
            {
                "A curious *{s}* bobs out of the flora and decides you are its best friend!",
                "*{s}* the cosmic critter joins your voyage!"
            });
            SetPrivate(narrative, "_sidekickFullTexts", new[]
            {
                "It waves goodbye and gifts you a *snack* instead. <b>+{heal} HP</b>."
            });
            SetPrivate(narrative, "_trapTexts", new[]
            {
                "You step on a *snapvine*! It lashes out before you break free. <b>-{dmg} HP</b>.",
                "A gas bloom bursts - *toxic spores*! <b>-{dmg} HP</b>."
            });
            SetPrivate(narrative, "_treasureTexts", new[]
            {
                "Half-buried in the moss: an *alien artifact*! Analyzing grants <b>+{xp} XP</b>.",
                "You crack open a *meteor geode* full of knowledge crystals. <b>+{xp} XP</b>."
            });
            SetPrivate(narrative, "_planetClearTexts", new[]
            {
                "The skies calm. *{p}* is pacified - your ship beams you to the next world."
            });
            SetPrivate(narrative, "_levelUpSuffix", " <b>Level up! Lv.{lv}</b>");
            SetPrivate(narrative, "_introNewRun",
                "You beam down onto *{p}* - bio-luminescent flora hums in the dusk. Tap <b>ENGAGE</b> to explore.");
            SetPrivate(narrative, "_introResume",
                "Signal restored - your voyage on *{p}* continues. Tap <b>ENGAGE</b>.");
            SetPrivate(narrative, "_deathText",
                "Your suit's life support fades... the *{e}* was too much. The mothership retrieves your escape pod.");
            SetPrivate(narrative, "_critterNames", new[]
            {
                "Wobblor", "Zorp", "Muncher", "Blinkoid", "Squishex", "Gnarp", "Floob", "Krellik"
            });
            SetPrivate(narrative, "_bossNames", new[]
            {
                "Overmind Gluttox", "Warden Xal", "The Devourer", "Empress Vex", "Null Titan"
            });
            SetPrivate(narrative, "_glyphChars", "0123456789<>/|+=*#@%&?!~^");

            // ---- game config (content arrays only; numbers keep asset values) ----
            var config = LoadOrCreateAsset<GameConfig>(DataDir + "/GameConfig.asset");
            SetPrivate(config, "_upgrades", upgrades);
            SetPrivate(config, "_fortunes", fortunes);
            SetPrivate(config, "_sidekicks", sidekicks);
            SetPrivate(config, "_planets", planets);
            SetPrivate(config, "_critterColors", new[]
            {
                Hex("#ff6b8f"), Hex("#ffa53d"), Hex("#8f6bff"), Hex("#4ad48f"),
                Hex("#ff5c5c"), Hex("#5cb8ff"), Hex("#e8d43d")
            });

            return new Content { Config = config, Narrative = narrative };
        }

        private static Planet MakePlanet(string name, string sky1, string sky2, string lake,
            string lakeDeep, string ground, string rock, string moon, string[] flora,
            float rootHz, bool minor)
        {
            var p = LoadOrCreateAsset<Planet>(DataDir + "/Planets/Planet_" + name + ".asset");
            SetPrivate(p, "_displayName", name);
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

        private static Sidekick MakeSidekick(string id, string name, string desc, string iconPath,
            string colorHex, SidekickType type, float value)
        {
            var s = LoadOrCreateAsset<Sidekick>(DataDir + "/Sidekicks/Sidekick_" + name + ".asset");
            SetPrivate(s, "_id", id);
            SetPrivate(s, "_displayName", name);
            SetPrivate(s, "_description", desc);
            SetPrivate(s, "_icon", LoadSprite(iconPath));
            SetPrivate(s, "_color", Hex(colorHex));
            SetPrivate(s, "_type", type);
            SetPrivate(s, "_value", value);
            return s;
        }

        private static Fortune MakeFortune(string name, string iconPath, params StatMod[] mods)
        {
            string file = name.Replace(" ", "").Replace("%", "").Replace("+", "");
            var f = LoadOrCreateAsset<Fortune>(DataDir + "/Fortunes/Fortune_" + file + ".asset");
            SetPrivate(f, "_displayName", name);
            SetPrivate(f, "_icon", LoadSprite(iconPath));
            SetPrivate(f, "_mods", mods);
            return f;
        }

        private static UpgradeCard MakeUpgrade(string name, string desc, string iconPath,
            params StatMod[] mods)
        {
            var u = LoadOrCreateAsset<UpgradeCard>(
                DataDir + "/Upgrades/Upgrade_" + name.Replace(" ", "") + ".asset");
            SetPrivate(u, "_displayName", name);
            SetPrivate(u, "_description", desc);
            SetPrivate(u, "_icon", LoadSprite(iconPath));
            SetPrivate(u, "_mods", mods);
            return u;
        }

        // ================= prefabs =================

        private static Prefabs BuildPrefabs(Content content, CcqSpriteBaker.Sprites art,
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
            // Critter — one renderer per baked layer, stacked in draw order
            {
                var go = new GameObject("Critter");
                var view = go.AddComponent<CritterView>();
                SpriteRenderer shadow = NewSprite(go, "Shadow", 3, art.CritterShadow,
                    new Vector3(0f, 0.02f, 0f));
                SpriteRenderer glow = NewSprite(go, "Glow", 4, art.CritterGlow);
                SpriteRenderer horns = NewSprite(go, "Horns", 5, art.CritterHorns);
                SpriteRenderer spikes = NewSprite(go, "Spikes", 6, art.CritterSpikes);
                SpriteRenderer body = NewSprite(go, "Body", 7, art.CritterBodies[0]);
                SpriteRenderer spots = NewSprite(go, "Spots", 8, art.CritterSpots);
                SpriteRenderer eyes = NewSprite(go, "Eyes", 9, art.CritterEyes[0]);
                SpriteRenderer mouth = NewSprite(go, "Mouth", 10, art.CritterMouth);
                SpriteRenderer flash = NewSprite(go, "Flash", 11, art.CritterFlash,
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
                SetPrivate(view, "_bodySprites", art.CritterBodies);
                SetPrivate(view, "_eyeSprites", art.CritterEyes);
                SetPrivate(view, "_tintLayers",
                    new[] { glow, horns, spikes, body, spots, eyes, mouth });
                prefabs.Critter = SavePrefab(go, PrefabDir + "/Critter.prefab");
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

        private static void BuildScene(Content content, CcqSpriteBaker.Sprites art,
            Prefabs prefabs, TMP_FontAsset font, Material worldMat)
        {
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
            var bgGo = new GameObject("PlanetBackground");
            var background = bgGo.AddComponent<PlanetBackgroundRenderer>();
            Planet firstPlanet = content.Config.Planets[0];
            SpriteRenderer sky = NewSprite(bgGo, "Sky", -12, firstPlanet.SkyLayer);
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
                groundCopies[i] = NewSprite(bgGo, "Ground" + i, -10, firstPlanet.GroundLayer);
                lakeGlows[i] = NewSprite(groundCopies[i].gameObject, "LakeGlow", -9, art.LakeGlow,
                    new Vector3(0f, CcqSpriteBaker.BgLakeLocalY, 0f));
            }
            SetPrivate(background, "_sky", sky);
            SetPrivate(background, "_groundCopies", groundCopies);
            SetPrivate(background, "_lakeGlows", lakeGlows);
            SetPrivate(background, "_twinkles", twinkles);
            // world metrics must match the pixels the baker produced
            SetPrivate(background, "_worldWidth", CcqSpriteBaker.BgWorldWidth);
            SetPrivate(background, "_worldHeight", CcqSpriteBaker.BgWorldHeight);
            SetPrivate(background, "_bottomWorldY", CcqSpriteBaker.BgBottomY);
            SetPrivate(background, "_horizonWorldY", CcqSpriteBaker.BgHorizonY);

            // ---- battle stage ----
            var stageGo = new GameObject("BattleStage");
            var stage = stageGo.AddComponent<BattleStageView>();
            GameObject heroGo = Spawn(prefabs.Hero, stageGo.transform, new Vector3(-1.85f, 3.1f, 0f));
            GameObject critterGo = Spawn(prefabs.Critter, stageGo.transform, new Vector3(1.85f, 3.1f, 0f));
            critterGo.SetActive(false);
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
            SetPrivate(stage, "_critter", critterGo.GetComponent<CritterView>());
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
            SetPrivate(ui, "_config", content.Config);
            SetPrivate(ui, "_hud", hud);
            SetPrivate(ui, "_console", console);
            SetPrivate(ui, "_banner", banner);
            SetPrivate(ui, "_choices", choices);
            SetPrivate(ui, "_chips", chips);
            SetPrivate(ui, "_engage", engage);
            SetPrivate(ui, "_overlay", overlay);

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
            CcqSpriteBaker.Sprites art, TMP_FontAsset font)
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

            // planet tag (bottom-left of viewport, above stats bar)
            RectTransform planet = Place(NewUiChild(frame, "PlanetTag"),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 1126f),
                new Vector2(300f, 58f));
            AddImage(planet, uiSprite, pillColor);
            RectTransform planetIcon = Place(NewUiChild(planet, "Icon"), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(36f, 36f));
            AddImage(planetIcon, LoadSprite(PictoIcons + "Pictoicon_Planet.Png"), Color.white,
                false, false);
            var planetLabel = AddTmp(Place(NewUiChild(planet, "Name"), new Vector2(0f, 0.5f),
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
            string[] titles = { "XP", "ALIEN HP", "COSMIC ATK", "SHIELD DEF" };
            string[] iconPaths =
            {
                ItemIcons + "Icon_Star.png", ItemIcons + "Icon_Heart.png",
                ItemIcons + "Icon_Sword.png", ItemIcons + "Icon_Shield.png"
            };
            for (int i = 0; i < 4; i++)
            {
                float x = -405f + i * 270f;
                var title = AddTmp(Place(NewUiChild(bar, "Title" + i), new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), new Vector2(x, 58f), new Vector2(250f, 30f)),
                    titles[i], 20f, Hex("#8f9bd4"), font, TextAlignmentOptions.Center);
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
                        new Vector2(224f, CcqSpriteBaker.CapsuleHeight));
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
            SetPrivate(hud, "_planetLabel", planetLabel);
            SetPrivate(hud, "_speedLabel", speedLabel);
            SetPrivate(hud, "_speedButton", speedBtn);
            SetPrivate(hud, "_gearButton", gearBtn);
            SetPrivate(hud, "_levelLabel", levelLabel);
            SetPrivate(hud, "_xpFill", xpFill);
            SetPrivate(hud, "_hpLabel", hpLabel);
            SetPrivate(hud, "_atkLabel", atkLabel);
            SetPrivate(hud, "_defLabel", defLabel);
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
                "ENGAGE", 62f, Color.white, font, TextAlignmentOptions.Center);
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
                "Small fortune", 24f, Hex("#ffd35c"), font, TextAlignmentOptions.MidlineLeft);
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
                "Settings", 58f, Hex("#ffd35c"), font, TextAlignmentOptions.Center);
            var body = AddTmp(Place(NewUiChild(card, "Body"), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 560f)),
                "", 33f, Hex("#e8eaf6"), font, TextAlignmentOptions.Top, true);
            body.richText = true;
            body.lineSpacing = 10f;

            string[] btnSprites =
            {
                ButtonsDir + "Btn_MainButton_Green.Png",
                ButtonsDir + "Btn_MainButton_Sky.Png",
                ButtonsDir + "Btn_MainButton_Sky.Png",
                ButtonsDir + "Btn_MainButton_Orange.Png",
                ButtonsDir + "Btn_MainButton_Red.Png"
            };
            var buttons = new Button[5];
            var labels = new TextMeshProUGUI[5];
            for (int i = 0; i < 5; i++)
            {
                RectTransform b = Place(NewUiChild(card, "Button" + i), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 570f - i * 118f), new Vector2(600f, 104f));
                Image img = AddImage(b, LoadSprite(btnSprites[i]), Color.white, true);
                buttons[i] = AddButton(b, img);
                labels[i] = AddTmp(Stretch(NewUiChild(b, "Label"), 0f, 0f, 0f, 10f), "Button",
                    36f, Color.white, font, TextAlignmentOptions.Center);
            }

            SetPrivate(overlay, "_title", title);
            SetPrivate(overlay, "_body", body);
            SetPrivate(overlay, "_buttons", buttons);
            SetPrivate(overlay, "_buttonLabels", labels);
            root.gameObject.SetActive(false);
            return overlay;
        }
    }
}
