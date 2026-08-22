using System.IO;
using Game.Data;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Game.EditorTools.BuilderUtil;

namespace Game.EditorTools
{
    /// <summary>
    /// Bakes every procedural sprite to a .png asset under <see cref="ArtDir"/> at build time.
    /// The runtime owns no painting code: views just reference the sprites wired into their
    /// prefab by <see cref="GameBuilder"/>.
    ///
    /// The enemy is baked as separate layers instead of one sprite per look combination
    /// (7 colors x 3 eye counts x horns x spots x 3 kinds = 252): the prefab holds one
    /// SpriteRenderer per layer and <c>EnemyView.Init</c> only swaps the body sprite and
    /// toggles the optional layers.
    /// </summary>
    public static class SpriteBaker
    {
        public const string ArtDir = "Assets/_OneTapImmortal/Art/Generated";
        private const string AuthoredArtDir = "Assets/_OneTapImmortal/Art/Immortal";
        private const string AzureCloudBackdrop = AuthoredArtDir + "/azure_side_scroll_v3.png";
        public const string CultivatorRunSheet = AuthoredArtDir +
            "/cultivator_side_run_v3.png";
        public const string CultivatorPoseSheet = AuthoredArtDir +
            "/hero_pose_sheet_v1.png";
        public const string CultivatorRangedPoseSheet = AuthoredArtDir +
            "/hero_ranged_pose_sheet_v1.png";

        /// <summary>True when all three authored hero sheets exist — the mode where the
        /// hero prefab streams them as weak references (HeroArt bundle) instead of having
        /// procedural sprites wired hard. All-or-nothing keeps the wiring branch-free.</summary>
        public static bool AuthoredHeroArtComplete =>
            File.Exists(CultivatorRunSheet) &&
            File.Exists(CultivatorPoseSheet) &&
            File.Exists(CultivatorRangedPoseSheet);

        private const float AuthoredHeroPpu = 280f;
        private const float AuthoredPosePpu = 220f;
        private const float AuthoredRangedPosePpu = 280f;
        private const int AuthoredHeroFrameCount = 5;
        private const int AuthoredPoseCount = 4;
private const float AuthoredBackdropPpu = 87.13f;

        public const float Ppu = 100f;
        private const float BurstPpu = 120f;

        // Background world metrics — pushed onto WorldBackgroundRenderer by the builder so
        // the baked pixels and the scene placement can never drift apart. BgWorldWidth doubles
        // as the scroll period of the sky belt.
        public const float BgWorldWidth = 14.6f;
        public const float BgWorldHeight = 12f;
        public const float BgBottomY = -2.4f;
        public const float BgHorizonY = 3f;

        // uGUI progress-bar pill. Show it at CapsuleHeight px tall; any width >= 2*CapsuleRadius
        // renders correctly thanks to the horizontal 9-slice border.
        public const int CapsuleHeight = 12;
        public const int CapsuleRadius = 6;
        private const int CapsuleWidth = 40;

        /// <summary>
        /// Compression for the two big per-world backdrop layers. Uncompressed keeps the baked
        /// pixels exact but costs ~10 MB of texture memory per world; CompressedHQ (BC7 on
        /// desktop, ASTC on mobile) cuts that ~3.5x and showed no visible banding on the sky
        /// gradient when measured. Flip this when the target platform is settled.
        /// </summary>
        private const TextureImporterCompression BackdropCompression =
            TextureImporterCompression.Uncompressed;

