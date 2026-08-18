using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NAK.CleanPlates.Build
{
    public static class BundleBuilder
    {
        private const string Bundle = "cleanplates.assets";

        [MenuItem("NAK/Build CleanPlates Bundle")]
        public static void BuildMenu() => Build(null);
        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-bundleOut");
            string outPath = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;

            bool ok = Build(outPath);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Build(string copyTo)
        {
            string[] assets = AssetDatabase.GetAssetPathsFromAssetBundle(Bundle);
            if (assets.Length == 0)
            {
                Debug.LogError($"No assets assigned to bundle {Bundle}.");
                return false;
            }

            const string outputDir = "Temp/AssetBundles";
            Directory.CreateDirectory(outputDir);

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                outputDir,
                new[] { new AssetBundleBuild { assetBundleName = Bundle, assetNames = assets } },
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                Debug.LogError("Bundle build failed.");
                return false;
            }

            Debug.Log($"Built {Bundle} from {assets.Length} assets.");
            foreach (string asset in assets) Debug.Log($"  {asset}");
            if (string.IsNullOrEmpty(copyTo)) return true;

            Directory.CreateDirectory(Path.GetDirectoryName(copyTo));
            File.Copy(Path.Combine(outputDir, Bundle), copyTo, true);
            Debug.Log($"Copied bundle to {copyTo}");
            return true;
        }
    }
}