using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    [CreateAssetMenu(fileName = "RewardPool_", menuName = "Exponentia/Inventory/Reward Pool")]
    public class RewardPool : ScriptableObject
    {
        [Serializable]
        public class RewardPoolEntry
        {
            public RewardDefinition reward;
            [Min(0f)] public float weight = 1f;
        }

        [Serializable]
        public class ContextRewardBucket
        {
            public RewardContextType contextType = RewardContextType.RoomClear;
            [Min(1)] public int minFloorIndex = 1;
            [Min(1)] public int maxFloorIndex = 99;
            public List<RewardPoolEntry> rewards = new List<RewardPoolEntry>();
        }

        [Serializable]
        public class RarityWeight
        {
            public ItemRarity rarity = ItemRarity.Common;
            [Min(0f)] public float weight = 1f;
        }

        [Header("Fallback Rewards")]
        [SerializeField] private List<RewardPoolEntry> defaultRewards = new List<RewardPoolEntry>();

        [Header("Context Buckets")]
        [SerializeField] private List<ContextRewardBucket> contextBuckets = new List<ContextRewardBucket>();

        [Header("Rarity Roll Weights")]
        [SerializeField] private List<RarityWeight> rarityWeights = new List<RarityWeight>
        {
            new RarityWeight { rarity = ItemRarity.Common, weight = 55f },
            new RarityWeight { rarity = ItemRarity.Uncommon, weight = 25f },
            new RarityWeight { rarity = ItemRarity.Rare, weight = 12f },
            new RarityWeight { rarity = ItemRarity.Epic, weight = 5f },
            new RarityWeight { rarity = ItemRarity.Legendary, weight = 2f },
            new RarityWeight { rarity = ItemRarity.Divine, weight = 0.9f },
            new RarityWeight { rarity = ItemRarity.Void, weight = 0.1f }
        };

        public List<RewardDefinition> GetRandomRewards(RoomRewardContext context, int count)
        {
            int safeCount = Mathf.Clamp(count, 1, 8);
            List<RewardPoolEntry> candidates = BuildCandidates(context);
            List<RewardDefinition> result = new List<RewardDefinition>(safeCount);

            for (int i = 0; i < safeCount; i++)
            {
                if (candidates.Count == 0)
                {
                    break;
                }

                ItemRarity rolledRarity = RollRarity();
                RewardPoolEntry selected = PickEntry(candidates, rolledRarity);
                if (selected == null || selected.reward == null)
                {
                    break;
                }

                result.Add(selected.reward);
                candidates.Remove(selected);
            }

            return result;
        }

        private List<RewardPoolEntry> BuildCandidates(RoomRewardContext context)
        {
            List<RewardPoolEntry> bucketRewards = new List<RewardPoolEntry>();
            if (context != null)
            {
                for (int i = 0; i < contextBuckets.Count; i++)
                {
                    ContextRewardBucket bucket = contextBuckets[i];
                    if (bucket == null || bucket.rewards == null)
                    {
                        continue;
                    }

                    if (bucket.contextType != context.contextType)
                    {
                        continue;
                    }

                    if (context.floorIndex < bucket.minFloorIndex || context.floorIndex > bucket.maxFloorIndex)
                    {
                        continue;
                    }

                    bucketRewards.AddRange(bucket.rewards);
                }
            }

            List<RewardPoolEntry> source = bucketRewards.Count > 0 ? bucketRewards : defaultRewards;
            List<RewardPoolEntry> filtered = new List<RewardPoolEntry>(source.Count);
            bool allowVoid = context == null || context.allowVoidRewards;

            for (int i = 0; i < source.Count; i++)
            {
                RewardPoolEntry entry = source[i];
                if (entry == null || entry.reward == null)
                {
                    continue;
                }

                if (!allowVoid && entry.reward.isVoidReward)
                {
                    continue;
                }

                if (entry.weight <= 0f)
                {
                    continue;
                }

                filtered.Add(entry);
            }

            return filtered;
        }

        private RewardPoolEntry PickEntry(List<RewardPoolEntry> entries, ItemRarity preferredRarity)
        {
            List<RewardPoolEntry> rarityFiltered = new List<RewardPoolEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                RewardPoolEntry entry = entries[i];
                if (entry == null || entry.reward == null)
                {
                    continue;
                }

                if (entry.reward.rarity == preferredRarity)
                {
                    rarityFiltered.Add(entry);
                }
            }

            List<RewardPoolEntry> pickList = rarityFiltered.Count > 0 ? rarityFiltered : entries;
            return WeightedPick(pickList);
        }

        private RewardPoolEntry WeightedPick(List<RewardPoolEntry> list)
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            float total = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                RewardPoolEntry entry = list[i];
                if (entry != null)
                {
                    total += Mathf.Max(0f, entry.weight);
                }
            }

            if (total <= 0.0001f)
            {
                return list[UnityEngine.Random.Range(0, list.Count)];
            }

            float roll = UnityEngine.Random.value * total;
            float cumulative = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                RewardPoolEntry entry = list[i];
                if (entry == null)
                {
                    continue;
                }

                cumulative += Mathf.Max(0f, entry.weight);
                if (roll <= cumulative)
                {
                    return entry;
                }
            }

            return list[list.Count - 1];
        }

        private ItemRarity RollRarity()
        {
            float total = 0f;
            for (int i = 0; i < rarityWeights.Count; i++)
            {
                RarityWeight weight = rarityWeights[i];
                if (weight != null)
                {
                    total += Mathf.Max(0f, weight.weight);
                }
            }

            if (total <= 0.0001f)
            {
                return ItemRarity.Common;
            }

            float roll = UnityEngine.Random.value * total;
            float cumulative = 0f;
            for (int i = 0; i < rarityWeights.Count; i++)
            {
                RarityWeight weight = rarityWeights[i];
                if (weight == null)
                {
                    continue;
                }

                cumulative += Mathf.Max(0f, weight.weight);
                if (roll <= cumulative)
                {
                    return weight.rarity;
                }
            }

            return ItemRarity.Common;
        }
    }
}
