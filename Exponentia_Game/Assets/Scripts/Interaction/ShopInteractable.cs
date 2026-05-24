/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.6.0
 * BUILD_DATE: 2026-05-18
 * BUILD_TIME: 20:40
 * DESCRIPTION: Shop NPC/objesi ile etkilesimde market UI acmak icin genel kapi.
 */

using Exponentia.Interaction;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class ShopInteractable : MonoBehaviour, IInteractable
{
    [Header("Shop")]
    [SerializeField] private string interactionLabel = "Open Shop";
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool requiresAlivePlayer = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onShopOpenRequested;

    public Vector3 GetInteractionPoint()
    {
        return interactionPoint != null ? interactionPoint.position : transform.position;
    }

    public string GetInteractionLabel()
    {
        return interactionLabel;
    }

    public bool CanInteract(GameObject interactor)
    {
        if (!requiresAlivePlayer)
        {
            return true;
        }

        if (interactor == null)
        {
            return false;
        }

        PlayerMechanics mechanics = interactor.GetComponent<PlayerMechanics>();
        if (mechanics == null)
        {
            mechanics = interactor.GetComponentInParent<PlayerMechanics>();
        }

        return mechanics != null && mechanics.Yasiyor;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        // Turkish: Shop UI baglantisini kod bagimliligi olmadan UnityEvent ile aciyoruz.
        onShopOpenRequested?.Invoke();
    }
}