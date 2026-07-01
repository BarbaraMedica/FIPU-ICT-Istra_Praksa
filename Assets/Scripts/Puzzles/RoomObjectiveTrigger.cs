using UnityEngine;

/// <summary>
/// Invisible trigger volume that fills a room. When the player walks in, it refreshes the on-screen
/// objective so it reflects the room they are currently standing in (and its current sequence step).
///
/// The builder adds a kinematic Rigidbody + trigger BoxCollider alongside this component so the
/// CharacterController reliably fires OnTriggerEnter.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomObjectiveTrigger : MonoBehaviour
{
    public RoomSequenceController room;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (room == null)
        {
            return;
        }

        if (other.GetComponentInParent<FirstPersonController>() == null)
        {
            return;
        }

        room.RefreshObjective();
    }
}
