using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Grab and fire that does not depend on XR Interaction Toolkit input events.
/// Polls the XR device directly, so it keeps working even if the toolkit's
/// select action never fires on a given runtime.
/// </summary>
public sealed class PitManualGrab : MonoBehaviour
{
    [SerializeField] private XRNode m_Node = XRNode.RightHand;
    [SerializeField] private float m_Reach = 0.62f;
    [SerializeField] private float m_HapticAmplitude = 0.35f;

    private ShotgunController m_Gun;
    private Rigidbody m_Body;
    private Behaviour m_GrabInteractable;
    private Transform m_Hand;

    private bool m_Held;
    private bool m_LastGrip;
    private Vector3 m_LocalPosition;
    private Quaternion m_LocalRotation;
    private float m_NextFire;

    public void SetNode(XRNode node) => m_Node = node;

    private void Start()
    {
        m_Hand = transform;

        var gun = GameObject.Find("Shotgun");
        if (gun == null)
        {
            Debug.LogWarning("[PitGrab] Shotgun not found");
            return;
        }

        m_Gun = gun.GetComponentInChildren<ShotgunController>(true);
        m_Body = gun.GetComponent<Rigidbody>();
        m_GrabInteractable = gun.GetComponent("XRGrabInteractable") as Behaviour;

        Debug.Log("[PitGrab] ready on " + name + " gun=" + (m_Gun != null) + " body=" + (m_Body != null));
    }

    private void Update()
    {
        var device = InputDevices.GetDeviceAtXRNode(m_Node);
        if (!device.isValid || m_Gun == null) return;

        bool grip = device.TryGetFeatureValue(CommonUsages.gripButton, out bool g) && g;
        bool trigger = device.TryGetFeatureValue(CommonUsages.triggerButton, out bool t) && t;

        if (!m_Held && grip && !m_LastGrip)
        {
            float distance = Vector3.Distance(m_Hand.position, m_Gun.transform.position);
            if (distance <= m_Reach) Grab(device);
            else Debug.Log("[PitGrab] out of reach, distance=" + distance.ToString("F2"));
        }
        else if (m_Held && !grip)
        {
            Release();
        }

        if (m_Held && trigger && Time.time >= m_NextFire)
        {
            m_NextFire = Time.time + 0.45f;
            m_Gun.Fire();
            Harptic(device, 0.6f);
        }

        m_LastGrip = grip;
    }

    private void Grab(InputDevice device)
    {
        m_Held = true;
        transform.GetPositionAndRotation(out _, out _);
        m_Gun.transform.SetParent(m_Hand, true);
        m_LocalPosition = m_Hand.InverseTransformPoint(m_Gun.transform.position);
        m_LocalRotation = Quaternion.Inverse(m_Hand.rotation) * m_Gun.transform.rotation;

        if (m_Body != null) m_Body.isKinematic = true;
        if (m_GrabInteractable != null) m_GrabInteractable.enabled = false;

        Harptic(device, m_HapticAmplitude);
        Debug.Log("[PitGrab] grabbed by " + name);
    }

    private void Release()
    {
        m_Held = false;
        m_Gun.transform.SetParent(null, true);
        if (m_Body != null)
        {
            m_Body.isKinematic = false;
            m_Body.linearVelocity = Vector3.zero;
        }
        if (m_GrabInteractable != null) m_GrabInteractable.enabled = true;
        Debug.Log("[PitGrab] released by " + name);
    }

    private void LateUpdate()
    {
        if (!m_Held || m_Gun == null) return;
        m_Gun.transform.SetPositionAndRotation(
            m_Hand.TransformPoint(m_LocalPosition),
            m_Hand.rotation * m_LocalRotation);
    }

    private static void Harptic(InputDevice device, float amplitude)
    {
        if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
            device.SendHapticImpulse(0u, amplitude, 0.08f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var rig = GameObject.Find("XR Origin (XR Rig)");
        if (rig == null) { Debug.LogWarning("[PitGrab] rig not found"); return; }

        Attach(rig.transform, "Left Controller", XRNode.LeftHand);
        Attach(rig.transform, "Right Controller", XRNode.RightHand);
    }

    private static void Attach(Transform rig, string controllerName, XRNode node)
    {
        Transform target = null;
        foreach (var t in rig.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == controllerName) { target = t; break; }
        }
        if (target == null) { Debug.LogWarning("[PitGrab] " + controllerName + " not found"); return; }
        if (target.GetComponent<PitManualGrab>() != null) return;

        var grab = target.gameObject.AddComponent<PitManualGrab>();
        grab.SetNode(node);
        Debug.Log("[PitGrab] attached to " + controllerName);
    }
}
