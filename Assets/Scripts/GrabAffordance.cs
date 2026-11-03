using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Teaches the grab before it happens: the shotgun warms up while a controller hovers it,
/// clicks softly and pulses haptics when it is taken or released.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class GrabAffordance : MonoBehaviour
{
    [SerializeField] private Color hoverWarmth = new Color(0.09f, 0.06f, 0.02f);
    [SerializeField] private float hoverGain = 1.45f;
    [SerializeField] private float hoverFadeSpeed = 9f;

    [SerializeField] private float selectHapticAmplitude = 0.28f;
    [SerializeField] private float selectHapticDuration = 0.06f;
    [SerializeField] private float releaseHapticAmplitude = 0.12f;
    [SerializeField] private float releaseHapticDuration = 0.04f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private XRGrabInteractable grab;
    private Renderer[] renderers;
    private Color[] originals;
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    private AudioSource source;
    private AudioClip handleClick;

    private bool hovering;
    private bool selected;
    private float blend;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        CacheRenderers();

        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = 0.4f;
        source.maxDistance = 8f;

        handleClick = BuildHandleClick();
    }

    private void OnEnable()
    {
        grab.hoverEntered.AddListener(OnHoverEntered);
        grab.hoverExited.AddListener(OnHoverExited);
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        grab.hoverEntered.RemoveListener(OnHoverEntered);
        grab.hoverExited.RemoveListener(OnHoverExited);
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    private void Update()
    {
        float target = hovering && !selected ? 1f : 0f;
        blend = Mathf.MoveTowards(blend, target, Time.deltaTime * hoverFadeSpeed);
        ApplyTint(blend);
    }

    private void CacheRenderers()
    {
        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        var kept = new System.Collections.Generic.List<Renderer>(all.Length);

        foreach (Renderer renderer in all)
        {
            if (renderer is ParticleSystemRenderer)
                continue;
            kept.Add(renderer);
        }

        renderers = kept.ToArray();
        originals = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            originals[i] = material != null && material.HasProperty(BaseColorId)
                ? material.GetColor(BaseColorId)
                : Color.white;
        }
    }

    private void ApplyTint(float amount)
    {
        if (renderers == null || renderers.Length == 0)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Color original = originals[i];
            Color lit = original * hoverGain + hoverWarmth;
            lit.a = original.a;

            renderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(original, lit, amount));
            renderers[i].SetPropertyBlock(block);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        hovering = true;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        hovering = false;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        selected = true;
        hovering = false;

        if (source != null && handleClick != null)
            source.PlayOneShot(handleClick, 0.6f);

        Pulse(args, selectHapticAmplitude, selectHapticDuration);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        selected = false;
        Pulse(args, releaseHapticAmplitude, releaseHapticDuration);
    }

    private static void Pulse(BaseInteractionEventArgs args, float amplitude, float duration)
    {
        if (args.interactorObject is XRBaseInputInteractor interactor)
            interactor.SendHapticImpulse(amplitude, duration);
    }

    private static AudioClip BuildHandleClick()
    {
        const int rate = 22050;
        const float duration = 0.05f;
        int count = Mathf.CeilToInt(rate * duration);
        var samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float envelope = Mathf.Exp(-t * 90f);
            float wood = Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.5f;
            float tick = Mathf.Sin(2f * Mathf.PI * 1250f * t) * 0.3f;
            samples[i] = (wood + tick) * envelope * 0.6f;
        }

        var clip = AudioClip.Create("Grab Handle Click", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
