using UnityEngine;

public class SatyrEnemy : EnemyMechanics
{
    [Header("Satyr Attack")]
    [SerializeField] private float shootRange = 7f;
    [SerializeField] private float attackCooldown = 2.2f;
    [SerializeField] private float projectileSpeed = 4.2f;
    [SerializeField] private float projectileDamage = 14f;
    [SerializeField] private float projectileLifeTime = 4f;
    [SerializeField] private float projectileSize = 0.34f;
    [SerializeField] private float shootOffset = 0.85f;
    [SerializeField] private float curveOffset = 1.1f;
    [SerializeField] private float explosionRadius = 1.1f;
    [SerializeField] private Color projectileColor = new Color(0.75f, 0.45f, 0.25f);

    private float nextShootTime;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Satyr",
            38f,
            1f,
            7f,
            14f,
            true,
            4.8f,
            new Color(0.7f, 0.52f, 0.32f),
            new Vector2(0.95f, 0.95f));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive || PlayerTarget == null || Time.time < nextShootTime)
        {
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();
        if (distanceToPlayer > shootRange)
        {
            return;
        }

        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        nextShootTime = Time.time + attackCooldown;

        Vector3 spawnPosition = transform.position + (Vector3)(direction * shootOffset);
        Vector2 targetPosition = PlayerTarget.position;
        float travelDuration = Mathf.Max(0.2f, distanceToPlayer / Mathf.Max(0.1f, projectileSpeed));

        EnemyProjectile projectile = SpawnEnemyProjectile(
            "SatyrWineJar",
            spawnPosition,
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            projectileColor,
            projectileSize);

        projectile.ConfigureCurvedPath(targetPosition, travelDuration, curveOffset, explosionRadius);
    }
}
