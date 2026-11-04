using System.Collections;
using UnityEngine;

/// <summary>
/// On a desktop build with no XR device available, falls back to the XR Device Simulator so the
/// prototype can still be grabbed, aimed and fired with the mouse and keyboard.
/// </summary>
public sealed class PitDesktopInput : MonoBehaviour
{
    private const string SimulatorResource = "PitDeviceSimulator";

#if UNITY_STANDALONE
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var runner = new GameObject("~PitDesktopInput");
        DontDestroyOnLoad(runner);
        runner.AddComponent<PitDesktopInput>();
    }

    private IEnumerator Start()
    {
        // Give the XR loader a moment to report whether a headset is actually present.
        yield return new WaitForSeconds(1.5f);

        if (UnityEngine.XR.XRSettings.isDeviceActive)
            yield break;

        GameObject prefab = Resources.Load<GameObject>(SimulatorResource);
        if (prefab == null)
        {
            Debug.LogWarning("[PitDesktopInput] simulator prefab not found in Resources");
            yield break;
        }

        Instantiate(prefab);
        Debug.Log("[PitDesktopInput] no XR device, XR Device Simulator instantiated");
    }
#endif
}
