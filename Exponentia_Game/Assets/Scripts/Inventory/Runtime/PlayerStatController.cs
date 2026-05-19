using System;
using System.Collections.Generic;
using Exponentia.Player;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerStatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerMechanics playerMechanics;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("Health Behavior")]
        [SerializeField] private bool preserveHealthPercentOnMaxHealthChange;
        [SerializeField] private bool healToNewMaxWhenMaxHealthIncreases;

        [Header("Runtime Extras (Read Only)")]
        [SerializeField] private float runtimeCooldownReduction;
        [SerializeField] private float runtimeProjectileCount = 1f;
        [SerializeField] private float runtimeCritChance;
        [SerializeField] private float runtimeArmor;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs;
        [SerializeField] private List<StatModifier> runtimeModifiers = new List<StatModifier>();

        private bool initialized;

        private float baseMaxHealth;
        private float baseDamage;
        private float baseMoveSpeed;
        private float baseAttackSpeed;
        private float baseProjectileSpeed;
        private float baseDefense;

        private float baseCooldownReduction;
        private float baseProjectileCount = 1f;
        private float baseCritChance;
        private float baseArmor;

        public event Action OnStatsRecalculated;

        public float CooldownReduction => runtimeCooldownReduction;
        public int ProjectileCount => Mathf.Max(1, Mathf.RoundToInt(runtimeProjectileCount));
        public float CritChance => runtimeCritChance;
        public float Armor => runtimeArmor;

        private void Reset()
        {
            playerStats = GetComponent<PlayerStats>();
            playerMechanics = GetComponent<PlayerMechanics>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        private void Awake()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerMechanics == null)
            {
                playerMechanics = GetComponent<PlayerMechanics>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }
        }

        private void Start()
        {
            InitializeFromCurrentStats();
        }

        public void InitializeFromCurrentStats()
        {
            if (playerStats == null)
            {
                Debug.LogError("PlayerStatController: PlayerStats reference is missing.", this);
                return;
            }

            baseMaxHealth = Mathf.Max(1f, playerStats.MaxHealth);
            baseDamage = Mathf.Max(0f, playerStats.Damage);
            baseMoveSpeed = Mathf.Max(0f, playerStats.MoveSpeed);
            baseAttackSpeed = Mathf.Max(0f, playerStats.AttackSpeed);
            baseProjectileSpeed = Mathf.Max(0f, playerStats.ProjectileSpeed);
            baseDefense = Mathf.Max(0f, playerStats.Defense);

            baseCooldownReduction = Mathf.Clamp01(baseCooldownReduction);
            baseProjectileCount = Mathf.Max(1f, baseProjectileCount);
            baseCritChance = Mathf.Clamp01(baseCritChance);
            baseArmor = Mathf.Max(0f, baseArmor);

            initialized = true;
            RecalculateStats();
        }

        public void AddModifier(string sourceId, StatType statType, ModifierType modifierType, float value)
        {
            if (!initialized)
            {
                InitializeFromCurrentStats();
            }

            runtimeModifiers.Add(new StatModifier(sourceId, statType, modifierType, value));
            RecalculateStats();
        }

        public void RemoveSourceModifiers(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            runtimeModifiers.RemoveAll(m => string.Equals(m.sourceId, sourceId, StringComparison.Ordinal));
            RecalculateStats();
        }

        public void SetSourceModifiers(string sourceId, List<StatModifierDefinition> modifiers, int stackCount = 1)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                Debug.LogWarning("PlayerStatController: sourceId is empty.", this);
                return;
            }

            RemoveSourceModifiers(sourceId);

            if (modifiers == null || modifiers.Count == 0)
            {
                RecalculateStats();
                return;
            }

            int safeStacks = Mathf.Max(1, stackCount);
            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifierDefinition definition = modifiers[i];
                if (definition == null)
                {
                    continue;
                }

                float appliedValue = definition.value * safeStacks;
                runtimeModifiers.Add(new StatModifier(sourceId, definition.statType, definition.modifierType, appliedValue));
            }

            RecalculateStats();
        }

        public float GetFinalStatValue(StatType statType)
        {
            switch (statType)
            {
                case StatType.MaxHealth:
                    return playerStats != null ? playerStats.MaxHealth : 0f;
                case StatType.Damage:
                    return playerStats != null ? playerStats.Damage : 0f;
                case StatType.MoveSpeed:
                    return playerStats != null ? playerStats.MoveSpeed : 0f;
                case StatType.FireRate:
                    return playerStats != null ? playerStats.AttackSpeed : 0f;
                case StatType.ProjectileSpeed:
                    return playerStats != null ? playerStats.ProjectileSpeed : 0f;
                case StatType.Armor:
                    return runtimeArmor;
                case StatType.CooldownReduction:
                    return runtimeCooldownReduction;
                case StatType.ProjectileCount:
                    return runtimeProjectileCount;
                case StatType.CritChance:
                    return runtimeCritChance;
                default:
                    return 0f;
            }
        }

        public void RecalculateStats()
        {
            if (!initialized || playerStats == null)
            {
                return;
            }

            float oldMaxHealth = Mathf.Max(1f, playerStats.MaxHealth);
            float oldCurrentHealth = playerMechanics != null ? playerMechanics.MevcutCan : playerStats.CurrentHealth;

            float finalMaxHealth = EvaluateWithModifiers(baseMaxHealth, StatType.MaxHealth, 1f, 100000f);
            float finalDamage = EvaluateWithModifiers(baseDamage, StatType.Damage, 0f, 100000f);
            float finalMoveSpeed = EvaluateWithModifiers(baseMoveSpeed, StatType.MoveSpeed, 0f, 1000f);
            float finalAttackSpeed = EvaluateWithModifiers(baseAttackSpeed, StatType.FireRate, 0f, 1000f);
            float finalProjectileSpeed = EvaluateWithModifiers(baseProjectileSpeed, StatType.ProjectileSpeed, 0f, 1000f);
            float finalDefense = EvaluateWithModifiers(baseDefense, StatType.Armor, 0f, 100000f);

            runtimeCooldownReduction = Mathf.Clamp01(EvaluateWithModifiers(baseCooldownReduction, StatType.CooldownReduction, 0f, 0.95f));
            runtimeProjectileCount = EvaluateWithModifiers(baseProjectileCount, StatType.ProjectileCount, 1f, 64f);
            runtimeCritChance = Mathf.Clamp01(EvaluateWithModifiers(baseCritChance, StatType.CritChance, 0f, 1f));
            runtimeArmor = Mathf.Max(0f, finalDefense);

            playerStats.MaxHealth = finalMaxHealth;
            playerStats.Damage = finalDamage;
            playerStats.MoveSpeed = finalMoveSpeed;
            playerStats.AttackSpeed = finalAttackSpeed;
            playerStats.ProjectileSpeed = finalProjectileSpeed;
            playerStats.Defense = finalDefense;

            float targetHealth = Mathf.Min(oldCurrentHealth, playerStats.MaxHealth);
            if (preserveHealthPercentOnMaxHealthChange && oldMaxHealth > 0.001f)
            {
                float ratio = Mathf.Clamp01(oldCurrentHealth / oldMaxHealth);
                targetHealth = ratio * playerStats.MaxHealth;
            }
            else if (healToNewMaxWhenMaxHealthIncreases && playerStats.MaxHealth > oldMaxHealth)
            {
                targetHealth = playerStats.MaxHealth;
            }

            playerStats.CurrentHealth = Mathf.Clamp(targetHealth, 0f, playerStats.MaxHealth);
            if (playerMechanics != null)
            {
                playerMechanics.SyncResourcesFromStats();
            }

            if (playerMovement != null)
            {
                playerMovement.SetMoveSpeed(playerStats.MoveSpeed);
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"PlayerStatController: HP={playerStats.MaxHealth}, DMG={playerStats.Damage}, SPD={playerStats.MoveSpeed}, FIRERATE={playerStats.AttackSpeed}, CDR={runtimeCooldownReduction:0.##}",
                    this);
            }

            OnStatsRecalculated?.Invoke();
        }

        private float EvaluateWithModifiers(float baseValue, StatType statType, float minValue, float maxValue)
        {
            float flat = 0f;
            float percent = 0f;

            for (int i = 0; i < runtimeModifiers.Count; i++)
            {
                StatModifier modifier = runtimeModifiers[i];
                if (modifier == null || modifier.statType != statType)
                {
                    continue;
                }

                if (modifier.modifierType == ModifierType.Flat)
                {
                    flat += modifier.value;
                }
                else
                {
                    percent += modifier.value;
                }
            }

            float value = baseValue + flat;
            value *= 1f + (percent / 100f);
            return Mathf.Clamp(value, minValue, maxValue);
        }
    }
}
