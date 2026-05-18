/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.4.0
 * BUILD_DATE: 2026-04-30
 * BUILD_TIME: 12:00
 * DESCRIPTION: Runtime player stats that are applied from CharacterData.
 */

using Exponentia.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Exponentia.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Core Stats")]
        [FormerlySerializedAs("can")]
        [SerializeField] private float maxHealth = 100f;
        [FormerlySerializedAs("hasar")]
        [SerializeField] private float damage = 10f;
        [FormerlySerializedAs("hareketHizi")]
        [SerializeField] private float moveSpeed = 5f;
        [FormerlySerializedAs("saldiriHizi")]
        [SerializeField] private float attackSpeed = 1f;
        [FormerlySerializedAs("projectileHizi")]
        [SerializeField] private float projectileSpeed = 14f;
        [FormerlySerializedAs("savunma")]
        [SerializeField] private float defense = 0f;
        [FormerlySerializedAs("canCalma")]
        [SerializeField] private float lifeSteal = 0f;
        [FormerlySerializedAs("kalkan")]
        [SerializeField] private float shield = 0f;
        [FormerlySerializedAs("mana")]
        [SerializeField] private float mana = 100f;

        [Header("Progression")]
        [FormerlySerializedAs("level")]
        [SerializeField] private int level = 1;
        [FormerlySerializedAs("xp")]
        [SerializeField] private float xp = 0f;
        [FormerlySerializedAs("sonrakiLevelXp")]
        [SerializeField] private float nextLevelXp = 100f;
        [FormerlySerializedAs("levelXpCarpani")]
        [SerializeField] private float levelXpMultiplier = 1.35f;
        [FormerlySerializedAs("levelBasinaCanArtisi")]
        [SerializeField] private float levelUpMaxHealthBonus = 15f;
        [FormerlySerializedAs("levelBasinaHasarArtisi")]
        [SerializeField] private float levelUpDamageBonus = 3f;
        [FormerlySerializedAs("levelBasinaManaArtisi")]
        [SerializeField] private float levelUpManaBonus = 10f;
        [FormerlySerializedAs("levelBasinaSavunmaArtisi")]
        [SerializeField] private float levelUpDefenseBonus = 1f;
        [FormerlySerializedAs("levelBasinaKalkanArtisi")]
        [SerializeField] private float levelUpShieldBonus = 2f;

        [Header("Runtime Resources")]
        [SerializeField] private float currentHealth;

        public float MaxHealth { get => maxHealth; set => maxHealth = Mathf.Max(0f, value); }
        public float CurrentHealth { get => currentHealth; set => currentHealth = Mathf.Clamp(value, 0f, maxHealth); }
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = Mathf.Max(0f, value); }
        public float AttackSpeed { get => attackSpeed; set => attackSpeed = Mathf.Max(0f, value); }
        public float ProjectileSpeed { get => projectileSpeed; set => projectileSpeed = Mathf.Max(0f, value); }
        public float Defense { get => defense; set => defense = Mathf.Max(0f, value); }
        public float LifeSteal { get => lifeSteal; set => lifeSteal = Mathf.Max(0f, value); }
        public float Shield { get => shield; set => shield = Mathf.Max(0f, value); }
        public float Mana { get => mana; set => mana = Mathf.Max(0f, value); }
        public int Level { get => level; set => level = Mathf.Max(1, value); }
        public float Xp { get => xp; set => xp = Mathf.Max(0f, value); }
        public float NextLevelXp { get => nextLevelXp; set => nextLevelXp = Mathf.Max(1f, value); }
        public float LevelXpMultiplier { get => levelXpMultiplier; set => levelXpMultiplier = Mathf.Max(1f, value); }
        public float LevelUpMaxHealthBonus { get => levelUpMaxHealthBonus; set => levelUpMaxHealthBonus = Mathf.Max(0f, value); }
        public float LevelUpDamageBonus { get => levelUpDamageBonus; set => levelUpDamageBonus = Mathf.Max(0f, value); }
        public float LevelUpManaBonus { get => levelUpManaBonus; set => levelUpManaBonus = Mathf.Max(0f, value); }
        public float LevelUpDefenseBonus { get => levelUpDefenseBonus; set => levelUpDefenseBonus = Mathf.Max(0f, value); }
        public float LevelUpShieldBonus { get => levelUpShieldBonus; set => levelUpShieldBonus = Mathf.Max(0f, value); }

        private void Awake()
        {
            // Turkish: Sahne başında kaynak değerlerini güvenli aralığa çekiyoruz.
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
            mana = Mathf.Max(0f, mana);
            shield = Mathf.Max(0f, shield);
            level = Mathf.Max(1, level);
            nextLevelXp = Mathf.Max(1f, nextLevelXp);
            levelXpMultiplier = Mathf.Max(1f, levelXpMultiplier);
        }

        public void ApplyStats(float newMaxHealth, float newDamage, float newMoveSpeed, float newAttackSpeed)
        {
            // Turkish: CharacterData.baseStats içinden gelen temel savaş statları burada tek yerden uygulanır.
            MaxHealth = newMaxHealth;
            CurrentHealth = MaxHealth;
            Damage = newDamage;
            MoveSpeed = newMoveSpeed;
            AttackSpeed = newAttackSpeed;
        }

        public void ApplyFromStatBlock(StatBlock statBlock)
        {
            if (statBlock == null)
            {
                Debug.LogWarning("PlayerStats: ApplyFromStatBlock called with null StatBlock.");
                return;
            }

            // Turkish: CharacterData'dan gelen temel statların tamamını runtime'a taşıyoruz.
            MaxHealth = statBlock.maxHealth;
            CurrentHealth = MaxHealth;
            Damage = statBlock.damage;
            MoveSpeed = statBlock.moveSpeed;
            AttackSpeed = statBlock.attackSpeed;
            ProjectileSpeed = statBlock.projectileSpeed;
            Defense = statBlock.defense;
            LifeSteal = statBlock.lifeSteal;
            Shield = statBlock.shield;
            Mana = statBlock.mana;
        }
    }
}
