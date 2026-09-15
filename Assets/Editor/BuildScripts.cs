using UnityEditor;
using UnityEngine;

namespace ShatterlineEditor
{
    /// <summary>
    /// Real player builds (not just a compile check), for the MVP checklist's
    /// "builds and runs on Windows + Android" item. -buildWindows64Player and
    /// -buildAndroidPlayer aren't both reliable CLI flags on this Unity
    /// version, so both go through BuildPipeline.BuildPlayer explicitly.
    /// </summary>
    public static class BuildScripts
    {
        static readonly string[] Scenes = { "Assets/Scenes/Game.unity" };

        [MenuItem("Tools/Shatterline/Build Windows Player")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "Builds/Windows/Shatterline.exe",
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"SHATTERLINE Windows build: {summary.result}, size={summary.totalSize} bytes, " +
                $"errors={summary.totalErrors}, warnings={summary.totalWarnings}, time={summary.totalTime}");
        }

        [MenuItem("Tools/Shatterline/Build Android Player")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "Builds/Android/Shatterline.apk",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"SHATTERLINE Android build: {summary.result}, size={summary.totalSize} bytes, " +
                $"errors={summary.totalErrors}, warnings={summary.totalWarnings}, time={summary.totalTime}");
        }
    }
}
