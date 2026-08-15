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
    /// Partition policy: Main.unity is always loaded, so anything it references ships with
    /// the player and must NOT be addressable — marking it would duplicate it (once in the
    /// scene data, once in a bundle). Only content that is mutually exclusive at runtime
    /// with a known swap moment goes into bundles; today that is the per-world sky backdrop
    /// (one bundle per world, streamed by WorldBackgroundRenderer.Enter, previous world
    /// released after the swap). ValidateNoSceneDuplication enforces the policy.
    /// </summary>
    public static class AddressablesConfigurator
    {
        private const string WorldGroupName = "WorldBackdrops";
        private const string ScenePath = "Assets/_OneTapImmortal/Scenes/Main.unity";
        private const string GameConfigPath = "Assets/_OneTapImmortal/Data/GameConfig.asset";

        private struct DesiredEntry
        {
            public string Address;
            public string Label;
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
            Dictionary<string, DesiredEntry> desired = DesiredWorldEntries(config);
            AddressableAssetGroup worldGroup = EnsureWorldGroup(settings);
            RemoveStrayEntries(settings, desired);

            foreach (KeyValuePair<string, DesiredEntry> pair in desired)
            {
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(pair.Key, worldGroup);
                entry.SetAddress(pair.Value.Address);
                SyncLabels(entry, pair.Value.Label);
            }

            bool clean = ValidateNoSceneDuplication(desired);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification,
                null, true, true);
            Debug.Log("[Addressables] Synced " + desired.Count + " entries into '" +
                      WorldGroupName + "'" + (clean ? "" : " — WITH ERRORS, see above"));
            return clean;
        }

        /// <summary>One entry per world: the sky sprite's texture, addressed
        /// "World_X/sky" and labeled "World_X" so PackTogetherByLabel makes one
        /// bundle per world.</summary>
        private static Dictionary<string, DesiredEntry> DesiredWorldEntries(GameConfig config)
        {
            var desired = new Dictionary<string, DesiredEntry>(config.Worlds.Length);
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
                };
            }
            return desired;
        }

        private static AddressableAssetGroup EnsureWorldGroup(AddressableAssetSettings settings)
        {
            AddressableAssetGroup group = settings.FindGroup(WorldGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(WorldGroupName, false, false, false, null,
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
