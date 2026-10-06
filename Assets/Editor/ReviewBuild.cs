using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace PokeIdle.Editor
{
    public static class ReviewBuild
    {
        [MenuItem("PokeIdle/Build Windows Review")]
        public static void Build()
        {
            MilestoneValidation.Run();
            string path = Path.GetFullPath("Builds/Review/PokeIdle.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var options = new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = path, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            UnityEngine.Debug.Log("REVIEW BUILD PASSED: " + path);
        }
        public static void RunBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
