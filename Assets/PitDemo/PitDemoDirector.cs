using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Deterministic frame renderer for the demo video. Drives the real gameplay systems from Edit mode,
/// one frame at a time, so the capture never depends on the editor's play-mode frame loop.
/// </summary>
public sealed class PitDemoDirector : MonoBehaviour
{
    public const string DefaultOutput = @"C:\Users\faifa\Documents\deepseek-harness\default-workspace\pit-art\video\frames";

    private const int Width = 1600;
    private const int Height = 900;
    private const float Fps = 30f;
    private const int LiveShotIndex = 2;
    private const float HitFlashSeconds = 0.7f;

    private static readonly float[] ShotTimes = { 58f, 61.5f, 65f, 74f, 77.5f, 81f };

    private struct CamKey
    {
        public float t;
        public Vector3 pos;
        public Vector3 look;
        public float fov;
    }

    private static readonly CamKey[] CameraKeys =
    {
        new CamKey { t = 0f, pos = new Vector3(-2.35f, 1.02f, -1.95f), look = new Vector3(0.35f, 1.15f, 2.30f), fov = 52f },
        new CamKey { t = 14f, pos = new Vector3(-1.35f, 1.42f, -1.05f), look = new Vector3(0.10f, 1.25f, 2.70f), fov = 58f },
        new CamKey { t = 24f, pos = new Vector3(-0.05f, 1.62f, 0.02f), look = new Vector3(0f, 1.05f, 2.20f), fov = 68f },
        new CamKey { t = 32f, pos = new Vector3(0.15f, 1.52f, 0.10f), look = new Vector3(0f, 0.92f, 1.05f), fov = 50f },
        new CamKey { t = 40f, pos = new Vector3(0.52f, 1.34f, 0.52f), look = new Vector3(-0.02f, 0.95f, 1.00f), fov = 46f },
        new CamKey { t = 47f, pos = new Vector3(0.42f, 1.44f, 0.50f), look = new Vector3(0.02f, 1.04f, 1.05f), fov = 48f },
        new CamKey { t = 54f, pos = new Vector3(0.05f, 1.62f, 0.02f), look = new Vector3(0f, 1.18f, 2.40f), fov = 70f },
        new CamKey { t = 60f, pos = new Vector3(0f, 1.62f, 0.03f), look = new Vector3(0f, 1.16f, 3.10f), fov = 68f },
        new CamKey { t = 84f, pos = new Vector3(0f, 1.63f, 0.03f), look = new Vector3(0f, 1.16f, 3.25f), fov = 66f },
        new CamKey { t = 92f, pos = new Vector3(0.85f, 1.52f, 0.25f), look = new Vector3(0f, 1.05f, 1.85f), fov = 56f },
        new CamKey { t = 102f, pos = new Vector3(0.55f, 1.35f, 0.15f), look = new Vector3(-0.05f, 0.92f, 0.95f), fov = 45f },
        new CamKey { t = 112f, pos = new Vector3(0f, 1.55f, 2.35f), look = new Vector3(0f, 1.15f, 3.30f), fov = 42f },
        new CamKey { t = 122f, pos = new Vector3(-1.70f, 2.05f, -1.15f), look = new Vector3(0.45f, 1.05f, 2.60f), fov = 58f },
        new CamKey { t = 132f, pos = new Vector3(2.45f, 2.35f, -1.95f), look = new Vector3(-0.50f, 0.90f, 2.20f), fov = 60f },
        new CamKey { t = 145f, pos = new Vector3(2.45f, 2.35f, -1.95f), look = new Vector3(-0.50f, 0.90f, 2.20f), fov = 60f },
    };

    private Camera demoCamera;
    private RenderTexture rt;
    private Texture2D tex;
    private Transform gun;
    private ShotgunController shotgun;
    private TargetHitReceiver target;
    private Light muzzleLight;
    private Transform barrelTip;
    private Transform flashCore;
    private Transform impactCore;
    private Renderer flashRenderer;
    private Renderer impactRenderer;

