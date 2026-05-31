using UnityEngine;

public class SpartanEnemy : EnemyMechanics
{
    [Header("Spartan Spear Throw")]
    [SerializeField] private float shootRange = 8.5f;
    [SerializeField] private float attackCooldown = 1.9f;
    [SerializeField] private float projectileSpeed = 5.5f;
    [SerializeField] private float projectileDamage = 11f;
    [SerializeField] private float projectileLifeTime = 4.2f;
    [SerializeField] private float projectileSize = 0.26f;
    [SerializeField] private float projectileSpawnOffset = 0.9f;
    [SerializeField] private Color projectileColor = new Color(0.8f, 0.8f, 0.72f);

    private float nextShootTime;

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Spartan",
            44f,
            1.2f,
            6f,
            15f,
            true,
            5.2f,
            new Color(0.7f, 0.68f, 0.5f),
            new Vector2(1f, 1f));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive || PlayerTarget == null || Time.time < nextShootTime)
        {
            return;
        }

        if (GetDistanceToPlayer() > shootRange)
        {
            return;
        }

        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        nextShootTime = Time.time + attackCooldown;

        SpawnEnemyProjectile(
            "SpartanSpear",
            transform.position + (Vector3)(direction * projectileSpawnOffset),
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            projectileColor,
            projectileSize);
    }
}
