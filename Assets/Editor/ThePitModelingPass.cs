using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds reusable, low-poly visual detail to the first The Pit scene.</summary>
[InitializeOnLoad]
public static class ThePitModelingPass
{
    private const string ScenePath = "Assets/Scenes/ThePit.unity";
    private const string MarkerName = "Modeling Pass V2";
    private const string MaterialFolder = "Assets/DevilsRoulette/Materials";

    static ThePitModelingPass()
    {
        EditorApplication.delayCall += ApplyIfNeeded;
    }

    private static void ApplyIfNeeded()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (sceneAsset == null)
            return;

        if (FileHasMarker())
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (GameObject.Find(MarkerName) != null)
            return;

        var table = GameObject.Find("Tabletop");
        var gun = GameObject.Find("Shotgun");
        var target = GameObject.Find("Target");
        if (table == null || gun == null || target == null)
        {
            Debug.LogWarning("The Pit modeling pass skipped: expected prototype objects were not found.");
            return;
        }

        EnsureFolder("Assets", "DevilsRoulette");
        EnsureFolder("Assets/DevilsRoulette", "Materials");
        Material steel = LoadOrCreate("Steel Brushed", new Color(0.20f, 0.23f, 0.26f), 0.82f, 0.55f);
        Material darkSteel = LoadOrCreate("Steel Dark", new Color(0.055f, 0.065f, 0.075f), 0.75f, 0.48f);
        Material brass = LoadOrCreate("Aged Brass", new Color(0.58f, 0.36f, 0.12f), 0.7f, 0.5f);
        Material grip = LoadOrCreate("Walnut Grip Detail", new Color(0.27f, 0.12f, 0.06f), 0.05f, 0.28f);
        Material red = LoadOrCreate("Pit Red Emission", new Color(0.55f, 0.025f, 0.018f), 0.1f, 0.3f, true);
        Material cream = LoadOrCreate("Pit Sign", new Color(0.72f, 0.62f, 0.44f), 0.1f, 0.3f, true);
        Material tabletopMaterial = GameObject.Find("Tabletop").GetComponent<Renderer>().sharedMaterial;

        AddTableDetails(table.transform, steel, darkSteel, brass, tabletopMaterial);
        AddShotgunDetails(gun.transform, steel, darkSteel, brass, grip);
        AddTargetDetails(target.transform, steel, darkSteel, red, cream);
        AddRoomDetails(steel, darkSteel, red, cream);

