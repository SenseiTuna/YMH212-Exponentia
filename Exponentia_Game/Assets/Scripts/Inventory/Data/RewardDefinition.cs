using UnityEngine;

namespace Exponentia.InventorySystem
{
    [CreateAssetMenu(fileName = "RWD_", menuName = "Exponentia/Inventory/Reward Definition")]
    public class RewardDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string rewardId = "reward.new";
        public string displayName = "New Reward";
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        public ItemRarity rarity = ItemRarity.Common;

        [Header("Behavior")]
        public RewardType rewardType = RewardType.IncreaseDamage;
        public StatType targetStat = StatType.Damage;
        public ModifierType modifierType = ModifierType.Percent;
        public float value = 10f;
        public ItemDefinitionBase linkedItemDefinition;

        [Header("Void")]
        public bool isVoidReward;
        public float voidCorruptionIncrease = 0f;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                rewardId = name.ToLowerInvariant().Replace(" ", ".");
            }

            if (!isVoidReward)
            {
                voidCorruptionIncrease = 0f;
            }
        }
    }
}