    private Vector3 gunRestPos;
    private Quaternion gunRestRot;
    private readonly bool[] shotsHappened = new bool[ShotTimes.Length];
    private readonly StringBuilder events = new StringBuilder();
    private float lastShotTime = -100f;
    private float shake;
    private float demoTime;
    private float muzzleTimer;
    private float impactTimer;
    private float hitFlashAt = -100f;
    private int framesWritten;
    private float duration = 140f;
    private string outputRoot = DefaultOutput;

    public void Configure(float seconds, string root)
    {
        duration = seconds;
        outputRoot = root;
    }

    /// <summary>Renders the whole take synchronously. Returns the number of frames written.</summary>
    public int RunCapture()
    {
        Setup();
        Directory.CreateDirectory(outputRoot);

        int totalFrames = Mathf.RoundToInt(duration * Fps);
        Debug.Log($"[PitDemo] rendering {totalFrames} frames at {Fps} fps -> {outputRoot}");

        for (int frame = 0; frame < totalFrames; frame++)
        {
            demoTime = frame / Fps;
            ApplyCamera(demoTime);
            ApplyGun(demoTime);
            ApplyShots(demoTime);
            ApplyEffects(1f / Fps);
            Capture(frame);

            if (frame % 300 == 0)
                Debug.Log($"[PitDemo] frame {frame}/{totalFrames}");
        }

        WriteEvents();
        Debug.Log($"[PitDemo] done: {framesWritten} frames");
        return framesWritten;
    }

    /// <summary>Renders a handful of key moments for quick review.</summary>
    public int RunProbe(float[] times)
    {
        Setup();
        Directory.CreateDirectory(outputRoot);

        for (int i = 0; i < times.Length; i++)
        {
            demoTime = times[i];
            ApplyCamera(demoTime);
            ApplyGun(demoTime);
            ApplyShots(demoTime);
            ApplyEffects(1f / Fps);
            SaveFrame("probe_" + i.ToString("D2") + "_" + Mathf.RoundToInt(times[i]));
        }

        Debug.Log("[PitDemo] probe done: " + times.Length + " frames");
        return times.Length;
    }

    private void Setup()
    {
        GameObject gunObject = GameObject.Find("Shotgun");
        if (gunObject == null)
            throw new InvalidOperationException("Shotgun not found in scene.");

        gun = gunObject.transform;
        gunRestPos = gun.position;
        gunRestRot = gun.rotation;

        // The runtime scripts normally configure themselves in Awake; in Edit mode we do it explicitly.
        var display = gunObject.GetComponent<RouletteRoundDisplay>();
        if (display == null)
            display = gunObject.AddComponent<RouletteRoundDisplay>();
        display.Build();

        shotgun = gunObject.GetComponent<ShotgunController>();
        shotgun.ShotResolved += OnShotResolved;
        shotgun.Initialize();
        shotgun.ForceLiveChamber(LiveShotIndex);

        GameObject targetObject = GameObject.Find("Target");
        if (targetObject != null)
            target = targetObject.GetComponent<TargetHitReceiver>();

        barrelTip = gunObject.transform.Find("BarrelTip");

        var lightObject = new GameObject("~Demo Muzzle Light");
        muzzleLight = lightObject.AddComponent<Light>();
        muzzleLight.type = LightType.Point;
        muzzleLight.color = new Color(1f, 0.72f, 0.36f);
        muzzleLight.range = 4.5f;
        muzzleLight.intensity = 0f;
        muzzleLight.shadows = LightShadows.None;

        Material glow = FindGlowMaterial();
        flashCore = MakeFlashSphere("~Demo Muzzle Flash", glow, 0.075f, out flashRenderer);
        impactCore = MakeFlashSphere("~Demo Impact Flash", glow, 0.05f, out impactRenderer);

        var cameraObject = new GameObject("~Demo Camera");
        demoCamera = cameraObject.AddComponent<Camera>();
        demoCamera.clearFlags = CameraClearFlags.SolidColor;
        demoCamera.backgroundColor = new Color(0.004f, 0.004f, 0.006f, 1f);
        demoCamera.nearClipPlane = 0.02f;
        demoCamera.farClipPlane = 100f;
        demoCamera.allowHDR = true;

        UniversalAdditionalCameraData data = demoCamera.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.Create();
        demoCamera.targetTexture = rt;
        tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
    }

