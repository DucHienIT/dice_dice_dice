using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;
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
        public const string ArtDir = "Assets/Art/Generated";

        public const float Ppu = 100f;
        private const float BurstPpu = 120f;

        // Background world metrics — pushed onto WorldBackgroundRenderer by the builder so
        // the baked pixels and the scene placement can never drift apart. BgWorldWidth doubles
        // as the scroll period: the ground strip tiles seamlessly every BgWorldWidth units.
        public const float BgWorldWidth = 14.6f;
        public const float BgWorldHeight = 12f;
        public const float BgBottomY = -2.4f;
        public const float BgHorizonY = 3f;
        /// <summary>Lake center, local to a ground strip copy — where the animated glow sits.</summary>
        public const float BgLakeLocalY = BgHorizonY - BgBottomY - 0.52f;

        private const int GroundStripHeight = 700;

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
            public Sprite Twinkle;
            public Sprite LakeGlow;

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

            // ---- hero ----
            s.HeroBody = Save(HeroBody(), "hero_body", new Vector2(0.5f, 0.03f));
            s.HeroSword = Save(HeroSword(), "hero_sword", new Vector2(0.5f, 0.1f));
            s.HeroShadow = Save(HeroShadow(), "hero_shadow", Center);
            s.HeroFlash = Save(HeroFlash(), "hero_flash", Center);

            // ---- enemy layers ----
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

            // ---- hud / fx ----
            s.HpFrame = Save(HpFrame(), "hpbar_frame", Center);
            s.HpFill = Save(HpFill(), "hpbar_fill", new Vector2(0f, 0.5f));
            s.BurstStar = Save(BurstStar(), "burst_star", Center, BurstPpu);
            s.Twinkle = Save(Twinkle(), "twinkle", Center);
            s.LakeGlow = Save(LakeGlow(), "lake_glow", Center);
            s.UiCapsule = Save(UiCapsule(), "ui_capsule", Center, Ppu,
                border: new Vector4(CapsuleRadius, 0f, CapsuleRadius, 0f));

            // ---- per-asset art ----
            Sidekick[] sidekicks = config.Sidekicks;
            for (int i = 0; i < sidekicks.Length; i++)
            {
                Sidekick sk = sidekicks[i];
                SetPrivate(sk, "_orbSprite", Save(Orb(sk.Color), "orb_" + sk.Id, Center));
            }

            World[] worlds = config.Worlds;
            for (int i = 0; i < worlds.Length; i++)
            {
                World def = worlds[i];
                // File names key on the asset name (stable id), never DisplayName —
                // DisplayName is localized, so it depends on the editor language at bake time.
                SetPrivate(def, "_skyLayer", Save(PaintSky(i, def),
                    "bg_" + def.name + "_sky", new Vector2(0.5f, 0f), Ppu,
                    opaque: true, backdrop: true));
                SetPrivate(def, "_groundLayer", Save(PaintGround(i, def),
                    "bg_" + def.name + "_ground", new Vector2(0.5f, 0f), Ppu,
                    opaque: false, backdrop: true));
            }

            return s;
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

        // ================= hero =================

        private static Painter HeroBody()
        {
            var p = new Painter(170, 210);
            float cx = 85f;

            // legs
            p.FillEllipse(cx - 20f, 16f, 15f, 10f, new Color(0.87f, 0.90f, 0.93f));
            p.FillEllipse(cx + 20f, 16f, 15f, 10f, new Color(0.87f, 0.90f, 0.93f));
            // suit body
            p.FillEllipseShaded(cx, 58f, 41f, 39f,
                new Color(0.97f, 0.98f, 1f), new Color(0.72f, 0.77f, 0.86f));
            p.OutlineEllipse(cx, 58f, 41f, 39f, 2.5f, new Color(0.45f, 0.51f, 0.64f, 0.55f));
            // under-belly shading
            p.FillEllipse(cx, 42f, 25f, 15f, new Color(0.79f, 0.82f, 0.88f));
            // cyan utility belt
            p.FillRoundRect(cx - 29f, 55f, 58f, 11f, 5f, new Color(0.29f, 0.83f, 1f));
            p.Glow(cx, 60f, 34f, new Color(0.29f, 0.83f, 1f), 0.16f);
            // head — friendly green alien
            p.FillEllipseShaded(cx, 141f, 30f, 30f,
                new Color(0.62f, 0.94f, 0.5f), new Color(0.42f, 0.76f, 0.31f));
            // eyes
            p.FillEllipse(cx - 12f, 143f, 7f, 10f, Ink);
            p.FillEllipse(cx + 12f, 143f, 7f, 10f, Ink);
            p.FillCircle(cx - 14f, 148f, 2.6f, Color.white);
            p.FillCircle(cx + 10f, 148f, 2.6f, Color.white);
            // smile
            p.Arc(cx, 132f, 7f, Mathf.PI * 1.25f, Mathf.PI * 1.75f, 3.2f,
                new Color(0.18f, 0.36f, 0.14f));
            // antenna
            p.Line(new Vector2(cx, 170f), new Vector2(cx + 11f, 186f), 3.6f,
                new Color(0.49f, 0.87f, 0.42f));
            p.Glow(cx + 13f, 188f, 14f, new Color(1f, 0.83f, 0.36f), 0.85f);
            p.FillCircle(cx + 13f, 188f, 5.5f, new Color(1f, 0.83f, 0.36f));
            // helmet dome
            p.FillCircle(cx, 141f, 38f, new Color(0.63f, 0.86f, 1f, 0.13f));
            p.OutlineEllipse(cx, 141f, 38f, 38f, 4f, new Color(0.71f, 0.9f, 1f, 0.8f));
            p.FillEllipse(cx - 16f, 158f, 10f, 6f, new Color(1f, 1f, 1f, 0.45f));
            return p;
        }

        private static Painter HeroSword()
        {
            var p = new Painter(48, 132);
            var glowCyan = new Color(0.29f, 0.83f, 1f);
            p.Line(new Vector2(24f, 30f), new Vector2(24f, 116f), 15f,
                new Color(glowCyan.r, glowCyan.g, glowCyan.b, 0.30f));
            p.Line(new Vector2(24f, 30f), new Vector2(24f, 118f), 8f,
                new Color(0.61f, 0.91f, 1f, 0.95f));
            p.Line(new Vector2(24f, 32f), new Vector2(24f, 114f), 3.4f, Color.white);
            p.Glow(24f, 118f, 14f, glowCyan, 0.5f);
            p.FillRoundRect(18f, 6f, 12f, 24f, 4f, new Color(0.33f, 0.37f, 0.44f));
            p.FillRoundRect(12f, 24f, 24f, 7f, 3f, new Color(0.42f, 0.47f, 0.55f));
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
            Painter p = EnemyCanvasNew();
            p.Glow(EnemyCx, EnemyCy, 116f, new Color(0.49f, 1f, 0.42f), 0.55f);
            return p;
        }

        private static Painter EnemyHorns()
        {
            Painter p = EnemyCanvasNew();
            var cream = new Color(1f, 0.91f, 0.79f);
            p.FillTriangle(new Vector2(EnemyCx - 55f, EnemyCy + 70f),
                new Vector2(EnemyCx - 80f, EnemyCy + 128f),
                new Vector2(EnemyCx - 28f, EnemyCy + 88f), cream);
            p.FillTriangle(new Vector2(EnemyCx + 55f, EnemyCy + 70f),
                new Vector2(EnemyCx + 80f, EnemyCy + 128f),
                new Vector2(EnemyCx + 28f, EnemyCy + 88f), cream);
            return p;
        }

        private static Painter EnemySpikes()
        {
            Painter p = EnemyCanvasNew();
            var gold = new Color(1f, 0.83f, 0.36f);
            for (int k = -2; k <= 2; k++)
            {
                float bx = EnemyCx + k * 40f;
                float peak = EnemyCy + 132f + (2 - Mathf.Abs(k)) * 16f;
                p.FillTriangle(new Vector2(bx - 16f, EnemyCy + 78f), new Vector2(bx, peak),
                    new Vector2(bx + 16f, EnemyCy + 78f), gold);
            }
            return p;
        }

        private static Painter EnemyBody(Color color)
        {
            Painter p = EnemyCanvasNew();
            Color dark = Color.Lerp(color, Color.black, 0.35f);
            Color light = Color.Lerp(color, Color.white, 0.25f);
            p.OutlineEllipse(EnemyCx, EnemyCy, 90f, 90f, 7f, dark);
            p.FillEllipseShaded(EnemyCx, EnemyCy, 88f, 88f, light,
                Color.Lerp(color, Color.black, 0.12f));
            // glowing belly
            p.FillEllipse(EnemyCx, EnemyCy - 42f, 52f, 34f, new Color(1f, 1f, 1f, 0.35f));
            return p;
        }

        private static Painter EnemySpots()
        {
            Painter p = EnemyCanvasNew();
            var spot = new Color(0f, 0f, 0f, 0.15f);
            p.FillCircle(EnemyCx - 48f, EnemyCy + 22f, 14f, spot);
            p.FillCircle(EnemyCx + 42f, EnemyCy + 42f, 10f, spot);
            p.FillCircle(EnemyCx + 56f, EnemyCy - 12f, 8f, spot);
            return p;
        }

        private static Painter EnemyEyes(int n)
        {
            Painter p = EnemyCanvasNew();
            for (int i = 0; i < n; i++)
            {
                float ex = EnemyCx + (i - (n - 1) * 0.5f) * 44f;
                float ey = EnemyCy + 22f + (i % 2) * 10f;
                p.FillCircle(ex, ey, 22f, Color.white);
                p.OutlineEllipse(ex, ey, 22f, 22f, 2.5f, new Color(0f, 0f, 0f, 0.25f));
                p.FillCircle(ex - 5f, ey - 2f, 10f, Ink);
                p.FillCircle(ex - 8f, ey + 3f, 3.4f, Color.white);
            }
            return p;
        }

        private static Painter EnemyMouth()
        {
            Painter p = EnemyCanvasNew();
            p.Arc(EnemyCx, EnemyCy - 32f, 20f, Mathf.PI * 0.15f, Mathf.PI * 0.85f, 6.5f,
                new Color(0.48f, 0.11f, 0.18f));
            p.FillTriangle(new Vector2(EnemyCx - 17f, EnemyCy - 22f),
                new Vector2(EnemyCx - 10f, EnemyCy - 40f),
                new Vector2(EnemyCx - 3f, EnemyCy - 22f), Color.white);
            p.FillTriangle(new Vector2(EnemyCx + 3f, EnemyCy - 22f),
                new Vector2(EnemyCx + 10f, EnemyCy - 40f),
                new Vector2(EnemyCx + 17f, EnemyCy - 22f), Color.white);
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
            var p = new Painter(124, 26);
            p.FillRoundRect(1f, 1f, 122f, 24f, 10f, new Color(0f, 0f, 0f, 0.58f));
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

        private static Painter Orb(Color color)
        {
            var p = new Painter(64, 64);
            p.Glow(32f, 32f, 28f, color, 0.9f);
            p.FillCircle(32f, 32f, 18f, color);
            p.FillEllipse(26f, 40f, 7f, 5f, new Color(1f, 1f, 1f, 0.5f));
            p.FillCircle(26f, 32f, 3.2f, Ink);
            p.FillCircle(38f, 32f, 3.2f, Ink);
            p.Arc(32f, 26f, 4f, Mathf.PI * 1.2f, Mathf.PI * 1.8f, 2.2f, Ink);
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

        private static Painter LakeGlow()
        {
            var p = new Painter(500, 140);
            p.Glow(250f, 70f, 240f, Color.white, 0.5f);
            p.FillEllipse(250f, 70f, 220f, 52f, new Color(1f, 1f, 1f, 0.25f));
            return p;
        }

        // ================= world backdrop =================
        // Two layers so the world can scroll: the sky never moves (distant parallax), the
        // ground strip scrolls and is drawn twice side by side, so it must tile seamlessly.

        private static Painter PaintSky(int worldIndex, World def)
        {
            int w = Mathf.RoundToInt(BgWorldWidth * Ppu);
            int h = Mathf.RoundToInt(BgWorldHeight * Ppu);
            float groundTop = (BgHorizonY - BgBottomY) * Ppu;
            var rng = new System.Random(worldIndex * 1337 + 7);
            var p = new Painter(w, h);

            p.Clear(new Color32(0, 0, 0, 255));
            p.GradientV(def.SkyTop, def.SkyBottom, 0, h - 1);

            // nebula wisps
            for (int i = 0; i < 3; i++)
            {
                Color neb = Color.Lerp(def.SkyBottom, def.Flora[i % def.Flora.Length], 0.55f);
                p.Glow(Rand(rng, 0, w), Rand(rng, groundTop + (h - groundTop) * 0.35f, h * 0.95f),
                    Rand(rng, 220f, 380f), neb, 0.16f);
            }

            // stars
            for (int i = 0; i < 70; i++)
            {
                float sx = Rand(rng, 0, w);
                float sy = Rand(rng, groundTop + (h - groundTop) * 0.25f, h);
                float sr = Rand(rng, 0.6f, 2.2f);
                p.FillCircle(sx, sy, sr, new Color(1f, 1f, 1f, Rand(rng, 0.25f, 0.85f)));
            }

            // moon with craters
            float mx = w * 0.82f, my = h * 0.9f, mr = 56f;
            p.Glow(mx, my, mr * 2.4f, def.Moon, 0.35f);
            p.FillCircle(mx, my, mr, def.Moon);
            var crater = new Color(0f, 0f, 0f, 0.22f);
            p.FillCircle(mx + 18f, my + 10f, 13f, crater);
            p.FillCircle(mx - 15f, my - 14f, 9f, crater);
            p.FillCircle(mx + 2f, my - 24f, 6f, crater);
            return p;
        }

        /// <summary>
        /// The scrolling strip. Everything that sticks out near an edge is drawn three times
        /// (x, x-w, x+w) so the left and right seams match — off-canvas copies are clipped away
        /// for free by the painter, so this costs nothing for elements in the middle.
        /// </summary>
        private static Painter PaintGround(int worldIndex, World def)
        {
            int w = Mathf.RoundToInt(BgWorldWidth * Ppu);
            int h = GroundStripHeight;
            float groundTop = (BgHorizonY - BgBottomY) * Ppu;
            var rng = new System.Random(worldIndex * 7919 + 13);
            var p = new Painter(w, h);
            float cx = w * 0.5f;

            // distant rocks (behind the ground line)
            for (int r = -1; r <= 1; r++)
            {
                RockCluster(p, def.Rock, 110f + r * w, groundTop + 10f, 2.9f);
                RockCluster(p, def.Rock, w - 120f + r * w, groundTop + 4f, 3.3f);
            }

            // ground — flat bands, seamless by construction
            p.FillRect(0f, 0f, w, groundTop, def.Ground);
            p.FillRect(0f, 0f, w, groundTop - 380f, Color.Lerp(def.Ground, Color.black, 0.25f));

            // glowing lake, centered in the strip so it never crosses a seam
            float ly = groundTop - 52f;
            p.Glow(cx, ly, 300f, def.Lake, 0.25f);
            p.FillEllipseShaded(cx, ly, 430f, 100f, def.Lake, def.LakeDeep);
            p.OutlineEllipse(cx, ly, 430f, 100f, 3f, new Color(1f, 1f, 1f, 0.25f));
            // reflection streaks
            for (int i = 0; i < 5; i++)
            {
                float rx = cx + Rand(rng, -320f, 320f);
                p.FillEllipse(rx, ly + Rand(rng, -40f, 40f), Rand(rng, 22f, 60f), 3f,
                    new Color(1f, 1f, 1f, 0.16f));
            }

            // bio-luminescent flora on both flanks
            for (int f = 0; f < 8; f++)
            {
                float fx = f < 4 ? Rand(rng, 30f, 260f) : w - Rand(rng, 30f, 260f);
                float fy = Rand(rng, groundTop - 175f, groundTop - 6f);
                Color fc = def.Flora[f % def.Flora.Length];
                float s = Rand(rng, 0.8f, 1.7f);
                bool mushroom = rng.NextDouble() < 0.5;
                for (int r = -1; r <= 1; r++)
                {
                    if (mushroom) Mushroom(p, fx + r * w, fy, fc, s);
                    else CrystalGrass(p, fx + r * w, fy, fc, s);
                }
            }
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

        private static void Mushroom(Painter p, float x, float y, Color color, float s)
        {
            p.Glow(x, y + 14f * s, 30f * s, color, 0.55f);
            p.FillRoundRect(x - 4f * s, y - 8f * s, 8f * s, 20f * s, 3f * s,
                Color.Lerp(color, Color.white, 0.35f));
            p.FillEllipse(x, y + 14f * s, 17f * s, 11f * s, color);
            p.FillCircle(x - 5f * s, y + 17f * s, 2.6f * s, new Color(1f, 1f, 1f, 0.5f));
            p.FillCircle(x + 6f * s, y + 13f * s, 2f * s, new Color(1f, 1f, 1f, 0.5f));
        }

        private static void CrystalGrass(Painter p, float x, float y, Color color, float s)
        {
            p.Glow(x, y + 10f * s, 26f * s, color, 0.45f);
            for (int k = -1; k <= 1; k++)
            {
                float bx = x + k * 7f * s;
                float height = (22f - Mathf.Abs(k) * 6f) * s;
                p.FillTriangle(new Vector2(bx - 3.5f * s, y - 6f * s),
                    new Vector2(bx, y + height),
                    new Vector2(bx + 3.5f * s, y - 6f * s),
                    Color.Lerp(color, Color.white, 0.15f));
            }
        }
    }
}
