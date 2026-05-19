using System.Collections.Generic;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    public class RewardChoiceUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private RewardChoiceCardUI[] cardSlots = new RewardChoiceCardUI[3];

        private RewardChoiceController boundController;

        private void Awake()
        {
            Hide();
        }

        public void ShowChoices(RewardChoiceController controller, IReadOnlyList<RewardDefinition> rewards)
        {
            boundController = controller;

            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            for (int i = 0; i < cardSlots.Length; i++)
            {
                RewardChoiceCardUI card = cardSlots[i];
                if (card == null)
                {
                    continue;
                }

                RewardDefinition reward = rewards != null && i < rewards.Count ? rewards[i] : null;
                int selectedIndex = i;
                card.Bind(reward, () =>
                {
                    if (boundController != null)
                    {
                        boundController.SelectReward(selectedIndex);
                    }
                });
            }
        }

        public void Hide()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
