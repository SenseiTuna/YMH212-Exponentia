using UnityEngine;

public class LightningCrowEnemy : EnemyMechanics
{
    [Header("Lightning Crow Attack")]
    [SerializeField] private float shootRange = 8f;
    [SerializeField] private float attackCooldown = 2.1f;
    [SerializeField] private float projectileSpeed = 2.2f;
    [SerializeField] private float projectileDamage = 13f;
    [SerializeField] private float projectileLifeTime = 4.5f;
    [SerializeField] private float projectileSize = 0.42f;
    [SerializeField] private float projectileSpawnOffset = 0.85f;
    [SerializeField] private float spreadAngle = 12f;
    [SerializeField] private Color projectileColor = new Color(0.5f, 0.95f, 1f);

    private float nextShootTime;

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Lightning Crow",
            26f,
            3.6f,
            9f,
            16f,
            true,
            4.5f,
            new Color(0.55f, 0.75f, 0.95f),
            new Vector2(0.9f, 0.9f));
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

        FireTornado("LightningCrowLeftStorm", Quaternion.Euler(0f, 0f, spreadAngle) * direction);
        FireTornado("LightningCrowRightStorm", Quaternion.Euler(0f, 0f, -spreadAngle) * direction);
    }

    private void FireTornado(string projectileName, Vector2 direction)
    {
        EnemyProjectile projectile = SpawnEnemyProjectile(
            projectileName,
            transform.position + (Vector3)(direction.normalized * projectileSpawnOffset),
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            projectileColor,
            projectileSize);

        if (projectile != null)
        {
            projectile.SetRotateVisualToDirection(false);
        }
    }
}
