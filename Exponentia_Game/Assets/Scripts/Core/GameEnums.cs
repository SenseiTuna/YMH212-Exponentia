/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.1.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 00:00
 * DESCRIPTION: Common enum definitions for the game.
 */

namespace Exponentia.Core
{
    public enum EntityType
    {
        Player,
        Enemy,
        Boss,
        Neutral
    }

    public enum Faction
    {
        Player,
        Enemy,
        Neutral
    }

    public enum WeaponType
    {
        Melee,
        Ranged,
        MagicBeam,
        Throwing,
        Area
    }


    public enum EnemyType
    {
        Melee,
        Ranged,
        Support,
        Shielder,
        Exploder,
        Elite,
        Boss
    }

    public enum EnemyState
    {
        Chase,
        Attack,
        Stunned,
        Dead
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}