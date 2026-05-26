using System;
using System.Collections.Generic;
using Exponentia.Player;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    [DisallowMultipleComponent]
    public class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        public struct PassiveItemStackInfo
        {
            public PassiveItemDefinition passiveItem;
            public int stackCount;
        }

        [Serializable]
        private class PassiveStackEntry
        {
            public PassiveItemDefinition passiveItem;
            public int stackCount = 1;
        }

        [Header("References")]
        [SerializeField] private PlayerStatController statController;
        [SerializeField] private PlayerMechanics playerMechanics;
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private VoidCorruptionManager voidCorruptionManager;

        [Header("Inventory State")]
        [SerializeField] private WeaponDefinition activeWeapon;
        [SerializeField] private SkillDefinition equippedSkill;
        [SerializeField] private ActiveItemDefinition activeItem;
        [SerializeField] private List<WeaponDefinition> weaponInventory = new List<WeaponDefinition>();
        [SerializeField] private List<SkillDefinition> skillInventory = new List<SkillDefinition>();
        [SerializeField] private List<PassiveStackEntry> passiveInventory = new List<PassiveStackEntry>();
        [SerializeField] private List<RewardDefinition> rewardHistory = new List<RewardDefinition>();

        [Header("Active Item Runtime")]
        [SerializeField] private bool allowKeyboardActiveItemTest = true;
        [SerializeField] private KeyCode activeItemTestKey = KeyCode.Q;
        [SerializeField] private LayerMask activeItemDamageMask = ~0;
        [SerializeField] private float activeItemAreaRadius = 2.5f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private float nextActiveItemUseTime;
        private int rewardApplyCounter;

        public event Action OnInventoryChanged;
        public event Action<WeaponDefinition> OnWeaponChanged;
        public event Action<SkillDefinition> OnSkillAdded;
        public event Action<PassiveItemDefinition, int> OnPassiveItemAdded;
        public event Action<ActiveItemDefinition> OnActiveItemChanged;
        public event Action<RewardDefinition> OnRewardApplied;

        public WeaponDefinition ActiveWeapon => activeWeapon;
        public SkillDefinition EquippedSkill => equippedSkill;
        public ActiveItemDefinition ActiveItem => activeItem;
        public IReadOnlyList<WeaponDefinition> Weapons => weaponInventory;
        public IReadOnlyList<SkillDefinition> Skills => skillInventory;
        public IReadOnlyList<RewardDefinition> RewardHistory => rewardHistory;

        private void Reset()
        {
            statController = GetComponent<PlayerStatController>();
            playerMechanics = GetComponent<PlayerMechanics>();
            playerAttack = GetComponent<PlayerAttack>();
            voidCorruptionManager = FindFirstObjectByType<VoidCorruptionManager>();
        }

        private void Awake()
        {
            if (statController == null)
            {
                statController = GetComponent<PlayerStatController>();
            }

            if (playerMechanics == null)
            {
                playerMechanics = GetComponent<PlayerMechanics>();
            }

            if (playerAttack == null)
            {
                playerAttack = GetComponent<PlayerAttack>();
            }

            if (voidCorruptionManager == null)
            {
                voidCorruptionManager = FindFirstObjectByType<VoidCorruptionManager>();
            }
        }

        private void Start()
        {
            if (activeWeapon != null)
            {
                AddWeapon(activeWeapon);
            }

            if (activeItem != null)
            {
                SetActiveItem(activeItem);
            }

            SyncEquippedSkillToPlayerAttack();
        }

        private void Update()
        {
            if (allowKeyboardActiveItemTest && Input.GetKeyDown(activeItemTestKey))
            {
                TryUseActiveItem();
            }
        }

        public bool AddItem(ItemDefinitionBase item)
        {
            if (item == null)
            {
                Debug.LogWarning("PlayerInventory: AddItem called with null item.", this);
                return false;
            }

            switch (item.category)
            {
                case ItemCategory.Weapon:
                    return AddWeapon(item as WeaponDefinition);
                case ItemCategory.Skill:
                    return AddSkill(item as SkillDefinition);
                case ItemCategory.PassiveItem:
                    return AddPassiveItem(item as PassiveItemDefinition);
                case ItemCategory.ActiveItem:
                    return SetActiveItem(item as ActiveItemDefinition);
                default:
                    Debug.LogWarning($"PlayerInventory: Unsupported item category '{item.category}' for {item.name}.", this);
                    return false;
            }
        }

        public bool AddWeapon(WeaponDefinition weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("PlayerInventory: AddWeapon called with null weapon.", this);
                return false;
            }

            if (!ContainsWeapon(weapon.itemId))
            {
                weaponInventory.Add(weapon);
            }

            activeWeapon = weapon;
            if (playerAttack != null)
            {
                playerAttack.ApplyWeaponDefinition(weapon);
            }
            else
            {
                Debug.LogWarning("PlayerInventory: PlayerAttack reference missing, weapon visuals will not change.", this);
            }

            if (verboseLogs)
            {
                Debug.Log($"Inventory: Equipped weapon '{weapon.displayName}'.", this);
            }

            OnWeaponChanged?.Invoke(activeWeapon);
            RaiseInventoryChanged();
            return true;
        }

        public bool AddSkill(SkillDefinition skill)
        {
            if (skill == null)
            {
                Debug.LogWarning("PlayerInventory: AddSkill called with null skill.", this);
                return false;
            }

            if (ContainsSkill(skill.itemId))
            {
                if (verboseLogs)
                {
                    Debug.Log($"Inventory: Skill '{skill.displayName}' already owned, skipping duplicate.", this);
                }
                return false;
            }

            skillInventory.Add(skill);

            // Yeni gelen skill, aktif skill slotunu her zaman devralir.
            equippedSkill = skill;

            SyncEquippedSkillToPlayerAttack();

            if (verboseLogs)
            {
                Debug.Log($"Inventory: Added skill '{skill.displayName}'.", this);
            }

            OnSkillAdded?.Invoke(skill);
            RaiseInventoryChanged();
            return true;
        }

        public bool EquipSkill(SkillDefinition skill)
        {
            if (skill == null || !ContainsSkill(skill.itemId))
            {
                return false;
            }

            equippedSkill = skill;
            SyncEquippedSkillToPlayerAttack();
            RaiseInventoryChanged();
            return true;
        }

        public bool AddPassiveItem(PassiveItemDefinition passive)
        {
            if (passive == null)
            {
                Debug.LogWarning("PlayerInventory: AddPassiveItem called with null passive.", this);
                return false;
            }

            PassiveStackEntry entry = FindPassiveEntry(passive.itemId);
            if (entry == null)
            {
                entry = new PassiveStackEntry
                {
                    passiveItem = passive,
                    stackCount = 1
                };
                passiveInventory.Add(entry);
            }
            else
            {
                if (!passive.allowDuplicatePickup || !passive.canStack)
                {
                    if (verboseLogs)
                    {
                        Debug.Log($"Inventory: Passive '{passive.displayName}' cannot stack. Duplicate ignored.", this);
                    }
                    return false;
                }

                if (entry.stackCount >= passive.maxStacks)
                {
                    if (verboseLogs)
                    {
                        Debug.Log($"Inventory: Passive '{passive.displayName}' reached max stacks ({passive.maxStacks}).", this);
                    }
                    return false;
                }

                entry.stackCount += 1;
            }

            ApplyPassiveStatEntry(entry);

            if (verboseLogs)
            {
                Debug.Log($"Inventory: Passive '{passive.displayName}' stack={entry.stackCount}.", this);
            }

            OnPassiveItemAdded?.Invoke(passive, entry.stackCount);
            RaiseInventoryChanged();
            return true;
        }

        public bool SetActiveItem(ActiveItemDefinition newActiveItem)
        {
            if (newActiveItem == null)
            {
                Debug.LogWarning("PlayerInventory: SetActiveItem called with null item.", this);
                return false;
            }

            activeItem = newActiveItem;
            nextActiveItemUseTime = 0f;

            if (verboseLogs)
            {
                Debug.Log($"Inventory: Active item set to '{newActiveItem.displayName}'.", this);
            }

            OnActiveItemChanged?.Invoke(activeItem);
            RaiseInventoryChanged();
            return true;
        }

        public bool TryUseActiveItem()
        {
            if (activeItem == null)
            {
                return false;
            }

            if (Time.time < nextActiveItemUseTime)
            {
                return false;
            }

            bool used = ApplyActiveItemEffect(activeItem);
            if (!used)
            {
                return false;
            }

            float cooldownMultiplier = 1f;
            if (statController != null)
            {
                cooldownMultiplier = 1f - Mathf.Clamp01(statController.CooldownReduction);
            }

            float effectiveCooldown = Mathf.Max(0f, activeItem.cooldown * cooldownMultiplier);
            nextActiveItemUseTime = Time.time + effectiveCooldown;
            RaiseInventoryChanged();
            return true;
        }

        public float GetActiveItemCooldownRemaining()
        {
            return Mathf.Max(0f, nextActiveItemUseTime - Time.time);
        }

        public float GetActiveItemCooldownNormalized()
        {
            if (activeItem == null || activeItem.cooldown <= 0.001f)
            {
                return 0f;
            }

            return Mathf.Clamp01(GetActiveItemCooldownRemaining() / activeItem.cooldown);
        }

        public bool ApplyReward(RewardDefinition reward)
        {
            if (reward == null)
            {
                Debug.LogWarning("PlayerInventory: ApplyReward called with null reward.", this);
                return false;
            }

            bool applied = false;
            switch (reward.rewardType)
            {
                case RewardType.RestoreHealth:
                    applied = ApplyRestoreHealthReward(reward.value);
                    break;
                case RewardType.IncreaseMaxHealth:
                case RewardType.IncreaseDamage:
                case RewardType.IncreaseMoveSpeed:
                case RewardType.IncreaseFireRate:
                case RewardType.ReduceCooldown:
                    applied = ApplyStatReward(reward);
                    break;
                case RewardType.GiveWeapon:
                case RewardType.GiveSkill:
                case RewardType.GivePassiveItem:
                case RewardType.GiveActiveItem:
                    applied = reward.linkedItemDefinition != null && AddItem(reward.linkedItemDefinition);
                    break;
                case RewardType.VoidTradeOff:
                    applied = ApplyStatReward(reward);
                    break;
            }

            if (!applied)
            {
                return false;
            }

            rewardHistory.Add(reward);
            if (reward.isVoidReward && reward.voidCorruptionIncrease > 0f)
            {
                if (voidCorruptionManager == null)
                {
                    voidCorruptionManager = FindFirstObjectByType<VoidCorruptionManager>();
                }

                if (voidCorruptionManager != null)
                {
                    voidCorruptionManager.AddCorruption(reward.voidCorruptionIncrease);
                }
                else
                {
                    Debug.LogWarning("PlayerInventory: Void reward selected but VoidCorruptionManager not found.", this);
                }
            }

            OnRewardApplied?.Invoke(reward);
            RaiseInventoryChanged();
            return true;
        }

        public bool HasItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (ContainsWeapon(itemId) || ContainsSkill(itemId))
            {
                return true;
            }

            return FindPassiveEntry(itemId) != null || (activeItem != null && activeItem.itemId == itemId);
        }

        public int GetStackCount(string itemId)
        {
            PassiveStackEntry entry = FindPassiveEntry(itemId);
            return entry != null ? entry.stackCount : 0;
        }

        public IReadOnlyList<PassiveItemStackInfo> GetPassiveStacks()
        {
            List<PassiveItemStackInfo> result = new List<PassiveItemStackInfo>(passiveInventory.Count);
            for (int i = 0; i < passiveInventory.Count; i++)
            {
                PassiveStackEntry entry = passiveInventory[i];
                if (entry == null || entry.passiveItem == null)
                {
                    continue;
                }

                result.Add(new PassiveItemStackInfo
                {
                    passiveItem = entry.passiveItem,
                    stackCount = entry.stackCount
                });
            }

            return result;
        }

        private bool ApplyRestoreHealthReward(float amount)
        {
            if (playerMechanics == null)
            {
                playerMechanics = GetComponent<PlayerMechanics>();
            }

            if (playerMechanics == null)
            {
                Debug.LogWarning("PlayerInventory: RestoreHealth reward failed, PlayerMechanics not found.", this);
                return false;
            }

            playerMechanics.Heal(Mathf.Max(0f, amount));
            return true;
        }

        private bool ApplyStatReward(RewardDefinition reward)
        {
            if (statController == null)
            {
                statController = GetComponent<PlayerStatController>();
            }

            if (statController == null)
            {
                Debug.LogWarning("PlayerInventory: Stat reward failed, PlayerStatController not found.", this);
                return false;
            }

            StatType statType = ResolveRewardStatType(reward);
            string sourceId = $"reward:{reward.rewardId}:{rewardApplyCounter++}";
            statController.AddModifier(sourceId, statType, reward.modifierType, reward.value);
            return true;
        }

        private StatType ResolveRewardStatType(RewardDefinition reward)
        {
            switch (reward.rewardType)
            {
                case RewardType.IncreaseMaxHealth:
                    return StatType.MaxHealth;
                case RewardType.IncreaseDamage:
                    return StatType.Damage;
                case RewardType.IncreaseMoveSpeed:
                    return StatType.MoveSpeed;
                case RewardType.IncreaseFireRate:
                    return StatType.FireRate;
                case RewardType.ReduceCooldown:
                    return StatType.CooldownReduction;
                default:
                    return reward.targetStat;
            }
        }

        private bool ApplyActiveItemEffect(ActiveItemDefinition item)
        {
            if (item == null)
            {
                return false;
            }

            switch (item.activeEffectType)
            {
                case ActiveEffectType.TemporaryShield:
                    if (playerMechanics != null)
                    {
                        playerMechanics.KalkanYenile(Mathf.Max(0f, item.effectPower));
                        playerMechanics.SetTemporaryInvulnerable(Mathf.Max(0f, item.duration));
                        return true;
                    }
                    return false;

                case ActiveEffectType.Heal:
                    return ApplyRestoreHealthReward(item.effectPower);

                case ActiveEffectType.AreaDamage:
                    if (playerMechanics == null)
                    {
                        return false;
                    }

                    float radius = item.duration > 0f ? item.duration : activeItemAreaRadius;
                    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.1f, radius), activeItemDamageMask);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        Collider2D hit = hits[i];
                        if (hit == null || !hit.CompareTag("Enemy"))
                        {
                            continue;
                        }

                        float multiplier = Mathf.Max(0f, item.effectPower);
                        if (multiplier <= 0.001f)
                        {
                            multiplier = 1f;
                        }

                        playerMechanics.DealDamage(hit.gameObject, multiplier);
                    }
                    return true;

                case ActiveEffectType.SlowEnemies:
                case ActiveEffectType.Custom:
                case ActiveEffectType.None:
                default:
                    if (verboseLogs)
                    {
                        Debug.Log($"Inventory: Active item '{item.displayName}' triggered without explicit runtime effect implementation.", this);
                    }
                    return true;
            }
        }

        private void ApplyPassiveStatEntry(PassiveStackEntry entry)
        {
            if (entry == null || entry.passiveItem == null || statController == null)
            {
                return;
            }

            string sourceId = $"passive:{entry.passiveItem.itemId}";
            statController.SetSourceModifiers(sourceId, entry.passiveItem.statModifiers, entry.stackCount);
        }

        private PassiveStackEntry FindPassiveEntry(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            for (int i = 0; i < passiveInventory.Count; i++)
            {
                PassiveStackEntry entry = passiveInventory[i];
                if (entry == null || entry.passiveItem == null)
                {
                    continue;
                }

                if (string.Equals(entry.passiveItem.itemId, itemId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private bool ContainsWeapon(string itemId)
        {
            for (int i = 0; i < weaponInventory.Count; i++)
            {
                WeaponDefinition weapon = weaponInventory[i];
                if (weapon != null && weapon.itemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsSkill(string itemId)
        {
            for (int i = 0; i < skillInventory.Count; i++)
            {
                SkillDefinition skill = skillInventory[i];
                if (skill != null && skill.itemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private void RaiseInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        private void SyncEquippedSkillToPlayerAttack()
        {
            if (playerAttack == null || equippedSkill == null)
            {
                return;
            }

            playerAttack.TryEquipSkillByDefinition(equippedSkill);
        }
    }
}
