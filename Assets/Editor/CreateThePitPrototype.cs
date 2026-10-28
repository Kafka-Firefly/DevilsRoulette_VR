using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Builds the first playable room once the project imports this script.</summary>
[InitializeOnLoad]
public static class CreateThePitPrototype
{
    private const string ScenePath = "Assets/Scenes/ThePit.unity";
    private const string DoneKey = "DevilsRoulette.ThePitPrototypeCreated";
    private const string RigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

    static CreateThePitPrototype()
    {
        EditorApplication.delayCall += CreateOnce;
    }

    [MenuItem("Devils Roulette/Create The Pit Prototype")]
    private static void CreateFromMenu()
    {
        BuildScene();
    }

    private static void CreateOnce()
    {
        if (SessionState.GetBool(DoneKey, false) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            return;

        BuildScene();
    }

    private static void BuildScene()
    {
        SessionState.SetBool(DoneKey, true);
        try
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMaterials();

            var manager = new GameObject("XR Interaction Manager");
            manager.AddComponent<XRInteractionManager>();

            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (rigPrefab != null)
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
                rig.name = "XR Origin (XR Rig)";
                rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("Starter Assets XR Origin prefab was not found. Add it to ThePit manually.");
            }

            Material wall = MakeMaterial("Pit Wall", new Color(0.035f, 0.04f, 0.05f));
            Material floor = MakeMaterial("Pit Floor", new Color(0.07f, 0.075f, 0.085f));
            Material table = MakeMaterial("Table", new Color(0.16f, 0.09f, 0.055f));
            Material metal = MakeMaterial("Gun Metal", new Color(0.12f, 0.14f, 0.16f), 0.72f, 0.55f);
            Material wood = MakeMaterial("Gun Grip", new Color(0.22f, 0.10f, 0.055f));
            Material target = MakeMaterial("Target", new Color(0.68f, 0.68f, 0.62f));

            CreateBox("Floor", new Vector3(0f, -0.06f, 1.1f), new Vector3(6f, 0.12f, 7f), floor);
            CreateBox("Back Wall", new Vector3(0f, 2f, 4.6f), new Vector3(6f, 4f, 0.15f), wall);
            CreateBox("Left Wall", new Vector3(-3f, 2f, 1.1f), new Vector3(0.15f, 4f, 7f), wall);
            CreateBox("Right Wall", new Vector3(3f, 2f, 1.1f), new Vector3(0.15f, 4f, 7f), wall);
            CreateBox("Front Wall", new Vector3(0f, 2f, -2.4f), new Vector3(6f, 4f, 0.15f), wall);

            CreateBox("Tabletop", new Vector3(0f, 0.78f, 1f), new Vector3(1.45f, 0.12f, 0.95f), table);
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                CreateBox("Table Leg", new Vector3(x * 0.58f, 0.37f, 1f + z * 0.36f), new Vector3(0.09f, 0.72f, 0.09f), table);

            GameObject targetObject = CreateBox("Target", new Vector3(0f, 1.1f, 3.25f), new Vector3(0.42f, 0.55f, 0.12f), target);
            targetObject.AddComponent<TargetHitReceiver>();

            CreateShotgun(new Vector3(0f, 0.93f, 0.88f), metal, wood);
            CreateSpotlight();

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.11f, 0.14f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.025f, 0.03f, 0.04f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.025f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("The Pit prototype created at " + ScenePath + ". The original template scene is unchanged.");
        }
        catch (System.Exception exception)
        {
            SessionState.SetBool(DoneKey, false);
            Debug.LogException(exception);
        }
    }

    private static void CreateMaterials()
    {
        EnsureFolder("Assets", "DevilsRoulette");
        EnsureFolder("Assets/DevilsRoulette", "Materials");
    }

    private static Material MakeMaterial(string name, Color color, float metallic = 0f, float smoothness = 0.25f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 size, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static void CreateShotgun(Vector3 position, Material metal, Material wood)
    {
        var gun = new GameObject("Shotgun");
        gun.transform.position = position;
        var bodyCollider = gun.AddComponent<BoxCollider>();
        bodyCollider.size = new Vector3(0.15f, 0.14f, 0.62f);
        var body = gun.AddComponent<Rigidbody>();
        body.mass = 2.4f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        var grab = gun.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = true;

        AddGunPart(gun.transform, "Receiver", PrimitiveType.Cube, new Vector3(0f, 0f, -0.03f), new Vector3(0.15f, 0.14f, 0.32f), metal);
        AddGunPart(gun.transform, "Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0.25f), new Vector3(0.055f, 0.24f, 0.055f), metal, Quaternion.Euler(90f, 0f, 0f));
        AddGunPart(gun.transform, "Stock", PrimitiveType.Cube, new Vector3(0f, 0f, -0.30f), new Vector3(0.12f, 0.10f, 0.25f), wood);
        AddGunPart(gun.transform, "Grip", PrimitiveType.Cube, new Vector3(0f, -0.12f, -0.09f), new Vector3(0.09f, 0.19f, 0.10f), wood, Quaternion.Euler(-12f, 0f, 0f));

        var muzzle = new GameObject("BarrelTip");
        muzzle.transform.SetParent(gun.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.015f, 0.50f);

        var audio = gun.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        audio.minDistance = 0.7f;
        audio.maxDistance = 14f;
        var controller = gun.AddComponent<ShotgunController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("barrelTip").objectReferenceValue = muzzle.transform;
        serialized.FindProperty("audioSource").objectReferenceValue = audio;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddGunPart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 scale, Material material, Quaternion? localRotation = null)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation ?? Quaternion.identity;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    private static void CreateSpotlight()
    {
        var lightObject = new GameObject("Table Spotlight");
        lightObject.transform.SetPositionAndRotation(new Vector3(0f, 3.2f, 1f), Quaternion.Euler(90f, 0f, 0f));
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(1f, 0.82f, 0.64f);
        light.intensity = 950f;
        light.range = 6f;
        light.spotAngle = 62f;
        light.shadows = LightShadows.Soft;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path != ScenePath)
                scenes.Add(scene);
        }
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
