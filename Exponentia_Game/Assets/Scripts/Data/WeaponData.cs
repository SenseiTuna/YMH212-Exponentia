/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.1.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 00:00
 * DESCRIPTION: ScriptableObject definition for weapon data.
 */

using Exponentia.Core;
using UnityEngine;

namespace Exponentia.Data
{
    [CreateAssetMenu(fileName = "WPN_NewWeapon", menuName = "Exponentia/Data/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponId;
        public string weaponName;
        [TextArea(3, 6)]
        public string description;

        [Header("Type")]
        public WeaponType weaponType = WeaponType.Ranged;
        public Rarity rarity = Rarity.Common;

        [Header("Stats")]
        public float baseDamage = 10f;
        public float baseFireRate = 0.5f;
        public float baseProjectileSpeed = 10f;
        public float baseRange = 8f;
        public float knockbackForce = 0f;

        [Header("Projectile")]
        public ProjectileData projectileData;
        public int projectileCount = 1;
        public float spreadAngle = 0f;
        public bool canPierce = false;
        public int pierceCount = 0;

        [Header("Visual / Audio")]
        public Sprite icon;
        public Sprite weaponSprite;
        public AudioClip attackSound;
        public GameObject attackVfx;
    }
}