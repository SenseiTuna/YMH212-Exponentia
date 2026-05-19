using UnityEngine;

namespace Exponentia.InventorySystem
{
    public abstract class ItemDefinitionBase : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique stable id used for save/load and duplicate checks.")]
        public string itemId = "item.new";

        public string displayName = "New Item";

        [TextArea(2, 5)]
        public string description;

        [Header("Presentation")]
        public Sprite icon;
        public ItemRarity rarity = ItemRarity.Common;
        public ItemCategory category = ItemCategory.Consumable;

        [Header("Stack")]
        public bool canStack;
        [Min(1)] public int maxStacks = 1;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = name.ToLowerInvariant().Replace(" ", ".");
            }

            maxStacks = Mathf.Max(1, maxStacks);
            if (!canStack)
            {
                maxStacks = 1;
            }
        }
    }
}
