using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// The Starter Assets rig predates XRI 3.6, where select/activate input moved into
/// ActionBasedController. Without it the controllers track their pose but never emit a select,
/// so nothing can be grabbed. Adds the component and wires the existing actions.
/// </summary>
public static class PitFixControllerInput
{
    private const string PrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string ActionsPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/XRI Default Input Actions.inputactions";

    public static void Run()
    {
        var references = new Dictionary<string, InputActionReference>();
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(ActionsPath))
            if (obj is InputActionReference r) references[r.name] = r;

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        int changed = 0;

        foreach (string side in new[] { "Left", "Right" })
        {
            Transform t = FindDeep(root.transform, side + " Controller");
            if (t == null) { Debug.LogWarning("[FixInput] missing " + side + " Controller"); continue; }

            var controller = t.GetComponent<ActionBasedController>();
            bool added = controller == null;
            if (added) controller = t.gameObject.AddComponent<ActionBasedController>();

            var so = new SerializedObject(controller);
            Assign(so, "m_SelectAction", references, "XRI " + side + " Interaction/Select");
            Assign(so, "m_SelectActionValue", references, "XRI " + side + " Interaction/Select Value");
            Assign(so, "m_ActivateAction", references, "XRI " + side + " Interaction/Activate");
            Assign(so, "m_ActivateActionValue", references, "XRI " + side + " Interaction/Activate Value");

            var tracking = so.FindProperty("m_EnableInputTracking");
            if (tracking != null) tracking.boolValue = false; // TrackedPoseDriver already drives the pose

            so.ApplyModifiedPropertiesWithoutUndo();
            changed++;
            Debug.Log("[FixInput] " + side + " Controller input " + (added ? "added" : "updated"));
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FixInput] done, controllers=" + changed);
        EditorApplication.Exit(0);
    }

    private static void Assign(SerializedObject so, string field, Dictionary<string, InputActionReference> refs, string name)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning("[FixInput] no field " + field); return; }
        if (!refs.TryGetValue(name, out var reference)) { Debug.LogWarning("[FixInput] no action " + name); return; }

        var useRef = prop.FindPropertyRelative("m_UseReference");
        var refProp = prop.FindPropertyRelative("m_Reference");
        if (useRef != null) useRef.boolValue = true;
        if (refProp != null) refProp.objectReferenceValue = reference;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
