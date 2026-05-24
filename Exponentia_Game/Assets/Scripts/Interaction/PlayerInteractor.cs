/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.6.0
 * BUILD_DATE: 2026-05-18
 * BUILD_TIME: 20:20
 * DESCRIPTION: Oyuncunun yakin cevredeki interactable objeleri bulup E/A/Triangle ile etkilesmesini saglar.
 */

using Exponentia.Interaction;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Interact Detection")]
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private float interactRadius = 1.25f;
    [SerializeField] private Vector2 interactOffset = Vector2.zero;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private readonly Collider2D[] hitsBuffer = new Collider2D[16];
    private IInteractable currentTarget;

    public IInteractable CurrentTarget => currentTarget;
    public string CurrentTargetLabel => currentTarget != null ? currentTarget.GetInteractionLabel() : string.Empty;
    public event System.Action<IInteractable> OnTargetChanged;

    private void Reset()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        UpdateCurrentTarget();
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnInteractPressed += HandleInteractPressed;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnInteractPressed -= HandleInteractPressed;
        }
    }

    private void HandleInteractPressed()
    {
        // Turkish: Tus basildigi anda o an hedefte olan obje uzerinden etkilesim cagrisi yapiyoruz.
        IInteractable target = currentTarget;
        if (target == null)
        {
            return;
        }

        if (!target.CanInteract(gameObject))
        {
            return;
        }

        target.Interact(gameObject);
    }

    private IInteractable FindNearestInteractable()
    {
        Vector2 center = (Vector2)transform.position + interactOffset;
        ContactFilter2D contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = interactableLayers,
            useTriggers = true
        };
        int count = Physics2D.OverlapCircle(center, interactRadius, contactFilter, hitsBuffer);

        float bestDistance = float.MaxValue;
        IInteractable bestTarget = null;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hitsBuffer[i];
            if (hit == null)
            {
                continue;
            }

            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                interactable = hit.GetComponent<IInteractable>();
            }

            if (interactable == null)
            {
                continue;
            }

            if (!interactable.CanInteract(gameObject))
            {
                continue;
            }

            float sqrDistance = ((Vector2)interactable.GetInteractionPoint() - center).sqrMagnitude;
            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                bestTarget = interactable;
            }
        }

        return bestTarget;
    }

    private void UpdateCurrentTarget()
    {
        IInteractable nextTarget = FindNearestInteractable();
        if (ReferenceEquals(nextTarget, currentTarget))
        {
            return;
        }

        currentTarget = nextTarget;
        OnTargetChanged?.Invoke(currentTarget);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = new Color(0.15f, 0.9f, 0.4f, 0.8f);
        Vector3 center = transform.position + (Vector3)interactOffset;
        Gizmos.DrawWireSphere(center, interactRadius);
    }
}
