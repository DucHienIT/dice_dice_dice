using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.EditorTools
{
    /// <summary>
    /// Declarative SpriteAtlas config (Sprite Atlas V2): two folder-driven atlases so every
    /// sprite the scene ships batches from a handful of pages instead of ~100 loose textures.
    /// Run by the game builder and re-runnable from the menu — the .spriteatlasv2 assets are
    /// rewritten from scratch on every sync, so never edit them by hand.
    ///
    /// Partition policy mirrors Addressables: the streamed world backdrop (and the authored
    /// sheets under Art/Immortal, which are already packed textures) must NOT be atlased —
    /// packing the backdrop would ship it twice, once in the atlas and once in its bundle.
    /// Only the two always-shipped sprite pools are packed:
    ///   Atlas_UI       — Textures/ (Layer Lab chrome: buttons, frames, icons, runes, glow),
    ///                    compressed like its sources.
    ///   Atlas_Gameplay — Art/Generated/ (baked procedural sprites), uncompressed so the
    ///                    crisp baked pixels stay exact, matching SpriteBaker's importers.
    /// </summary>
    public static class AtlasConfigurator
    {
        private const string AtlasDir = "Assets/_OneTapImmortal/Atlases";
        private const string UiAtlasPath = AtlasDir + "/Atlas_UI.spriteatlasv2";
        private const string GameplayAtlasPath = AtlasDir + "/Atlas_Gameplay.spriteatlasv2";
        private const string UiSourceDir = "Assets/_OneTapImmortal/Textures";
        private const string GameplaySourceDir = "Assets/_OneTapImmortal/Art/Generated";

        [MenuItem("Tools/Game/Atlases/Sync")]
        public static void SyncMenu()
        {
            Sync();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Rebuilds both atlas assets from their source folders and packs them.</summary>
        public static void Sync()
        {
            if (!AssetDatabase.IsValidFolder(AtlasDir))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(AtlasDir).Replace('\\', '/'),
                    Path.GetFileName(AtlasDir));
            }

            SyncAtlas(UiAtlasPath, UiSourceDir, TextureImporterCompression.Compressed);
            SyncAtlas(GameplayAtlasPath, GameplaySourceDir,
                TextureImporterCompression.Uncompressed);

            // pack now so a broken setup surfaces in the editor, not in a player build
            var atlases = new[]
            {
                AssetDatabase.LoadAssetAtPath<SpriteAtlas>(UiAtlasPath),
                AssetDatabase.LoadAssetAtPath<SpriteAtlas>(GameplayAtlasPath),
            };
            SpriteAtlasUtility.PackAtlases(atlases, EditorUserBuildSettings.activeBuildTarget);
            for (int i = 0; i < atlases.Length; i++)
            {
                if (atlases[i] == null) { Debug.LogError("[Atlas] Missing atlas after sync"); continue; }
                Debug.Log("[Atlas] " + atlases[i].name + ": " + atlases[i].spriteCount + " sprites");
            }
        }

        private static void SyncAtlas(string path, string sourceDir,
            TextureImporterCompression compression)
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(sourceDir);
            if (folder == null)
            {
                Debug.LogError("[Atlas] Source folder missing: " + sourceDir);
                return;
            }
            NormalizeSourceImporters(sourceDir);

            // rewrite the asset from scratch — the folder packable is the whole config,
            // so a stale hand-edit can never survive a sync
            var asset = new SpriteAtlasAsset();
            asset.Add(new[] { folder });
            SpriteAtlasAsset.Save(asset, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[Atlas] No SpriteAtlasImporter at " + path);
                return;
            }
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                padding = 4,
                blockOffset = 1,
                enableRotation = false,   // rotated UI sprites render wrong in uGUI
                enableTightPacking = false, // 9-sliced chrome needs full rects
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                generateMipMaps = false,
                filterMode = FilterMode.Bilinear,
                sRGB = true,
                anisoLevel = 1,
            };
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = 2048,
                format = TextureImporterFormat.Automatic,
                textureCompression = compression,
            });
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Atlased source textures must stay uncompressed: only the atlas page ships, and
        /// packing an already-compressed source would bake its artifacts into the page
        /// (Unity warns about exactly this). The atlas platform settings own compression.
        /// </summary>
        private static void NormalizeSourceImporters(string sourceDir)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceDir });
            for (int i = 0; i < guids.Length; i++)
            {
                string texPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var texImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (texImporter == null ||
                    texImporter.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    continue;
                }
                texImporter.textureCompression = TextureImporterCompression.Uncompressed;
                texImporter.SaveAndReimport();
            }
        }
    }
}
