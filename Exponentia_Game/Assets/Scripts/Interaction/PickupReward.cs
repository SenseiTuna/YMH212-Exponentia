/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.6.0
 * BUILD_DATE: 2026-05-18
 * BUILD_TIME: 20:20
 * DESCRIPTION: Interact ile toplanan yerdeki odul/esya davranisi.
 */

using Exponentia.Interaction;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PickupReward : MonoBehaviour, IInteractable
{
    public enum RewardType
    {
        Health,
        Mana,
        Shield,
        Experience
    }

    [Header("Reward")]
    [SerializeField] private RewardType rewardType = RewardType.Health;
    [SerializeField] private float amount = 25f;
    [SerializeField] private bool consumeOnInteract = true;

    [Header("Availability")]
    [SerializeField] private bool oneTimeUse = true;
    [SerializeField] private string interactionLabel = "Pick Up";

    private bool consumed;

    public Vector3 GetInteractionPoint()
    {
        return transform.position;
    }

    public string GetInteractionLabel()
    {
        return interactionLabel;
    }

    public bool CanInteract(GameObject interactor)
    {
        if (consumed)
        {
            return false;
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

        return mechanics != null;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        PlayerMechanics mechanics = interactor.GetComponent<PlayerMechanics>();
        if (mechanics == null)
        {
            mechanics = interactor.GetComponentInParent<PlayerMechanics>();
        }

        if (mechanics == null)
        {
            Debug.LogWarning("PickupReward: PlayerMechanics not found on interactor.", this);
            return;
        }

        float safeAmount = Mathf.Max(0f, amount);

        // Turkish: Odul turune gore tek bir merkezden oyuncu kaynagini guncelliyoruz.
        switch (rewardType)
        {
            case RewardType.Health:
                mechanics.Heal(safeAmount);
                break;
            case RewardType.Mana:
                mechanics.ManaYenile(safeAmount);
                break;
            case RewardType.Shield:
                mechanics.KalkanYenile(safeAmount);
                break;
            case RewardType.Experience:
                mechanics.GainXp(safeAmount);
                break;
            default:
                Debug.LogWarning($"PickupReward: Unsupported reward type {rewardType}", this);
                break;
        }

        if (consumeOnInteract)
        {
            Consume();
        }
    }

    private void Consume()
    {
        consumed = true;

        if (oneTimeUse)
        {
            Destroy(gameObject);
        }
    }
}
