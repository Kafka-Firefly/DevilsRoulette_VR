using System.Collections.Generic;
using UnityEngine;

/// <summary>Hit response for the prototype target: ring flash, bullet mark, and a short impact puff.</summary>
public sealed class TargetHitReceiver : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color hitColor = new Color(0.85f, 0.08f, 0.04f);
    [SerializeField] private float resetAfterSeconds = 0.7f;
    [SerializeField] private int maxMarks = 18;

    // Marks sit just in front of the bullseye plate, which is the front-most ring surface.
    private const float FaceZ = -0.042f;

    private MaterialPropertyBlock propertyBlock;
    private Color[] originalColors;
    private float resetAt = -1f;
    private readonly Queue<GameObject> marks = new Queue<GameObject>();
    private ParticleSystem impactTemplate;
    private Material markMaterial;
    private AudioSource impactSource;
    private AudioClip impactClip;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>Caches renderers and the impact template. Awake calls this; the demo capture calls it in Edit mode.</summary>
    public void Initialize()
    {
        if (propertyBlock != null)
            return;

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(false);

        propertyBlock = new MaterialPropertyBlock();
        originalColors = new Color[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer targetRenderer = targetRenderers[i];
            if (targetRenderer == null)
                continue;

            Material material = targetRenderer.sharedMaterial;
            originalColors[i] = material != null && material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material != null && material.HasProperty("_Color")
                    ? material.color
                    : Color.white;
        }

        GameObject flash = GameObject.Find("Muzzle Flash");
        if (flash != null)
            impactTemplate = flash.GetComponent<ParticleSystem>();

        var audioObject = new GameObject("Impact Audio");
        audioObject.transform.SetParent(transform, false);
        impactSource = audioObject.AddComponent<AudioSource>();
        impactSource.playOnAwake = false;
        impactSource.spatialBlend = 1f;
        impactSource.minDistance = 0.6f;
        impactSource.maxDistance = 14f;
        impactClip = BuildImpactClip();
    }

    private void Update()
    {
        if (resetAt < 0f || Time.time < resetAt)
            return;

        ClearHit();
    }

    public void OnShot()
    {
        OnShot(transform.position);
    }

    public void OnShot(Vector3 worldPoint)
    {
        if (propertyBlock == null)
            Initialize();

        foreach (Renderer targetRenderer in targetRenderers)
            SetColor(targetRenderer, hitColor);

        resetAt = Time.time + resetAfterSeconds;
        AddMark(worldPoint);
        EmitImpact(worldPoint);
        PlayImpactSound(worldPoint);
        Debug.Log("Target hit.", this);
    }

    /// <summary>Clears the red flash immediately (also used when the demo resets a beat).</summary>
    public void ClearHit()
    {
        for (int i = 0; i < targetRenderers.Length; i++)
            SetColor(targetRenderers[i], originalColors[i]);
        resetAt = -1f;
    }

    private void AddMark(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        local.z = FaceZ;

        GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mark.name = "Bullet Mark";
        mark.transform.SetParent(transform, false);
        mark.transform.localPosition = local;
        mark.transform.localRotation = Quaternion.Euler(90f, 0f, Random.Range(0f, 360f));
        mark.transform.localScale = new Vector3(0.05f, 0.003f, 0.05f);

        Collider markCollider = mark.GetComponent<Collider>();
        if (markCollider != null)
            Destroy(markCollider);

        Renderer renderer = mark.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = GetMarkMaterial();

        marks.Enqueue(mark);
        while (marks.Count > maxMarks)
        {
            GameObject oldest = marks.Dequeue();
            if (oldest != null)
                Destroy(oldest);
        }
    }

    private Material GetMarkMaterial()
    {
        if (markMaterial != null)
            return markMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            markMaterial = new Material(shader) { name = "Bullet Mark" };
            if (markMaterial.HasProperty("_BaseColor"))
                markMaterial.SetColor("_BaseColor", new Color(0.02f, 0.018f, 0.016f));
            if (markMaterial.HasProperty("_Color"))
                markMaterial.color = new Color(0.02f, 0.018f, 0.016f);
        }
        else if (targetRenderers.Length > 0 && targetRenderers[0] != null)
        {
            markMaterial = targetRenderers[0].sharedMaterial;
        }

        return markMaterial;
    }

    private void EmitImpact(Vector3 worldPoint)
    {
        if (impactTemplate == null)
            return;

        Vector3 local = transform.InverseTransformPoint(worldPoint);
        local.z = FaceZ - 0.01f;
        Vector3 position = transform.TransformPoint(local);

        ParticleSystem puff = Instantiate(impactTemplate, position, Quaternion.LookRotation(-transform.forward));
        puff.Play();
        Destroy(puff.gameObject, 1.5f);
    }

    private void PlayImpactSound(Vector3 worldPoint)
    {
        if (impactSource == null || impactClip == null)
            return;

        impactSource.transform.position = worldPoint;
        impactSource.PlayOneShot(impactClip, 0.85f);
    }

    private static AudioClip BuildImpactClip()
    {
        const int rate = 22050;
        const float duration = 0.5f;
        int count = Mathf.CeilToInt(rate * duration);
        var samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float body = Mathf.Sin(2f * Mathf.PI * 1180f * t) * Mathf.Exp(-t * 14f);
            float overtone = Mathf.Sin(2f * Mathf.PI * 1830f * t) * Mathf.Exp(-t * 19f) * 0.55f;
            float ring = Mathf.Sin(2f * Mathf.PI * 2650f * t) * Mathf.Exp(-t * 26f) * 0.30f;
            float clank = (Random.value * 2f - 1f) * Mathf.Exp(-t * 120f) * 0.35f;
            samples[i] = Mathf.Clamp((body + overtone + ring + clank) * 0.7f, -1f, 1f);
        }

        var clip = AudioClip.Create("Target Impact", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void SetColor(Renderer targetRenderer, Color color)
    {
        if (targetRenderer == null)
            return;

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
