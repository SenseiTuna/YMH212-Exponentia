using System;
using UnityEngine;
using UnityEngine.UI;

namespace Exponentia.InventorySystem
{
    public class RewardChoiceCardUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text rarityText;
        [SerializeField] private Text effectText;
        [SerializeField] private GameObject voidBadge;
        [SerializeField] private Button selectButton;

        public void Bind(RewardDefinition reward, Action onSelected)
        {
            if (reward == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (iconImage != null)
            {
                iconImage.sprite = reward.icon;
                iconImage.enabled = reward.icon != null;
            }

            if (titleText != null)
            {
                titleText.text = reward.displayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = reward.description;
            }

            if (rarityText != null)
            {
                rarityText.text = reward.rarity.ToString();
            }

            if (effectText != null)
            {
                effectText.text = BuildEffectSummary(reward);
            }

            if (voidBadge != null)
            {
                voidBadge.SetActive(reward.isVoidReward);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                if (onSelected != null)
                {
                    selectButton.onClick.AddListener(() => onSelected.Invoke());
                }
            }
        }

        private static string BuildEffectSummary(RewardDefinition reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            if (reward.rewardType == RewardType.GiveWeapon ||
                reward.rewardType == RewardType.GiveSkill ||
                reward.rewardType == RewardType.GivePassiveItem ||
                reward.rewardType == RewardType.GiveActiveItem)
            {
                return reward.linkedItemDefinition != null
                    ? $"Grants: {reward.linkedItemDefinition.displayName}"
                    : "Grants: Missing Item";
            }

            if (reward.rewardType == RewardType.RestoreHealth)
            {
                return $"+{reward.value:0.##} HP";
            }

            string suffix = reward.modifierType == ModifierType.Percent ? "%" : string.Empty;
            string sign = reward.value >= 0f ? "+" : string.Empty;
            return $"{reward.targetStat}: {sign}{reward.value:0.##}{suffix}";
        }
    }
}
