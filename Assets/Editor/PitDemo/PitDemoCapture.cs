using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Batch entry point: renders the scripted demo take synchronously in Edit mode.</summary>
public static class PitDemoCapture
{
    private const string ScenePath = "Assets/Scenes/ThePit.unity";

    public static void Run()
    {
        Capture(140f, PitDemoDirector.DefaultOutput);
    }

    public static void RunTest()
    {
        Capture(6f, @"C:\Users\faifa\Documents\deepseek-harness\default-workspace\pit-art\video\test");
    }

    public static void RunProbe()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var go = new GameObject("~PitDemoDirector");
            var director = go.AddComponent<PitDemoDirector>();
            director.Configure(140f, @"C:\Users\faifa\Documents\deepseek-harness\default-workspace\pit-art\video\probe");
            int frames = director.RunProbe(new[] { 8f, 20f, 30f, 40f, 46f, 54f, 60f, 65.2f, 70f, 92f, 112f, 134f });
            Debug.Log("[PitDemo] probe finished: " + frames);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Capture(float seconds, string root)
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var go = new GameObject("~PitDemoDirector");
            var director = go.AddComponent<PitDemoDirector>();
            director.Configure(seconds, root);
            int frames = director.RunCapture();
            Debug.Log("[PitDemo] capture finished: " + frames + " frames");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
