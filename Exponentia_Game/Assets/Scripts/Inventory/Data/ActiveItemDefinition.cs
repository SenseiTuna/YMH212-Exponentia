using UnityEngine;

namespace Exponentia.InventorySystem
{
    [CreateAssetMenu(fileName = "ITM_Active_", menuName = "Exponentia/Inventory/Active Item Definition")]
    public class ActiveItemDefinition : ItemDefinitionBase
    {
        [Header("Timing")]
        [Min(0f)] public float cooldown = 18f;
        [Min(0f)] public float duration = 2f;

        [Header("Effect")]
        public ActiveEffectType activeEffectType = ActiveEffectType.Custom;
        public float effectPower = 10f;
        public GameObject visualEffectPrefab;

        private void Reset()
        {
            category = ItemCategory.ActiveItem;
            canStack = false;
            maxStacks = 1;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            category = ItemCategory.ActiveItem;
            cooldown = Mathf.Max(0f, cooldown);
            duration = Mathf.Max(0f, duration);
        }
    }
}
