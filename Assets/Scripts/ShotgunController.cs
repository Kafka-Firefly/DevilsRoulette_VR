using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Prototype trigger behavior for the six-chamber roulette shotgun.
/// XRI 3 calls this when the held interactable receives its Activate action.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class ShotgunController : MonoBehaviour
{
    private const int ProceduralSampleRate = 22050;
    private static AudioClip generatedLiveShot;
    private static AudioClip generatedClick;

    [SerializeField] private Transform barrelTip;
    [SerializeField] private Transform playerHead;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip liveShotSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField, Min(1)] private int chamberCount = 6;
    [SerializeField, Min(0)] private int liveRoundCount = 1;
    [SerializeField, Min(0.1f)] private float range = 20f;
    [SerializeField, Range(1f, 45f)] private float selfAimAngle = 12f;
    [SerializeField, Min(0.1f)] private float selfAimDistance = 2.5f;

    private XRGrabInteractable grabInteractable;
    private bool[] chambers;
    private int chamberIndex;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (liveShotSound == null)
            liveShotSound = GetGeneratedLiveShot();
        if (clickSound == null)
            clickSound = GetGeneratedClick();
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        ShuffleChambers();
    }

    private void OnEnable()
    {
        grabInteractable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.activated.RemoveListener(OnActivated);
    }

    /// <summary>Starts a new randomized round. One live round is used by default.</summary>
    public void ShuffleChambers()
    {
        chamberCount = Mathf.Max(1, chamberCount);
        chambers = new bool[chamberCount];
        int rounds = Mathf.Clamp(liveRoundCount, 0, chamberCount);
        for (int i = 0; i < rounds; i++)
            chambers[i] = true;

        for (int i = chambers.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (chambers[i], chambers[j]) = (chambers[j], chambers[i]);
        }

        chamberIndex = 0;
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (barrelTip == null || chambers == null)
            return;

        if (chamberIndex >= chambers.Length)
            ShuffleChambers();

        bool liveRound = chambers[chamberIndex++];
        bool aimedAtSelf = IsAimedAtPlayer();
        bool hitTarget = Physics.Raycast(barrelTip.position, barrelTip.forward,
            out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (liveRound)
        {
            Play(liveShotSound);
            if (muzzleFlash != null)
                muzzleFlash.Play();

            if (hitTarget && hit.collider.GetComponentInParent<TargetHitReceiver>() is TargetHitReceiver target)
                target.OnShot();

            if (aimedAtSelf)
                Debug.Log("Live round aimed toward the player (prototype game event only).", this);

            SendHaptic(args, 0.8f, 0.16f);
        }
        else
        {
            Play(clickSound);
            SendHaptic(args, 0.25f, 0.07f);
        }
    }

    private bool IsAimedAtPlayer()
    {
        if (playerHead == null || barrelTip == null)
            return false;

        Vector3 toHead = playerHead.position - barrelTip.position;
        return toHead.sqrMagnitude <= selfAimDistance * selfAimDistance &&
               Vector3.Angle(barrelTip.forward, toHead) <= selfAimAngle;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private static AudioClip GetGeneratedLiveShot()
    {
        if (generatedLiveShot != null)
            return generatedLiveShot;

        const float duration = 0.42f;
        int sampleCount = Mathf.CeilToInt(ProceduralSampleRate * duration);
        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)ProceduralSampleRate;
            float noise = Random.Range(-1f, 1f) * Mathf.Exp(-time * 24f);
            float lowBody = Mathf.Sin(2f * Mathf.PI * 68f * time) * Mathf.Exp(-time * 7f);
            float snap = Mathf.Sin(2f * Mathf.PI * 1450f * time) * Mathf.Exp(-time * 65f);
            samples[i] = Mathf.Clamp((noise * 0.48f + lowBody * 0.48f + snap * 0.12f) * 0.85f, -1f, 1f);
        }

        generatedLiveShot = AudioClip.Create("Generated Shot", sampleCount, 1, ProceduralSampleRate, false);
        generatedLiveShot.SetData(samples, 0);
        return generatedLiveShot;
    }

    private static AudioClip GetGeneratedClick()
    {
        if (generatedClick != null)
            return generatedClick;

        const float duration = 0.075f;
        int sampleCount = Mathf.CeilToInt(ProceduralSampleRate * duration);
        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)ProceduralSampleRate;
            float envelope = Mathf.Exp(-time * 85f);
            float click = Mathf.Sin(2f * Mathf.PI * 1750f * time) * 0.65f;
            float noise = Random.Range(-1f, 1f) * 0.35f;
            samples[i] = (click + noise) * envelope;
        }

        generatedClick = AudioClip.Create("Generated Empty Click", sampleCount, 1, ProceduralSampleRate, false);
        generatedClick.SetData(samples, 0);
        return generatedClick;
    }

    private static void SendHaptic(ActivateEventArgs args, float amplitude, float duration)
    {
        if (args.interactorObject is XRBaseInputInteractor inputInteractor)
            inputInteractor.SendHapticImpulse(amplitude, duration);
    }
}
