using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    public class RewardChoiceController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RewardPool rewardPool;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private RewardChoiceUI rewardChoiceUI;

        [Header("Config")]
        [Range(2, 3)]
        [SerializeField] private int choiceCount = 3;
        [SerializeField] private bool pauseGameplayWhileChoosing = true;
        [SerializeField] private bool allowVoidRewards = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private readonly List<RewardDefinition> currentChoices = new List<RewardDefinition>(3);
        private bool isOpen;
        private float previousTimeScale = 1f;

        public event Action<IReadOnlyList<RewardDefinition>> OnRewardsGenerated;
        public event Action<RewardDefinition> OnRewardSelected;
        public event Action OnRewardScreenClosed;

        public bool IsOpen => isOpen;
        public IReadOnlyList<RewardDefinition> CurrentChoices => currentChoices;

        private void Reset()
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
            rewardChoiceUI = FindFirstObjectByType<RewardChoiceUI>();
        }

        public void ShowRewardChoices()
        {
            RoomRewardContext defaultContext = RoomRewardContext.DefaultRoomClear();
            defaultContext.allowVoidRewards = allowVoidRewards;
            ShowRewardsForRoom(defaultContext);
        }

        public void ShowRewardsForRoom(RoomRewardContext context)
        {
            if (rewardPool == null)
            {
                Debug.LogWarning("RewardChoiceController: RewardPool reference is missing.", this);
                return;
            }

            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            }

            if (playerInventory == null)
            {
                Debug.LogWarning("RewardChoiceController: PlayerInventory not found.", this);
                return;
            }

            currentChoices.Clear();
            List<RewardDefinition> rolled = rewardPool.GetRandomRewards(context, choiceCount);
            currentChoices.AddRange(rolled);

            if (currentChoices.Count == 0)
            {
                Debug.LogWarning("RewardChoiceController: No rewards generated from pool.", this);
                return;
            }

            isOpen = true;
            if (pauseGameplayWhileChoosing)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            if (rewardChoiceUI != null)
            {
                rewardChoiceUI.ShowChoices(this, currentChoices);
            }
            else if (verboseLogs)
            {
                for (int i = 0; i < currentChoices.Count; i++)
                {
                    RewardDefinition reward = currentChoices[i];
                    if (reward == null)
                    {
                        continue;
                    }

                    Debug.Log($"Reward Choice {i + 1}: {reward.displayName} ({reward.description})", this);
                }
            }

            OnRewardsGenerated?.Invoke(currentChoices);
        }

        public void SelectReward(int index)
        {
            if (!isOpen)
            {
                return;
            }

            if (index < 0 || index >= currentChoices.Count)
            {
                Debug.LogWarning($"RewardChoiceController: Invalid reward index {index}.", this);
                return;
            }

            RewardDefinition selected = currentChoices[index];
            if (selected == null)
            {
                return;
            }

            bool applied = playerInventory != null && playerInventory.ApplyReward(selected);
            if (verboseLogs)
            {
                Debug.Log(applied
                    ? $"RewardChoiceController: Applied reward '{selected.displayName}'."
                    : $"RewardChoiceController: Failed to apply reward '{selected.displayName}'.", this);
            }

            if (applied)
            {
                OnRewardSelected?.Invoke(selected);
            }

            CloseRewardChoices();
        }

        public void CloseRewardChoices()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            currentChoices.Clear();

            if (rewardChoiceUI != null)
            {
                rewardChoiceUI.Hide();
            }

            if (pauseGameplayWhileChoosing)
            {
                Time.timeScale = previousTimeScale;
            }

            OnRewardScreenClosed?.Invoke();
        }
    }
}
