/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.6.0
 * BUILD_DATE: 2026-05-18
 * BUILD_TIME: 20:40
 * DESCRIPTION: Interact oldugunda UnityEvent tetikleyen genel amacli interactable.
 */

using Exponentia.Interaction;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class InteractableEventRelay : MonoBehaviour, IInteractable
{
    [Header("Interact")]
    [SerializeField] private string interactionLabel = "Interact";
    [SerializeField] private bool oneTimeUse;
    [SerializeField] private Transform interactionPoint;

    [Header("Events")]
    [SerializeField] private UnityEvent onInteracted;

    private bool consumed;

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
        return !consumed;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        // Turkish: Sahneye ozel logic yazmadan inspector event zinciriyle etkilesim aciyoruz.
        onInteracted?.Invoke();

        if (oneTimeUse)
        {
            consumed = true;
        }
    }
}