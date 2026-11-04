using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Puts the shotgun back on the table if it ends up somewhere the player cannot reach.
/// There is no locomotion in the prototype, so a gun under the table would otherwise end the session.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class GunRecovery : MonoBehaviour
{
    [SerializeField] private float lowestReachableHeight = 0.5f;
    [SerializeField] private float maxHorizontalDistance = 2.2f;
    [SerializeField] private float gracePeriod = 2.5f;

    private Rigidbody body;
    private XRGrabInteractable grab;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float awayTimer;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    private void Update()
    {
        if (grab.isSelected)
        {
            awayTimer = 0f;
            return;
        }

        if (!IsOutOfReach())
        {
            awayTimer = 0f;
            return;
        }

        awayTimer += Time.deltaTime;
        if (awayTimer < gracePeriod)
            return;

        awayTimer = 0f;
        ReturnHome();
    }

    private bool IsOutOfReach()
    {
        Vector3 position = transform.position;
        if (position.y < lowestReachableHeight)
            return true;

        var flatDistance = Vector2.Distance(
            new Vector2(position.x, position.z),
            new Vector2(homePosition.x, homePosition.z));

        return flatDistance > maxHorizontalDistance;
    }

    private void ReturnHome()
    {
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(homePosition + Vector3.up * 0.02f, homeRotation);

        if (body != null)
            body.Sleep();
    }
}
