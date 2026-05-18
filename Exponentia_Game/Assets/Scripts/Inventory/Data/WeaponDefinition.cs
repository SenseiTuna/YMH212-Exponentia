using UnityEngine;

namespace Exponentia.InventorySystem
{
    [CreateAssetMenu(fileName = "ITM_Weapon_", menuName = "Exponentia/Inventory/Weapon Definition")]
    public class WeaponDefinition : ItemDefinitionBase
    {
        [Header("Combat")]
        [Tooltip("Weapon damage factor. 1 means default, 1.5 means +50%.")]
        [Min(0f)] public float damage = 1f;
        [Min(0.01f)] public float fireRate = 2f;

        [Header("Projectile")]
        public GameObject projectilePrefab;
        [Min(0f)] public float projectileSpeed = 12f;
        [Min(0.05f)] public float projectileLifetime = 2.5f;
        [Min(1)] public int projectileCount = 1;
        [Min(0f)] public float spreadAngle = 0f;
        [Min(0)] public int pierceCount = 0;
        [Min(0f)] public float areaRadius = 0f;
        [Min(0f)] public float knockback = 0f;

        private void Reset()
        {
            category = ItemCategory.Weapon;
            canStack = false;
            maxStacks = 1;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            category = ItemCategory.Weapon;
            projectileCount = Mathf.Max(1, projectileCount);
            fireRate = Mathf.Max(0.01f, fireRate);
            projectileLifetime = Mathf.Max(0.05f, projectileLifetime);
            damage = Mathf.Max(0f, damage);
        }
    }
}
