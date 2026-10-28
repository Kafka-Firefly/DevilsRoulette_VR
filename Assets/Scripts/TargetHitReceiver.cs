using UnityEngine;

/// <summary>Small visual response for the prototype target.</summary>
public sealed class TargetHitReceiver : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color hitColor = new Color(0.85f, 0.08f, 0.04f);
    [SerializeField] private float resetAfterSeconds = 0.7f;

    private MaterialPropertyBlock propertyBlock;
    private Color[] originalColors;
    private float resetAt = -1f;

    private void Awake()
    {
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
    }

    private void Update()
    {
        if (resetAt < 0f || Time.time < resetAt)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
            SetColor(targetRenderers[i], originalColors[i]);
        resetAt = -1f;
    }

    public void OnShot()
    {
        foreach (Renderer targetRenderer in targetRenderers)
            SetColor(targetRenderer, hitColor);
        resetAt = Time.time + resetAfterSeconds;
        Debug.Log("Target hit.", this);
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
