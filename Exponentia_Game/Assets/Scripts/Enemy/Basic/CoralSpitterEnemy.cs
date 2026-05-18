using UnityEngine;

public class CoralSpitterEnemy : EnemyMechanics
{
    [Header("Coral Spitter Attack")]
    [SerializeField] private float attackCooldown = 1.7f;
    [SerializeField] private float shootRange = 7f;
    [SerializeField] private float projectileSpeed = 3f;
    [SerializeField] private float projectileDamage = 9f;
    [SerializeField] private float projectileLifeTime = 4f;
    [SerializeField] private float projectileSize = 0.28f;
    [SerializeField] private float projectileSpawnOffset = 0.6f;
    [SerializeField] private float sideOffset = 0.28f;
    [SerializeField] private Color projectileColor = new Color(0.35f, 0.8f, 0.55f);

    private float nextShootTime;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Coral Spitter",
            36f,
            0f,
            6f,
            12f,
            false,
            0f,
            new Color(0.5f, 0.75f, 0.45f),
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

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        FireBubble("CoralSpitterLeftBubble", direction, -perpendicular * sideOffset);
        FireBubble("CoralSpitterRightBubble", direction, perpendicular * sideOffset);
    }

    private void FireBubble(string projectileName, Vector2 direction, Vector2 sideOffsetVector)
    {
        SpawnEnemyProjectile(
            projectileName,
            transform.position + (Vector3)(direction * projectileSpawnOffset + sideOffsetVector),
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            projectileColor,
            projectileSize);
    }
}
