using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Renders fixed camera angles of The Pit so the art pass can be reviewed without the editor UI.</summary>
internal static class PitArtPreview
{
    private static readonly string OutDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ArtPreviews");
    private const int Width = 1600;
    private const int Height = 900;

    private struct Shot
    {
        public string Name;
        public Vector3 Eye;
        public Vector3 Look;
        public float Fov;
    }

    internal static void RenderAll(string prefix)
    {
        Directory.CreateDirectory(OutDir);

        var shots = new[]
        {
            new Shot { Name = "hmd", Eye = new Vector3(0f, 1.62f, 0.0f), Look = new Vector3(0f, 0.98f, 2.7f), Fov = 70f },
            new Shot { Name = "wide", Eye = new Vector3(2.45f, 2.35f, -1.95f), Look = new Vector3(-0.5f, 0.9f, 2.2f), Fov = 60f },
            new Shot { Name = "table", Eye = new Vector3(0.72f, 1.42f, 0.02f), Look = new Vector3(-0.05f, 0.90f, 1.25f), Fov = 48f },
            new Shot { Name = "gun", Eye = new Vector3(0.62f, 1.26f, 0.40f), Look = new Vector3(-0.02f, 0.94f, 0.92f), Fov = 42f },
            new Shot { Name = "target", Eye = new Vector3(0.05f, 1.45f, 2.10f), Look = new Vector3(0f, 1.15f, 3.35f), Fov = 52f },
            new Shot { Name = "corner", Eye = new Vector3(-2.30f, 1.75f, -1.80f), Look = new Vector3(0.6f, 1.0f, 2.6f), Fov = 62f }
        };

        foreach (Shot shot in shots)
            Render(prefix, shot);

        Debug.Log("Pit preview frames written to " + OutDir);
    }

    private static void Render(string prefix, Shot shot)
    {
        var go = new GameObject("PitPreviewCamera");
        var camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.01f, 0.015f, 1f);
        camera.fieldOfView = shot.Fov;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 80f;
        camera.allowHDR = true;
        camera.transform.position = shot.Eye;
        camera.transform.LookAt(shot.Look);

        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.Create();
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;

        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        tex.Apply();

        string path = Path.Combine(OutDir, prefix + "_" + shot.Name + ".png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log("Preview written: " + path);

        RenderTexture.active = null;
        camera.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
    }
}
