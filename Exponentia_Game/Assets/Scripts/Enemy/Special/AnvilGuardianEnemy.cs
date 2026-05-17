using UnityEngine;

public class AnvilGuardianEnemy : EnemyMechanics
{
    [Header("Anvil Reflection")]
    [SerializeField] private float reflectRadius = 1.2f;
    [SerializeField] private float reflectedProjectileDamage = 12f;
    [SerializeField] private float reflectedProjectileSpeed = 8f;
    [SerializeField] private float reflectedProjectileLifeTime = 2.2f;
    [SerializeField] private float reflectedProjectileSize = 0.24f;
    [SerializeField] private float randomReflectAngle = 22f;

    [Header("Hammer Throw")]
    [SerializeField] private float hammerThrowRange = 7f;
    [SerializeField] private float hammerThrowCooldown = 2.6f;
    [SerializeField] private float hammerProjectileSpeed = 4.2f;
    [SerializeField] private float hammerProjectileDamage = 16f;
    [SerializeField] private float hammerProjectileLifeTime = 4f;
    [SerializeField] private float hammerProjectileSize = 0.32f;
    [SerializeField] private float hammerSpawnOffset = 0.8f;
    [SerializeField] private Color hammerColor = new Color(0.8f, 0.8f, 0.8f);

    private float nextHammerTime;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Anvil Guardian",
            95f,
            0.8f,
            9f,
            32f,
            true,
            5.4f,
            new Color(0.45f, 0.45f, 0.5f),
            new Vector2(1.15f, 1.15f));
    }

    protected override void Update()
    {
        base.Update();
        ReflectIncomingProjectiles();

        if (!IsAlive || PlayerTarget == null || Time.time < nextHammerTime)
        {
            return;
        }

        if (GetDistanceToPlayer() > hammerThrowRange)
        {
            return;
        }

        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        nextHammerTime = Time.time + hammerThrowCooldown;
        SpawnEnemyProjectile(
            "AnvilHammer",
            transform.position + (Vector3)(direction * hammerSpawnOffset),
            direction,
            hammerProjectileSpeed,
            hammerProjectileDamage,
            hammerProjectileLifeTime,
            hammerColor,
            hammerProjectileSize);
    }

    private void ReflectIncomingProjectiles()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, reflectRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            PlayerProjectile playerProjectile = hits[i].GetComponent<PlayerProjectile>();
            if (playerProjectile == null)
            {
                continue;
            }

            Vector2 reflectDirection = ((Vector2)playerProjectile.transform.position - (Vector2)transform.position).normalized;
            if (reflectDirection.sqrMagnitude <= 0.001f)
            {
                reflectDirection = GetDirectionToPlayer();
            }

            reflectDirection = Quaternion.Euler(0f, 0f, Random.Range(-randomReflectAngle, randomReflectAngle)) * reflectDirection;

            SpawnEnemyProjectile(
                "ReflectedHammerShard",
                playerProjectile.transform.position,
                reflectDirection,
                reflectedProjectileSpeed,
                reflectedProjectileDamage,
                reflectedProjectileLifeTime,
                hammerColor,
                reflectedProjectileSize);

            Destroy(playerProjectile.gameObject);
        }
    }
}
