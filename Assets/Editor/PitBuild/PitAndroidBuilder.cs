using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Batch entry point that produces a Quest-ready APK.</summary>
public static class PitAndroidBuilder
{
    private const string ApkPath = "Builds/ThePit.apk";

    public static void BuildApk()
    {
        try
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.tus.devilsroulette.thepit");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Quest 2 and 3 run Android 12L (API 32) on current Horizon OS, so keep the floor low
            // enough that the APK installs on any headset in the lab.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;

            // Vulkan first, GLES3 as a fallback: a Vulkan-only build fails to start
            // outright on drivers that do not expose the extensions OpenXR needs.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
            });

            EditorUserBuildSettings.buildAppBundle = false;

            // Start from a clean file: an incremental build reusing the previous APK can leave
            // duplicate zip entries, which some installers reject.
            if (System.IO.File.Exists(ApkPath))
                System.IO.File.Delete(ApkPath);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/ThePit.unity" },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log(string.Format("[PitBuild] {0} size={1} errors={2} warnings={3} time={4}",
                summary.result, summary.totalSize, summary.totalErrors, summary.totalWarnings, summary.totalTime));

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(2);
        }
    }
}
