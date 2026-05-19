using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class KnockbackReceiver2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Fallback")]
    [SerializeField] private bool useTransformFallback = true;

    private Coroutine knockbackRoutine;

    private void Reset()
    {
        targetRigidbody = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody2D>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        Vector2 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.zero;
        float safeForce = Mathf.Max(0f, force);
        float safeDuration = Mathf.Max(0.01f, duration);

        if (safeDirection.sqrMagnitude <= 0.001f || safeForce <= 0f)
        {
            return;
        }

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(KnockbackRoutine(safeDirection, safeForce, safeDuration));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        if (targetRigidbody != null)
        {
            targetRigidbody.linearVelocity = Vector2.zero;
            targetRigidbody.AddForce(direction * force, ForceMode2D.Impulse);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (playerMovement != null)
            {
                // Turkish: PlayerMovement velocity yazdigi icin knockback'i external kanal uzerinden surduruyoruz.
                playerMovement.ApplyExternalVelocity(direction * force);
            }
            else if (targetRigidbody == null && useTransformFallback)
            {
                transform.position += (Vector3)(direction * force * Time.deltaTime);
            }

            yield return null;
        }

        knockbackRoutine = null;
    }
}
