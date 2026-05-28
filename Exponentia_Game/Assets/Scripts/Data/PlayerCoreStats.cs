/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.1.0
 * BUILD_DATE: 2026-04-30
 * BUILD_TIME: 00:00
 * DESCRIPTION: Shared core stats model used by runtime PlayerStats and data StatBlock.
 */

using System;
using UnityEngine;

namespace Exponentia.Data
{
    [Serializable]
    public class PlayerCoreStats
    {
        [Header("Core Health / Combat")]
        public float maxHealth = 100f;
        public float damage = 10f;
        public float moveSpeed = 5f;
        public float attackSpeed = 1f;

        [Header("Core Advanced")]
        public float projectileSpeed = 14f;
        public float defense = 0f;
        public float lifeSteal = 0f;
        public float shield = 0f;
        public float mana = 100f;
    }
}
