/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.1.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 00:00
 * DESCRIPTION: ScriptableObject definition for enemy data.
 */

using Exponentia.Core;
using UnityEngine;

namespace Exponentia.Data
{
    [CreateAssetMenu(fileName = "EN_NewEnemy", menuName = "Exponentia/Data/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId;
        public string enemyName;
        [TextArea(3, 6)]
        public string description;

        [Header("Classification")]
        public EnemyType enemyType = EnemyType.Melee;
        public string layerTheme;
        public int difficultyRating = 1;

        [Header("Base Stats")]
        public StatBlock baseStats = new StatBlock();

        [Header("AI")]
        public float detectionRange = 8f;
        public float attackRange = 5f;
        public float attackCooldown = 1.5f;
        public float contactDamage = 10f;

        [Header("Projectile")]
        public ProjectileData projectileData;

        [Header("Spawn")]
        public int spawnWeight = 10;
        public bool isFlying = false;
        public bool isShielded = false;
        public bool canBeStunned = true;

        [Header("Visual / Audio")]
        public Sprite sprite;
        public RuntimeAnimatorController animatorController;
        public GameObject deathVfx;
        public AudioClip deathSound;
    }
}