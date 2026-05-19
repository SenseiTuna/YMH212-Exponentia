using Exponentia.Interaction;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    [DisallowMultipleComponent]
    public class PickupObject : MonoBehaviour, IInteractable
    {
        [Header("Payload")]
        [SerializeField] private ItemDefinitionBase itemDefinition;
        [SerializeField] private RewardDefinition rewardDefinition;

        [Header("Pickup Mode")]
        [SerializeField] private bool autoPickup = true;
        [SerializeField] private bool consumeOnPickup = true;
        [SerializeField] private bool oneTimeUse = true;
        [SerializeField] private string interactionLabel = "Pick Up";

        [Header("Floating Animation")]
        [SerializeField] private bool enableFloating = true;
        [SerializeField] private float floatAmplitude = 0.12f;
        [SerializeField] private float floatFrequency = 2f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private bool consumed;
        private Vector3 basePosition;

        private void Awake()
        {
            basePosition = transform.position;
        }

        private void Update()
        {
            if (!enableFloating)
            {
                return;
            }

            float offset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = basePosition + new Vector3(0f, offset, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!autoPickup || consumed)
            {
                return;
            }

            PlayerInventory inventory = FindInventory(other.gameObject);
            if (inventory == null)
            {
                return;
            }

            TryCollect(inventory);
        }

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

            return FindInventory(interactor) != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            PlayerInventory inventory = FindInventory(interactor);
            if (inventory != null)
            {
                TryCollect(inventory);
            }
        }

        private bool TryCollect(PlayerInventory inventory)
        {
            if (inventory == null || consumed)
            {
                return false;
            }

            bool applied = false;

            if (itemDefinition != null)
            {
                applied = inventory.AddItem(itemDefinition);
            }
            else if (rewardDefinition != null)
            {
                applied = inventory.ApplyReward(rewardDefinition);
            }
            else
            {
                Debug.LogWarning("PickupObject: Both ItemDefinition and RewardDefinition are null.", this);
            }

            if (!applied)
            {
                return false;
            }

            if (verboseLogs)
            {
                string payloadName = itemDefinition != null ? itemDefinition.displayName : rewardDefinition.displayName;
                Debug.Log($"PickupObject: Collected '{payloadName}'.", this);
            }

            if (consumeOnPickup)
            {
                Consume();
            }

            return true;
        }

        private void Consume()
        {
            consumed = true;

            if (oneTimeUse)
            {
                Destroy(gameObject);
            }
        }

        private static PlayerInventory FindInventory(GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            PlayerInventory inventory = source.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                inventory = source.GetComponentInParent<PlayerInventory>();
            }

            return inventory;
        }
    }
}
