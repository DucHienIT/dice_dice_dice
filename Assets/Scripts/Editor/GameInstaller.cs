using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DiceDiceDice.EditorTools
{
    /// <summary>
    /// One-shot, idempotent project installer: creates all balance assets, prefabs and the playable scene.
    /// Menu: Tools ▸ DICE DICE DICE ▸ Install All. Safe to re-run — assets are updated in place (GUIDs stable).
    /// </summary>
    public static partial class GameInstaller
    {
        private const string DataRoot = "Assets/Data";
        private const string PrefabRoot = "Assets/Prefabs";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        // Layer Lab GUI Pro skin sources (read-only third-party assets)
        private const string FhComponents = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/";
        private const string FhItemIcons = FhComponents + "Icon_ItemIcons/128/";
        private const string FhPictoIcons = FhComponents + "Icon_PictoIcons/128/";
        private const string GameFont = "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular SDF.asset";
        private const string GeneratedItemIcons = "Assets/Art/ItemIcons/Generated/";

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError("[Installer] Missing sprite: " + path);
            }
            return sprite;
        }

        private static TMPro.TMP_FontAsset LoadFont(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
            if (font == null)
            {
                Debug.LogError("[Installer] Missing font: " + path);
            }
            return font;
        }

        /// <summary>The one font used by every text in the game: Lilita One, a rounded casual display
        /// face that stays readable at small sizes. Its static atlas is ASCII-only, so all game strings
        /// must be plain ASCII (project rule: English text only).</summary>
        private static TMPro.TMP_FontAsset DisplayFont => LoadFont(GameFont);

        [MenuItem("Tools/DICE DICE DICE/Install All")]
        public static void InstallAll()
        {
            EnsureFolders();
            InstallConfigs();
            InstallItems();
            InstallEnemies();
            InstallWaves();
            InstallUpgrades();
            InstallPrefabs();
            AssetDatabase.SaveAssets();
            BuildScene();
            Debug.Log("[Installer] DICE DICE DICE installed: data, prefabs and scene are ready. Press Play.");
        }

        [MenuItem("Tools/DICE DICE DICE/Install Data Only")]
        public static void InstallDataOnly()
        {
            EnsureFolders();
            InstallConfigs();
            InstallItems();
            InstallEnemies();
            InstallWaves();
            InstallUpgrades();
            AssetDatabase.SaveAssets();
            Debug.Log("[Installer] Data assets installed.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder(DataRoot, "Items");
            EnsureFolder(DataRoot, "Enemies");
            EnsureFolder(DataRoot, "Waves");
            EnsureFolder(DataRoot, "Upgrades");
            EnsureFolder("Assets", "Prefabs");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ---------- Configs ----------

        private static void InstallConfigs()
        {
            var config = GetOrCreateAsset<GameConfig>(DataRoot + "/GameConfig.asset");
            // Touch-first layout: 140px slot pitch (was 112) so a finger can grab a slot; the wall
            // moves right to make room for the wider board. See Ui.SlotSize below.
            config.EditorSetLayout(new Vector2(-8.80f, 1.84f), 1.40f, -6.45f, -6.30f);
            var palette = GetOrCreateAsset<PaletteConfig>(DataRoot + "/PaletteConfig.asset");
            // Layer Lab panels are light parchment — body text must be dark to stay readable.
            palette.EditorSetTextColors(new Color(0.30f, 0.25f, 0.20f), new Color(0.52f, 0.46f, 0.38f));
            InstallUiSkin();
        }

        private static void InstallUiSkin()
        {
            var skin = GetOrCreateAsset<UiSkin>(DataRoot + "/UiSkin.asset");
            var frameByRarity = new[]
            {
                LoadSprite(FhComponents + "Frame/ItemFrame_Square_01_Gray.png"),    // Common
                LoadSprite(FhComponents + "Frame/ItemFrame_Square_01_Blue.png"),    // Rare
                LoadSprite(FhComponents + "Frame/ItemFrame_Square_01_Purple.png"),  // Epic
                LoadSprite(FhComponents + "Frame/ItemFrame_Square_01_Yellow.png")   // Legendary
            };
            var cardBgByGroup = new[]
            {
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Yellow_Bg.png"),  // Economy
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Red_Bg.png"),     // Weapon
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Purple_Bg.png"),  // Magic
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Green_Bg.png"),   // Support
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Blue_Bg.png")     // Defense
            };
            var cardBorderByGroup = new[]
            {
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Yellow_Border.png"),
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Red_Border.png"),
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Purple_Border.png"),
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Green_Border.png"),
                LoadSprite(FhComponents + "Frame/CardFrame_Rectangle_01_Blue_Border.png")
            };
            skin.EditorSetup(frameByRarity,
                LoadSprite(FhComponents + "Frame/ItemFrame_Square_01_Dim.png"),
                LoadSprite(FhComponents + "Frame/ItemFrame_Square_02_Single_Focus.png"),
                cardBgByGroup, cardBorderByGroup,
                LoadSprite(FhPictoIcons + "PictoIcon_Sound.Png"),
                LoadSprite(FhPictoIcons + "PictoIcon_Sound_Mute.Png"),
                LoadSprite(FhItemIcons + "ItemIcon_Coin_Gold.png"));
        }

        private static GameConfig Config => AssetDatabase.LoadAssetAtPath<GameConfig>(DataRoot + "/GameConfig.asset");
        private static PaletteConfig Palette => AssetDatabase.LoadAssetAtPath<PaletteConfig>(DataRoot + "/PaletteConfig.asset");
        private static UiSkin Skin => AssetDatabase.LoadAssetAtPath<UiSkin>(DataRoot + "/UiSkin.asset");

        // ---------- Items (spec 27.2) ----------

        private static void InstallItems()
        {
            var dice = GetOrCreateAsset<DiceDefinition>(DataRoot + "/Items/Dice.asset");
            dice.EditorSetup("Dice", ItemGroup.Economy, 10,
                "Rolls for gold during waves.",
                ItemIcon.Dice, 1f);
            dice.EditorSetupDice(5f, 0.7f, 1.2f);

            var bow = GetOrCreateAsset<BowDefinition>(DataRoot + "/Items/Bow.asset");
            bow.EditorSetup("Bow", ItemGroup.Weapon, 12,
                "Fast arrows, low damage.",
                ItemIcon.Bow, 1f);
            bow.EditorSetupCombat(4f, 0.8f, DamageType.Physical);

            var sword = GetOrCreateAsset<SwordDefinition>(DataRoot + "/Items/Sword.asset");
            sword.EditorSetup("Sword", ItemGroup.Weapon, 14,
                "Slashes monsters near the wall.",
                ItemIcon.Sword, 1f);
            sword.EditorSetupCombat(9f, 1.6f, DamageType.Physical);

            var crossbow = GetOrCreateAsset<CrossbowDefinition>(DataRoot + "/Items/Crossbow.asset");
            crossbow.EditorSetup("Crossbow", ItemGroup.Weapon, 16,
                "Slow bolts, high crit chance.",
                ItemIcon.Crossbow, 1f);
            crossbow.EditorSetupCombat(16f, 2.4f, DamageType.Physical);

            var cannon = GetOrCreateAsset<CannonDefinition>(DataRoot + "/Items/Cannon.asset");
            cannon.EditorSetup("Cannon", ItemGroup.Weapon, 18,
                "Explosive area damage.",
                ItemIcon.Cannon, 1f);
            cannon.EditorSetupCombat(10f, 3f, DamageType.Physical);

            var fireBook = GetOrCreateAsset<FireBookDefinition>(DataRoot + "/Items/FireBook.asset");
            fireBook.EditorSetup("Fire Book", ItemGroup.Magic, 18,
                "Meteors that burn over time.",
                ItemIcon.FireBook, 1f);
            fireBook.EditorSetupCombat(12f, 4f, DamageType.Magic);

            var frost = GetOrCreateAsset<FrostStoneDefinition>(DataRoot + "/Items/FrostStone.asset");
            frost.EditorSetup("Frost Stone", ItemGroup.Magic, 14,
                "Slows monsters, can freeze.",
                ItemIcon.FrostStone, 1f);
            frost.EditorSetupCombat(4f, 2.5f, DamageType.Magic);

            var lightning = GetOrCreateAsset<LightningOrbDefinition>(DataRoot + "/Items/LightningOrb.asset");
            lightning.EditorSetup("Lightning Orb", ItemGroup.Magic, 16,
                "Chain lightning between monsters.",
                ItemIcon.LightningOrb, 1f);
            lightning.EditorSetupCombat(8f, 3f, DamageType.Magic);

            var anvil = GetOrCreateAsset<SupportDefinition>(DataRoot + "/Items/Anvil.asset");
            anvil.EditorSetup("Anvil", ItemGroup.Support, 15,
                "Boosts physical damage.",
                ItemIcon.Anvil, 1f);
            anvil.EditorSetupSupport(0.15f, 0f);

            var hourglass = GetOrCreateAsset<SupportDefinition>(DataRoot + "/Items/Hourglass.asset");
            hourglass.EditorSetup("Hourglass", ItemGroup.Support, 15,
                "Speeds up attacks and rolls.",
                ItemIcon.Hourglass, 1f);
            hourglass.EditorSetupSupport(0f, 0.1f);

            var shield = GetOrCreateAsset<ShieldDefinition>(DataRoot + "/Items/Shield.asset");
            shield.EditorSetup("Shield", ItemGroup.Defense, 12,
                "Shields the wall each wave.",
                ItemIcon.Shield, 1f);
            shield.EditorSetupShield(15);

            AssignItemIcons();
        }

        /// <summary>Layer Lab icon sprites per item (Dice icon comes from the CasualGame pack — same vendor).</summary>
        private static void AssignItemIcons()
        {
            string[] names = { "Dice", "Bow", "Sword", "Crossbow", "Cannon", "FireBook", "FrostStone", "LightningOrb", "Anvil", "Hourglass", "Shield" };
            foreach (string name in names)
            {
                string path = GeneratedItemIcons + name + ".png";
                ConfigureItemIcon(path);
                SetItemIcon(name, path);
            }
        }

        private static void ConfigureItemIcon(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[Installer] Missing generated item icon: " + path);
                return;
            }

            bool dirty = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || importer.wrapMode != TextureWrapMode.Clamp;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 256;
            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }


        private static void SetItemIcon(string assetName, string spritePath)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(DataRoot + "/Items/" + assetName + ".asset");
            item.EditorSetIconSprite(LoadSprite(spritePath));
            EditorUtility.SetDirty(item);
        }

        private static ItemDefinition[] LoadItemPool()
        {
            string[] names = { "Dice", "Bow", "Sword", "Crossbow", "Cannon", "FireBook", "FrostStone", "LightningOrb", "Anvil", "Hourglass", "Shield" };
            var pool = new ItemDefinition[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                pool[i] = AssetDatabase.LoadAssetAtPath<ItemDefinition>(DataRoot + "/Items/" + names[i] + ".asset");
            }
            return pool;
        }

        // ---------- Enemies (spec 16-17) ----------

        private static void InstallEnemies()
        {
            CreateEnemy("Basic", "Basic", 22f, 0.55f, 10, 3, 0.18f, Hex("c96a4a"), 0f, 0f, false, false, false, 0f);
            CreateEnemy("Runner", "Runner", 12f, 1.15f, 8, 3, 0.14f, Hex("e0c74a"), 0f, 0f, false, false, false, 0f);
            CreateEnemy("Tank", "Tank", 70f, 0.3f, 20, 6, 0.24f, Hex("8a5adf"), 0f, 0f, false, false, false, 0f);
            CreateEnemy("Swarm", "Swarm", 8f, 0.82f, 5, 2, 0.11f, Hex("5ad08a"), 0f, 0f, false, false, false, 0f);
            CreateEnemy("Armored", "Armored", 38f, 0.4f, 12, 5, 0.19f, Hex("9aa4bd"), 0.5f, 0f, false, false, false, 0f);
            CreateEnemy("MagicResist", "M.Resist", 38f, 0.4f, 12, 5, 0.19f, Hex("5a8adf"), 0f, 0.5f, false, false, false, 0f);
            CreateEnemy("Healer", "Healer", 26f, 0.46f, 8, 6, 0.16f, Hex("ff8ac8"), 0f, 0f, true, false, false, 0f);
            CreateEnemy("Elite", "ELITE", 230f, 0.24f, 35, 25, 0.3f, Hex("ff4a6b"), 0f, 0f, false, true, false, 0f);
            CreateEnemy("Boss", "LOADED GOLEM", 750f, 0.13f, 999, 60, 0.42f, Hex("ffb830"), 0f, 0f, false, false, true, 300f);
        }

        private static void CreateEnemy(string assetName, string displayName, float hp, float speed, int damage, int xp,
            float radius, Color color, float physResist, float magicResist, bool healer, bool elite, bool boss, float armor)
        {
            var enemy = GetOrCreateAsset<EnemyDefinition>(DataRoot + "/Enemies/" + assetName + ".asset");
            enemy.EditorSetup(displayName, hp, speed, damage, xp, radius, color, physResist, magicResist, healer, elite, boss, armor);
        }

        private static EnemyDefinition Enemy(string name)
        {
            return AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataRoot + "/Enemies/" + name + ".asset");
        }

        private static Color Hex(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            return color;
        }

        // ---------- Waves (spec 15) ----------

        private static void InstallWaves()
        {
            var waveSet = GetOrCreateAsset<WaveSet>(DataRoot + "/Waves/WaveSet.asset");
            var waves = new List<WaveDefinition>
            {
                Wave("Wave 1", false, S("Basic", 5, 2.2f, 1f)),
                Wave("Wave 2", false, S("Basic", 8, 1.7f, 1f), S("Runner", 2, 3f, 14f)),
                Wave("Wave 3", false, S("Runner", 6, 1.4f, 1f), S("Basic", 6, 2f, 4f)),
                Wave("Wave 4", false, S("Swarm", 12, 0.5f, 1f), S("Basic", 6, 2f, 8f), S("Runner", 4, 1.5f, 15f)),
                Wave("Wave 5 - MINI-BOSS", true, S("Basic", 6, 1.8f, 1f), S("Tank", 2, 5f, 6f), S("Elite", 1, 0f, 20f)),
                Wave("Wave 6", false, S("Armored", 5, 3f, 1f), S("Basic", 8, 1.5f, 3f), S("Runner", 4, 1.2f, 16f)),
                Wave("Wave 7", false, S("MagicResist", 5, 3f, 1f), S("Swarm", 14, 0.45f, 5f), S("Tank", 2, 6f, 12f)),
                Wave("Wave 8", false, S("Healer", 2, 8f, 4f), S("Armored", 4, 3.2f, 1f), S("MagicResist", 4, 3.2f, 2.5f), S("Basic", 10, 1.4f, 6f)),
                Wave("Wave 9", false, S("Tank", 4, 4f, 1f), S("Runner", 8, 1f, 3f), S("Healer", 2, 7f, 8f), S("Swarm", 12, 0.5f, 12f), S("Elite", 1, 0f, 24f)),
                Wave("BOSS: LOADED GOLEM", true, S("Swarm", 8, 0.6f, 2f), S("Basic", 6, 2.5f, 6f), S("Boss", 1, 0f, 10f), S("Runner", 6, 1.5f, 26f))
            };
            waveSet.EditorSetWaves(waves);
        }

        private static WaveDefinition Wave(string label, bool bossAlarm, params SpawnEntry[] entries)
        {
            return new WaveDefinition(label, bossAlarm, new List<SpawnEntry>(entries));
        }

        private static SpawnEntry S(string enemyName, int count, float interval, float startTime)
        {
            return new SpawnEntry(Enemy(enemyName), count, interval, startTime);
        }

        // ---------- Upgrades (spec 18) ----------

        private static void InstallUpgrades()
        {
            CreateUpgrade("DiceSpeed", "Quick Dice", "Dice roll 20% faster.", ItemGroup.Economy, UpgradeStat.DiceSpeed, UpgradeOperation.Multiply, 1.2f, false);
            CreateUpgrade("DiceMinFace", "Lucky Faces", "Minimum dice roll +1.", ItemGroup.Economy, UpgradeStat.DiceMinFace, UpgradeOperation.Add, 1f, false);
            CreateUpgrade("SixBonus", "Jackpot", "Rolling a 6 grants 5 bonus gold.", ItemGroup.Economy, UpgradeStat.SixBonusGold, UpgradeOperation.Add, 5f, false);
            CreateUpgrade("DoubleRoll", "Double Roll", "Dice have a 25% chance to roll twice.", ItemGroup.Economy, UpgradeStat.DoubleRollChance, UpgradeOperation.Add, 0.25f, false);
            CreateUpgrade("MergeDiceGold", "Merge Essence", "Gain 10 gold whenever you merge Dice.", ItemGroup.Economy, UpgradeStat.MergeDiceGold, UpgradeOperation.Add, 10f, false);
            CreateUpgrade("Interest", "Compound Interest", "End of wave: earn 10% interest on held gold (max 15).", ItemGroup.Economy, UpgradeStat.Interest, UpgradeOperation.Add, 0.1f, false);
            CreateUpgrade("WaveGold", "Wave Bounty", "Gain 10 extra gold after each wave.", ItemGroup.Economy, UpgradeStat.WaveEndGold, UpgradeOperation.Add, 10f, false);
            CreateUpgrade("FreeReroll", "Regular Customer", "First reroll each wave is free.", ItemGroup.Economy, UpgradeStat.FreeRerollPerWave, UpgradeOperation.Add, 1f, false);
            CreateUpgrade("SellRate", "Haggler", "Sell items for 75% of their value instead of 50%.", ItemGroup.Economy, UpgradeStat.SellRate, UpgradeOperation.Set, 0.75f, true);
            CreateUpgrade("PhysDamage", "Sharpened Edges", "Physical damage +25%.", ItemGroup.Weapon, UpgradeStat.PhysicalDamage, UpgradeOperation.Multiply, 1.25f, false);
            CreateUpgrade("AttackSpeed", "Quick Hands", "Weapon attack speed +20%.", ItemGroup.Weapon, UpgradeStat.AttackSpeed, UpgradeOperation.Multiply, 1.2f, false);
            CreateUpgrade("CritChance", "Weak Spot", "Critical chance +15%.", ItemGroup.Weapon, UpgradeStat.CritChance, UpgradeOperation.Add, 0.15f, false);
            CreateUpgrade("Pierce", "Piercing Shots", "Projectiles pierce 1 extra target.", ItemGroup.Weapon, UpgradeStat.Pierce, UpgradeOperation.Add, 1f, false);
            CreateUpgrade("CritExplode", "Critical Blast", "Critical hits cause a small explosion.", ItemGroup.Weapon, UpgradeStat.CritExplode, UpgradeOperation.Set, 1f, true);
            CreateUpgrade("MagicDamage", "Arcane Power", "Magic damage +25%.", ItemGroup.Magic, UpgradeStat.MagicDamage, UpgradeOperation.Multiply, 1.25f, false);
            CreateUpgrade("MagicCooldown", "Fast Casting", "Spell cooldowns -20%.", ItemGroup.Magic, UpgradeStat.MagicCooldown, UpgradeOperation.Multiply, 0.8f, false);
            CreateUpgrade("DotDuration", "Elemental Echo", "Burn / slow / freeze duration +50%.", ItemGroup.Magic, UpgradeStat.DotDuration, UpgradeOperation.Multiply, 1.5f, false);
            CreateUpgrade("DoubleCast", "Echo Cast", "Spells have a 25% chance to cast twice.", ItemGroup.Magic, UpgradeStat.DoubleCastChance, UpgradeOperation.Add, 0.25f, false);
            CreateUpgrade("SupportPower", "Resonance", "Support items (Anvil, Hourglass) are 50% more effective.", ItemGroup.Support, UpgradeStat.SupportPower, UpgradeOperation.Multiply, 1.5f, false);
            CreateUpgrade("WallMaxHp", "Reinforced Wall", "Wall max HP +25, healed on pickup.", ItemGroup.Defense, UpgradeStat.WallMaxHp, UpgradeOperation.Add, 25f, false);
            CreateUpgrade("HealPerWave", "Repairs", "Restore 10 wall HP after each wave.", ItemGroup.Defense, UpgradeStat.HealPerWave, UpgradeOperation.Add, 10f, false);
            CreateUpgrade("WaveShield", "Opening Shield", "Gain a 20-point shield at the start of each wave.", ItemGroup.Defense, UpgradeStat.WaveShield, UpgradeOperation.Add, 20f, false);
        }

        private static void CreateUpgrade(string assetName, string displayName, string description, ItemGroup group,
            UpgradeStat stat, UpgradeOperation operation, float value, bool once)
        {
            var upgrade = GetOrCreateAsset<UpgradeDefinition>(DataRoot + "/Upgrades/" + assetName + ".asset");
            upgrade.EditorSetup(displayName, description, group, stat, operation, value, once);
        }

        private static UpgradeDefinition[] LoadUpgradePool()
        {
            string[] guids = AssetDatabase.FindAssets("t:UpgradeDefinition", new[] { DataRoot + "/Upgrades" });
            var pool = new UpgradeDefinition[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                pool[i] = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
            return pool;
        }

        // ---------- Prefabs ----------

        private static void InstallPrefabs()
        {
            InstallEnemyPrefab();
            InstallSimpleSpritePrefab<Projectile>("Projectile", "_renderer", 15);
            InstallSimpleSpritePrefab<VisualEffect>("VisualEffect", "_renderer", 16);
            InstallLightningPrefab();
            InstallFloatingTextPrefab();
        }

        private static void InstallEnemyPrefab()
        {
            var root = new GameObject("Enemy", typeof(Enemy));
            try
            {
                SpriteRenderer body = ChildSprite(root.transform, "Body", 10);
                SpriteRenderer status = ChildSprite(root.transform, "StatusRing", 11);
                SpriteRenderer burn = ChildSprite(root.transform, "BurnMark", 12);
                SpriteRenderer hpBack = ChildSprite(root.transform, "HpBarBack", 13);
                SpriteRenderer hpFill = ChildSprite(root.transform, "HpBarFill", 14);
                SpriteRenderer armorFill = ChildSprite(root.transform, "ArmorBarFill", 14);

                var so = new SerializedObject(root.GetComponent<Enemy>());
                so.FindProperty("_body").objectReferenceValue = body;
                so.FindProperty("_statusRing").objectReferenceValue = status;
                so.FindProperty("_burnMark").objectReferenceValue = burn;
                so.FindProperty("_hpBarBack").objectReferenceValue = hpBack;
                so.FindProperty("_hpBarFill").objectReferenceValue = hpFill;
                so.FindProperty("_armorBarFill").objectReferenceValue = armorFill;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/Enemy.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void InstallSimpleSpritePrefab<T>(string name, string rendererField, int sortingOrder) where T : Component
        {
            var root = new GameObject(name, typeof(T));
            try
            {
                SpriteRenderer renderer = ChildSprite(root.transform, "Sprite", sortingOrder);
                var so = new SerializedObject(root.GetComponent<T>());
                so.FindProperty(rendererField).objectReferenceValue = renderer;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/" + name + ".prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void InstallLightningPrefab()
        {
            var root = new GameObject("LightningBolt", typeof(LightningBolt), typeof(LineRenderer));
            try
            {
                var line = root.GetComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.startWidth = 0.05f;
                line.endWidth = 0.05f;
                line.sortingOrder = 16;
                line.textureMode = LineTextureMode.Stretch;

                var so = new SerializedObject(root.GetComponent<LightningBolt>());
                so.FindProperty("_line").objectReferenceValue = line;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/LightningBolt.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void InstallFloatingTextPrefab()
        {
            var root = new GameObject("FloatingText", typeof(RectTransform), typeof(FloatingText));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(220f, 60f);
                var label = CreateTmp(root.transform, "Label", string.Empty, 30f, TMPro.TextAlignmentOptions.Center, true);
                Stretch(label.rectTransform);

                var so = new SerializedObject(root.GetComponent<FloatingText>());
                so.FindProperty("_label").objectReferenceValue = label;
                so.FindProperty("_rect").objectReferenceValue = rect;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/FloatingText.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SpriteRenderer ChildSprite(Transform parent, string name, int sortingOrder)
        {
            var child = new GameObject(name, typeof(SpriteRenderer));
            child.transform.SetParent(parent, false);
            var renderer = child.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
