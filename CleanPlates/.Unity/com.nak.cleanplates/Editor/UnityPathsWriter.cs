using System.IO;
using UnityEditor;
using UnityEngine;

namespace NAK.CleanPlates.Build
{
    [InitializeOnLoad]
    public static class UnityPathsWriter
    {
        private const string PackageRoot = "Packages/com.nak.cleanplates";
        private const string PropsName = "UnityPaths.props";

        static UnityPathsWriter() => Write();

        [MenuItem("NAK/Write Unity Paths")]
        private static void WriteMenu()
        {
            if (Write()) Debug.Log($"Wrote {PropsName}.");
        }

        private static bool Write()
        {
            string packageDir = Path.GetFullPath(PackageRoot);
            if (!Directory.Exists(packageDir)) return false;

            DirectoryInfo modDir = Directory.GetParent(packageDir)?.Parent;
            if (modDir == null || !modDir.Exists) return false;

            string editor = EditorApplication.applicationPath.Replace('/', '\\');
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).TrimEnd('\\', '/');
            string path = Path.Combine(modDir.FullName, PropsName);

            string contents =
                "<Project>\n" +
                "  <PropertyGroup>\n" +
                $"    <UnityExe>{editor}</UnityExe>\n" +
                $"    <UnityProject>{project}</UnityProject>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n";

            if (File.Exists(path) && File.ReadAllText(path) == contents) return false;

            File.WriteAllText(path, contents);
            return true;
        }
    }
}