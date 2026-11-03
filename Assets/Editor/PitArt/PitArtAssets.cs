using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Bakes the procedural textures and URP materials used by The Pit art pass.</summary>
internal sealed class PitMaterials
{
    internal Material ConcreteFloor;
    internal Material ConcreteWall;
    internal Material ConcreteDark;
    internal Material Steel;
    internal Material SteelDark;
    internal Material Brass;
    internal Material Cream;
    internal Material Wood;
    internal Material Felt;
    internal Material PaintRed;
    internal Material Grout;
    internal Material GlowWarm;
    internal Material GlowRed;
    internal Material GlowCool;
    internal Material SignPaint;
    internal Material Dust;
    internal Material MuzzleFlash;
}

internal static class PitArtAssets
{
    internal const string Root = "Assets/DevilsRoulette/Art";
    internal const string TexDir = Root + "/Textures";
    internal const string MatDir = Root + "/Materials";
    internal const string PostDir = Root + "/Post";

    internal static void EnsureFolders()
    {
        EnsureFolder("Assets", "DevilsRoulette");
        EnsureFolder("Assets/DevilsRoulette", "Art");
        EnsureFolder(Root, "Textures");
        EnsureFolder(Root, "Materials");
        EnsureFolder(Root, "Post");
    }

    internal static PitMaterials BuildAll()
    {
        EnsureFolders();

        BakeConcrete("Floor", 3.17f, new Color(0.80f, 0.79f, 0.76f), 0.66f, out Texture2D concreteA, out Texture2D concreteN);
        BakeSteel(5.51f, out Texture2D steelA, out Texture2D steelN);
        BakeWood(8.23f, out Texture2D woodA, out Texture2D woodN);
        Texture2D dot = BakeDot();

        var m = new PitMaterials
        {
            ConcreteFloor = Lit("Concrete Floor", concreteA, concreteN, new Color(0.54f, 0.53f, 0.50f), 0f, 0.16f, new Vector2(3.1f, 3.6f), 0.40f),
            ConcreteWall = Lit("Concrete Wall", concreteA, concreteN, new Color(0.43f, 0.42f, 0.41f), 0f, 0.18f, new Vector2(2.0f, 1.4f), 0.28f),
            ConcreteDark = Lit("Concrete Dark", concreteA, concreteN, new Color(0.21f, 0.21f, 0.22f), 0f, 0.14f, new Vector2(3.1f, 1.6f), 0.30f),
            Steel = Lit("Steel", steelA, steelN, new Color(0.45f, 0.46f, 0.47f), 0.40f, 0.28f, new Vector2(2f, 2f), 0.45f),
            SteelDark = Lit("Steel Dark", steelA, steelN, new Color(0.14f, 0.145f, 0.15f), 0.30f, 0.22f, new Vector2(2f, 2f), 0.40f),
            Brass = Lit("Brass", steelA, steelN, new Color(0.50f, 0.37f, 0.17f), 0.55f, 0.38f, Vector2.one),
            Cream = Lit("Cream Paint", concreteA, concreteN, new Color(0.70f, 0.66f, 0.56f), 0f, 0.32f, Vector2.one),
            Wood = Lit("Walnut", woodA, woodN, new Color(0.30f, 0.19f, 0.11f), 0f, 0.06f, new Vector2(1.6f, 1.0f), 0.35f),
            Felt = Lit("Burgundy Felt", concreteA, concreteN, new Color(0.28f, 0.035f, 0.055f), 0f, 0.04f, new Vector2(3f, 3f), 0.10f),
            PaintRed = Lit("Red Paint", concreteA, concreteN, new Color(0.40f, 0.055f, 0.040f), 0.05f, 0.20f, Vector2.one),
            Grout = Lit("Grout", concreteA, concreteN, new Color(0.055f, 0.055f, 0.060f), 0f, 0.10f, new Vector2(4f, 4f)),
            GlowWarm = Emissive("Glow Warm", new Color(1f, 0.80f, 0.52f), 4.5f),
            GlowRed = Emissive("Glow Red", new Color(1f, 0.16f, 0.09f), 4f),
            GlowCool = Emissive("Glow Cool", new Color(0.55f, 0.72f, 1f), 2.5f),
            SignPaint = Emissive("Sign Paint", new Color(0.92f, 0.84f, 0.62f), 2.2f),
            Dust = Particle("Dust Mote", dot, new Color(1f, 0.92f, 0.80f, 0.5f), true),
            MuzzleFlash = Particle("Muzzle Flash", dot, new Color(1f, 0.76f, 0.42f, 1f), true),
        };
        return m;
    }

    // ---------------------------------------------------------------- textures