        private const int EnemyCanvas = 240;
        private const float EnemyCx = 120f;
        private const float EnemyCy = 104f;
        private static readonly Vector2 EnemyPivot = new Vector2(0.5f, 0.02f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private static readonly Color Ink = new Color(0.11f, 0.14f, 0.19f);

        /// <summary>Every baked sprite the builder needs to wire into prefabs and the scene.</summary>
        public sealed class Sprites
        {
            public Sprite HeroBody;
            public Sprite HeroRunPose;
            public Sprite HeroAttackPose;
            public Sprite HeroHitPose;
            public Sprite HeroFlyPose;
            public Sprite HeroRangedSwordPose;
            public Sprite HeroSpellPose;
            public Sprite HeroSpellProjectile;
            public Sprite HeroSword;
            public Sprite HeroShadow;
            public Sprite HeroFlash;

            public Sprite EnemyShadow;
            public Sprite EnemyFlash;
            public Sprite EnemyGlow;
            public Sprite EnemyHorns;
            public Sprite EnemySpikes;
            public Sprite EnemySpots;
            public Sprite EnemyMouth;
            public Sprite[] EnemyBodies;  // one per GameConfig.EnemyColors entry
            public Sprite[] EnemyEyes;    // index 0..2 = 1..3 eyes

            public Sprite HpFrame;
            public Sprite HpFill;
            public Sprite BurstStar;
            public Sprite Bolt;
            public Sprite Twinkle;

            /// <summary>White 9-sliced pill for uGUI bars — tint it, stretch it, caps stay round.</summary>
            public Sprite UiCapsule;
        }

        /// <summary>
        /// Bakes all art. Orb sprites land on their Sidekick asset and world backdrops on
        /// their World asset (data-driven, per CODE_RULES); the rest comes back for prefab
        /// wiring.
        /// </summary>
        public static Sprites BakeAll(GameConfig config)
        {
            EnsureFolder(ArtDir);
            var s = new Sprites();

            Sprite[] authoredHero = File.Exists(CultivatorRunSheet)
                ? LoadAuthoredSpriteSheet(CultivatorRunSheet, AuthoredHeroFrameCount,
                    new Vector2(0.5f, 0.025f), AuthoredHeroPpu)
                : null;
            Sprite[] authoredPoses = File.Exists(CultivatorPoseSheet)
                ? LoadAuthoredPoseSheet(CultivatorPoseSheet, AuthoredPosePpu,
                    2, 2, "hero_pose_")
                : null;
            Sprite[] authoredRangedPoses = File.Exists(CultivatorRangedPoseSheet)
                ? LoadAuthoredPoseSheet(CultivatorRangedPoseSheet, AuthoredRangedPosePpu,
                    2, 1, "hero_ranged_pose_")
                : null;

            s.HeroBody = authoredHero != null && authoredHero.Length == AuthoredHeroFrameCount
                ? authoredHero[0]
                : Save(HeroBody(0), "hero_body", new Vector2(0.5f, 0.03f));

            if (authoredPoses != null && authoredPoses.Length == AuthoredPoseCount)
            {
                s.HeroRunPose = authoredPoses[0];
                s.HeroAttackPose = authoredPoses[1];
                s.HeroHitPose = authoredPoses[2];
                s.HeroFlyPose = authoredPoses[3];
            }
            else
            {
                s.HeroRunPose = authoredHero != null ? authoredHero[1]
                    : Save(HeroBody(1), "hero_pose_run", new Vector2(0.5f, 0.03f));
                s.HeroAttackPose = authoredHero != null ? authoredHero[2]
                    : Save(HeroBody(2), "hero_pose_attack", new Vector2(0.5f, 0.03f));
                s.HeroHitPose = authoredHero != null ? authoredHero[3]
                    : Save(HeroBody(3), "hero_pose_hit", new Vector2(0.5f, 0.03f));
                s.HeroFlyPose = authoredHero != null ? authoredHero[4]
                    : Save(HeroBody(4), "hero_pose_fly", new Vector2(0.5f, 0.03f));
            }
            if (authoredRangedPoses != null && authoredRangedPoses.Length == 2)
            {
                s.HeroRangedSwordPose = authoredRangedPoses[0];
                s.HeroSpellPose = authoredRangedPoses[1];
            }
            else
            {
                s.HeroRangedSwordPose = s.HeroAttackPose;
                s.HeroSpellPose = s.HeroAttackPose;
            }
            s.HeroSword = Save(HeroSword(), "hero_sword", new Vector2(0.5f, 0.1f));
            s.HeroSpellProjectile = Save(
                Orb(new Color(0.34f, 0.78f, 1f), "spark"),
                "hero_spell_projectile", Center);
            s.HeroShadow = Save(HeroShadow(), "hero_shadow", Center);
            s.HeroFlash = Save(HeroFlash(), "hero_flash", Center);

            Color[] colors = config.EnemyColors;
            s.EnemyBodies = new Sprite[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                s.EnemyBodies[i] = Save(EnemyBody(colors[i]), "enemy_body_" + i, EnemyPivot);
            }
            s.EnemyEyes = new Sprite[3];
            for (int n = 1; n <= 3; n++)
            {
                s.EnemyEyes[n - 1] = Save(EnemyEyes(n), "enemy_eyes_" + n, EnemyPivot);
            }
            s.EnemyGlow = Save(EnemyGlow(), "enemy_glow", EnemyPivot);
            s.EnemyHorns = Save(EnemyHorns(), "enemy_horns", EnemyPivot);
            s.EnemySpikes = Save(EnemySpikes(), "enemy_spikes", EnemyPivot);
            s.EnemySpots = Save(EnemySpots(), "enemy_spots", EnemyPivot);
            s.EnemyMouth = Save(EnemyMouth(), "enemy_mouth", EnemyPivot);
            s.EnemyShadow = Save(EnemyShadow(), "enemy_shadow", Center);
            s.EnemyFlash = Save(EnemyFlash(), "enemy_flash", new Vector2(0.5f, 0.4f));

            s.HpFrame = Save(HpFrame(), "hpbar_frame", Center);
            s.HpFill = Save(HpFill(), "hpbar_fill", new Vector2(0f, 0.5f));
            s.BurstStar = Save(BurstStar(), "burst_star", Center, BurstPpu);
            s.Twinkle = Save(Twinkle(), "twinkle", Center);
            s.Bolt = Save(TribulationBolt(), "fx_bolt", new Vector2(0.5f, 1f));
            s.UiCapsule = Save(UiCapsule(), "ui_capsule", Center, Ppu,
                border: new Vector4(CapsuleRadius, 0f, CapsuleRadius, 0f));

            Sidekick[] sidekicks = config.Sidekicks;
            for (int i = 0; i < sidekicks.Length; i++)
            {
                Sidekick sk = sidekicks[i];
                SetPrivate(sk, "_orbSprite", Save(Orb(sk.Color, sk.Id), "orb_" + sk.Id, Center));
            }

            World[] worlds = config.Worlds;
            for (int i = 0; i < worlds.Length; i++)
            {
                World def = worlds[i];
                // every realm shares the authored backdrop — the baked procedural sky read
                // as off-theme, so PaintSky survives only as the fallback when the authored
                // art file is missing
                bool authoredBackdrop = File.Exists(AzureCloudBackdrop);
                SetPrivate(def, "_authoredBackdrop", authoredBackdrop);
                Sprite sky = authoredBackdrop
                    ? LoadAuthoredSprite(AzureCloudBackdrop, new Vector2(0.5f, 0.125f),
                        AuthoredBackdropPpu, true)
                    : Save(PaintSky(i, def), "bg_" + def.name + "_sky",
                        new Vector2(0.5f, 0f), Ppu, opaque: true, backdrop: true);
                // the World asset holds an AssetReference, never the sprite itself — the
                // backdrop ships in a bundle, not with the scene
                SetPrivate(def, "_skyLayerRef", SkyReference(sky));
            }

            return s;
        }

        /// <summary>GUID + sub-object reference to a baked sky sprite. The addressable entry
        /// itself is created later by <see cref="AddressablesConfigurator"/>.</summary>
        private static AssetReferenceSprite SkyReference(Sprite sprite)
        {
            if (sprite == null) return null;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite));
            return new AssetReferenceSprite(guid) { SubObjectName = sprite.name };
        }

