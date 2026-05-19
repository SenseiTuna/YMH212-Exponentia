using System.Collections.Generic;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    [CreateAssetMenu(fileName = "ITM_Passive_", menuName = "Exponentia/Inventory/Passive Item Definition")]
    public class PassiveItemDefinition : ItemDefinitionBase
    {
        [Header("Passive Effects")]
        [Tooltip("Free-form labels for gameplay hooks or VFX selection.")]
        public List<string> passiveEffects = new List<string>();

        [Tooltip("Stat modifiers applied while this passive item is owned.")]
        public List<StatModifierDefinition> statModifiers = new List<StatModifierDefinition>();

        [Header("Duplicate Behavior")]
        [Tooltip("If false, duplicate pickup is ignored even if the asset is picked up again.")]
        public bool allowDuplicatePickup = true;

        private void Reset()
        {
            category = ItemCategory.PassiveItem;
            canStack = true;
            maxStacks = 5;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            category = ItemCategory.PassiveItem;
            maxStacks = Mathf.Max(1, maxStacks);
        }
    }
}
