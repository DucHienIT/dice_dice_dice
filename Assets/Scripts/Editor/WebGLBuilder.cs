using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// One menu item = one correct build. Every PlayerSetting that changes the output is
    /// WRITTEN here on each run, never read — those settings are global state editable from
    /// half a dozen places, so the tool owns them rather than hoping they were left right.
    /// Settings owned: WebGL compression + decompression fallback + data caching, WebGL
    /// template, default web canvas size, interface orientation / autorotate, product name.
    /// </summary>
    public static class WebGLBuilder
    {
        private const string OutputRoot = "Builds/WebGL";
        private const string ReleaseDirectory = OutputRoot + "/Release";
        private const string DevelopmentDirectory = OutputRoot + "/Development";
        private const string FallbackScenePath = "Assets/Scenes/Main.unity";

        private const string TemplateName = "PortraitGame";
        private const string TemplateDirectory = "Assets/WebGLTemplates/" + TemplateName;

        // Product name flows into the browser tab, the loading screen and — the reason it
        // must be settled before the first deploy — the WebGL save path.
        private const string ProductName = "One Tap Immortal";
        private const string CompanyName = "Imba";
        private const int PortraitWidth = 1080;
        private const int PortraitHeight = 1920;

        [MenuItem("Tools/Game/Build WebGL (Release)", false, 100)]
        public static void BuildRelease() =>
            Build(ReleaseDirectory, BuildOptions.None, WebGLCompressionFormat.Brotli, true);

        [MenuItem("Tools/Game/Build WebGL (Development)", false, 101)]
        public static void BuildDevelopment() =>
            Build(DevelopmentDirectory, BuildOptions.Development,
                WebGLCompressionFormat.Disabled, false);

        [MenuItem("Tools/Game/Build And Run WebGL (Development)", false, 102)]
        public static void BuildAndRunDevelopment() =>
            Build(DevelopmentDirectory, BuildOptions.Development | BuildOptions.AutoRunPlayer,
                WebGLCompressionFormat.Disabled, false);

        [MenuItem("Tools/Game/Apply Portrait Presentation", false, 120)]
        public static void ApplyPresentationMenu()
        {
            ApplyPresentation();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Build] Portrait presentation applied — template '{TemplateName}', " +
                      $"canvas {PortraitWidth}x{PortraitHeight}, product '{ProductName}'.");
        }

        [MenuItem("Tools/Game/Open WebGL Build Folder", false, 140)]
        public static void OpenBuildFolder() =>
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputRoot) + Path.DirectorySeparatorChar);

        [MenuItem("Tools/Game/Open WebGL Build Folder", true)]
        public static bool CanOpenBuildFolder() => Directory.Exists(OutputRoot);

        // ---------------------------------------------------------------- build

        private static void Build(string directory, BuildOptions options,
            WebGLCompressionFormat compression, bool decompressionFallback)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(ProductName,
                    "Exit Play Mode before building.", "OK");
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog(ProductName,
                    "WebGL Build Support is not installed for Unity " +
                    Application.unityVersion + ".\nAdd the module in Unity Hub, then retry.",
                    "OK");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL,
                    BuildTarget.WebGL))
            {
                Debug.LogError("[Build] Could not switch the active build target to WebGL.");
                return;
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogWarning("[Build] No scenes enabled in Build Settings — falling back to " +
                                 FallbackScenePath);
                scenes = new[] { FallbackScenePath };
            }

            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = decompressionFallback;
            PlayerSettings.WebGL.dataCaching = true;
            ApplyPresentation();
            AssetDatabase.SaveAssets();

            Directory.CreateDirectory(directory);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = directory,
                target = BuildTarget.WebGL,
                options = options,
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Build] WebGL build {summary.result} after " +
                               $"{summary.totalTime.TotalSeconds:F0}s — {summary.totalErrors} error(s).");
                return;
            }

            Debug.Log(BuildLog(directory, summary, compression, decompressionFallback));
            string index = Path.Combine(directory, "index.html");
            if (File.Exists(index)) EditorUtility.RevealInFinder(Path.GetFullPath(index));
        }

        /// <summary>
        /// Three layers have to agree for the aspect lock to survive any host: the WebGL
        /// page sizes the canvas element, the native player settings cover the mobile
        /// build, and ScreenLockView pillarboxes in-game for hosts that ignore both
        /// (editor Game view, iframes, a default template served by mistake).
        /// </summary>
        private static void ApplyPresentation()
        {
            if (Directory.Exists(TemplateDirectory))
            {
                PlayerSettings.WebGL.template = "PROJECT:" + TemplateName;
            }
            else
            {
                // an invalid template string does not error when set, only at build time
                Debug.LogWarning($"[Build] {TemplateDirectory} is missing — the build will NOT " +
                                 "lock its aspect ratio in the browser.");
            }

            // companyName + productName derive the WebGL save path — both are owned here
            // so a stray Player Settings edit can never orphan players' saves.
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultWebScreenWidth = PortraitWidth;
            PlayerSettings.defaultWebScreenHeight = PortraitHeight;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        /// <summary>
        /// Total size answers nothing; the per-file payload table is what explains a slow
        /// first load, so it goes in the log every time.
        /// </summary>
        private static string BuildLog(string directory, BuildSummary summary,
            WebGLCompressionFormat compression, bool decompressionFallback)
        {
            var sb = new StringBuilder(512);
            sb.Append("[Build] WebGL ").Append(summary.options.HasFlag(BuildOptions.Development)
                ? "Development" : "Release");
            sb.Append(" succeeded in ").Append(summary.totalTime.TotalSeconds.ToString("F0"))
                .Append("s — ").Append(Mb(summary.totalSize)).AppendLine(" total");
            sb.Append("  output      : ").AppendLine(Path.GetFullPath(directory));
            sb.Append("  template    : ").AppendLine(PlayerSettings.WebGL.template);
            sb.Append("  canvas      : ").Append(PortraitWidth).Append('x')
                .AppendLine(PortraitHeight.ToString());
            sb.Append("  compression : ").Append(compression)
                .Append(decompressionFallback ? " (+ decompression fallback)" : "").AppendLine();
            sb.AppendLine("  payload:");

            string buildDir = Path.Combine(directory, "Build");
            if (!Directory.Exists(buildDir))
            {
                sb.AppendLine("    (no Build folder found)");
                return sb.ToString();
            }

            foreach (FileInfo f in new DirectoryInfo(buildDir).GetFiles()
                         .OrderByDescending(f => f.Length))
            {
                sb.Append("    ").Append(Mb((ulong)f.Length).PadLeft(10)).Append("  ")
                    .AppendLine(f.Name);
            }
            return sb.ToString();
        }

        private static string Mb(ulong bytes) =>
            (bytes / (1024f * 1024f)).ToString("F2") + " MB";
    }
}
