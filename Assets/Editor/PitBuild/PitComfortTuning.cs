using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Moves the player into reach of the table and calms the locomotion down.</summary>
public static class PitComfortTuning
{
    private const string ScenePath = "Assets/Scenes/ThePit.unity";

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject rig = GameObject.Find("XR Origin (XR Rig)");
        if (rig == null)
        {
            Debug.LogError("[PitTune] rig not found");
            EditorApplication.Exit(1);
            return;
        }

        // The gun sits 0.98 m from the origin: outside a comfortable reach. Move the play area
        // forward so the grip is about 0.72 m away and the table edge about 0.26 m.
        rig.transform.position = new Vector3(0f, 0f, 0.26f);

        int tuned = 0;
        foreach (MonoBehaviour behaviour in rig.GetComponentsInChildren<MonoBehaviour>(true))
        {
            string type = behaviour.GetType().Name;
            var serialized = new SerializedObject(behaviour);

            if (type.Contains("ContinuousMoveProvider"))
            {
                SerializedProperty speed = serialized.FindProperty("m_MoveSpeed");
                if (speed != null)
                {
                    speed.floatValue = 1.3f;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    tuned++;
                    Debug.Log("[PitTune] move speed -> 1.3");
                }
            }
            else if (type.Contains("SnapTurnProvider"))
            {
                SerializedProperty turn = serialized.FindProperty("m_TurnAmount");
                if (turn != null)
                {
                    turn.floatValue = 30f;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    tuned++;
                    Debug.Log("[PitTune] snap turn -> 30");
                }
            }
        }

        GameObject gun = GameObject.Find("Shotgun");
        if (gun != null)
        {
            var box = gun.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.size = new Vector3(0.22f, 0.26f, 1.0f);
                box.center = new Vector3(0f, 0.01f, 0.04f);
                tuned++;
                Debug.Log("[PitTune] gun grab collider widened");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[PitTune] done, changes=" + tuned);
        EditorApplication.Exit(0);
    }
}
