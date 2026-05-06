/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.1.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 00:00
 * DESCRIPTION: ScriptableObject definition for projectile data.
 */

using Exponentia.Core;
using UnityEngine;

namespace Exponentia.Data
{
    [CreateAssetMenu(fileName = "PRJ_NewProjectile", menuName = "Exponentia/Data/Projectile Data")]
    public class ProjectileData : ScriptableObject
    {
        [Header("Identity")]
        public string projectileId;
        public string projectileName;

        [Header("Prefab")]
        public GameObject projectilePrefab;

        [Header("Stats")]
        public float damage = 10f;
        public float speed = 10f;
        public float lifetime = 3f;
        public float size = 1f;
        public float knockbackForce = 0f;

        [Header("Behavior")]
        public bool canPierce = false;
        public int pierceCount = 0;
        public bool canBounce = false;
        public int bounceCount = 0;
        public bool isHoming = false;
        public float homingStrength = 0f;

        [Header("Visual / Audio")]
        public Sprite sprite;
        public GameObject impactVfx;
        public AudioClip impactSound;
    }
}