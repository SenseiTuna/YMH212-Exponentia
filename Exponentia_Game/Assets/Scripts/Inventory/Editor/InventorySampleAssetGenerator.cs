using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Exponentia.InventorySystem.EditorTools
{
    public static class InventorySampleAssetGenerator
    {
        private const string Root = "Assets/GameData";

        [MenuItem("Exponentia/Inventory/Create Sample Assets")]
        public static void CreateSampleAssets()
        {
            EnsureFolders();

            WeaponDefinition basicBow = CreateOrLoad<WeaponDefinition>($"{Root}/Items/Weapons/WPN_BasicBow.asset");
            basicBow.itemId = "weapon.basic_bow";
            basicBow.displayName = "Basic Bow";
            basicBow.description = "Balanced starter bow.";
            basicBow.rarity = ItemRarity.Common;
            basicBow.damage = 1f;
            basicBow.fireRate = 2.2f;
            basicBow.projectileSpeed = 14f;
            basicBow.projectileLifetime = 2.2f;
            basicBow.projectileCount = 1;
            basicBow.spreadAngle = 0f;
            MarkDirty(basicBow);

            WeaponDefinition spearShot = CreateOrLoad<WeaponDefinition>($"{Root}/Items/Weapons/WPN_SpearShot.asset");
            spearShot.itemId = "weapon.spear_shot";
            spearShot.displayName = "Spear Shot";
            spearShot.description = "High-velocity piercing throw.";
            spearShot.rarity = ItemRarity.Uncommon;
            spearShot.damage = 1.25f;
            spearShot.fireRate = 1.6f;
            spearShot.projectileSpeed = 18f;
            spearShot.projectileLifetime = 2.8f;
            spearShot.projectileCount = 1;
            spearShot.pierceCount = 1;
            MarkDirty(spearShot);

            WeaponDefinition lightningStaff = CreateOrLoad<WeaponDefinition>($"{Root}/Items/Weapons/WPN_LightningStaff.asset");
            lightningStaff.itemId = "weapon.lightning_staff";
            lightningStaff.displayName = "Lightning Staff";
            lightningStaff.description = "Rapid arcane bolts with light spread.";
            lightningStaff.rarity = ItemRarity.Rare;
            lightningStaff.damage = 0.9f;
            lightningStaff.fireRate = 3.4f;
            lightningStaff.projectileSpeed = 16f;
            lightningStaff.projectileLifetime = 2f;
            lightningStaff.projectileCount = 2;
            lightningStaff.spreadAngle = 8f;
            MarkDirty(lightningStaff);

            PassiveItemDefinition hermesSandals = CreateOrLoad<PassiveItemDefinition>($"{Root}/Items/PassiveItems/PAS_HermesSandals.asset");
            hermesSandals.itemId = "passive.hermes_sandals";
            hermesSandals.displayName = "Hermes Sandals";
            hermesSandals.description = "The messenger's wind raises your stride.";
            hermesSandals.rarity = ItemRarity.Uncommon;
            hermesSandals.canStack = true;
            hermesSandals.maxStacks = 3;
            hermesSandals.statModifiers = new List<StatModifierDefinition>
            {
                new StatModifierDefinition { statType = StatType.MoveSpeed, modifierType = ModifierType.Percent, value = 12f }
            };
            MarkDirty(hermesSandals);

            PassiveItemDefinition aresMark = CreateOrLoad<PassiveItemDefinition>($"{Root}/Items/PassiveItems/PAS_AresMark.asset");
            aresMark.itemId = "passive.ares_mark";
            aresMark.displayName = "Ares Mark";
            aresMark.description = "War sigil that amplifies raw damage.";
            aresMark.rarity = ItemRarity.Rare;
            aresMark.canStack = true;
            aresMark.maxStacks = 3;
            aresMark.statModifiers = new List<StatModifierDefinition>
            {
                new StatModifierDefinition { statType = StatType.Damage, modifierType = ModifierType.Percent, value = 10f }
            };
            MarkDirty(aresMark);

            PassiveItemDefinition artemisEye = CreateOrLoad<PassiveItemDefinition>($"{Root}/Items/PassiveItems/PAS_ArtemisEye.asset");
            artemisEye.itemId = "passive.artemis_eye";
            artemisEye.displayName = "Artemis Eye";
            artemisEye.description = "Sharper focus for critical shots.";
            artemisEye.rarity = ItemRarity.Rare;
            artemisEye.canStack = true;
            artemisEye.maxStacks = 2;
            artemisEye.statModifiers = new List<StatModifierDefinition>
            {
                new StatModifierDefinition { statType = StatType.CritChance, modifierType = ModifierType.Flat, value = 0.08f }
            };
            MarkDirty(artemisEye);

            ActiveItemDefinition athenaShield = CreateOrLoad<ActiveItemDefinition>($"{Root}/Items/ActiveItems/ACT_AthenaShield.asset");
            athenaShield.itemId = "active.athena_shield";
            athenaShield.displayName = "Athena Shield";
            athenaShield.description = "Brief invulnerability and shield refresh.";
            athenaShield.rarity = ItemRarity.Epic;
            athenaShield.cooldown = 20f;
            athenaShield.duration = 2f;
            athenaShield.activeEffectType = ActiveEffectType.TemporaryShield;
            athenaShield.effectPower = 30f;
            MarkDirty(athenaShield);

            SkillDefinition zeusBolt = CreateOrLoad<SkillDefinition>($"{Root}/Skills/SKL_ZeusBolt.asset");
            zeusBolt.itemId = "skill.zeus_bolt";
            zeusBolt.displayName = "Zeus Bolt";
            zeusBolt.description = "A focused lightning burst on activation.";
            zeusBolt.rarity = ItemRarity.Rare;
            zeusBolt.cooldown = 8f;
            zeusBolt.duration = 0.2f;
            zeusBolt.skillEffectType = SkillEffectType.LightningBurst;
            MarkDirty(zeusBolt);

            RewardDefinition maxHealthReward = CreateOrLoad<RewardDefinition>($"{Root}/Rewards/RWD_MaxHealthPlus1.asset");
            maxHealthReward.rewardId = "reward.max_health_plus_1";
            maxHealthReward.displayName = "+1 Max Health";
            maxHealthReward.description = "Increase maximum health by 1.";
            maxHealthReward.rewardType = RewardType.IncreaseMaxHealth;
            maxHealthReward.targetStat = StatType.MaxHealth;
            maxHealthReward.modifierType = ModifierType.Flat;
            maxHealthReward.value = 1f;
            maxHealthReward.rarity = ItemRarity.Common;
            MarkDirty(maxHealthReward);

            RewardDefinition restoreHealthReward = CreateOrLoad<RewardDefinition>($"{Root}/Rewards/RWD_Restore25Health.asset");
            restoreHealthReward.rewardId = "reward.restore_25_health";
            restoreHealthReward.displayName = "Restore 25 Health";
            restoreHealthReward.description = "Recover 25 health immediately.";
            restoreHealthReward.rewardType = RewardType.RestoreHealth;
            restoreHealthReward.targetStat = StatType.CurrentHealth;
            restoreHealthReward.modifierType = ModifierType.Flat;
            restoreHealthReward.value = 25f;
            restoreHealthReward.rarity = ItemRarity.Common;
            MarkDirty(restoreHealthReward);

            RewardDefinition damageReward = CreateOrLoad<RewardDefinition>($"{Root}/Rewards/RWD_Damage10Percent.asset");
            damageReward.rewardId = "reward.damage_10_percent";
            damageReward.displayName = "+10% Damage";
            damageReward.description = "Increase damage by 10%.";
            damageReward.rewardType = RewardType.IncreaseDamage;
            damageReward.targetStat = StatType.Damage;
            damageReward.modifierType = ModifierType.Percent;
            damageReward.value = 10f;
            damageReward.rarity = ItemRarity.Uncommon;
            MarkDirty(damageReward);

            RewardDefinition fireRateReward = CreateOrLoad<RewardDefinition>($"{Root}/Rewards/RWD_FireRate15Percent.asset");
            fireRateReward.rewardId = "reward.fire_rate_15_percent";
            fireRateReward.displayName = "+15% Fire Rate";
            fireRateReward.description = "Increase fire rate by 15%.";
            fireRateReward.rewardType = RewardType.IncreaseFireRate;
            fireRateReward.targetStat = StatType.FireRate;
            fireRateReward.modifierType = ModifierType.Percent;
            fireRateReward.value = 15f;
            fireRateReward.rarity = ItemRarity.Uncommon;
            MarkDirty(fireRateReward);

            RewardDefinition voidReward = CreateOrLoad<RewardDefinition>($"{Root}/Rewards/RWD_VoidDamage40.asset");
            voidReward.rewardId = "reward.void_damage_40";
            voidReward.displayName = "Void Pact";
            voidReward.description = "+40% Damage, but corruption rises.";
            voidReward.rewardType = RewardType.VoidTradeOff;
            voidReward.targetStat = StatType.Damage;
            voidReward.modifierType = ModifierType.Percent;
            voidReward.value = 40f;
            voidReward.rarity = ItemRarity.Void;
            voidReward.isVoidReward = true;
            voidReward.voidCorruptionIncrease = 15f;
            MarkDirty(voidReward);

            RewardPool rewardPool = CreateOrLoad<RewardPool>($"{Root}/RewardPools/RP_Default.asset");
            SerializedObject serializedPool = new SerializedObject(rewardPool);
            SerializedProperty defaultRewardsProperty = serializedPool.FindProperty("defaultRewards");
            defaultRewardsProperty.ClearArray();

            AddRewardEntry(defaultRewardsProperty, maxHealthReward, 1f);
            AddRewardEntry(defaultRewardsProperty, restoreHealthReward, 1f);
            AddRewardEntry(defaultRewardsProperty, damageReward, 1f);
            AddRewardEntry(defaultRewardsProperty, fireRateReward, 1f);
            AddRewardEntry(defaultRewardsProperty, voidReward, 0.35f);

            serializedPool.ApplyModifiedPropertiesWithoutUndo();
            MarkDirty(rewardPool);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("InventorySampleAssetGenerator: sample assets created/updated.");
        }

        private static void AddRewardEntry(SerializedProperty listProperty, RewardDefinition reward, float weight)
        {
            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("reward").objectReferenceValue = reward;
            element.FindPropertyRelative("weight").floatValue = weight;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "GameData");
            EnsureFolder($"{Root}", "Items");
            EnsureFolder($"{Root}/Items", "Weapons");
            EnsureFolder($"{Root}/Items", "PassiveItems");
            EnsureFolder($"{Root}/Items", "ActiveItems");
            EnsureFolder($"{Root}", "Skills");
            EnsureFolder($"{Root}", "Rewards");
            EnsureFolder($"{Root}", "RewardPools");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static T CreateOrLoad<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void MarkDirty(Object obj)
        {
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
            }
        }
    }
}
