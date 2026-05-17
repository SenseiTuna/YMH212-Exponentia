using UnityEngine;

public class CreeperSnakeEnemy : EnemyMechanics
{
    [Header("Creeper Snake Trap")]
    [SerializeField] private float triggerRange = 3.4f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float whipRange = 1.4f;
    [SerializeField] private float whipDamage = 13f;
    [SerializeField] private float projectileSpeed = 3.8f;
    [SerializeField] private float projectileDamage = 8f;
    [SerializeField] private float projectileLifeTime = 3.5f;
    [SerializeField] private float projectileSize = 0.24f;
    [SerializeField] private float projectileSpawnOffset = 0.7f;
    [SerializeField] private float sideProjectileAngle = 55f;
    [SerializeField] private Color projectileColor = new Color(0.45f, 0.9f, 0.35f);

    private float nextAttackTime;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Creeper Snake",
            34f,
            0f,
            5f,
            13f,
            false,
            0f,
            new Color(0.28f, 0.62f, 0.24f),
            new Vector2(0.9f, 0.9f));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive || PlayerTarget == null || Time.time < nextAttackTime)
        {
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();
        if (distanceToPlayer > triggerRange)
        {
            return;
        }

        Vector2 directionToPlayer = GetDirectionToPlayer();
        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;

        if (distanceToPlayer <= whipRange && PlayerMechanics != null)
        {
            PlayerMechanics.TakeDamage(whipDamage);
        }

        FireSideProjectiles(directionToPlayer);
    }

    private void FireSideProjectiles(Vector2 directionToPlayer)
    {
        Vector2 leftDirection = Quaternion.Euler(0f, 0f, sideProjectileAngle) * directionToPlayer;
        Vector2 rightDirection = Quaternion.Euler(0f, 0f, -sideProjectileAngle) * directionToPlayer;

        SpawnEnemyProjectile(
            "CreeperSnakeLeftShot",
            transform.position + (Vector3)(leftDirection.normalized * projectileSpawnOffset),
            leftDirection,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            projectileColor,
            projectileSize);

        SpawnEnemyProjectile(
            "CreeperSnakeRightShot",
            transform.position + (Vector3)(rightDirection.normalized * projectileSpawnOffset),
            rightDirection,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            projectileColor,
            projectileSize);
    }
}
