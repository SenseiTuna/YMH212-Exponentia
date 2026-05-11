using UnityEngine;

public class StyxJellyfishEnemy : EnemyMechanics
{
    [Header("Ring Attack")]
    [SerializeField] private float ringCooldown = 3f;
    [SerializeField] private int projectileCount = 8;
    [SerializeField] private float projectileSpeed = 2.8f;
    [SerializeField] private float projectileDamage = 9f;
    [SerializeField] private float projectileLifeTime = 4f;
    [SerializeField] private float projectileSize = 0.2f;
    [SerializeField] private float spawnRadius = 0.7f;
    [SerializeField] private Color projectileColor = new Color(0.55f, 1f, 0.95f);

    private float nextRingTime;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Styx Jellyfish",
            28f,
            0f,
            5f,
            12f,
            false,
            0f,
            new Color(0.45f, 0.9f, 0.95f),
            new Vector2(0.9f, 0.9f));
    }

    protected override void Update()
    {
        base.Update();

        // Ensure player reference is cached
        if (PlayerTarget == null)
        {
            CachePlayerReferences();
            return;
        }

        if (!IsAlive || Time.time < nextRingTime)
        {
            return;
        }

        nextRingTime = Time.time + ringCooldown;
        FireRing();
    }

    private void CachePlayerReferences()
    {
        if (playerMechanics != null && playerMechanics.gameObject != null)
        {
            return;
        }

        playerMechanics = FindAnyObjectByType<PlayerMechanics>();
        if (playerMechanics != null)
        {
            playerTarget = playerMechanics.transform;
        }
    }

    private void FireRing()
    {
        int clampedProjectileCount = Mathf.Max(4, projectileCount);
        float angleStep = 360f / clampedProjectileCount;

        for (int i = 0; i < clampedProjectileCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnRadius);

            SpawnEnemyProjectile(
                "StyxRingShot",
                spawnPosition,
                direction,
                projectileSpeed,
                projectileDamage,
                projectileLifeTime,
                projectileColor,
                projectileSize);
        }
    }
}