        // ================= asset writing =================

        private static Sprite Save(Painter p, string fileName, Vector2 pivot, float ppu = Ppu,
            bool opaque = false, bool backdrop = false, Vector4 border = default)
        {
            string path = ArtDir + "/" + fileName + ".png";
            File.WriteAllBytes(path, p.EncodeToPng());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[Baker] Import failed for " + path);
                return null;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = !opaque;
            // Small sprites stay uncompressed (they are tiny and must stay crisp); the two big
            // per-world backdrop layers follow BackdropCompression — see that const.
            importer.textureCompression = backdrop
                ? BackdropCompression
                : TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            // a non-zero border makes uGUI 9-slice it, so rounded caps survive any width
            settings.spriteBorder = border;
            settings.alphaSource = opaque
                ? TextureImporterAlphaSource.None
                : TextureImporterAlphaSource.FromInput;
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError("[Baker] No sprite produced at " + path);
            return sprite;
        }

        private static Sprite LoadAuthoredSprite(string path, Vector2 pivot, float ppu, bool opaque)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[Baker] Import failed for " + path);
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = !opaque;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.alphaSource = opaque
                ? TextureImporterAlphaSource.None
                : TextureImporterAlphaSource.FromInput;
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError("[Baker] No sprite produced at " + path);
            return sprite;
        }


        

        private static Sprite[] LoadAuthoredSpriteSheet(string path, int frameCount,
            Vector2 pivot, float ppu)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[Baker] Sprite sheet import failed for " + path);
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.isReadable = true;
            importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();

            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (source == null) return null;
            Color32[] pixels = source.GetPixels32();
            int[] alphaCount = new int[source.width];
            for (int x = 0; x < source.width; x++)
            {
                for (int y = 0; y < source.height; y++)
                    if (pixels[y * source.width + x].a > 12) alphaCount[x]++;
            }

            int[] cuts = new int[frameCount + 1];
            cuts[0] = 0;
            cuts[frameCount] = source.width;
            float nominalWidth = source.width / (float)frameCount;
            int searchRadius = Mathf.RoundToInt(nominalWidth * 0.30f);
            for (int i = 1; i < frameCount; i++)
            {
                int expected = Mathf.RoundToInt(i * nominalWidth);
                int bestX = expected;
                int bestAlpha = int.MaxValue;
                for (int x = Mathf.Max(cuts[i - 1] + 1, expected - searchRadius);
                    x <= Mathf.Min(source.width - 2, expected + searchRadius); x++)
                {
                    int amount = alphaCount[x];
                    if (amount >= bestAlpha &&
                        (amount != bestAlpha ||
                         Mathf.Abs(x - expected) >= Mathf.Abs(bestX - expected))) continue;
                    bestX = x;
                    bestAlpha = amount;
                }
                cuts[i] = bestX;
            }

            var frames = new SpriteMetaData[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                frames[i] = new SpriteMetaData
                {
                    name = "cultivator_side_" + i,
                    rect = new Rect(cuts[i], 0, cuts[i + 1] - cuts[i], source.height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = pivot,
                    border = Vector4.zero
                };
            }

            importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.isReadable = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SetTextureSettings(settings);
#pragma warning disable 0618
            importer.spritesheet = frames;
#pragma warning restore 0618
            importer.SaveAndReimport();
            return LoadOrderedSprites(path, "cultivator_side_", frameCount, false);
        }

        private static Sprite[] LoadAuthoredPoseSheet(string path, float ppu,
            int columns, int rows, string prefix)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[Baker] Pose sheet import failed for " + path);
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.isReadable = true;
            importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();

            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (source == null || source.width % columns != 0 || source.height % rows != 0)
            {
                Debug.LogError("[Baker] Invalid " + columns + "x" + rows +
                    " hero pose sheet dimensions for " + path);
                return null;
            }

            int poseCount = columns * rows;
            int cellWidth = source.width / columns;
            int cellHeight = source.height / rows;
            Color32[] pixels = source.GetPixels32();
            var poses = new SpriteMetaData[poseCount];
            var desiredPivots = new Vector2[poseCount];

            for (int pose = 0; pose < poseCount; pose++)
            {
                int x0 = (pose % columns) * cellWidth;
                int y0 = source.height - (pose / columns + 1) * cellHeight;
                int lowestOpaqueY = cellHeight;
                for (int y = 0; y < cellHeight && lowestOpaqueY == cellHeight; y++)
                {
                    int rowOffset = (y0 + y) * source.width + x0;
                    for (int x = 0; x < cellWidth; x++)
                    {
                        if (pixels[rowOffset + x].a <= 100) continue;
                        lowestOpaqueY = y;
                        break;
                    }
                }

                float pivotY = lowestOpaqueY < cellHeight
                    ? Mathf.Clamp01((lowestOpaqueY - 2f) / cellHeight)
                    : 0.03f;
                desiredPivots[pose] = new Vector2(0.5f, pivotY);
                poses[pose] = new SpriteMetaData
                {
                    name = prefix + pose.ToString("00"),
                    rect = new Rect(x0, y0, cellWidth, cellHeight),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = desiredPivots[pose],
                    border = Vector4.zero
                };
            }

            importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SetTextureSettings(settings);