    private static float Fbm(float x, float y, float seed, int octaves)
    {
        float sum = 0f;
        float amp = 0.5f;
        float freq = 1f;
        for (int i = 0; i < octaves; i++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq + seed * 13.37f, y * freq + seed * 7.91f);
            freq *= 2.03f;
            amp *= 0.5f;
        }
        return sum;
    }

    private static void BakeConcrete(string key, float seed, Color tint, float brightness, out Texture2D albedo, out Texture2D normal)
    {
        const int size = 512;
        var colors = new Color[size * size];
        var heights = new float[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float big = Fbm(u * 3.2f, v * 3.2f, seed, 4);
                float mid = Fbm(u * 12f, v * 12f, seed + 7.3f, 3);
                float fine = Mathf.PerlinNoise(u * 170f + seed, v * 170f + seed * 2.1f);
                float stain = Mathf.Clamp01(Fbm(u * 2.4f, v * 2.4f, seed + 31.7f, 3) - 0.50f) * 2.4f;

                float value = brightness * (0.80f + 0.36f * big) * (0.93f + 0.14f * mid) * (0.96f + 0.08f * fine) - stain * 0.20f;
                value = Mathf.Clamp01(value);
                colors[y * size + x] = new Color(tint.r * value, tint.g * value, tint.b * value, 1f);
                heights[y * size + x] = big * 0.5f + mid * 0.3f + fine * 0.2f - stain * 0.25f;
            }
        }

        albedo = ImportAlbedo("T_Concrete_" + key, colors, size);
        normal = ImportNormal("T_Concrete_" + key + "_N", heights, size, 1.1f);
    }

    private static void BakeSteel(float seed, out Texture2D albedo, out Texture2D normal)
    {
        const int size = 512;
        var colors = new Color[size * size];
        var heights = new float[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float brushed = Fbm(u * 240f, v * 7f, seed, 2);
                float blotch = Fbm(u * 5f, v * 5f, seed + 13f, 3);
                float scratch = Mathf.PerlinNoise(u * 380f, v * 22f + seed);

                float value = 0.62f + 0.16f * brushed + 0.10f * blotch;
                if (scratch > 0.78f)
                    value -= 0.14f;
                value = Mathf.Clamp01(value);

                colors[y * size + x] = new Color(value, value, value, 1f);
                heights[y * size + x] = brushed * 0.5f + blotch * 0.3f + scratch * 0.2f;
            }
        }

        albedo = ImportAlbedo("T_Steel", colors, size);
        normal = ImportNormal("T_Steel_N", heights, size, 1.1f);
    }

    private static void BakeWood(float seed, out Texture2D albedo, out Texture2D normal)
    {
        const int size = 512;
        var colors = new Color[size * size];
        var heights = new float[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float warp = Fbm(u * 4f, v * 2f, seed, 3);
                float line = Mathf.Abs(Mathf.Sin((v * 22f + warp * 3.5f) * Mathf.PI));
                float grain = Mathf.Pow(1f - line, 4f);
                float fibre = Fbm(u * 40f, v * 10f, seed + 5f, 2);

                float value = Mathf.Clamp01(0.78f + 0.16f * fibre - grain * 0.22f);
                colors[y * size + x] = new Color(value, value, value, 1f);
                heights[y * size + x] = grain * 0.6f + fibre * 0.2f;
            }
        }

        albedo = ImportAlbedo("T_Wood", colors, size);
        normal = ImportNormal("T_Wood_N", heights, size, 1.8f);
    }

    private static Texture2D BakeDot()
    {
        const int size = 64;
        var colors = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Mathf.SmoothStep(0.15f, 0.95f, r);
                colors[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
        }

        return ImportAlbedo("T_Dot", colors, size, false);
    }

    private static Texture2D ImportAlbedo(string name, Color[] pixels, int size, bool repeat = true)
    {
        string assetPath = TexDir + "/" + name + ".png";
        WritePng(assetPath, pixels, size);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static Texture2D ImportNormal(string name, float[] heights, int size, float strength)
    {
        var colors = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float left = heights[y * size + Mathf.Max(0, x - 1)];
                float right = heights[y * size + Mathf.Min(size - 1, x + 1)];
                float down = heights[Mathf.Max(0, y - 1) * size + x];
                float up = heights[Mathf.Min(size - 1, y + 1) * size + x];

                var n = new Vector3(-(right - left) * strength, -(up - down) * strength, 1f);
                n.Normalize();
                colors[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        }

        string assetPath = TexDir + "/" + name + ".png";
        WritePng(assetPath, colors, size);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.NormalMap;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static void WritePng(string assetPath, Color[] pixels, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        texture.SetPixels(pixels);
        texture.Apply();
        byte[] png = texture.EncodeToPNG();
        Object.DestroyImmediate(texture);

        string fullPath = Path.Combine(ProjectRoot, assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ProjectRoot);
        File.WriteAllBytes(fullPath, png);
    }

    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

    // ---------------------------------------------------------------- materials

    private static Material Lit(string name, Texture2D albedo, Texture2D normal, Color tint, float metallic, float smoothness, Vector2 tiling, float bumpScale = 1f)
    {
        Material material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
        SetTexture(material, "_BaseMap", albedo);
        SetTexture(material, "_BumpMap", normal);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color")) material.color = tint;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", bumpScale);
        if (normal != null)
            material.EnableKeyword("_NORMALMAP");
        material.SetTextureScale("_BaseMap", tiling);
        material.SetTextureScale("_BumpMap", tiling);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Emissive(string name, Color color, float strength)
    {
        Material material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color * 0.18f);
        if (material.HasProperty("_Color")) material.color = color * 0.18f;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.35f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Particle(string name, Texture2D texture, Color tint, bool additive)
    {
        Material material = LoadOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
        SetTexture(material, "_BaseMap", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.SrcAlpha : UnityEngine.Rendering.BlendMode.SrcAlpha));
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material LoadOrCreate(string name, string shaderName)
    {
        string path = MatDir + "/M_" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "M_" + name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        Shader target = Shader.Find(shaderName);
        if (target != null && material.shader != target)
            material.shader = target;
        return material;
    }

    private static void SetTexture(Material material, string property, Texture texture)
    {
        if (texture != null && material.HasProperty(property))
            material.SetTexture(property, texture);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
