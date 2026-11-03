using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Adds the interaction-feel components to the existing scene without rebuilding it.</summary>
public static class PitInteractionUpgrade
{
    private const string ScenePath = "Assets/Scenes/ThePit.unity";

    [MenuItem("Devils Roulette/Upgrade Interaction Feel")]
    public static void ApplyFromMenu()
    {
        Apply(false);
    }

    public static void ApplyBatch()
    {
        Apply(true);
    }

    private static void Apply(bool quit)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject gun = GameObject.Find("Shotgun");
        if (gun == null)
        {
            Debug.LogError("[PitUpgrade] Shotgun not found.");
            if (quit) EditorApplication.Exit(1);
            return;
        }

        bool added = false;

        if (gun.GetComponent<GrabAffordance>() == null)
        {
            gun.AddComponent<GrabAffordance>();
            added = true;
        }

        if (gun.GetComponent<GunRecovery>() == null)
        {
            gun.AddComponent<GunRecovery>();
            added = true;
        }

        if (added)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[PitUpgrade] interaction feel components " + (added ? "added" : "already present"));
        if (quit)
            EditorApplication.Exit(0);
    }
}
