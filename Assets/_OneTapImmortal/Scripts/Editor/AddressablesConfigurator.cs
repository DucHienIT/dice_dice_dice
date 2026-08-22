using System.Collections.Generic;
using Game.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Declarative Addressables config: the desired entry set is computed from the content
    /// assets and the groups are made to match — run by the game builder and the WebGL
    /// builder, never edited by hand in the Groups window (hand-added entries are removed
    /// on the next sync).
    ///
    /// Partition policy: Main.unity is always loaded (scenes themselves are never
    /// addressable in this project), so anything the scene HARD-references ships with
    /// the player and must NOT also be addressable — that would duplicate it (once in
    /// the scene data, once in a bundle). Streamed content is instead weak-referenced
    /// from the scene via AssetReference (per CODE_RULES' streaming exception), which
    /// leaves the pixels out of the player payload. ValidateNoSceneDuplication enforces
    /// the split.
    ///
    /// Streamed content is organized in categories, one group per category and one
    /// Add* method in DesiredEntries per category — adding a category means one method
    /// here plus a runtime loader (see Utils/StreamedAsset for the handle lifecycle).
    /// Categories: the per-world sky backdrop (one bundle per world via its label,
    /// streamed by WorldBackgroundRenderer.Enter) and the authored hero pose sheets
    /// (one HeroArt bundle, streamed by HeroView at Awake).
    /// </summary>
    public static class AddressablesConfigurator
    {
        private const string WorldGroupName = "WorldBackdrops";
        private const string HeroArtGroupName = "HeroArt";
        private const string HeroArtLabel = "Hero";
        private const string ScenePath = "Assets/_OneTapImmortal/Scenes/Main.unity";
        private const string GameConfigPath = "Assets/_OneTapImmortal/Data/GameConfig.asset";

        private struct DesiredEntry
        {
            public string Address;
            public string Label;
            public string Group;
        }

        [MenuItem("Tools/Game/Addressables/Sync Groups")]
        public static void SyncMenu()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config == null)
            {
                Debug.LogError("[Addressables] " + GameConfigPath + " not found — run " +
                               "Tools > Game > Build Game (Full) first");
                return;
            }
            if (Sync(config)) AssetDatabase.SaveAssets();
        }

        /// <summary>Makes groups, addresses and labels match the content assets. Returns
        /// false when the setup is unusable (no settings, missing refs, scene duplication).</summary>
        public static bool Sync(GameConfig config)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[Addressables] No AddressableAssetSettings — create them via " +
                               "Window > Asset Management > Addressables > Groups");
                return false;
            }

            ConfigureDataBuilders(settings);
            Dictionary<string, DesiredEntry> desired = DesiredEntries(config);
            RemoveStrayEntries(settings, desired);

            foreach (KeyValuePair<string, DesiredEntry> pair in desired)
            {
                AddressableAssetGroup group = EnsureGroup(settings, pair.Value.Group);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(pair.Key, group);
                entry.SetAddress(pair.Value.Address);
                SyncLabels(entry, pair.Value.Label);
            }
            RemoveEmptyStrayGroups(settings, desired);

            bool clean = ValidateNoSceneDuplication(desired);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification,
                null, true, true);
            Debug.Log("[Addressables] Synced " + desired.Count + " entr" +
                      (desired.Count == 1 ? "y" : "ies") +
                      (clean ? "" : " — WITH ERRORS, see above"));
            return clean;
        }

        /// <summary>The full desired entry set — one Add* call per streamed category.</summary>
        private static Dictionary<string, DesiredEntry> DesiredEntries(GameConfig config)
        {
            var desired = new Dictionary<string, DesiredEntry>(config.Worlds.Length + 3);
            AddWorldBackdrops(config, desired);
            AddHeroArt(desired);
            return desired;
        }

        /// <summary>The three authored hero pose sheets, one shared label so they pack
        /// into a single HeroArt bundle fetched once at boot (weak-referenced by
        /// HeroView, wired by the builder). Absent sheets mean procedural fallback art
        /// is wired hard instead — then there is nothing to stream.</summary>
        private static void AddHeroArt(Dictionary<string, DesiredEntry> desired)
        {
            if (!SpriteBaker.AuthoredHeroArtComplete) return;

            AddSheet(desired, SpriteBaker.CultivatorRunSheet, "Hero/run");
            AddSheet(desired, SpriteBaker.CultivatorPoseSheet, "Hero/poses");
            AddSheet(desired, SpriteBaker.CultivatorRangedPoseSheet, "Hero/ranged");
        }

        private static void AddSheet(Dictionary<string, DesiredEntry> desired,
            string path, string address)
        {
            desired[AssetDatabase.AssetPathToGUID(path)] = new DesiredEntry
            {
                Address = address,
                Label = HeroArtLabel,
                Group = HeroArtGroupName,
            };
        }

        /// <summary>One entry per world: the sky sprite's texture, addressed
        /// "World_X/sky" and labeled "World_X" so PackTogetherByLabel makes one
        /// bundle per world.</summary>
        private static void AddWorldBackdrops(GameConfig config,
            Dictionary<string, DesiredEntry> desired)
        {
            for (int i = 0; i < config.Worlds.Length; i++)
            {
                World world = config.Worlds[i];
                if (world == null || world.SkyLayerRef == null ||
                    !world.SkyLayerRef.RuntimeKeyIsValid())
                {
                    Debug.LogError("[Addressables] World " + i + " has no valid sky " +
                                   "reference — run Tools > Game > Build Game (Full)");
                    continue;
                }
                desired[world.SkyLayerRef.AssetGUID] = new DesiredEntry
                {
                    Address = world.name + "/sky",
                    Label = world.name,
                    Group = WorldGroupName,
                };
            }
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings,
            string name)
        {
            AddressableAssetGroup group = settings.FindGroup(name);
            if (group == null)
            {
                group = settings.CreateGroup(name, false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            // LZ4: the only chunk-decompressed option WebGL handles well (LZMA is not supported)
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
            // local bundles ship inside the build — a CRC pass on load would only cost time
            schema.UseAssetBundleCrc = false;
            EditorUtility.SetDirty(schema);
            return group;
        }

        /// <summary>Anything not in the desired set — hand-dragged entries included — is
        /// removed, so the Groups window always mirrors this file.</summary>
        private static void RemoveStrayEntries(AddressableAssetSettings settings,
            Dictionary<string, DesiredEntry> desired)
        {
            var strays = new List<string>();
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null || group.ReadOnly) continue;
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (!desired.ContainsKey(entry.guid)) strays.Add(entry.guid);
                }
            }
            for (int i = 0; i < strays.Count; i++)
            {
                settings.RemoveAssetEntry(strays[i]);
            }
            if (strays.Count > 0)
            {
                Debug.Log("[Addressables] Removed " + strays.Count +
                          " stray entr" + (strays.Count == 1 ? "y" : "ies") +
                          " not owned by AddressablesConfigurator");
            }
        }

        /// <summary>A renamed or deleted category leaves its old group behind, empty —
        /// remove it so the Groups window mirrors the categories exactly. The default
        /// group and read-only groups (Built In Data) are Addressables-owned and stay.</summary>
        private static void RemoveEmptyStrayGroups(AddressableAssetSettings settings,
            Dictionary<string, DesiredEntry> desired)
        {
            var desiredGroups = new HashSet<string>();
            foreach (KeyValuePair<string, DesiredEntry> pair in desired)
            {
                desiredGroups.Add(pair.Value.Group);
            }

            var stale = new List<AddressableAssetGroup>();
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null || group.ReadOnly || group == settings.DefaultGroup) continue;
                if (desiredGroups.Contains(group.Name) || group.entries.Count > 0) continue;
                stale.Add(group);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                Debug.Log("[Addressables] Removed empty stray group '" + stale[i].Name + "'");
                settings.RemoveGroup(stale[i]);
            }
        }

        private static void SyncLabels(AddressableAssetEntry entry, string label)
        {
            var stale = new List<string>();
            foreach (string existing in entry.labels)
            {
                if (existing != label) stale.Add(existing);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                entry.SetLabel(stale[i], false);
            }
            entry.SetLabel(label, true, true);
        }

        /// <summary>Play mode resolves through the AssetDatabase (no content build needed in
        /// the editor); player builds use the packed builder.</summary>
        private static void ConfigureDataBuilders(AddressableAssetSettings settings)
        {
            for (int i = 0; i < settings.DataBuilders.Count; i++)
            {
                if (settings.DataBuilders[i] is BuildScriptFastMode)
                {
                    settings.ActivePlayModeDataBuilderIndex = i;
                }
                else if (settings.DataBuilders[i] is BuildScriptPackedMode)
                {
                    settings.ActivePlayerDataBuilderIndex = i;
                }
            }
        }

        /// <summary>The policy guard: an addressable asset that is also a dependency of the
        /// always-loaded scene would ship twice. Fails loudly, naming the offender.</summary>
        private static bool ValidateNoSceneDuplication(Dictionary<string, DesiredEntry> desired)
        {
            bool clean = true;
            string[] dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string guid = AssetDatabase.AssetPathToGUID(dependencies[i]);
                if (!desired.ContainsKey(guid)) continue;
                Debug.LogError("[Addressables] " + dependencies[i] + " is addressable AND a " +
                               "direct dependency of " + ScenePath + " — it would ship twice. " +
                               "Remove the scene reference (or the entry).");
                clean = false;
            }
            return clean;
        }
    }
}
