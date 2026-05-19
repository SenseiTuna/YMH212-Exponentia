using UnityEngine;

namespace Exponentia.InventorySystem
{
    public enum ItemCategory
    {
        Weapon,
        Skill,
        PassiveItem,
        ActiveItem,
        Reward,
        Currency,
        Consumable
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Divine,
        Void
    }

    public enum RewardType
    {
        RestoreHealth,
        IncreaseMaxHealth,
        IncreaseDamage,
        IncreaseMoveSpeed,
        IncreaseFireRate,
        ReduceCooldown,
        GiveWeapon,
        GiveSkill,
        GivePassiveItem,
        GiveActiveItem,
        VoidTradeOff
    }

    public enum StatType
    {
        MaxHealth,
        CurrentHealth,
        Damage,
        MoveSpeed,
        FireRate,
        CooldownReduction,
        ProjectileSpeed,
        ProjectileCount,
        CritChance,
        Armor
    }

    public enum ModifierType
    {
        Flat,
        Percent
    }

    public enum SkillEffectType
    {
        None,
        LightningBurst,
        WaterBubble,
        ShieldDash,
        AreaDamage,
        Heal,
        Custom
    }

    public enum ActiveEffectType
    {
        None,
        TemporaryShield,
        AreaDamage,
        SlowEnemies,
        Heal,
        Custom
    }

    public enum RewardContextType
    {
        RoomClear,
        BossClear,
        TreasureRoom,
        ChallengeRoom,
        Custom
    }
}
