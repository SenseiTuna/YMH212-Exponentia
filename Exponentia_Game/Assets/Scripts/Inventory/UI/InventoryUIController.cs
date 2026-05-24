using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Exponentia.InventorySystem
{
    public class InventoryUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerAttack playerAttack;

        [Header("Weapon UI")]
        [SerializeField] private Image activeWeaponIcon;
        [SerializeField] private Text activeWeaponText;

        [Header("Skill UI")]
        [SerializeField] private Image activeSkillIcon;
        [SerializeField] private Text activeSkillText;
        [SerializeField] private Text activeSkillCooldownText;

        [Header("Active Item UI")]
        [SerializeField] private Image activeItemIcon;
        [SerializeField] private Image activeItemCooldownFill;
        [SerializeField] private Text activeItemCooldownText;

        [Header("Passive UI")]
        [SerializeField] private Transform passiveItemContainer;
        [SerializeField] private GameObject passiveItemEntryPrefab;

        [Header("Debug Fallback")]
        [SerializeField] private Text debugText;
        [SerializeField] private bool logIfNoUi = true;

        private readonly List<GameObject> spawnedPassiveEntries = new List<GameObject>();

        private void Awake()
        {
            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            }

            if (playerAttack == null)
            {
                playerAttack = FindFirstObjectByType<PlayerAttack>();
            }
        }

        private void OnEnable()
        {
            Subscribe(true);
            RefreshAll();
        }

        private void OnDisable()
        {
            Subscribe(false);
        }

        private void Update()
        {
            RefreshCooldowns();
        }

        private void Subscribe(bool subscribe)
        {
            if (playerInventory == null)
            {
                return;
            }

            if (subscribe)
            {
                playerInventory.OnInventoryChanged += RefreshAll;
                playerInventory.OnWeaponChanged += HandleWeaponChanged;
                playerInventory.OnSkillAdded += HandleSkillAdded;
                playerInventory.OnActiveItemChanged += HandleActiveItemChanged;
                playerInventory.OnPassiveItemAdded += HandlePassiveAdded;
            }
            else
            {
                playerInventory.OnInventoryChanged -= RefreshAll;
                playerInventory.OnWeaponChanged -= HandleWeaponChanged;
                playerInventory.OnSkillAdded -= HandleSkillAdded;
                playerInventory.OnActiveItemChanged -= HandleActiveItemChanged;
                playerInventory.OnPassiveItemAdded -= HandlePassiveAdded;
            }
        }

        private void HandleWeaponChanged(WeaponDefinition _)
        {
            RefreshWeapon();
        }

        private void HandleSkillAdded(SkillDefinition _)
        {
            RefreshSkill();
        }

        private void HandleActiveItemChanged(ActiveItemDefinition _)
        {
            RefreshActiveItem();
        }

        private void HandlePassiveAdded(PassiveItemDefinition _, int __)
        {
            RefreshPassiveItems();
        }

        public void RefreshAll()
        {
            RefreshWeapon();
            RefreshSkill();
            RefreshActiveItem();
            RefreshPassiveItems();
            RefreshDebugText();
        }

        private void RefreshWeapon()
        {
            WeaponDefinition weapon = playerInventory != null ? playerInventory.ActiveWeapon : null;

            if (activeWeaponIcon != null)
            {
                activeWeaponIcon.sprite = weapon != null ? weapon.icon : null;
                activeWeaponIcon.enabled = weapon != null && weapon.icon != null;
            }

            if (activeWeaponText != null)
            {
                activeWeaponText.text = weapon != null ? weapon.displayName : "Weapon: None";
            }
        }

        private void RefreshSkill()
        {
            SkillDefinition skill = playerInventory != null ? playerInventory.EquippedSkill : null;

            if (activeSkillIcon != null)
            {
                activeSkillIcon.sprite = skill != null ? skill.icon : null;
                activeSkillIcon.enabled = skill != null && skill.icon != null;
            }

            if (activeSkillText != null)
            {
                activeSkillText.text = skill != null ? skill.displayName : "Skill: None";
            }
        }

        private void RefreshActiveItem()
        {
            ActiveItemDefinition activeItem = playerInventory != null ? playerInventory.ActiveItem : null;

            if (activeItemIcon != null)
            {
                activeItemIcon.sprite = activeItem != null ? activeItem.icon : null;
                activeItemIcon.enabled = activeItem != null && activeItem.icon != null;
            }
        }

        private void RefreshPassiveItems()
        {
            if (passiveItemContainer == null || passiveItemEntryPrefab == null || playerInventory == null)
            {
                return;
            }

            for (int i = 0; i < spawnedPassiveEntries.Count; i++)
            {
                if (spawnedPassiveEntries[i] != null)
                {
                    Destroy(spawnedPassiveEntries[i]);
                }
            }
            spawnedPassiveEntries.Clear();

            IReadOnlyList<PlayerInventory.PassiveItemStackInfo> stacks = playerInventory.GetPassiveStacks();
            for (int i = 0; i < stacks.Count; i++)
            {
                PlayerInventory.PassiveItemStackInfo info = stacks[i];
                if (info.passiveItem == null)
                {
                    continue;
                }

                GameObject entry = Instantiate(passiveItemEntryPrefab, passiveItemContainer);
                spawnedPassiveEntries.Add(entry);

                Image icon = entry.GetComponentInChildren<Image>();
                if (icon != null)
                {
                    icon.sprite = info.passiveItem.icon;
                    icon.enabled = info.passiveItem.icon != null;
                }

                Text text = entry.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = info.stackCount > 1 ? $"x{info.stackCount}" : info.passiveItem.displayName;
                }
            }
        }

        private void RefreshCooldowns()
        {
            if (playerInventory != null)
            {
                float normalized = playerInventory.GetActiveItemCooldownNormalized();
                float remaining = playerInventory.GetActiveItemCooldownRemaining();

                if (activeItemCooldownFill != null)
                {
                    activeItemCooldownFill.fillAmount = normalized;
                }

                if (activeItemCooldownText != null)
                {
                    activeItemCooldownText.text = remaining > 0f ? remaining.ToString("0.0") : string.Empty;
                }
            }

            if (playerAttack != null)
            {
                float skillCooldown = playerAttack.GetEquippedSkillRemainingCooldown();
                if (activeSkillCooldownText != null)
                {
                    activeSkillCooldownText.text = skillCooldown > 0f ? skillCooldown.ToString("0.0") : string.Empty;
                }
            }
        }

        private void RefreshDebugText()
        {
            if (playerInventory == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder(128);
            sb.AppendLine(playerInventory.ActiveWeapon != null
                ? $"Weapon: {playerInventory.ActiveWeapon.displayName}"
                : "Weapon: None");
            sb.AppendLine(playerInventory.EquippedSkill != null
                ? $"Skill: {playerInventory.EquippedSkill.displayName}"
                : "Skill: None");
            sb.AppendLine(playerInventory.ActiveItem != null
                ? $"Active: {playerInventory.ActiveItem.displayName}"
                : "Active: None");

            IReadOnlyList<PlayerInventory.PassiveItemStackInfo> stacks = playerInventory.GetPassiveStacks();
            sb.AppendLine($"Passive Count: {stacks.Count}");

            if (debugText != null)
            {
                debugText.text = sb.ToString();
            }
            else if (logIfNoUi)
            {
                Debug.Log(sb.ToString(), this);
            }
        }
    }
}
