using UnityEngine;

namespace Exponentia.InventorySystem
{
    [CreateAssetMenu(fileName = "ITM_Skill_", menuName = "Exponentia/Inventory/Skill Definition")]
    public class SkillDefinition : ItemDefinitionBase
    {
        [Header("Skill Timing")]
        [Min(0f)] public float cooldown = 6f;
        [Min(0f)] public float duration = 1.5f;

        [Header("Skill Effect")]
        public SkillEffectType skillEffectType = SkillEffectType.Custom;
        public GameObject visualEffectPrefab;
        public AudioClip audioClip;

        private void Reset()
        {
            category = ItemCategory.Skill;
            canStack = false;
            maxStacks = 1;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            category = ItemCategory.Skill;
            cooldown = Mathf.Max(0f, cooldown);
            duration = Mathf.Max(0f, duration);
        }
    }
}