#pragma warning disable 0618
            importer.spritesheet = poses;
#pragma warning restore 0618
            importer.SaveAndReimport();

            importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] spriteRects = provider.GetSpriteRects();
            for (int i = 0; i < spriteRects.Length; i++)
            {
                if (!spriteRects[i].name.StartsWith(prefix)) continue;
                int pose;
                if (!int.TryParse(spriteRects[i].name.Substring(prefix.Length), out pose) ||
                    pose < 0 || pose >= desiredPivots.Length) continue;
                spriteRects[i].alignment = SpriteAlignment.Custom;
                spriteRects[i].pivot = desiredPivots[pose];
            }
            provider.SetSpriteRects(spriteRects);
            provider.Apply();

            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.isReadable = false;
            importer.SaveAndReimport();
            return LoadOrderedSprites(path, prefix, poseCount, true);
        }

        private static Sprite[] LoadOrderedSprites(string path, string prefix, int frameCount,
            bool twoDigitSuffix)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var result = new Sprite[frameCount];
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite sprite = assets[i] as Sprite;
                if (sprite == null) continue;
                for (int frame = 0; frame < frameCount; frame++)
                {
                    string suffix = twoDigitSuffix ? frame.ToString("00") : frame.ToString();
                    if (sprite.name != prefix + suffix) continue;
                    result[frame] = sprite;
                    break;
                }
            }
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != null) continue;
                Debug.LogError("[Baker] Missing sprite frame " + i + " in " + path);
                return null;
            }
            return result;
        }

        // ================= hero =================

        // Cultivator palette — ivory robe, jade trim, vermillion sash, ink hair.
        private static readonly Color RobeLight = new Color(0.96f, 0.92f, 0.84f);
        private static readonly Color RobeShade = new Color(0.78f, 0.72f, 0.60f);
        private static readonly Color JadeTrim = new Color(0.18f, 0.58f, 0.46f);
        private static readonly Color JadeGlow = new Color(0.25f, 0.83f, 0.63f);
        private static readonly Color SashRed = new Color(0.66f, 0.20f, 0.20f);
        private static readonly Color Skin = new Color(0.96f, 0.80f, 0.62f);
        private static readonly Color SkinShade = new Color(0.84f, 0.64f, 0.46f);
        private static readonly Color HairInk = new Color(0.16f, 0.13f, 0.15f);

