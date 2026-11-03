using UnityEngine;

/// <summary>Subtle, non-random-looking intensity drift for the work lamp hanging over the table.</summary>
[RequireComponent(typeof(Light))]
public sealed class PitLampFlicker : MonoBehaviour
{
    [SerializeField] private Light targetLight;
    [SerializeField] private float baseIntensity = 2600f;
    [SerializeField] private float flickerDepth = 0.07f;
    [SerializeField] private float speed = 6f;

    private float seed;
    private float noiseOffset;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();
        if (baseIntensity <= 0f && targetLight != null)
            baseIntensity = targetLight.intensity;

        seed = transform.position.sqrMagnitude * 0.37f;
        noiseOffset = (transform.position.x + transform.position.z) * 1.7f;
    }

    private void Update()
    {
        if (targetLight == null)
            return;

        float t = Time.time * speed + noiseOffset;
        float slow = Mathf.PerlinNoise(seed, t * 0.35f);
        float fast = Mathf.PerlinNoise(seed + 11.3f, t * 3.1f);

        // Mostly steady, with occasional small dips so the room feels alive.
        float dip = Mathf.Clamp01((0.42f - fast) * 2.4f) * 0.55f;
        float factor = 1f - flickerDepth * (1f - slow) - flickerDepth * dip;
        targetLight.intensity = baseIntensity * Mathf.Clamp(factor, 0.55f, 1f);
    }
}
