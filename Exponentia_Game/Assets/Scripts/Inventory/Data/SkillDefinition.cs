using UnityEngine;

namespace Exponentia.InventorySystem
{
    [CreateAssetMenu(fileName = "ITM_Skill_", menuName = "Exponentia/Inventory/Skill Definition")]
    public class SkillDefinition : ItemDefinitionBase
    {
        [Header("Skill Timing")]
        [Min(0f)] public float cooldown = 6f;
        [Min(0f)] public float duration = 1.5f;

        [Header("Runtime Binding")]
        public GodSkillType linkedGodSkillType = GodSkillType.None;

        [Header("Skill Effect")]
        public SkillEffectType skillEffectType = SkillEffectType.Custom;
        public GameObject visualEffectPrefab;
        public AudioClip audioClip;

        [Header("Skill Effect Rendering")]
        public bool renderVfxBehindOwner;
        [Tooltip("Used when Render Vfx Behind Owner is off.")]
        public int runtimeVfxSortingOrder = 500;
        [Tooltip("Used when Render Vfx Behind Owner is on. -1 draws behind the player sprite.")]
        public int runtimeVfxSortingOrderOffsetFromOwner = -1;

        [Header("Runtime Tuning")]
        public bool overrideRuntimeTuning;
        [Min(0f)] public float runtimeDamage = 25f;
        [Min(0.1f)] public float runtimeRadius = 2.5f;
        [Min(0f)] public float runtimeRange = 5f;
        [Min(0f)] public float runtimeForce = 260f;
        [Min(0.05f)] public float runtimeTickInterval = 0.35f;
        [Min(0f)] public float runtimeBleedDps = 6f;
        [Min(0f)] public float runtimeBleedDuration = 4f;
        [Min(0f)] public float runtimeStatusDuration = 3f;
        [Min(1)] public int runtimeMaxStacks = 5;
        [Min(0f)] public float runtimeMoveSpeedPerStack = 0.08f;
        [Tooltip("0 means no absolute move speed cap.")]
        [Min(0f)] public float runtimeMaxMoveSpeed = 0f;
        [Tooltip("Used by slow/time effects. 0.1 means 90% slower.")]
        [Range(0.01f, 1f)] public float runtimeSlowMultiplier = 0.1f;
        [Tooltip("0 means use the first animation clip length. Use this to cut looping VFX short.")]
        [Min(0f)] public float runtimeVfxLifetime = 0f;

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
            runtimeDamage = Mathf.Max(0f, runtimeDamage);
            runtimeRadius = Mathf.Max(0.1f, runtimeRadius);
            runtimeRange = Mathf.Max(0f, runtimeRange);
            runtimeForce = Mathf.Max(0f, runtimeForce);
            runtimeTickInterval = Mathf.Max(0.05f, runtimeTickInterval);
            runtimeBleedDps = Mathf.Max(0f, runtimeBleedDps);
            runtimeBleedDuration = Mathf.Max(0f, runtimeBleedDuration);
            runtimeStatusDuration = Mathf.Max(0f, runtimeStatusDuration);
            runtimeMaxStacks = Mathf.Max(1, runtimeMaxStacks);
            runtimeMoveSpeedPerStack = Mathf.Max(0f, runtimeMoveSpeedPerStack);
            runtimeMaxMoveSpeed = Mathf.Max(0f, runtimeMaxMoveSpeed);
            runtimeSlowMultiplier = Mathf.Clamp(runtimeSlowMultiplier, 0.01f, 1f);
            runtimeVfxLifetime = Mathf.Max(0f, runtimeVfxLifetime);
        }
    }
}
