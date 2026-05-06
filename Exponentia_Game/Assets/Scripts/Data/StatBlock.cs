/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.2.0
 * BUILD_DATE: 2026-04-30
 * BUILD_TIME: 00:00
 * DESCRIPTION: Shared stat container for player, enemy, boss, weapon and abilities.
 */

using System;
using UnityEngine;

namespace Exponentia.Data
{
    [Serializable]
    public class StatBlock : PlayerCoreStats
    {
        [Header("Extra Health")]
        public float armor = 0f;

        [Header("Extra Movement")]
        public float knockbackResistance = 0f;
        public bool isDashing = false;
        public float dashSpeed = 10f;
        public float dashDuration = 0.5f;

        [Header("Extra Combat")]
        public float fireRate = 0.5f;
        public float range = 8f;

        [Header("Extra Projectile")]
        public float projectileSize = 1f;
        public float projectileLifetime = 3f;

        [Header("Critical")]
        public float critChance = 0.05f;
        public float critMultiplier = 1.5f;

        [Header("Utility")]
        [Range(0f, 1f)]
        public float cooldownReduction = 0f;
        public float luck = 0f;
        public float pickupRange = 2f;
    }
}
