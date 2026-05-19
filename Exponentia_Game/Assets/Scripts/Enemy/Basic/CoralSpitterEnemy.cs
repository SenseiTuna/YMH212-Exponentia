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
    [SerializeField] private float centerSpawnOffset = 0.72f;
    [SerializeField] private float sideSpawnOffset = 0.34f;
    [SerializeField] private float sideOffset = 0.44f;
    [SerializeField] private float sideSpreadAngle = 18f;
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
        Vector2 leftDirection = Quaternion.Euler(0f, 0f, -sideSpreadAngle) * direction;
        Vector2 rightDirection = Quaternion.Euler(0f, 0f, sideSpreadAngle) * direction;

        FireBubble("CoralSpitterCenterBubble", direction, direction * centerSpawnOffset);
        FireBubble("CoralSpitterLeftBubble", leftDirection, direction * sideSpawnOffset - perpendicular * sideOffset);
        FireBubble("CoralSpitterRightBubble", rightDirection, direction * sideSpawnOffset + perpendicular * sideOffset);
    }

    private void FireBubble(string projectileName, Vector2 direction, Vector2 spawnOffset)
    {
        SpawnEnemyProjectile(
            projectileName,
            transform.position + (Vector3)spawnOffset,
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            projectileColor,
            projectileSize);
    }
}
