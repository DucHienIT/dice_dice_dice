using System.Collections.Generic;
using System.IO;
using System.Text;
using I2.Loc;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// The CSV at <see cref="CsvPath"/> is the single source of truth for every player-facing
    /// string; Assets/Resources/I2Languages.asset is a generated cache of it. Import rewrites
    /// the asset from the CSV (Replace mode — terms deleted in the CSV disappear), Export
    /// dumps the asset back out for when someone edits terms in the I2 window instead.
    /// Import also runs as the first step of Tools ▸ Game ▸ Build Game (Full).
    /// </summary>
    public static class LocalizationImporter
    {
        public const string CsvPath = "Assets/_OneTapImmortal/Localization/Localization.csv";
        private const string SourcePath = "Assets/_OneTapImmortal/Resources/I2Languages.asset";
        private const char Separator = ',';

        [MenuItem("Tools/Game/Localization/Import CSV %#l")]
        public static void ImportMenu()
        {
            if (Import()) AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Game/Localization/Export CSV")]
        public static void ExportMenu()
        {
            LanguageSourceAsset source = LoadSource();
            if (source == null) return;
            string csv = source.SourceData.Export_CSV(null, Separator);
            File.WriteAllText(CsvPath, csv, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("[Localization] Exported " + source.SourceData.mTerms.Count + " terms → " + CsvPath);
        }

        /// <summary>Rebuilds I2Languages.asset from the CSV. Returns false on any failure.</summary>
        public static bool Import()
        {
            LanguageSourceAsset source = LoadSource();
            if (source == null) return false;
            if (!File.Exists(CsvPath))
            {
                Debug.LogError("[Localization] CSV not found: " + CsvPath);
                return false;
            }

            string csv = File.ReadAllText(CsvPath, Encoding.UTF8);
            string error = source.SourceData.Import_CSV(string.Empty, csv,
                eSpreadsheetUpdateMode.Replace, Separator);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("[Localization] CSV import failed: " + error);
                return false;
            }

            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssets();
            LocalizationManager.LocalizeAll(true);

            LanguageSourceData data = source.SourceData;
            Debug.Log("[Localization] Imported " + data.mTerms.Count + " terms, " +
                      data.mLanguages.Count + " languages from " + CsvPath);
            WarnAboutGaps(data);
            return true;
        }

        /// <summary>Missing cells silently fall back to English at runtime — surface them.</summary>
        private static void WarnAboutGaps(LanguageSourceData data)
        {
            for (int lang = 0; lang < data.mLanguages.Count; lang++)
            {
                var missing = new List<string>();
                foreach (TermData term in data.mTerms)
                {
                    if (lang >= term.Languages.Length || string.IsNullOrEmpty(term.Languages[lang]))
                    {
                        missing.Add(term.Term);
                    }
                }
                if (missing.Count == 0) continue;
                Debug.LogWarning("[Localization] " + data.mLanguages[lang].Name + " is missing " +
                                 missing.Count + " translation(s): " +
                                 string.Join(", ", missing.ToArray(), 0, Mathf.Min(10, missing.Count)) +
                                 (missing.Count > 10 ? ", ..." : string.Empty));
            }
        }

        private static LanguageSourceAsset LoadSource()
        {
            var source = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(SourcePath);
            if (source == null)
            {
                Debug.LogError("[Localization] " + SourcePath +
                               " not found — create it via Tools ▸ I2 Localization ▸ Open the I2 window.");
            }
            return source;
        }
    }
}
