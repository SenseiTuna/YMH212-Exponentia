using UnityEngine;

// Attach this to the player prefab to log collision/trigger events for debugging.
public class PlayerCollisionDebugger : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[CollisionEnter2D] Player collided with {collision.gameObject.name} (tag={collision.gameObject.tag}, isTrigger={collision.collider.isTrigger}, layer={LayerMask.LayerToName(collision.gameObject.layer)})");
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log($"[CollisionExit2D] Player exited collision with {collision.gameObject.name}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[TriggerEnter2D] Player triggered {other.gameObject.name} (tag={other.gameObject.tag}, isTrigger={other.isTrigger}, layer={LayerMask.LayerToName(other.gameObject.layer)})");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"[TriggerExit2D] Player left trigger {other.gameObject.name}");
    }
}
