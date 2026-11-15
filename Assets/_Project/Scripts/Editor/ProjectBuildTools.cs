#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpaceMissionGame.Editor
{
    /// <summary>
    /// Editor menu and batchmode CLI helpers to configure MainGame.unity and export
    /// static-host-ready WebGL builds (localhost / GitHub Pages / itch.io) and Windows 64-bit builds.
    /// </summary>
    public static class ProjectBuildTools
    {
        public const string MainScenePath = "Assets/_Project/Scenes/MainGame.unity";
        public const string WebGLOutputDir = "Builds/WebGL";
        public const string WindowsOutputExe = "Builds/Windows/SpaceMissionDesignGame.exe";

        [MenuItem("Space Mission Build/1. Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            ApplyBuildConfiguration();
            Debug.Log("[ProjectBuildTools] Build settings configured: MainGame.unity enabled at index 0, WebGL Gzip + decompressionFallback enabled.");
        }

        [MenuItem("Space Mission Build/2. Build WebGL (localhost & Web)")]
        public static void BuildWebGL()
        {
            bool ok = RunBuild(BuildTarget.WebGL, WebGLOutputDir);
            HandleBatchmodeResult(ok);
        }

        [MenuItem("Space Mission Build/3. Build Windows (.exe)")]
        public static void BuildWindows()
        {
            bool ok = RunBuild(BuildTarget.StandaloneWindows64, WindowsOutputExe);
            HandleBatchmodeResult(ok);
        }

        [MenuItem("Space Mission Build/4. Build Both (Windows + WebGL)")]
        public static void BuildAll()
        {
            bool winOk = RunBuild(BuildTarget.StandaloneWindows64, WindowsOutputExe);
            bool webOk = winOk && RunBuild(BuildTarget.WebGL, WebGLOutputDir);
            HandleBatchmodeResult(winOk && webOk);
        }

        public static void ApplyBuildConfiguration()
        {
            if (!File.Exists(MainScenePath))
            {
                throw new FileNotFoundException($"Required main scene not found at '{MainScenePath}'.");
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            // Decompression fallback allows WebGL builds to run cleanly on localhost static servers,
            // GitHub Pages, and itch.io without requiring custom server Content-Encoding headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            AssetDatabase.SaveAssets();
        }

        private static bool RunBuild(BuildTarget target, string locationPathName)
        {
            try
            {
                ApplyBuildConfiguration();

                string fullLocation = Path.GetFullPath(locationPathName);
                string dirToCreate = target == BuildTarget.WebGL
                    ? fullLocation
                    : Path.GetDirectoryName(fullLocation);

                if (!string.IsNullOrEmpty(dirToCreate))
                {
                    Directory.CreateDirectory(dirToCreate);
                }

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { MainScenePath },
                    locationPathName = locationPathName,
                    target = target,
                    targetGroup = target == BuildTarget.WebGL
                        ? BuildTargetGroup.WebGL
                        : BuildTargetGroup.Standalone,
                    options = BuildOptions.None
                };

                Debug.Log($"[ProjectBuildTools] Starting {target} build -> {locationPathName}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"[ProjectBuildTools] {target} build SUCCEEDED: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s -> {summary.outputPath}");
                    return true;
                }

                Debug.LogError($"[ProjectBuildTools] {target} build FAILED ({summary.result}) with {summary.totalErrors} error(s) and {summary.totalWarnings} warning(s).");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private static void HandleBatchmodeResult(bool succeeded)
        {
            if (Application.isBatchMode && !succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
