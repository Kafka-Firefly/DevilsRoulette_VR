using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Logs the XR input and interaction state so a headset run can be diagnosed from logcat.</summary>
public sealed class PitDiagnostics : MonoBehaviour
{
    private float m_Next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("~PitDiagnostics");
        DontDestroyOnLoad(go);
        go.AddComponent<PitDiagnostics>();
    }

    private void Start()
    {
        var sb = new StringBuilder("[PitDiag] devices=");
        int xrDevices = 0;
        foreach (var d in InputSystem.devices)
        {
            string layout = d.layout;
            if (layout.Contains("XR") || layout.Contains("Oculus") || layout.Contains("Touch"))
            {
                sb.Append(layout).Append(',');
                xrDevices++;
            }
        }
        sb.Append(" count=").Append(xrDevices);
        Debug.Log(sb.ToString());
        Debug.Log("[PitDiag] XR active=" + XRSettings.isDeviceActive + " loaded=" + XRSettings.loadedDeviceName);
    }

    private void Update()
    {
        if (Time.unscaledTime < m_Next) return;
        m_Next = Time.unscaledTime + 2f;

        string hands = "";
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        Vector3 lp = Vector3.zero, rp = Vector3.zero;
        bool lg = left.isValid && left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out lp);
        bool rg = right.isValid && right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out rp);
        bool lgrip = false, rgrip = false, ltrig = false, rtrig = false;
        if (left.isValid) left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out lgrip);
        if (right.isValid) right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out rgrip);
        if (left.isValid) left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out ltrig);
        if (right.isValid) right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out rtrig);

        hands = string.Format(" L valid={0} pos={1} grip={2} trig={3} | R valid={4} pos={5} grip={6} trig={7}",
            left.isValid, lp.ToString("F2"), lgrip, ltrig,
            right.isValid, rp.ToString("F2"), rgrip, rtrig);

        string interactors = "";
        var found = Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None);
        foreach (var i in found)
        {
            if (!i.isActiveAndEnabled) continue;
            if (i.hasSelection || i.hasHover)
                interactors += " [" + i.gameObject.name + " hover=" + i.hasHover + " sel=" + i.hasSelection + "]";
        }
        if (interactors.Length == 0) interactors = " none";

        Debug.Log("[PitDiag] active=" + XRSettings.isDeviceActive + hands + " | interacting:" + interactors);
    }
}