        var marker = new GameObject(MarkerName);
        marker.hideFlags = HideFlags.HideInHierarchy;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("The Pit low-poly modeling pass complete.");
    }

    private static void AddTableDetails(Transform table, Material steel, Material darkSteel, Material brass, Material wood)
    {
        Vector3 center = table.position;
        Detail(table, "Front Steel Apron", PrimitiveType.Cube, new Vector3(0f, -0.115f, -0.405f), new Vector3(1.43f, 0.13f, 0.035f), darkSteel);
        Detail(table, "Rear Steel Apron", PrimitiveType.Cube, new Vector3(0f, -0.115f, 0.405f), new Vector3(1.43f, 0.13f, 0.035f), darkSteel);
        Detail(table, "Left Steel Apron", PrimitiveType.Cube, new Vector3(-0.71f, -0.115f, 0f), new Vector3(0.035f, 0.13f, 0.78f), darkSteel);
        Detail(table, "Right Steel Apron", PrimitiveType.Cube, new Vector3(0.71f, -0.115f, 0f), new Vector3(0.035f, 0.13f, 0.78f), darkSteel);
        Detail(table, "Brass Front Inlay", PrimitiveType.Cube, new Vector3(0f, 0.061f, -0.43f), new Vector3(1.2f, 0.008f, 0.012f), brass);
        for (int i = -2; i <= 2; i++)
            Detail(table, "Tabletop Wood Inlay", PrimitiveType.Cube, new Vector3(i * 0.24f, 0.061f, 0.02f), new Vector3(0.012f, 0.004f, 0.73f), wood);

        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Detail(table, "Leg Foot", PrimitiveType.Cube, new Vector3(x * 0.58f, -0.72f, z * 0.36f), new Vector3(0.14f, 0.045f, 0.14f), steel);
            Detail(table, "Apron Rivet", PrimitiveType.Sphere, new Vector3(x * 0.64f, -0.11f, -0.43f), Vector3.one * 0.028f, brass);
        }
    }

    private static void AddShotgunDetails(Transform gun, Material steel, Material darkSteel, Material brass, Material wood)
    {
        // Reinforced receiver, pump grip, barrel hardware, and a readable front/rear sight profile.
        Detail(gun, "Receiver Left Side Plate", PrimitiveType.Cube, new Vector3(-0.078f, 0.005f, -0.04f), new Vector3(0.012f, 0.095f, 0.22f), steel);
        Detail(gun, "Receiver Right Side Plate", PrimitiveType.Cube, new Vector3(0.078f, 0.005f, -0.04f), new Vector3(0.012f, 0.095f, 0.22f), steel);
        Detail(gun, "Ejection Port", PrimitiveType.Cube, new Vector3(-0.086f, 0.027f, 0.015f), new Vector3(0.006f, 0.035f, 0.09f), darkSteel);
        Detail(gun, "Ejection Port Brass Lip", PrimitiveType.Cube, new Vector3(-0.091f, 0.027f, 0.015f), new Vector3(0.003f, 0.006f, 0.09f), brass);
        Detail(gun, "Bolt Handle", PrimitiveType.Cylinder, new Vector3(-0.12f, 0.045f, -0.08f), new Vector3(0.026f, 0.035f, 0.026f), steel, Quaternion.Euler(0f, 0f, 90f));
        Detail(gun, "Pump Fore End", PrimitiveType.Cube, new Vector3(0f, -0.045f, 0.16f), new Vector3(0.13f, 0.09f, 0.17f), wood);
        Detail(gun, "Pump Front Band", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0.255f), new Vector3(0.073f, 0.018f, 0.073f), brass, Quaternion.Euler(90f, 0f, 0f));
        Detail(gun, "Magazine Tube", PrimitiveType.Cylinder, new Vector3(0f, -0.075f, 0.25f), new Vector3(0.034f, 0.24f, 0.034f), darkSteel, Quaternion.Euler(90f, 0f, 0f));
        Detail(gun, "Magazine Cap", PrimitiveType.Cylinder, new Vector3(0f, -0.075f, 0.505f), new Vector3(0.044f, 0.018f, 0.044f), steel, Quaternion.Euler(90f, 0f, 0f));
        Detail(gun, "Top Sight Rail", PrimitiveType.Cube, new Vector3(0f, 0.085f, 0.01f), new Vector3(0.055f, 0.018f, 0.43f), darkSteel);
        Detail(gun, "Front Bead", PrimitiveType.Sphere, new Vector3(0f, 0.105f, 0.46f), Vector3.one * 0.025f, brass);
        Detail(gun, "Rear Sight", PrimitiveType.Cube, new Vector3(0f, 0.11f, -0.19f), new Vector3(0.075f, 0.055f, 0.025f), steel);
        Detail(gun, "Rear Sight Notch", PrimitiveType.Cube, new Vector3(0f, 0.139f, -0.19f), new Vector3(0.026f, 0.012f, 0.03f), darkSteel);
        Detail(gun, "Stock Butt Pad", PrimitiveType.Cube, new Vector3(0f, 0.005f, -0.425f), new Vector3(0.135f, 0.115f, 0.045f), darkSteel);
        Detail(gun, "Trigger", PrimitiveType.Cube, new Vector3(0f, -0.09f, -0.065f), new Vector3(0.018f, 0.065f, 0.018f), brass, Quaternion.Euler(-12f, 0f, 0f));
        Detail(gun, "Trigger Guard Front", PrimitiveType.Cube, new Vector3(0f, -0.145f, -0.01f), new Vector3(0.018f, 0.014f, 0.085f), steel);
        Detail(gun, "Trigger Guard Rear", PrimitiveType.Cube, new Vector3(0f, -0.145f, -0.12f), new Vector3(0.018f, 0.014f, 0.07f), steel);
        Detail(gun, "Trigger Guard Bottom", PrimitiveType.Cube, new Vector3(0f, -0.185f, -0.055f), new Vector3(0.018f, 0.07f, 0.014f), steel);

        for (int i = 0; i < 4; i++)
            Detail(gun, "Pump Grip Groove", PrimitiveType.Cube, new Vector3(-0.067f, -0.046f + i * 0.025f, 0.16f), new Vector3(0.004f, 0.008f, 0.13f), darkSteel);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
            Detail(gun, "Receiver Screw", PrimitiveType.Cylinder, new Vector3(x * 0.087f, 0.025f, -0.105f + (z + 1) * 0.045f), new Vector3(0.014f, 0.004f, 0.014f), brass, Quaternion.Euler(90f, 0f, 0f));

        var flash = new GameObject("Muzzle Flash");
        flash.transform.SetParent(gun, false);
        flash.transform.localPosition = new Vector3(0f, 0.015f, 0.5f);
        var particles = flash.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = false;
        main.duration = 0.12f;
        main.startLifetime = 0.09f;
        main.startSpeed = 0.7f;
        main.startSize = 0.12f;
        main.startColor = new Color(1f, 0.55f, 0.12f, 0.9f);
        main.maxParticles = 8;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.015f;

        var controller = gun.GetComponent<ShotgunController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("muzzleFlash").objectReferenceValue = particles;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddTargetDetails(Transform target, Material steel, Material darkSteel, Material red, Material cream)
    {
        var originalTargetRenderer = target.GetComponent<Renderer>();
        if (originalTargetRenderer != null)
            originalTargetRenderer.enabled = false;
        Detail(target, "Target Back Plate", PrimitiveType.Cube, new Vector3(0f, 0f, 0.095f), new Vector3(0.54f, 0.67f, 0.07f), darkSteel);
        Detail(target, "Target Steel Face", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.06f), new Vector3(0.52f, 0.018f, 0.52f), steel, Quaternion.Euler(90f, 0f, 0f));
        Detail(target, "Target Outer Ring", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.075f), new Vector3(0.39f, 0.014f, 0.39f), darkSteel, Quaternion.Euler(90f, 0f, 0f));
        Detail(target, "Target Scoring Ring", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.088f), new Vector3(0.29f, 0.012f, 0.29f), cream, Quaternion.Euler(90f, 0f, 0f));
        Detail(target, "Target Bullseye", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.1f), new Vector3(0.14f, 0.012f, 0.14f), red, Quaternion.Euler(90f, 0f, 0f));
        Detail(target, "Target Stand Pole", PrimitiveType.Cylinder, new Vector3(0f, -0.66f, 0.25f), new Vector3(0.045f, 0.45f, 0.045f), steel);
        Detail(target, "Target Stand Foot", PrimitiveType.Cube, new Vector3(0f, -1.06f, 0.25f), new Vector3(0.65f, 0.08f, 0.46f), darkSteel);
        Detail(target, "Stand Cross Brace", PrimitiveType.Cube, new Vector3(0f, -0.87f, 0.25f), new Vector3(0.54f, 0.045f, 0.08f), steel);
    }

    private static void AddRoomDetails(Material steel, Material darkSteel, Material red, Material cream)
    {
        CreateBox("Ceiling", new Vector3(0f, 4.05f, 1.1f), new Vector3(6f, 0.12f, 7f), darkSteel);
        for (int i = -2; i <= 2; i++)
        {
            float z = 0.2f + i * 0.55f;
            Detail(null, "Ceiling Pipe", PrimitiveType.Cylinder, new Vector3(-2.45f, 3.83f, z), new Vector3(0.055f, 0.44f, 0.055f), steel, Quaternion.Euler(0f, 0f, 90f));
            Detail(null, "Pipe Bracket", PrimitiveType.Cube, new Vector3(-2.45f, 3.72f, z), new Vector3(0.22f, 0.035f, 0.09f), darkSteel);
        }

        var sign = new GameObject("THE PIT Sign");
        sign.transform.position = new Vector3(0f, 3.35f, 4.48f);
        Detail(sign.transform, "Sign Backing", PrimitiveType.Cube, Vector3.zero, new Vector3(1.5f, 0.42f, 0.05f), darkSteel);
        Detail(sign.transform, "Sign Top Accent", PrimitiveType.Cube, new Vector3(0f, 0.215f, -0.03f), new Vector3(1.5f, 0.018f, 0.02f), red);
        var textObject = new GameObject("Sign Text");
        textObject.transform.SetParent(sign.transform, false);
        textObject.transform.localPosition = new Vector3(0f, -0.075f, -0.035f);
        var text = textObject.AddComponent<TextMesh>();
        text.text = "THE PIT";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 72;
        text.characterSize = 0.13f;
        text.color = new Color(0.88f, 0.74f, 0.52f);
        text.GetComponent<Renderer>().sharedMaterial = cream;

        for (int z = -1; z <= 1; z += 2)
        {
            Detail(null, "Wall Safety Stripe", PrimitiveType.Cube, new Vector3(2.91f, 0.34f, 1.1f + z * 1.2f), new Vector3(0.035f, 0.09f, 1.35f), red);
            Detail(null, "Wall Service Panel", PrimitiveType.Cube, new Vector3(-2.90f, 1.0f, 1.1f + z * 1.6f), new Vector3(0.045f, 0.68f, 0.58f), darkSteel);
        }
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.position = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static GameObject Detail(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 scale, Material material, Quaternion? localRotation = null)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = name;
        if (parent != null)
        {
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localRotation = localRotation ?? Quaternion.identity;
        }
        else
        {
            item.transform.position = localPosition;
            item.transform.rotation = localRotation ?? Quaternion.identity;
        }
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        return item;
    }

    private static Material LoadOrCreate(string name, Color color, float metallic, float smoothness, bool emission = false)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static bool FileHasMarker()
    {
        string absolutePath = System.IO.Path.GetFullPath(ScenePath);
        return System.IO.File.Exists(absolutePath) && System.IO.File.ReadAllText(absolutePath).Contains(MarkerName);
    }
}
