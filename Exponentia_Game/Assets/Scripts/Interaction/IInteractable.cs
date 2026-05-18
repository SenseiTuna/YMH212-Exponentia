/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.6.0
 * BUILD_DATE: 2026-05-18
 * BUILD_TIME: 20:20
 * DESCRIPTION: Player ile etkilesime girebilen dunya objeleri icin temel arayuz.
 */

using UnityEngine;

namespace Exponentia.Interaction
{
    public interface IInteractable
    {
        Vector3 GetInteractionPoint();
        string GetInteractionLabel();
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}
