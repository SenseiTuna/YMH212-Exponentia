using UnityEngine;

public class CerberosHatchlingEnemy : EnemyMechanics
{
    [Header("Cerberos Attack")]
    [SerializeField] private float shootRange = 6f;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private float projectileSpeed = 4f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float projectileLifeTime = 3.5f;
    [SerializeField] private float projectileSize = 0.24f;
    [SerializeField] private float projectileSpawnOffset = 0.7f;
    [SerializeField] private float spreadAngle = 20f;
    [SerializeField] private Color projectileColor = new Color(0.95f, 0.55f, 0.3f);

    private float nextShootTime;

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Cerberos Hatchling",
            32f,
            2.2f,
            8f,
            14f,
            true,
            3.8f,
            new Color(0.65f, 0.4f, 0.25f),
            new Vector2(0.95f, 0.95f));
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

        Vector2 centerDirection = GetDirectionToPlayer();
        if (centerDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        nextShootTime = Time.time + attackCooldown;

        FireShot(centerDirection, "CerberosCenterShot");
        FireShot(Quaternion.Euler(0f, 0f, spreadAngle) * centerDirection, "CerberosLeftShot");
        FireShot(Quaternion.Euler(0f, 0f, -spreadAngle) * centerDirection, "CerberosRightShot");
    }

    private void FireShot(Vector2 direction, string projectileName)
    {
        SpawnEnemyProjectile(
            projectileName,
            transform.position + (Vector3)(direction.normalized * projectileSpawnOffset),
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            projectileColor,
            projectileSize);
    }
}
