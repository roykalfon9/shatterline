using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShatterlineEditor
{
    /// <summary>Build the saved submission scene and retain a machine-readable result.</summary>
    public static class BuildScripts
    {
        [MenuItem("Tools/Shatterline/Build Windows Player")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64,
            BuildTargetGroup.Standalone, "Builds/Windows/Shatterline.exe");

        [MenuItem("Tools/Shatterline/Build Android Player")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, BuildTargetGroup.Android, "Builds/Android/Shatterline.apk");
        }

        // Supplemental local verification; does not certify the Windows/Android targets.
        [MenuItem("Tools/Shatterline/Build Mac Test Player")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX,
            BuildTargetGroup.Standalone, "Builds/Mac/Shatterline.app");

        static void Build(BuildTarget target, BuildTargetGroup group, string output)
        {
            if (EditorApplication.isPlaying)
                throw new BuildFailedException("Stop Play Mode before building.");
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                throw new BuildFailedException($"Install {target} build support for Unity {Application.unityVersion} first.");

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Game.unity" },
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            });
            var summary = report.summary;
            var receipt = new BuildReceipt
            {
                utc = DateTime.UtcNow.ToString("o"),
                unity = Application.unityVersion,
                target = target.ToString(),
                output = output,
                result = summary.result.ToString(),
                errors = summary.totalErrors,
                warnings = summary.totalWarnings,
                bytes = summary.totalSize,
                seconds = summary.totalTime.TotalSeconds,
                artifactExists = target == BuildTarget.StandaloneOSX ? Directory.Exists(output) : File.Exists(output)
            };
            Directory.CreateDirectory("Logs");
            File.WriteAllText($"Logs/Build-{target}.json", JsonUtility.ToJson(receipt, true));
            Debug.Log($"SHATTERLINE {target} build: {summary.result}, errors={summary.totalErrors}, output={output}");
            if (summary.result != BuildResult.Succeeded || summary.totalErrors > 0 || !receipt.artifactExists)
                throw new BuildFailedException($"{target} build failed. Inspect the Console and Logs/Build-{target}.json.");
        }

        [Serializable]
        class BuildReceipt
        {
            public string utc, unity, target, output, result;
            public int errors, warnings;
            public ulong bytes;
            public double seconds;
            public bool artifactExists;
        }
    }
}
