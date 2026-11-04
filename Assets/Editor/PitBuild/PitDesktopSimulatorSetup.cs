using System.IO;
using UnityEditor;
using UnityEngine;

public static class PitDesktopSimulatorSetup
{
    private const string Source = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Device Simulator/XR Device Simulator.prefab";
    private const string Target = "Assets/Resources/PitDeviceSimulator.prefab";

    public static void Run()
    {
        Directory.CreateDirectory("Assets/Resources");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(Target) != null)
        {
            Debug.Log("[PitSetup] simulator prefab already in Resources");
        }
        else
        {
            bool copied = AssetDatabase.CopyAsset(Source, Target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PitSetup] simulator prefab copied: " + copied);
        }

        EditorApplication.Exit(0);
    }
}