private static Painter HeroBody(int frame)
        {
            var p = new Painter(180, 225);
            float cx = 90f;
            bool running = frame > 0;
            int pose = running ? (frame - 1) & 3 : 0;
            float stride = pose == 0 ? -1f : pose == 2 ? 1f : 0f;
            float bounce = running && (pose == 1 || pose == 3) ? 4f : 0f;
            float bodyX = cx + stride * 2f;
            float leftFootX = cx - 17f - stride * 11f;
            float rightFootX = cx + 17f + stride * 11f;
            float leftFootY = 15f + (pose == 0 ? 6f : 0f);
            float rightFootY = 15f + (pose == 2 ? 6f : 0f);

            // Two broad legs and cloth shoes make every stride readable at phone size.
            p.TaperedLine(new Vector2(bodyX - 12f, 61f + bounce),
                new Vector2(leftFootX, leftFootY + 8f), 14f, 9f, RobeShade);
            p.TaperedLine(new Vector2(bodyX + 12f, 61f + bounce),
                new Vector2(rightFootX, rightFootY + 8f), 14f, 9f, RobeLight);
            p.FillEllipse(leftFootX - 3f, leftFootY, 15f, 8f, HairInk);
            p.FillEllipse(rightFootX + 3f, rightFootY, 15f, 8f, HairInk);

            // Large robe shapes, a single sash and one jade collar: no micro-detail.
            float hemSway = stride * 8f;
            p.FillTriangle(new Vector2(bodyX - 43f + hemSway, 31f + bounce),
                new Vector2(bodyX - 24f, 98f + bounce),
                new Vector2(bodyX + 2f, 34f + bounce), RobeShade);
            p.FillTriangle(new Vector2(bodyX + 43f + hemSway, 31f + bounce),
                new Vector2(bodyX + 24f, 98f + bounce),
                new Vector2(bodyX - 2f, 34f + bounce), RobeLight);
            p.FillEllipseShaded(bodyX, 93f + bounce, 34f, 39f, RobeLight, RobeShade);

            float armSwing = running ? stride * 16f : 0f;
            p.TaperedLine(new Vector2(bodyX - 27f, 116f + bounce),
                new Vector2(bodyX - 42f - armSwing, 78f + bounce), 19f, 10f, RobeShade);
            p.TaperedLine(new Vector2(bodyX + 27f, 116f + bounce),
                new Vector2(bodyX + 41f + armSwing, 83f + bounce), 19f, 10f, RobeLight);
            p.Line(new Vector2(bodyX - 17f, 127f + bounce),
                new Vector2(bodyX + 5f, 101f + bounce), 7f, JadeTrim);
            p.Line(new Vector2(bodyX + 17f, 127f + bounce),
                new Vector2(bodyX - 5f, 101f + bounce), 7f, JadeTrim);
            p.FillRoundRect(bodyX - 31f, 76f + bounce, 62f, 12f, 5f, SashRed);
            p.FillCircle(bodyX, 82f + bounce, 6f, JadeGlow);

            // Right-facing profile: one eye and a trailing ponytail make direction unmistakable.
            float headY = 165f + bounce;
            float tailLift = running ? 6f + Mathf.Abs(stride) * 4f : 0f;
            p.TaperedLine(new Vector2(bodyX - 12f, headY + 15f),
                new Vector2(bodyX - 48f - Mathf.Abs(stride) * 7f,
                    headY - 9f + tailLift), 10f, 2.5f, HairInk);
            p.FillEllipseShaded(bodyX + 2f, headY, 21f, 24f, Skin, SkinShade);
            p.FillTriangle(new Vector2(bodyX + 18f, headY + 6f),
                new Vector2(bodyX + 28f, headY),
                new Vector2(bodyX + 18f, headY - 2f), Skin);
            p.Arc(bodyX, headY + 2f, 21f, Mathf.PI * 0.08f, Mathf.PI * 0.92f,
                11f, HairInk);
            p.FillCircle(bodyX - 3f, headY + 31f, 9f, HairInk);
            p.TaperedLine(new Vector2(bodyX - 20f, headY + 34f),
                new Vector2(bodyX + 14f, headY + 28f), 4f, 2f, JadeGlow);
            p.FillEllipse(bodyX + 10f, headY + 3f, 3.2f, 5.2f, Ink);
            p.Arc(bodyX + 17f, headY - 7f, 5f, Mathf.PI * 1.2f,
                Mathf.PI * 1.7f, 2f, SkinShade);
            return p;
        }

        private static Painter HeroSword()
        {
            var p = new Painter(48, 132);
            // ivory-jade blade with a bright edge glint
            p.Line(new Vector2(24f, 30f), new Vector2(24f, 116f), 14f,
                new Color(JadeGlow.r, JadeGlow.g, JadeGlow.b, 0.28f));
            p.TaperedLine(new Vector2(24f, 30f), new Vector2(24f, 120f), 9f, 4f,
                new Color(0.88f, 0.95f, 0.90f, 0.97f));
            p.TaperedLine(new Vector2(24f, 32f), new Vector2(24f, 116f), 3.4f, 1.4f, Color.white);
            p.Glow(24f, 118f, 13f, JadeGlow, 0.5f);
            // gold guard, lacquer grip, vermillion tassel
            p.FillRoundRect(12f, 26f, 24f, 6f, 3f, new Color(0.83f, 0.65f, 0.24f));
            p.FillRoundRect(19f, 8f, 10f, 20f, 4f, HairInk);
            p.TaperedLine(new Vector2(24f, 8f), new Vector2(19f, 0f), 3f, 1.5f, SashRed);
            p.FillCircle(18f, 1f, 2.4f, SashRed);
            return p;
        }

        private static Painter HeroShadow()
        {
            var p = new Painter(130, 40);
            p.FillEllipse(65f, 20f, 58f, 15f, new Color(0f, 0f, 0f, 0.32f));
            return p;
        }

        private static Painter HeroFlash()
        {
            var p = new Painter(140, 140);
            p.FillCircle(70f, 70f, 62f, new Color(1f, 1f, 1f, 0.55f));
            return p;
        }

        // ================= enemy layers =================
        // All share the 240x240 canvas and the same pivot so they stack pixel-aligned.
        // Draw order: glow, horns, spikes, body, spots, eyes, mouth.

        private static Painter EnemyCanvasNew() => new Painter(EnemyCanvas, EnemyCanvas);

        private static Painter EnemyGlow()
        {
            // demonic-qi aura: vermillion core bleeding into violet — the elite marker
            Painter p = EnemyCanvasNew();
            p.Glow(EnemyCx, EnemyCy, 116f, new Color(0.95f, 0.30f, 0.42f), 0.5f);
            p.Glow(EnemyCx, EnemyCy, 90f, new Color(0.62f, 0.30f, 0.85f), 0.30f);
            return p;
        }

        private static Painter EnemyHorns()
        {
            Painter p = EnemyCanvasNew();
            var bone = new Color(0.88f, 0.78f, 0.57f);
            var edge = new Color(0.32f, 0.24f, 0.18f);
            p.Arc(73f, 162f, 37f, Mathf.PI * 0.45f, Mathf.PI * 1.08f, 13f, edge);
            p.Arc(73f, 162f, 37f, Mathf.PI * 0.48f, Mathf.PI * 1.03f, 7f, bone);
            p.Arc(167f, 162f, 37f, Mathf.PI * -0.08f, Mathf.PI * 0.55f, 13f, edge);
            p.Arc(167f, 162f, 37f, Mathf.PI * -0.03f, Mathf.PI * 0.52f, 7f, bone);
            return p;
        }

        private static Painter EnemySpikes()
        {
            Painter p = EnemyCanvasNew();
            var gold = new Color(0.85f, 0.66f, 0.27f);
            var deep = new Color(0.42f, 0.25f, 0.12f);
            for (int k = -2; k <= 2; k++)
            {
                float bx = EnemyCx + k * 31f;
                float peak = EnemyCy + 115f + (2 - Mathf.Abs(k)) * 10f;
                p.FillTriangle(new Vector2(bx - 11f, EnemyCy + 62f), new Vector2(bx, peak),
                    new Vector2(bx + 11f, EnemyCy + 62f), deep);
                p.FillTriangle(new Vector2(bx - 6f, EnemyCy + 65f), new Vector2(bx, peak - 7f),
                    new Vector2(bx + 6f, EnemyCy + 65f), gold);
            }
            return p;
        }

        private static Painter EnemyBody(Color color)
        {
            Painter p = EnemyCanvasNew();
            Color ink = Color.Lerp(color, Color.black, 0.62f);
            Color shade = Color.Lerp(color, Color.black, 0.28f);
            Color light = Color.Lerp(color, new Color(0.94f, 0.88f, 0.70f), 0.34f);

            // Long spirit tail and broad haunches give the beast a mythic, non-blob silhouette.
            p.Arc(184f, 76f, 43f, Mathf.PI * 0.8f, Mathf.PI * 2.15f, 20f, ink);
            p.Arc(184f, 76f, 43f, Mathf.PI * 0.88f, Mathf.PI * 2.05f, 11f, color);
            p.FillEllipseShaded(EnemyCx, 70f, 76f, 45f, color, shade);
            p.OutlineEllipse(EnemyCx, 70f, 76f, 45f, 6f, ink);

            // Layered mane reads like dry-brush tufts when reduced to mobile size.
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector2 inner = new Vector2(EnemyCx + Mathf.Cos(a) * 45f,
                    126f + Mathf.Sin(a) * 41f);
                Vector2 left = new Vector2(EnemyCx + Mathf.Cos(a - 0.20f) * 72f,
                    126f + Mathf.Sin(a - 0.20f) * 66f);
                Vector2 right = new Vector2(EnemyCx + Mathf.Cos(a + 0.20f) * 72f,
                    126f + Mathf.Sin(a + 0.20f) * 66f);
                p.FillTriangle(inner, left, right, ink);
            }

            // Upright ears and a faceted mask replace the former round mascot face.
            p.FillTriangle(new Vector2(75f, 150f), new Vector2(63f, 207f),
                new Vector2(104f, 169f), ink);
            p.FillTriangle(new Vector2(165f, 150f), new Vector2(177f, 207f),
                new Vector2(136f, 169f), ink);
            p.FillTriangle(new Vector2(78f, 157f), new Vector2(68f, 193f),
                new Vector2(99f, 166f), shade);
            p.FillTriangle(new Vector2(162f, 157f), new Vector2(172f, 193f),
                new Vector2(141f, 166f), shade);
            p.OutlineEllipse(EnemyCx, 128f, 59f, 54f, 6f, ink);
            p.FillEllipseShaded(EnemyCx, 128f, 57f, 52f, light, shade);
            p.FillTriangle(new Vector2(EnemyCx - 42f, 115f), new Vector2(EnemyCx, 78f),
                new Vector2(EnemyCx + 42f, 115f), color);
            p.FillEllipse(EnemyCx, 103f, 32f, 20f,
                Color.Lerp(light, new Color(0.92f, 0.82f, 0.66f), 0.45f));
            p.FillCircle(EnemyCx, 108f, 6f, ink);
            return p;
        }

        private static Painter EnemySpots()
        {
            Painter p = EnemyCanvasNew();
            var rune = new Color(0.46f, 0.08f, 0.12f, 0.58f);
            p.TaperedLine(new Vector2(120f, 168f), new Vector2(120f, 145f), 5f, 2f, rune);
            p.Arc(120f, 142f, 20f, Mathf.PI * 0.12f, Mathf.PI * 0.88f, 3f, rune);
            p.TaperedLine(new Vector2(91f, 116f), new Vector2(74f, 99f), 4f, 1.5f, rune);
            p.TaperedLine(new Vector2(149f, 116f), new Vector2(166f, 99f), 4f, 1.5f, rune);
            p.Arc(120f, 66f, 35f, Mathf.PI * 0.18f, Mathf.PI * 0.82f, 3f, rune);
            return p;
        }

        private static Painter EnemyEyes(int n)
        {
            Painter p = EnemyCanvasNew();
            var iris = new Color(0.96f, 0.72f, 0.22f);
            var sclera = new Color(1f, 0.93f, 0.72f, 0.92f);
            for (int i = 0; i < n; i++)
            {
                float ex = EnemyCx + (i - (n - 1) * 0.5f) * 40f;
                float ey = n == 1 ? 145f : 137f + (i % 2) * 4f;
                p.FillEllipse(ex, ey, 13f, 6f, sclera);
                p.OutlineEllipse(ex, ey, 13f, 6f, 2.5f, Ink);
                p.FillEllipse(ex, ey, 4f, 6f, iris);
                p.TaperedLine(new Vector2(ex, ey - 5f), new Vector2(ex, ey + 5f),
                    2.5f, 1.2f, Ink);
            }
            return p;
        }

        private static Painter EnemyMouth()
        {
            Painter p = EnemyCanvasNew();
            var mouth = new Color(0.35f, 0.08f, 0.10f);
            p.Arc(EnemyCx, 97f, 17f, Mathf.PI * 1.12f, Mathf.PI * 1.88f, 4f, mouth);
            p.FillTriangle(new Vector2(EnemyCx - 15f, 94f), new Vector2(EnemyCx - 10f, 82f),
                new Vector2(EnemyCx - 4f, 94f), new Color(0.96f, 0.91f, 0.78f));
            p.FillTriangle(new Vector2(EnemyCx + 4f, 94f), new Vector2(EnemyCx + 10f, 82f),
                new Vector2(EnemyCx + 15f, 94f), new Color(0.96f, 0.91f, 0.78f));
            return p;
        }

        private static Painter EnemyShadow()
        {
            var p = new Painter(150, 44);
            p.FillEllipse(75f, 22f, 66f, 16f, new Color(0f, 0f, 0f, 0.32f));
            return p;
        }

        private static Painter EnemyFlash()
        {
            var p = new Painter(170, 170);
            p.FillCircle(85f, 85f, 76f, new Color(1f, 1f, 1f, 0.6f));
            return p;
        }

        // ================= hud / fx =================

        private static Painter HpFrame()
        {
            var p = new Painter(132, 30);
            var ink = new Color(0.08f, 0.07f, 0.055f, 0.92f);
            var gold = new Color(0.77f, 0.60f, 0.30f, 0.95f);
            p.FillRoundRect(5f, 4f, 122f, 22f, 8f, ink);
            p.OutlineEllipse(10f, 15f, 8f, 8f, 2.5f, gold);
            p.OutlineEllipse(122f, 15f, 8f, 8f, 2.5f, gold);
            p.FillTriangle(new Vector2(1f, 15f), new Vector2(12f, 5f),
                new Vector2(12f, 25f), gold);
            p.FillTriangle(new Vector2(131f, 15f), new Vector2(120f, 5f),
                new Vector2(120f, 25f), gold);
            return p;
        }

        private static Painter HpFill()
        {
            var p = new Painter(112, 16);
            p.FillRoundRect(0f, 0f, 112f, 16f, 7f, Color.white);
            p.FillRoundRect(2f, 9f, 108f, 5f, 2.5f, new Color(1f, 1f, 1f, 0.35f));
            return p;
        }

        private static Painter BurstStar()
        {
            var p = new Painter(36, 36);
            Color c = Color.white;
            p.FillTriangle(new Vector2(18f, 34f), new Vector2(14f, 18f), new Vector2(22f, 18f), c);
            p.FillTriangle(new Vector2(18f, 2f), new Vector2(14f, 18f), new Vector2(22f, 18f), c);
            p.FillTriangle(new Vector2(2f, 18f), new Vector2(18f, 14f), new Vector2(18f, 22f), c);
            p.FillTriangle(new Vector2(34f, 18f), new Vector2(18f, 14f), new Vector2(18f, 22f), c);
            p.Glow(18f, 18f, 15f, c, 0.6f);
            return p;
        }

        /// <summary>One artifact silhouette per sidekick id — no faces, these are pháp bảo.</summary>
        /// <summary>Jagged tribulation bolt, pivot at the top so it hangs from its spawn point.</summary>
        private static Painter TribulationBolt()
        {
            var p = new Painter(96, 300);
            var violet = new Color(0.62f, 0.45f, 1f);
            var core = new Color(0.94f, 0.97f, 1f);
            Vector2[] pts =
            {
                new Vector2(50f, 298f), new Vector2(36f, 224f), new Vector2(58f, 168f),
                new Vector2(40f, 102f), new Vector2(54f, 40f), new Vector2(46f, 2f)
            };
            for (int i = 0; i < pts.Length - 1; i++)
            {
                p.TaperedLine(pts[i], pts[i + 1], 16f - i * 2f, 12f - i * 2f,
                    new Color(violet.r, violet.g, violet.b, 0.35f));
            }
            for (int i = 0; i < pts.Length - 1; i++)
            {
                p.TaperedLine(pts[i], pts[i + 1], 7f - i * 0.8f, 5f - i * 0.8f, core);
            }
            p.TaperedLine(new Vector2(58f, 168f), new Vector2(84f, 126f), 5f, 1.4f,
                new Color(core.r, core.g, core.b, 0.85f));
            p.Glow(50f, 288f, 26f, violet, 0.5f);
            return p;
        }

        private static Painter Orb(Color color, string id)
        {
            var p = new Painter(64, 64);
            p.Glow(32f, 32f, 28f, color, 0.85f);
            switch (id)
            {
                case "blob": // flying sword, point down
                    p.TaperedLine(new Vector2(32f, 8f), new Vector2(32f, 46f), 3f, 9f,
                        Color.Lerp(color, Color.white, 0.55f));
                    p.TaperedLine(new Vector2(32f, 10f), new Vector2(32f, 44f), 1.2f, 3.4f,
                        Color.white);
                    p.FillRoundRect(24f, 44f, 16f, 4.5f, 2f, new Color(0.83f, 0.65f, 0.24f));
                    p.FillRoundRect(29.5f, 48f, 5f, 9f, 2f, HairInk);
                    break;
                case "medic": // lingzhi mushroom
                    p.FillRoundRect(29f, 18f, 6f, 14f, 2.5f,
                        Color.Lerp(color, Color.white, 0.4f));
                    p.FillEllipse(32f, 36f, 15f, 9f, color);
                    p.Arc(32f, 36f, 11f, Mathf.PI * 0.15f, Mathf.PI * 0.85f, 3f,
                        Color.Lerp(color, Color.white, 0.45f));
                    break;
                case "shield": // turtle shell
                    p.FillEllipseShaded(32f, 32f, 17f, 14f, Color.Lerp(color, Color.white, 0.3f),
                        Color.Lerp(color, Color.black, 0.2f));
                    p.OutlineEllipse(32f, 32f, 17f, 14f, 2.4f, Color.Lerp(color, Color.black, 0.4f));
                    p.Line(new Vector2(32f, 20f), new Vector2(32f, 44f), 1.8f,
                        Color.Lerp(color, Color.black, 0.35f));
                    p.Line(new Vector2(20f, 27f), new Vector2(44f, 27f), 1.8f,
                        Color.Lerp(color, Color.black, 0.35f));
                    p.Line(new Vector2(21f, 38f), new Vector2(43f, 38f), 1.8f,
                        Color.Lerp(color, Color.black, 0.35f));
                    break;
                default: // "spark" — thunder pearl with a lightning glint
                    p.FillEllipseShaded(32f, 32f, 14f, 14f, Color.Lerp(color, Color.white, 0.4f),
                        color);
                    p.Line(new Vector2(35f, 43f), new Vector2(29f, 33f), 2.6f, Color.white);
                    p.Line(new Vector2(29f, 33f), new Vector2(35f, 30f), 2.6f, Color.white);
                    p.Line(new Vector2(35f, 30f), new Vector2(28f, 20f), 2.6f, Color.white);
                    break;
            }
            return p;
        }

        /// <summary>
        /// Rounded pill for uGUI progress bars. Horizontal 9-slice only (the vertical middle
        /// slice would be zero-height at this size), so it must be shown at its baked height.
        /// </summary>
        private static Painter UiCapsule()
        {
            var p = new Painter(CapsuleWidth, CapsuleHeight);
            p.FillRoundRect(0f, 0f, CapsuleWidth, CapsuleHeight, CapsuleRadius, Color.white);
            return p;
        }

        private static Painter Twinkle()
        {
            var p = new Painter(28, 28);
            p.Glow(14f, 14f, 12f, Color.white, 0.9f);
            p.FillCircle(14f, 14f, 2.6f, Color.white);
            return p;
        }

        // ================= world backdrop =================
        // A single sky layer per realm, streamed via Addressables and repeated as a
        // mirrored belt by WorldBackgroundRenderer so it can scroll seamlessly.

        private static Painter PaintSky(int worldIndex, World def)
        {
            int w = Mathf.RoundToInt(BgWorldWidth * Ppu);
            int h = Mathf.RoundToInt(BgWorldHeight * Ppu);
            float groundTop = (BgHorizonY - BgBottomY) * Ppu;
            var rng = new System.Random(worldIndex * 1337 + 7);
            var p = new Painter(w, h);

            p.Clear(new Color32(0, 0, 0, 255));
            p.GradientV(def.SkyTop, def.SkyBottom, 0, h - 1);

            // spirit-qi haze drifting between the peaks
            for (int i = 0; i < 3; i++)
            {
                Color haze = Color.Lerp(def.SkyBottom, def.Flora[i % def.Flora.Length], 0.45f);
                p.Glow(Rand(rng, 0, w), Rand(rng, groundTop + (h - groundTop) * 0.35f, h * 0.95f),
                    Rand(rng, 220f, 380f), haze, 0.14f);
            }

            // far floating mountains, each resting on its own bed of mist
            Color far = Color.Lerp(def.SkyBottom, Color.black, 0.30f);
            Color mist = Color.Lerp(def.SkyBottom, Color.white, 0.45f);
            for (int i = 0; i < 3; i++)
            {
                float ix = Rand(rng, w * 0.08f, w * 0.92f);
                float iy = Rand(rng, groundTop + (h - groundTop) * 0.42f,
                    groundTop + (h - groundTop) * 0.72f);
                float s = Rand(rng, 1.1f, 1.9f);
                RockCluster(p, far, ix, iy, s);
                p.FillEllipse(ix, iy - 42f, 85f * s, 16f * s, new Color(mist.r, mist.g, mist.b, 0.5f));
                p.Glow(ix, iy - 40f, 90f * s, mist, 0.22f);
            }

            // wide, calm cloud banks
            for (int i = 0; i < 4; i++)
            {
                float cy2 = Rand(rng, groundTop + (h - groundTop) * 0.2f, h * 0.9f);
                float cw = Rand(rng, 180f, 320f);
                p.FillEllipse(Rand(rng, 0, w), cy2, cw, cw * 0.14f,
                    new Color(mist.r, mist.g, mist.b, 0.20f));
            }

            // a few faint stars
            for (int i = 0; i < 26; i++)
            {
                float sx = Rand(rng, 0, w);
                float sy = Rand(rng, groundTop + (h - groundTop) * 0.35f, h);
                p.FillCircle(sx, sy, Rand(rng, 0.6f, 1.8f),
                    new Color(1f, 1f, 1f, Rand(rng, 0.2f, 0.6f)));
            }

            // jade moon — clean disc with a soft ring halo, no craters
            float mx = w * 0.82f, my = h * 0.9f, mr = 56f;
            p.Glow(mx, my, mr * 2.4f, def.Moon, 0.35f);
            p.FillCircle(mx, my, mr, def.Moon);
            p.OutlineEllipse(mx, my, mr * 1.5f, mr * 1.5f, 2.5f,
                new Color(def.Moon.r, def.Moon.g, def.Moon.b, 0.30f));
            return p;
        }

        private static float Rand(System.Random rng, float min, float max) =>
            min + (float)rng.NextDouble() * (max - min);

        private static void RockCluster(Painter p, Color color, float x, float y, float s)
        {
            var a = new Vector2(x - 45f * s, y - 50f);
            var b = new Vector2(x - 30f * s, y + 20f * s);
            var c = new Vector2(x - 10f * s, y);
            var d = new Vector2(x + 8f * s, y + 38f * s);
            var e = new Vector2(x + 28f * s, y + 5f * s);
            var f = new Vector2(x + 45f * s, y - 50f);
            p.FillTriangle(a, b, c, color);
            p.FillTriangle(a, c, f, color);
            p.FillTriangle(c, d, e, color);
            p.FillTriangle(c, e, f, color);
            // snow-lit peaks
            Color lit = Color.Lerp(color, Color.white, 0.3f);
            p.FillTriangle(new Vector2(d.x - 7f * s, d.y - 12f * s), d,
                new Vector2(d.x + 7f * s, d.y - 12f * s), lit);
        }
}
}
