using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Rebuilds The Pit's environment with correct metre-scale geometry, layered materials,
/// a readable lighting setup and dressed set pieces. Gameplay objects (XR rig, shotgun,
/// target) keep their components; only their visuals and colliders are rebuilt.
/// Safe to run repeatedly: everything it owns is recreated from scratch.
/// </summary>
public static class ThePitArtPass
{
    private const string ScenePath = "Assets/Scenes/ThePit.unity";
    private const string EnvName = "The Pit Environment";
    private const string MarkerName = "Modeling Pass V2";

    private static readonly HashSet<string> ObsoleteNames = new HashSet<string>
    {
        "Floor", "Back Wall", "Left Wall", "Right Wall", "Front Wall", "Ceiling",
        "Tabletop", "Table Leg", "Tabletop Details", "Table Spotlight",
        "THE PIT Sign", "Wall Safety Stripe", "Wall Service Panel",
        "Ceiling Pipe", "Pipe Bracket", "Ceiling Beam", "Post Process Volume",
        EnvName
    };

    [MenuItem("Devils Roulette/Apply The Pit Art Pass")]
    public static void ApplyFromMenu()
    {
        Build(false);
    }

    /// <summary>Batch entry point: apply the pass and render preview frames.</summary>
    public static void ApplyAndRender()
    {
        Build(true);
    }

    private static void Build(bool renderPreview)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        PatchPipeline();
        PitArtAssets.EnsureFolders();
        PitMaterials mats = PitArtAssets.BuildAll();

        RemoveObsolete();

        BuildShell(mats);
        BuildTable(mats);
        BuildTarget(mats);
        BuildGun(mats);
        BuildSign(mats);
        BuildSetDressing(mats);
        BuildLighting(mats);
        BuildAtmosphere(mats);
        BuildPostProcessing();
        ApplyRenderSettings();
        EnsureCameraPostProcessing();
        EnsureMarker();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("The Pit art pass applied to " + ScenePath);

