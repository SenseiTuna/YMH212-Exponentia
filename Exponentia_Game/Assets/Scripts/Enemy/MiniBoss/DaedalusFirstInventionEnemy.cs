using UnityEngine;

public class DaedalusFirstInventionEnemy : EnemyMechanics
{
    [Header("Daedalus Invention")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float fullRingCooldown = 2.8f;
    [SerializeField] private float halfRingCooldown = 2.1f;
    [SerializeField] private float laserCooldown = 4.2f;
    [SerializeField] private float featherProjectileSpeed = 9f;
    [SerializeField] private float featherProjectileDamage = 8f;
    [SerializeField] private float featherProjectileLifeTime = 3.5f;
    [SerializeField] private float featherProjectileSize = 0.18f;
    [SerializeField] private float laserProjectileSpeed = 13f;
    [SerializeField] private float laserProjectileDamage = 16f;
    [SerializeField] private float laserProjectileLifeTime = 2.2f;
    [SerializeField] private float laserProjectileSize = 0.28f;
    [SerializeField] private float laserSpawnOffset = 1.1f;
    [SerializeField] private int fullRingFeatherCount = 10;
    [SerializeField] private int halfRingFeatherCount = 7;
    [SerializeField] private Color featherColor = new Color(0.75f, 0.75f, 0.8f);
    [SerializeField] private Color laserColor = new Color(1f, 0.95f, 0.8f);
    [SerializeField] private Sprite featherVisualSprite;
    [SerializeField] private Sprite laserVisualSprite;

    private float nextAttackTime;
    private int attackIndex;

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Daedalus's First Invention",
            140f,
            1.5f,
            12f,
            45f,
            true,
            7.5f,
            new Color(0.65f, 0.68f, 0.72f),
            new Vector2(1.25f, 1.25f));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive || PlayerTarget == null)
        {
            return;
        }

        float distance = GetDistanceToPlayer();
        if (distance > attackRange || Time.time < nextAttackTime)
        {
            return;
        }

        Vector2 directionToPlayer = GetDirectionToPlayer();
        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            directionToPlayer = Vector2.right;
        }

        switch (attackIndex)
        {
            case 0:
                TriggerAttackAnimation("spinning feather attack", "wing slam");
                FireFullRing();
                nextAttackTime = Time.time + fullRingCooldown;
                break;
            case 1:
                TriggerAttackAnimation("wing slam", "spinning feather attack");
                FireHalfRing(directionToPlayer);
                nextAttackTime = Time.time + halfRingCooldown;
                break;
            default:
                TriggerAttackAnimation("laser beam attack");
                FireLaser(directionToPlayer);
                nextAttackTime = Time.time + laserCooldown;
                break;
        }

        attackIndex = (attackIndex + 1) % 3;
    }

    private void FireFullRing()
    {
        FireFeatherArc(Vector2.right, fullRingFeatherCount, 360f, featherProjectileSpeed, featherProjectileDamage, featherProjectileLifeTime, featherProjectileSize, featherColor, "DaedalusFeatherRing");
    }

    private void FireHalfRing(Vector2 forwardDirection)
    {
        FireFeatherArc(forwardDirection, halfRingFeatherCount, 180f, featherProjectileSpeed, featherProjectileDamage, featherProjectileLifeTime, featherProjectileSize, featherColor, "DaedalusFeatherHalfRing");
    }

    private void FireLaser(Vector2 directionToPlayer)
    {
        SpawnEnemyProjectile(
            "DaedalusEyeLaser",
            transform.position + (Vector3)(directionToPlayer.normalized * laserSpawnOffset),
            directionToPlayer,
            laserProjectileSpeed,
            laserProjectileDamage,
            laserProjectileLifeTime,
            laserColor,
            laserProjectileSize,
            laserVisualSprite);
    }

    private void FireFeatherArc(Vector2 forwardDirection, int count, float spreadAngle, float speed, float damage, float lifeTime, float size, Color color, string projectilePrefix)
    {
        if (count <= 0)
        {
            return;
        }

        Vector2 normalizedForward = forwardDirection.sqrMagnitude > 0.001f ? forwardDirection.normalized : Vector2.right;
        float startAngle = -spreadAngle * 0.5f;
        float step = count == 1 ? 0f : spreadAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * normalizedForward;
            SpawnEnemyProjectile(
                $"{projectilePrefix}_{i}",
                transform.position + (Vector3)(direction.normalized * 0.9f),
                direction,
                speed,
                damage,
                lifeTime,
                color,
                size,
                featherVisualSprite);
        }
    }
}