    private static Material FindGlowMaterial()
    {
        GameObject bulb = GameObject.Find("Lamp Bulb");
        if (bulb != null)
        {
            var renderer = bulb.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
                return renderer.sharedMaterial;
        }

        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(1f, 0.8f, 0.45f));
        return material;
    }

    private static Transform MakeFlashSphere(string name, Material material, float radius, out Renderer renderer)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
        sphere.transform.localScale = Vector3.one * (radius * 2f);
        renderer = sphere.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
        sphere.SetActive(false);
        return sphere.transform;
    }

    private void ApplyCamera(float t)
    {
        GetCameraPose(t, out Vector3 position, out Vector3 look, out float fov);

        position += new Vector3(
            Mathf.PerlinNoise(t * 0.37f, 0.31f) - 0.5f,
            Mathf.PerlinNoise(0.73f, t * 0.33f) - 0.5f,
            Mathf.PerlinNoise(t * 0.29f, 0.91f) - 0.5f) * 0.007f;

        if (shake > 0.001f)
        {
            position += new Vector3(
                Mathf.PerlinNoise(t * 31f, 3.1f) - 0.5f,
                Mathf.PerlinNoise(4.7f, t * 29f) - 0.5f, 0f) * shake;
        }

        demoCamera.transform.position = position;
        demoCamera.transform.LookAt(look + new Vector3(
            Mathf.PerlinNoise(t * 0.21f, 1.7f) - 0.5f,
            Mathf.PerlinNoise(2.3f, t * 0.19f) - 0.5f, 0f) * 0.02f);
        demoCamera.fieldOfView = fov;
    }

    private static void GetCameraPose(float t, out Vector3 position, out Vector3 look, out float fov)
    {
        if (t <= CameraKeys[0].t)
        {
            position = CameraKeys[0].pos;
            look = CameraKeys[0].look;
            fov = CameraKeys[0].fov;
            return;
        }

        for (int i = 0; i < CameraKeys.Length - 1; i++)
        {
            CamKey a = CameraKeys[i];
            CamKey b = CameraKeys[i + 1];
            if (t > b.t)
                continue;

            float u = Mathf.Clamp01((t - a.t) / Mathf.Max(0.001f, b.t - a.t));
            u = u * u * (3f - 2f * u);
            position = Vector3.Lerp(a.pos, b.pos, u);
            look = Vector3.Lerp(a.look, b.look, u);
            fov = Mathf.Lerp(a.fov, b.fov, u);
            return;
        }

        CamKey last = CameraKeys[CameraKeys.Length - 1];
        position = last.pos;
        look = last.look;
        fov = last.fov;
    }

    private void ApplyGun(float t)
    {
        GetHeldPose(out Vector3 heldPos, out Quaternion heldRot);

        float lift = Mathf.Clamp01(Mathf.InverseLerp(42f, 48f, t));
        lift = lift * lift * (3f - 2f * lift);
        float lower = Mathf.Clamp01(Mathf.InverseLerp(86f, 96f, t));
        float hold = lift * (1f - lower);

        Vector3 position = Vector3.Lerp(gunRestPos, heldPos, hold);
        Quaternion rotation = Quaternion.Slerp(gunRestRot, heldRot, hold);

        float sinceShot = t - lastShotTime;
        if (sinceShot >= 0f && sinceShot < 0.22f)
        {
            float k = 1f - sinceShot / 0.22f;
            k *= k;
            position -= rotation * Vector3.forward * 0.035f * k;
            rotation = Quaternion.AngleAxis(-5.5f * k, demoCamera.transform.right) * rotation;
        }

        gun.position = position;
        gun.rotation = rotation;

        if (barrelTip != null)
        {
            muzzleLight.transform.position = barrelTip.position + barrelTip.forward * 0.06f;
            flashCore.position = barrelTip.position + barrelTip.forward * 0.07f;
        }
    }

    private void GetHeldPose(out Vector3 position, out Quaternion rotation)
    {
        Transform cam = demoCamera.transform;
        position = cam.position + cam.forward * 0.58f + cam.right * 0.19f + cam.up * -0.19f;

        Vector3 aim = target != null
            ? target.transform.position + Vector3.up * 0.02f
            : cam.position + cam.forward * 6f;

        rotation = Quaternion.LookRotation(aim - position, Vector3.up) * Quaternion.Euler(2f, 0f, -9f);
    }

    private void ApplyShots(float t)
    {
        for (int i = 0; i < ShotTimes.Length; i++)
        {
            if (shotsHappened[i] || t < ShotTimes[i])
                continue;

            shotsHappened[i] = true;
            shotgun.Fire();
            lastShotTime = t;
            muzzleLight.intensity = 30f;
            muzzleTimer = 0.09f;
            shake = 0.012f;
        }
    }

    private void ApplyEffects(float dt)
    {
        muzzleTimer = Mathf.Max(0f, muzzleTimer - dt);
        impactTimer = Mathf.Max(0f, impactTimer - dt);
        muzzleLight.intensity = Mathf.Lerp(muzzleLight.intensity, 0f, 0.55f);
        shake = Mathf.Lerp(shake, 0f, 0.35f);

        if (flashCore != null)
        {
            flashCore.gameObject.SetActive(muzzleTimer > 0f);
            float k = muzzleTimer / 0.09f;
            flashCore.localScale = Vector3.one * Mathf.Lerp(0.06f, 0.16f, k);
        }

        if (impactCore != null)
        {
            impactCore.gameObject.SetActive(impactTimer > 0f);
            float k = impactTimer / 0.16f;
            impactCore.localScale = Vector3.one * Mathf.Lerp(0.04f, 0.12f, k);
        }

        // The hit flash normally clears from Update(), which does not run during the capture.
        if (target != null && demoTime - hitFlashAt > HitFlashSeconds && hitFlashAt > 0f)
        {
            target.ClearHit();
            hitFlashAt = -100f;
        }
    }

    private void OnShotResolved(bool liveRound, bool targetHit)
    {
        events.AppendFormat(CultureInfo.InvariantCulture, "{0:F3},{1},{2}\n",
            demoTime, liveRound ? 1 : 0, targetHit ? 1 : 0);

        if (!liveRound)
            return;

        shake = 0.03f;
        if (!targetHit)
            return;

        events.AppendFormat(CultureInfo.InvariantCulture, "{0:F3},hit,1\n", demoTime);
        hitFlashAt = demoTime;
        Vector3 point = target.transform.position;
        if (Physics.Raycast(barrelTip.position, barrelTip.forward, out RaycastHit hit, 20f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
        }

        impactCore.position = point - target.transform.forward * 0.05f;
        impactTimer = 0.16f;
    }

    private void Capture(int frame)
    {
        SaveFrame("frame_" + frame.ToString("D5"));
    }

    private void SaveFrame(string name)
    {
        demoCamera.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
        tex.Apply(false);
        RenderTexture.active = null;

        string path = Path.Combine(outputRoot, name + ".jpg");
        File.WriteAllBytes(path, tex.EncodeToJPG(92));
        framesWritten++;
    }

    private void WriteEvents()
    {
        string path = Path.Combine(outputRoot, "events.csv");
        File.WriteAllText(path, "time,live,targetHit\n" + events);
        Debug.Log("[PitDemo] events -> " + path);
    }
}