        if (renderPreview)
            PitArtPreview.RenderAll("after");
    }

    // ------------------------------------------------------------ pipeline

    private static void PatchPipeline()
    {
        string[] paths =
        {
            "Assets/Settings/Project Configuration/Performance URP Config.asset",
            "Assets/Settings/Project Configuration/Quality URP Config.asset"
        };

        foreach (string path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset == null)
                continue;

            var so = new SerializedObject(asset);
            SetBool(so, "m_SupportsHDR", true);
            SetEnum(so, "m_AdditionalLightsRenderingMode", 2); // PerPixel
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 4);
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetBool(so, "m_LocalShadowsSupported", true);
            SetInt(so, "m_ShadowAtlasResolution", 1024);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 1024);
            SetBool(so, "m_SupportsDynamicBatching", true);
            SetFloat(so, "m_ShadowDistance", 12f);
            SetInt(so, "m_MainLightShadowmapResolution", 2048);
            SetBool(so, "m_SoftShadowsSupported", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }
    }

    private static void SetBool(SerializedObject so, string name, bool value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null) p.boolValue = value;
    }

    private static void SetInt(SerializedObject so, string name, int value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null) p.intValue = value;
    }

    private static void SetEnum(SerializedObject so, string name, int value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null) p.enumValueIndex = value;
    }

    private static void SetFloat(SerializedObject so, string name, float value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null) p.floatValue = value;
    }

    // ------------------------------------------------------------ cleanup

    private static void RemoveObsolete()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            if (ObsoleteNames.Contains(root.name))
                Object.DestroyImmediate(root);
        }

        DestroyChildren(GameObject.Find("Target"));
        DestroyChildren(GameObject.Find("Shotgun"));
    }

    private static void DestroyChildren(GameObject parent)
    {
        if (parent == null)
            return;
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.transform.GetChild(i).gameObject);
    }

    // ------------------------------------------------------------ shell

    private static void BuildShell(PitMaterials m)
    {
        Transform root = PitShapes.Group(null, EnvName, Vector3.zero).transform;

        PitShapes.Box(root, "Floor", new Vector3(0f, -0.075f, 1.1f), new Vector3(6.2f, 0.15f, 7.2f), m.ConcreteFloor, null, true);
        PitShapes.Box(root, "Ceiling", new Vector3(0f, 3.3f, 1.1f), new Vector3(6.2f, 0.2f, 7.2f), m.ConcreteDark, null, true);
        PitShapes.Box(root, "Back Wall", new Vector3(0f, 1.6f, 4.7f), new Vector3(6.2f, 3.2f, 0.2f), m.ConcreteWall, null, true);
        PitShapes.Box(root, "Front Wall", new Vector3(0f, 1.6f, -2.5f), new Vector3(6.2f, 3.2f, 0.2f), m.ConcreteWall, null, true);
        PitShapes.Box(root, "Left Wall", new Vector3(-3.1f, 1.6f, 1.1f), new Vector3(0.2f, 3.2f, 7.2f), m.ConcreteWall, null, true);
        PitShapes.Box(root, "Right Wall", new Vector3(3.1f, 1.6f, 1.1f), new Vector3(0.2f, 3.2f, 7.2f), m.ConcreteWall, null, true);

        // Steel wainscot: a 1 m tall band of riveted plate around the room.
        PitShapes.Box(root, "Wainscot Back", new Vector3(0f, 0.525f, 4.582f), new Vector3(6.0f, 1.05f, 0.035f), m.Steel);
        PitShapes.Box(root, "Wainscot Front", new Vector3(0f, 0.525f, -2.382f), new Vector3(6.0f, 1.05f, 0.035f), m.Steel);
        PitShapes.Box(root, "Wainscot Left", new Vector3(-2.982f, 0.525f, 1.1f), new Vector3(0.035f, 1.05f, 7.0f), m.Steel);
        PitShapes.Box(root, "Wainscot Right", new Vector3(2.982f, 0.525f, 1.1f), new Vector3(0.035f, 1.05f, 7.0f), m.Steel);

        PitShapes.Box(root, "Wainscot Cap Back", new Vector3(0f, 1.075f, 4.578f), new Vector3(6.0f, 0.05f, 0.05f), m.SteelDark);
        PitShapes.Box(root, "Wainscot Cap Left", new Vector3(-2.978f, 1.075f, 1.1f), new Vector3(0.05f, 0.05f, 7.0f), m.SteelDark);
        PitShapes.Box(root, "Wainscot Cap Right", new Vector3(2.978f, 1.075f, 1.1f), new Vector3(0.05f, 0.05f, 7.0f), m.SteelDark);

        PitShapes.Rivets(root, "Wainscot Rivet", new Vector3(-2.9f, 0.95f, 4.558f), new Vector3(0.5f, 0f, 0f), 12, 0.012f, m.Brass);
        PitShapes.Rivets(root, "Wainscot Rivet", new Vector3(-2.9f, 0.16f, 4.558f), new Vector3(0.5f, 0f, 0f), 12, 0.012f, m.Brass);
        PitShapes.Rivets(root, "Wainscot Rivet", new Vector3(-2.958f, 0.95f, -2.1f), new Vector3(0f, 0f, 0.5f), 13, 0.012f, m.Brass);
        PitShapes.Rivets(root, "Wainscot Rivet", new Vector3(2.958f, 0.95f, -2.1f), new Vector3(0f, 0f, 0.5f), 13, 0.012f, m.Brass);

        // Floor: grout grid, firing line, drain grate.
        for (int i = -3; i <= 3; i++)
        {
            float x = i * 0.85f;
            PitShapes.Box(root, "Floor Grout", new Vector3(x, 0.002f, 1.1f), new Vector3(0.012f, 0.006f, 7.1f), m.Grout);
        }
        for (int i = -3; i <= 4; i++)
        {
            float z = -1.9f + i * 0.85f;
            PitShapes.Box(root, "Floor Grout", new Vector3(0f, 0.002f, z), new Vector3(6.1f, 0.006f, 0.012f), m.Grout);
        }

        PitShapes.Box(root, "Firing Line", new Vector3(0f, 0.003f, 0.52f), new Vector3(2.6f, 0.006f, 0.05f), m.PaintRed);

        PitShapes.Box(root, "Drain Frame", new Vector3(-1.05f, 0.004f, 2.15f), new Vector3(0.78f, 0.008f, 0.78f), m.SteelDark);
        PitShapes.Box(root, "Drain Hole", new Vector3(-1.05f, 0.006f, 2.15f), new Vector3(0.66f, 0.01f, 0.66f), m.Grout);
        for (int i = -3; i <= 3; i++)
            PitShapes.Box(root, "Drain Slat", new Vector3(-1.05f, 0.012f, 2.15f + i * 0.09f), new Vector3(0.66f, 0.012f, 0.03f), m.SteelDark);

        // Ceiling: I-beams, duct run, cable tray.
        foreach (float z in new[] { -1.3f, 1.05f, 3.4f })
        {
            PitShapes.Box(root, "Ceiling Beam Top", new Vector3(0f, 3.175f, z), new Vector3(6.2f, 0.05f, 0.20f), m.SteelDark);
            PitShapes.Box(root, "Ceiling Beam Web", new Vector3(0f, 3.075f, z), new Vector3(6.2f, 0.16f, 0.08f), m.SteelDark);
            PitShapes.Box(root, "Ceiling Beam Bottom", new Vector3(0f, 2.955f, z), new Vector3(6.2f, 0.04f, 0.20f), m.SteelDark);
        }

        PitShapes.Box(root, "Air Duct", new Vector3(2.3f, 2.88f, 1.1f), new Vector3(0.6f, 0.42f, 5.4f), m.SteelDark);
        for (int i = -2; i <= 2; i++)
            PitShapes.Box(root, "Duct Band", new Vector3(2.3f, 2.88f, 1.1f + i * 1.35f), new Vector3(0.64f, 0.46f, 0.05f), m.SteelDark);

        PitShapes.CylinderZ(root, "Pipe Main", new Vector3(-2.72f, 2.86f, 1.1f), 0.075f, 6.8f, m.Steel);
        PitShapes.CylinderZ(root, "Pipe Return", new Vector3(-2.72f, 2.64f, 1.1f), 0.05f, 6.8f, m.SteelDark);
        for (int i = 0; i < 5; i++)
            PitShapes.Box(root, "Pipe Bracket", new Vector3(-2.72f, 2.78f, -2.0f + i * 1.55f), new Vector3(0.16f, 0.03f, 0.08f), m.SteelDark);

        // Valve wheel on the main pipe.
        PitShapes.CylinderX(root, "Valve Hub", new Vector3(-2.72f, 2.35f, 2.2f), 0.028f, 0.07f, m.Steel);
        PitShapes.CylinderX(root, "Valve Wheel", new Vector3(-2.69f, 2.35f, 2.2f), 0.11f, 0.018f, m.PaintRed);
        for (int i = 0; i < 4; i++)
            PitShapes.Box(root, "Valve Spoke", new Vector3(-2.69f, 2.35f, 2.2f),
                new Vector3(0.018f, 0.20f, 0.018f), m.SteelDark, Quaternion.Euler(i * 45f, 0f, 0f));

        // Loose cables along the ceiling.
        for (int i = 0; i < 3; i++)
            PitShapes.CylinderZ(root, "Cable", new Vector3(-1.9f + i * 0.22f, 3.02f - i * 0.03f, 1.1f), 0.012f, 6.6f, m.SteelDark);

        // Wall dressing: service panel, conduit, steel door.
        PitShapes.Box(root, "Service Panel", new Vector3(-2.965f, 1.55f, -0.6f), new Vector3(0.06f, 0.8f, 0.55f), m.SteelDark);
        PitShapes.Box(root, "Service Placard", new Vector3(-2.925f, 1.62f, -0.6f), new Vector3(0.012f, 0.2f, 0.26f), m.PaintRed);
        PitShapes.CylinderY(root, "Conduit", new Vector3(-2.965f, 2.35f, -0.38f), 0.022f, 1.5f, m.SteelDark);
        PitShapes.Box(root, "Junction Box", new Vector3(-2.955f, 2.55f, -0.38f), new Vector3(0.09f, 0.12f, 0.14f), m.SteelDark);

        PitShapes.Box(root, "Door Frame", new Vector3(-2.995f, 1.14f, -1.75f), new Vector3(0.1f, 2.28f, 1.18f), m.SteelDark);
        PitShapes.Box(root, "Door Slab", new Vector3(-2.95f, 1.08f, -1.75f), new Vector3(0.07f, 2.16f, 1.0f), m.Steel);
        PitShapes.Box(root, "Door Window", new Vector3(-2.935f, 1.66f, -1.75f), new Vector3(0.02f, 0.2f, 0.46f), m.Grout);
        for (int i = -1; i <= 1; i++)
            PitShapes.Box(root, "Door Window Bar", new Vector3(-2.925f, 1.66f, -1.75f + i * 0.14f), new Vector3(0.012f, 0.22f, 0.018f), m.SteelDark);
        PitShapes.CylinderX(root, "Door Handle", new Vector3(-2.895f, 1.0f, -1.42f), 0.018f, 0.14f, m.Brass);
        PitShapes.Box(root, "Door Kick Plate", new Vector3(-2.915f, 0.2f, -1.75f), new Vector3(0.02f, 0.3f, 0.98f), m.SteelDark);

        // Red warning band on the back wall.
        PitShapes.Box(root, "Back Wall Warning Band", new Vector3(0f, 1.12f, 4.585f), new Vector3(5.9f, 0.06f, 0.02f), m.PaintRed);

        // Hanging chain with a hook on the right wall.
        for (int i = 0; i < 9; i++)
        {
            float y = 2.55f - i * 0.07f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, (i % 2 == 0) ? 90f : 0f);
            PitShapes.Cylinder(null, "Chain Link", new Vector3(2.94f, y, 3.55f), 0.016f, 0.06f, m.SteelDark, rotation, false)
                .transform.SetParent(root, true);
        }
        PitShapes.Box(root, "Hook", new Vector3(2.94f, 1.85f, 3.55f), new Vector3(0.03f, 0.14f, 0.05f), m.SteelDark);
    }

    // ------------------------------------------------------------ table

    private static void BuildTable(PitMaterials m)
    {
        Transform root = PitShapes.Group(null, "Table", new Vector3(0f, 0f, 1.0f)).transform;

        // Keeps the original anchor (0, 0.78, 1.0); RouletteRoundDisplay looks this object up by name.
        PitShapes.Box(root, "Tabletop", new Vector3(0f, 0.78f, 0f), new Vector3(1.5f, 0.09f, 0.95f), m.Wood, null, true);
        PitShapes.Box(root, "Felt Inlay", new Vector3(0f, 0.828f, 0f), new Vector3(1.30f, 0.006f, 0.76f), m.Felt);

        PitShapes.Box(root, "Felt Trim Front", new Vector3(0f, 0.8295f, -0.385f), new Vector3(1.34f, 0.008f, 0.02f), m.Brass);
        PitShapes.Box(root, "Felt Trim Rear", new Vector3(0f, 0.8295f, 0.385f), new Vector3(1.34f, 0.008f, 0.02f), m.Brass);
        PitShapes.Box(root, "Felt Trim Left", new Vector3(-0.655f, 0.8295f, 0f), new Vector3(0.02f, 0.008f, 0.78f), m.Brass);
        PitShapes.Box(root, "Felt Trim Right", new Vector3(0.655f, 0.8295f, 0f), new Vector3(0.02f, 0.008f, 0.78f), m.Brass);

        PitShapes.Box(root, "Apron Front", new Vector3(0f, 0.70f, -0.455f), new Vector3(1.46f, 0.12f, 0.03f), m.SteelDark, null, true);
        PitShapes.Box(root, "Apron Rear", new Vector3(0f, 0.70f, 0.455f), new Vector3(1.46f, 0.12f, 0.03f), m.SteelDark, null, true);
        PitShapes.Box(root, "Apron Left", new Vector3(-0.735f, 0.70f, 0f), new Vector3(0.03f, 0.12f, 0.92f), m.SteelDark);
        PitShapes.Box(root, "Apron Right", new Vector3(0.735f, 0.70f, 0f), new Vector3(0.03f, 0.12f, 0.92f), m.SteelDark);

        PitShapes.Rivets(root, "Apron Rivet", new Vector3(-0.62f, 0.70f, -0.472f), new Vector3(0.31f, 0f, 0f), 5, 0.013f, m.Brass);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                PitShapes.Box(root, "Table Leg", new Vector3(x * 0.62f, 0.33f, z * 0.40f),
                    new Vector3(0.07f, 0.66f, 0.07f), m.SteelDark, null, true);
                PitShapes.Box(root, "Leg Foot", new Vector3(x * 0.62f, 0.02f, z * 0.40f),
                    new Vector3(0.13f, 0.04f, 0.13f), m.Steel);
            }
        }

        PitShapes.Box(root, "Cross Brace Left", new Vector3(-0.62f, 0.14f, 0f), new Vector3(0.05f, 0.05f, 0.78f), m.SteelDark);
        PitShapes.Box(root, "Cross Brace Right", new Vector3(0.62f, 0.14f, 0f), new Vector3(0.05f, 0.05f, 0.78f), m.SteelDark);
        PitShapes.Box(root, "Cross Brace Front", new Vector3(0f, 0.14f, -0.40f), new Vector3(1.18f, 0.05f, 0.05f), m.SteelDark);

        // A spent brass case reads as "already played".
        PitShapes.CylinderZ(null, "Spent Case", new Vector3(-0.42f, 0.836f, 1.22f), 0.011f, 0.05f, m.Brass)
            .transform.SetParent(root, true);
    }

    // ------------------------------------------------------------ target

    private static void BuildTarget(PitMaterials m)
    {
        GameObject target = GameObject.Find("Target");
        if (target == null)
        {
            Debug.LogWarning("The Pit art pass: no Target object found.");
            return;
        }

        target.transform.localScale = Vector3.one;
        target.transform.position = new Vector3(0f, 1.15f, 3.3f);
        target.transform.rotation = Quaternion.identity;

        var collider = target.GetComponent<BoxCollider>();
        if (collider == null)
            collider = target.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0f, -0.02f);
        collider.size = new Vector3(0.60f, 0.64f, 0.18f);
        collider.isTrigger = false;

        Transform t = target.transform;

        PitShapes.Box(t, "Target Frame", new Vector3(0f, 0f, 0.075f), new Vector3(0.68f, 0.74f, 0.05f), m.SteelDark);
        PitShapes.Box(t, "Target Frame Rib", new Vector3(0f, 0f, 0.11f), new Vector3(0.26f, 0.72f, 0.03f), m.SteelDark);
        PitShapes.CylinderZ(t, "Target Steel Face", new Vector3(0f, 0f, 0.02f), 0.275f, 0.03f, m.Steel);
        PitShapes.CylinderZ(t, "Target Outer Ring", new Vector3(0f, 0f, -0.002f), 0.215f, 0.02f, m.SteelDark);
        PitShapes.CylinderZ(t, "Target Scoring Ring", new Vector3(0f, 0f, -0.016f), 0.155f, 0.018f, m.Cream);
        PitShapes.CylinderZ(t, "Target Bullseye", new Vector3(0f, 0f, -0.03f), 0.072f, 0.018f, m.PaintRed);
        PitShapes.CylinderZ(t, "Target Center Pin", new Vector3(0f, 0f, -0.042f), 0.012f, 0.016f, m.Brass);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
                PitShapes.CylinderZ(t, "Frame Bolt", new Vector3(x * 0.26f, y * 0.29f, 0.05f), 0.014f, 0.02f, m.Brass);
        }

        PitShapes.CylinderY(t, "Target Pole", new Vector3(0f, -0.745f, 0.10f), 0.035f, 0.77f, m.SteelDark, true);
        PitShapes.Box(t, "Target Base A", new Vector3(0f, -1.12f, 0.10f), new Vector3(0.86f, 0.05f, 0.16f), m.SteelDark, null, true);
        PitShapes.Box(t, "Target Base B", new Vector3(0f, -1.10f, 0.10f), new Vector3(0.16f, 0.05f, 0.86f), m.SteelDark, null, true);

        // Heavy backstop behind the target.
        PitShapes.Box(t, "Backstop Plate", new Vector3(0f, 0.05f, 0.66f), new Vector3(1.7f, 1.7f, 0.08f), m.SteelDark, null, true);
        PitShapes.Box(t, "Backstop Lip", new Vector3(0f, -0.82f, 0.55f), new Vector3(1.7f, 0.10f, 0.22f), m.Steel);
        PitShapes.Rivets(t, "Backstop Rivet", new Vector3(-0.72f, 0.75f, 0.61f), new Vector3(0.24f, 0f, 0f), 7, 0.016f, m.Brass);
        PitShapes.Rivets(t, "Backstop Rivet", new Vector3(-0.72f, -0.65f, 0.61f), new Vector3(0.24f, 0f, 0f), 7, 0.016f, m.Brass);
        for (int i = 0; i < 3; i++)
            PitShapes.CylinderZ(t, "Scorch Mark", new Vector3(-0.45f + i * 0.42f, 0.35f - i * 0.22f, 0.615f), 0.035f, 0.006f, m.Grout);

        // Only the rings flash on a hit, not the whole stand.
        var receiver = target.GetComponent<TargetHitReceiver>();
        if (receiver != null)
        {
            var serialized = new SerializedObject(receiver);
            SerializedProperty array = serialized.FindProperty("targetRenderers");
            if (array != null)
            {
                string[] ringNames =
                {
                    "Target Steel Face", "Target Outer Ring", "Target Scoring Ring", "Target Bullseye"
                };
                var ringRenderers = new List<Renderer>();
                foreach (string ringName in ringNames)
                {
                    Transform found = t.Find(ringName);
                    if (found != null)
                    {
                        var renderer = found.GetComponent<Renderer>();
                        if (renderer != null)
                            ringRenderers.Add(renderer);
                    }
                }

                array.arraySize = ringRenderers.Count;
                for (int i = 0; i < ringRenderers.Count; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = ringRenderers[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    // ------------------------------------------------------------ shotgun

    private static void BuildGun(PitMaterials m)
    {
        GameObject gun = GameObject.Find("Shotgun");
        if (gun == null)
        {
            Debug.LogWarning("The Pit art pass: no Shotgun object found.");
            return;
        }

        gun.transform.position = new Vector3(0f, 0.94f, 0.98f);
        gun.transform.rotation = Quaternion.Euler(0f, 6f, 0f);

        var collider = gun.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.center = new Vector3(0f, 0.01f, 0.04f);
            collider.size = new Vector3(0.14f, 0.20f, 0.98f);
        }

        Transform t = gun.transform;

        PitShapes.Box(t, "Receiver", new Vector3(0f, -0.005f, 0.02f), new Vector3(0.085f, 0.105f, 0.30f), m.SteelDark);
        PitShapes.Box(t, "Receiver Plate Left", new Vector3(-0.045f, 0f, 0.02f), new Vector3(0.006f, 0.085f, 0.24f), m.Steel);
        PitShapes.Box(t, "Receiver Plate Right", new Vector3(0.045f, 0f, 0.02f), new Vector3(0.006f, 0.085f, 0.24f), m.Steel);
        PitShapes.Box(t, "Ejection Port", new Vector3(-0.047f, 0.025f, 0.10f), new Vector3(0.004f, 0.03f, 0.07f), m.Grout);
        PitShapes.Box(t, "Ejection Port Lip", new Vector3(-0.049f, 0.043f, 0.10f), new Vector3(0.004f, 0.004f, 0.07f), m.Brass);

        PitShapes.CylinderZ(t, "Barrel", new Vector3(0f, 0.028f, 0.38f), 0.021f, 0.44f, m.SteelDark);
        PitShapes.Box(t, "Vent Rib", new Vector3(0f, 0.052f, 0.36f), new Vector3(0.018f, 0.010f, 0.44f), m.SteelDark);
        PitShapes.Sphere(t, "Front Bead", new Vector3(0f, 0.062f, 0.57f), 0.008f, m.Brass);
        PitShapes.Box(t, "Rear Sight", new Vector3(0f, 0.060f, -0.06f), new Vector3(0.036f, 0.020f, 0.022f), m.Steel);
        PitShapes.Box(t, "Rear Sight Notch", new Vector3(0f, 0.072f, -0.06f), new Vector3(0.012f, 0.006f, 0.024f), m.Grout);

        PitShapes.CylinderZ(t, "Magazine Tube", new Vector3(0f, -0.028f, 0.33f), 0.023f, 0.42f, m.SteelDark);
        PitShapes.CylinderZ(t, "Magazine Cap", new Vector3(0f, -0.028f, 0.545f), 0.026f, 0.025f, m.Brass);

        PitShapes.Box(t, "Pump Fore End", new Vector3(0f, -0.012f, 0.27f), new Vector3(0.075f, 0.070f, 0.21f), m.Wood);
        for (int i = 0; i < 4; i++)
        {
            PitShapes.Box(t, "Pump Groove L", new Vector3(-0.038f, -0.040f + i * 0.022f, 0.27f), new Vector3(0.004f, 0.008f, 0.19f), m.SteelDark);
            PitShapes.Box(t, "Pump Groove R", new Vector3(0.038f, -0.040f + i * 0.022f, 0.27f), new Vector3(0.004f, 0.008f, 0.19f), m.SteelDark);
        }

        PitShapes.Box(t, "Trigger Guard Bottom", new Vector3(0f, -0.090f, -0.10f), new Vector3(0.014f, 0.008f, 0.10f), m.Steel);
        PitShapes.Box(t, "Trigger Guard Front", new Vector3(0f, -0.062f, -0.055f), new Vector3(0.014f, 0.055f, 0.008f), m.Steel);
        PitShapes.Box(t, "Trigger Guard Rear", new Vector3(0f, -0.062f, -0.145f), new Vector3(0.014f, 0.055f, 0.008f), m.Steel);
        PitShapes.Box(t, "Trigger", new Vector3(0f, -0.070f, -0.098f), new Vector3(0.010f, 0.032f, 0.008f), m.Brass, Quaternion.Euler(-12f, 0f, 0f));

        PitShapes.Box(t, "Grip", new Vector3(0f, -0.078f, -0.148f), new Vector3(0.055f, 0.135f, 0.075f), m.Wood, Quaternion.Euler(-15f, 0f, 0f));
        PitShapes.Box(t, "Stock", new Vector3(0f, -0.014f, -0.290f), new Vector3(0.075f, 0.100f, 0.30f), m.Wood, Quaternion.Euler(-3f, 0f, 0f));
        PitShapes.Box(t, "Stock Comb", new Vector3(0f, 0.028f, -0.270f), new Vector3(0.052f, 0.030f, 0.22f), m.Wood);
        PitShapes.Box(t, "Butt Pad", new Vector3(0f, -0.040f, -0.440f), new Vector3(0.080f, 0.115f, 0.028f), m.SteelDark);
        PitShapes.Sphere(t, "Sling Stud", new Vector3(0f, -0.075f, -0.405f), 0.009f, m.Brass);

        for (int x = -1; x <= 1; x += 2)
            PitShapes.CylinderX(t, "Receiver Screw", new Vector3(x * 0.047f, -0.02f, 0.12f), 0.008f, 0.008f, m.Brass);

        GameObject barrelTip = new GameObject("BarrelTip");
        barrelTip.transform.SetParent(t, false);
        barrelTip.transform.localPosition = new Vector3(0f, 0.028f, 0.60f);

        GameObject attach = new GameObject("Attach");
        attach.transform.SetParent(t, false);
        attach.transform.localPosition = new Vector3(0f, -0.09f, -0.12f);

        GameObject muzzleGo = new GameObject("Muzzle Flash");
        muzzleGo.transform.SetParent(t, false);
        muzzleGo.transform.localPosition = new Vector3(0f, 0.028f, 0.61f);
        var muzzle = muzzleGo.AddComponent<ParticleSystem>();
        var main = muzzle.main;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = 0.07f;
        main.startSpeed = 1.8f;
        main.startSize = 0.05f;
        main.startColor = new Color(1f, 0.74f, 0.34f, 1f);
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        var emission = muzzle.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
        var shape = muzzle.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.01f;
        var muzzleRenderer = muzzleGo.GetComponent<ParticleSystemRenderer>();
        muzzleRenderer.material = m.MuzzleFlash;
        muzzleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        muzzleRenderer.alignment = ParticleSystemRenderSpace.View;
        muzzleRenderer.shadowCastingMode = ShadowCastingMode.Off;

        var grab = gun.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.attachTransform = attach.transform;

        if (gun.GetComponent<GrabAffordance>() == null)
            gun.AddComponent<GrabAffordance>();
        if (gun.GetComponent<GunRecovery>() == null)
            gun.AddComponent<GunRecovery>();

        var controller = gun.GetComponent<ShotgunController>();
        if (controller != null)
        {
            var serialized = new SerializedObject(controller);
            SerializedProperty tip = serialized.FindProperty("barrelTip");
            if (tip != null) tip.objectReferenceValue = barrelTip.transform;
            SerializedProperty flash = serialized.FindProperty("muzzleFlash");
            if (flash != null) flash.objectReferenceValue = muzzle;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ------------------------------------------------------------ sign

    private static void BuildSign(PitMaterials m)
    {
        var sign = PitShapes.Group(null, "THE PIT Sign", new Vector3(0f, 2.66f, 4.55f));
        Transform t = sign.transform;

        PitShapes.Box(t, "Sign Backing", Vector3.zero, new Vector3(2.0f, 0.5f, 0.06f), m.SteelDark);
        PitShapes.Box(t, "Sign Panel", new Vector3(0f, 0f, -0.038f), new Vector3(1.86f, 0.36f, 0.02f), m.Grout);
        PitShapes.Box(t, "Sign Brass Rule", new Vector3(0f, -0.19f, -0.048f), new Vector3(1.6f, 0.015f, 0.01f), m.Brass);
        PitShapes.Box(t, "Sign Neon", new Vector3(0f, -0.225f, -0.052f), new Vector3(1.7f, 0.022f, 0.022f), m.GlowRed);

        BuildStencilText(t, "THE PIT", new Vector3(0f, 0.02f, -0.056f), m.SignPaint, 0.17f, 0.026f, 0.014f);
    }

    /// <summary>Draws the sign as extruded metal strokes so it needs no font atlas and never draws through walls.</summary>
    private static void BuildStencilText(Transform parent, string text, Vector3 center, Material material, float height,
        float stroke, float depth)
    {
        float width = height * 0.60f;
        float gap = height * 0.30f;

        float total = 0f;
        foreach (char c in text)
            total += (c == ' ' ? gap * 0.6f : width) + gap;
        total -= gap;

        float x = -total * 0.5f;
        int index = 0;
        foreach (char c in text)
        {
            if (c != ' ')
            {
                var letter = PitShapes.Group(parent, "Sign Letter " + index,
                    center + new Vector3(x + width * 0.5f, 0f, 0f));
                DrawLetter(letter.transform, c, width, height, stroke, depth, material);
            }

            x += (c == ' ' ? gap * 0.6f : width) + gap;
            index++;
        }
    }

    private static void DrawLetter(Transform parent, char letter, float w, float h, float s, float d, Material material)
    {
        float left = -w * 0.5f + s * 0.5f;
        float right = w * 0.5f - s * 0.5f;
        float top = h * 0.5f - s * 0.5f;
        float bottom = -h * 0.5f + s * 0.5f;

        void Bar(float cx, float cy, float bw, float bh)
        {
            PitShapes.Box(parent, "Stroke", new Vector3(cx, cy, 0f), new Vector3(bw, bh, d), material);
        }

        switch (letter)
        {
            case 'T':
                Bar(0f, top, w, s);
                Bar(0f, 0f, s, h);
                break;
            case 'H':
                Bar(left, 0f, s, h);
                Bar(right, 0f, s, h);
                Bar(0f, 0f, w - s * 2f, s);
                break;
            case 'E':
                Bar(left, 0f, s, h);
                Bar(0f, top, w, s);
                Bar(0f, 0f, w, s);
                Bar(0f, bottom, w, s);
                break;
            case 'P':
                Bar(left, 0f, s, h);
                Bar(0f, top, w, s);
                Bar(0f, 0f, w, s);
                Bar(right, h * 0.25f, s, h * 0.5f);
                break;
            case 'I':
                Bar(0f, 0f, s, h);
                break;
            default:
                Bar(0f, 0f, w, h);
                break;
        }
    }

    // ------------------------------------------------------------ set dressing

    private static void BuildSetDressing(PitMaterials m)
    {
        Transform root = GameObject.Find(EnvName)?.transform;
        if (root == null)
            return;

        // Chair, kept out of the firing lane.
        var chair = PitShapes.Group(root, "Chair", new Vector3(-1.75f, 0f, 2.0f));
        chair.transform.localRotation = Quaternion.Euler(0f, 28f, 0f);
        Transform c = chair.transform;
        PitShapes.Box(c, "Chair Seat", new Vector3(0f, 0.45f, 0f), new Vector3(0.46f, 0.06f, 0.44f), m.Wood, null, true);
        PitShapes.Box(c, "Chair Back Rail", new Vector3(0f, 0.74f, -0.20f), new Vector3(0.44f, 0.06f, 0.05f), m.Wood);
        PitShapes.Box(c, "Chair Back Rail 2", new Vector3(0f, 0.62f, -0.20f), new Vector3(0.40f, 0.05f, 0.04f), m.Wood);
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
                PitShapes.CylinderY(c, "Chair Leg", new Vector3(x * 0.19f, 0.22f, z * 0.18f), 0.022f, 0.44f, m.SteelDark, true);
        }

        // Crate stack in the front-left corner.
        var crates = PitShapes.Group(root, "Crate Stack", new Vector3(-2.4f, 0f, 0.1f));
        Transform k = crates.transform;
        PitShapes.Box(k, "Crate A", new Vector3(0f, 0.28f, 0f), new Vector3(0.56f, 0.56f, 0.56f), m.Wood, null, true);
        PitShapes.Box(k, "Crate B", new Vector3(0.04f, 0.83f, 0.02f), new Vector3(0.50f, 0.50f, 0.50f), m.Wood, Quaternion.Euler(0f, 12f, 0f), true);
        foreach (float y in new[] { 0.05f, 0.51f })
            PitShapes.Box(k, "Crate Strap", new Vector3(0f, y, 0f), new Vector3(0.58f, 0.03f, 0.58f), m.SteelDark);

        // Steel drum on the right.
        var barrel = PitShapes.Group(root, "Barrel", new Vector3(2.45f, 0f, 1.35f));
        PitShapes.CylinderY(barrel.transform, "Barrel Body", new Vector3(0f, 0.40f, 0f), 0.26f, 0.80f, m.SteelDark, true);
        PitShapes.CylinderY(barrel.transform, "Barrel Hoop A", new Vector3(0f, 0.20f, 0f), 0.265f, 0.04f, m.Steel);
        PitShapes.CylinderY(barrel.transform, "Barrel Hoop B", new Vector3(0f, 0.62f, 0f), 0.265f, 0.04f, m.Steel);

        // Wall-mounted fire lamp housing on the right.
        PitShapes.Box(root, "Lamp Bracket Right", new Vector3(2.95f, 1.95f, -0.9f), new Vector3(0.18f, 0.04f, 0.04f), m.SteelDark);
    }

    // ------------------------------------------------------------ lighting

    private static void BuildLighting(PitMaterials m)
    {
        Transform root = GameObject.Find(EnvName)?.transform;

        // Hanging work lamp above the table: the key light of the room.
        var lamp = PitShapes.Group(root, "Table Lamp", new Vector3(0f, 0f, 1.05f));
        Transform l = lamp.transform;
        PitShapes.CylinderY(l, "Lamp Cord", new Vector3(0f, 2.87f, 0f), 0.008f, 0.66f, m.SteelDark);
        PitShapes.CylinderY(l, "Lamp Shade", new Vector3(0f, 2.53f, 0f), 0.19f, 0.10f, m.SteelDark);
        PitShapes.CylinderY(l, "Lamp Shade Rim", new Vector3(0f, 2.475f, 0f), 0.195f, 0.015f, m.Brass);
        PitShapes.Sphere(l, "Lamp Bulb", new Vector3(0f, 2.41f, 0f), 0.055f, m.GlowWarm);

        var keyGo = new GameObject("Table Lamp Light");
        keyGo.transform.SetParent(root, false);
        keyGo.transform.localPosition = new Vector3(0f, 2.41f, 1.05f);
        keyGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var key = keyGo.AddComponent<Light>();
        key.type = LightType.Spot;
        key.color = new Color(1f, 0.93f, 0.86f);
        key.intensity = 150f;
        key.range = 6.5f;
        key.spotAngle = 64f;
        key.innerSpotAngle = 34f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.92f;
        key.shadowBias = 0.03f;
        key.shadowNormalBias = 0.25f;
        key.renderMode = LightRenderMode.ForcePixel;

        var flicker = keyGo.AddComponent<PitLampFlicker>();
        var flickerSo = new SerializedObject(flicker);
        flickerSo.FindProperty("targetLight").objectReferenceValue = key;
        flickerSo.FindProperty("baseIntensity").floatValue = 150f;
        flickerSo.FindProperty("flickerDepth").floatValue = 0.07f;
        flickerSo.ApplyModifiedPropertiesWithoutUndo();

        // Cool rim light in front of the target: lights the scoring face, not its back.
        var rimGo = new GameObject("Target Lamp");
        rimGo.transform.SetParent(root, false);
        rimGo.transform.localPosition = new Vector3(0f, 2.70f, 2.50f);
        rimGo.transform.LookAt(new Vector3(0f, 1.15f, 3.3f));
        var rim = rimGo.AddComponent<Light>();
        rim.type = LightType.Spot;
        rim.color = new Color(1f, 0.92f, 0.84f);
        rim.intensity = 90f;
        rim.range = 4.5f;
        rim.spotAngle = 34f;
        rim.shadows = LightShadows.None;
        rim.renderMode = LightRenderMode.ForcePixel;

        PitShapes.CylinderY(root, "Target Lamp Cord", new Vector3(0f, 2.96f, 2.5f), 0.008f, 0.5f, m.SteelDark);
        PitShapes.CylinderY(root, "Target Lamp Shade", new Vector3(0f, 2.74f, 2.5f), 0.13f, 0.09f, m.SteelDark);
        PitShapes.Sphere(root, "Target Lamp Bulb", new Vector3(0f, 2.66f, 2.5f), 0.045f, m.GlowWarm);

        // Red emergency light on the back-left wall, kept weak so it only accents the corner.
        var redGo = new GameObject("Emergency Light");
        redGo.transform.SetParent(root, false);
        redGo.transform.localPosition = new Vector3(-2.45f, 2.9f, 4.1f);
        var red = redGo.AddComponent<Light>();
        red.type = LightType.Point;
        red.color = new Color(1f, 0.16f, 0.09f);
        red.intensity = 3.5f;
        red.range = 2.0f;
        red.shadows = LightShadows.None;
        red.renderMode = LightRenderMode.ForcePixel;

        PitShapes.Box(root, "Emergency Fixture", new Vector3(-2.9f, 2.86f, 4.4f), new Vector3(0.2f, 0.14f, 0.14f), m.SteelDark);
        PitShapes.Box(root, "Emergency Lens", new Vector3(-2.86f, 2.86f, 4.33f), new Vector3(0.04f, 0.09f, 0.09f), m.GlowRed);

        // Dim cool fill from the right so silhouettes stay readable.
        var fillGo = new GameObject("Fill Light");
        fillGo.transform.SetParent(root, false);
        fillGo.transform.localPosition = new Vector3(2.55f, 2.1f, -0.4f);
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(0.58f, 0.66f, 0.86f);
        fill.intensity = 30f;
        fill.range = 7f;
        fill.shadows = LightShadows.None;
        fill.renderMode = LightRenderMode.ForcePixel;
    }

    private static void BuildAtmosphere(PitMaterials m)
    {
        Transform root = GameObject.Find(EnvName)?.transform;

        // Dust motes drifting through the lamp cone.
        var dust = PitShapes.Group(root, "Dust Motes", new Vector3(0f, 1.55f, 1.05f));
        var particles = dust.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = 10f;
        main.startSpeed = 0.012f;
        main.startSize = 0.012f;
        main.startColor = new Color(1f, 0.90f, 0.74f, 0.5f);
        main.maxParticles = 90;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;
        main.gravityModifier = -0.002f;

        var emission = particles.emission;
        emission.rateOverTime = 9f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.1f, 1.2f, 1.1f);

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.06f;
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.05f;

        var dustRenderer = dust.GetComponent<ParticleSystemRenderer>();
        dustRenderer.material = m.Dust;
        dustRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        dustRenderer.alignment = ParticleSystemRenderSpace.View;
        dustRenderer.shadowCastingMode = ShadowCastingMode.Off;
        dustRenderer.receiveShadows = false;
        particles.Play();
    }

    // ------------------------------------------------------------ post & globals

    private static void BuildPostProcessing()
    {
        string path = PitArtAssets.PostDir + "/Pit Post.asset";

        // Volume components must be saved as sub-assets, otherwise the profile reloads with null entries.
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var bloom = AddVolumeComponent<Bloom>(profile);
        bloom.active = true;
        bloom.intensity.value = 0.55f;
        bloom.threshold.value = 0.95f;
        bloom.scatter.value = 0.68f;

        var vignette = AddVolumeComponent<Vignette>(profile);
        vignette.active = true;
        vignette.intensity.value = 0.30f;
        vignette.smoothness.value = 0.45f;

        var tonemapping = AddVolumeComponent<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.value = TonemappingMode.ACES;

        var color = AddVolumeComponent<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.value = 0.40f;
        color.contrast.value = 10f;
        color.saturation.value = -6f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        GameObject existing = GameObject.Find("Post Process Volume");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var go = new GameObject("Post Process Volume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.weight = 1f;
        volume.profile = profile;
    }

    private static T AddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        var component = ScriptableObject.CreateInstance<T>();
        component.name = typeof(T).Name;
        component.active = true;
        profile.components.Add(component);
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void ApplyRenderSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.058f, 0.060f, 0.072f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.045f;
        RenderSettings.fogColor = new Color(0.020f, 0.022f, 0.028f);
        RenderSettings.skybox = null;
    }

    private static void EnsureCameraPostProcessing()
    {
        GameObject rig = GameObject.Find("XR Origin (XR Rig)");
        Camera camera = rig != null ? rig.GetComponentInChildren<Camera>(true) : Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("The Pit art pass: XR camera not found, post processing left untouched.");
            return;
        }

        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        if (data != null)
            data.renderPostProcessing = true;
        camera.allowHDR = true;
    }

    private static void EnsureMarker()
    {
        if (GameObject.Find(MarkerName) != null)
            return;
        var marker = new GameObject(MarkerName);
        marker.hideFlags = HideFlags.HideInHierarchy;
    }
}
