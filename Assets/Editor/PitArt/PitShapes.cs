using UnityEditor;
using UnityEngine;

/// <summary>Small low-poly construction helpers. Parents are always identity-scaled so sizes stay in metres.</summary>
internal static class PitShapes
{
    internal static GameObject Group(Transform parent, string name, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go;
    }

    internal static GameObject Box(Transform parent, string name, Vector3 localPosition, Vector3 size,
        Material material, Quaternion? localRotation = null, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation ?? Quaternion.identity;
        go.transform.localScale = size;
        Finish(go, material, collider);
        return go;
    }

    internal static GameObject CylinderY(Transform parent, string name, Vector3 localPosition, float radius, float length,
        Material material, bool collider = false)
    {
        return Cylinder(parent, name, localPosition, radius, length, material, Quaternion.identity, collider);
    }

    internal static GameObject CylinderX(Transform parent, string name, Vector3 localPosition, float radius, float length,
        Material material, bool collider = false)
    {
        return Cylinder(parent, name, localPosition, radius, length, material, Quaternion.Euler(0f, 0f, 90f), collider);
    }

    internal static GameObject CylinderZ(Transform parent, string name, Vector3 localPosition, float radius, float length,
        Material material, bool collider = false)
    {
        return Cylinder(parent, name, localPosition, radius, length, material, Quaternion.Euler(90f, 0f, 0f), collider);
    }

    internal static GameObject Cylinder(Transform parent, string name, Vector3 localPosition, float radius, float length,
        Material material, Quaternion localRotation, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
        Finish(go, material, collider);
        return go;
    }

    internal static GameObject Sphere(Transform parent, string name, Vector3 localPosition, float radius,
        Material material, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = Vector3.one * (radius * 2f);
        Finish(go, material, collider);
        return go;
    }

    internal static void Rivets(Transform parent, string name, Vector3 start, Vector3 step, int count, float radius,
        Material material)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 position = start + step * i;
            if (Mathf.Abs(step.x) > Mathf.Abs(step.z))
                CylinderX(parent, name, position, radius, 0.012f, material);
            else
                CylinderZ(parent, name, position, radius, 0.012f, material);
        }
    }

    private static void Finish(GameObject go, Material material, bool collider)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        if (!collider)
        {
            var existing = go.GetComponent<Collider>();
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
    }
}
