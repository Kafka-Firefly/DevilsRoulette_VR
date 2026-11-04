using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PitSpeedTuning
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ThePit.unity", OpenSceneMode.Single);
        var rig = GameObject.Find("XR Origin (XR Rig)");
        int changed = 0;

        foreach (var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var so = new SerializedObject(mb);
            var speed = so.FindProperty("m_MoveSpeed");
            if (speed == null) continue;

            speed.floatValue = 1.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            changed++;
            Debug.Log("[PitSpeed] " + mb.GetType().Name + " m_MoveSpeed -> 1.3");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ThePit.unity");
        Debug.Log("[PitSpeed] done, changed=" + changed);
        EditorApplication.Exit(0);
    }
}
