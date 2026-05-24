using UnityEngine;

public class RagingBronzeMinotaurEnemy : EnemyMechanics
{
    [Header("Minotaur Charge")]
    [SerializeField] private float attackRange = 11f;
    [SerializeField] private float chargeCooldown = 3f;
    [SerializeField] private float chargeWindup = 0.3f;
    [SerializeField] private float chargeDuration = 0.95f;
    [SerializeField] private float chargeSpeed = 11f;
    [SerializeField] private float chargeTrailInterval = 0.12f;
    [SerializeField] private float chargeTrailDamage = 8f;
    [SerializeField] private float chargeTrailDuration = 2.25f;
    [SerializeField] private float chargeTrailRadius = 0.75f;
    [SerializeField] private Color chargeTrailColor = new Color(0.85f, 0.4f, 0.15f, 0.75f);

    [Header("Minotaur Slam")]
    [SerializeField] private float slamCooldown = 4.4f;
    [SerializeField] private float slamWindup = 0.35f;
    [SerializeField] private float slamDamage = 24f;
    [SerializeField] private float slamRadius = 2.4f;
    [SerializeField] private float slamDuration = 1.25f;
    [SerializeField] private Color slamColor = new Color(1f, 0.55f, 0.15f, 0.85f);

    [Header("Axe Spin")]
    [SerializeField] private float axeCooldown = 3.2f;
    [SerializeField] private int axeProjectileCount = 6;
    [SerializeField] private float axeProjectileSpeed = 8.5f;
    [SerializeField] private float axeProjectileDamage = 14f;
    [SerializeField] private float axeProjectileLifeTime = 3f;
    [SerializeField] private float axeProjectileSize = 0.24f;
    [SerializeField] private float axeSpawnRadius = 0.85f;
    [SerializeField] private Color axeColor = new Color(0.8f, 0.45f, 0.2f);

    private bool isCharging;
    private float chargeEndTime;
    private float chargeMoveStartTime;
    private float nextTrailDropTime;
    private Vector2 chargeDirection;
    private float nextChargeTime;
    private float nextSlamTime;
    private float nextAxeTime;

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Raging Bronze Minotaur",
            220f,
            1.35f,
            18f,
            70f,
            true,
            6.5f,
            new Color(0.72f, 0.36f, 0.12f),
            new Vector2(1.55f, 1.55f));
    }

    protected override void Update()
    {
        if (!IsAlive || PlayerTarget == null)
        {
            CachePlayerReferences();
            if (PlayerTarget == null)
            {
                return;
            }
        }

        if (isCharging)
        {
            StopAgentMovementCompletely();

            if (Time.time < chargeMoveStartTime)
            {
                return;
            }

            transform.position += (Vector3)(chargeDirection * chargeSpeed * Time.deltaTime);

            if (Time.time >= nextTrailDropTime)
            {
                nextTrailDropTime = Time.time + chargeTrailInterval;
                SpawnChargeTrail();
            }

            if (Time.time >= chargeEndTime)
            {
                isCharging = false;
                RestoreAgentMovement();

                if (Time.time >= nextSlamTime)
                {
                    nextSlamTime = Time.time + slamCooldown;
                    SpawnSlam();
                }
            }

            return;
        }

        RestoreAgentMovement();
        base.Update();

        if (GetDistanceToPlayer() > attackRange)
        {
            return;
        }

        if (Time.time >= nextChargeTime)
        {
            BeginCharge();
            return;
        }

        if (Time.time >= nextAxeTime)
        {
            ThrowSpinningAxes();
            nextAxeTime = Time.time + axeCooldown;
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }
    }

    private void BeginCharge()
    {
        chargeDirection = GetDirectionToPlayer();
        if (chargeDirection.sqrMagnitude <= 0.001f)
        {
            chargeDirection = Vector2.right;
        }

        isCharging = true;
        chargeMoveStartTime = Time.time + chargeWindup;
        chargeEndTime = chargeMoveStartTime + chargeDuration;
        nextChargeTime = Time.time + chargeCooldown;
        nextTrailDropTime = chargeMoveStartTime;
        nextSlamTime = Time.time + slamWindup;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    private void SpawnChargeTrail()
    {
        SpawnHazardArea(
            "MinotaurMoltenTrail",
            transform.position,
            chargeTrailDamage,
            0.35f,
            chargeTrailDuration,
            chargeTrailRadius,
            chargeTrailColor);
    }

    private void SpawnSlam()
    {
        SpawnHazardArea(
            "MinotaurSlam",
            transform.position,
            slamDamage,
            0.4f,
            slamDuration,
            slamRadius,
            slamColor);
    }

    private void ThrowSpinningAxes()
    {
        int count = Mathf.Max(1, axeProjectileCount);

        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            SpawnEnemyProjectile(
                $"BronzeAxe_{i}",
                transform.position + (Vector3)(direction.normalized * axeSpawnRadius),
                direction,
                axeProjectileSpeed,
                axeProjectileDamage,
                axeProjectileLifeTime,
                axeColor,
                axeProjectileSize);
        }
    }

    private void SpawnHazardArea(string objectName, Vector3 position, float damagePerTick, float cooldown, float lifeTime, float radius, Color color)
    {
        GameObject hazardObject = new GameObject(objectName);
        hazardObject.transform.position = position;
        EnemyHazardArea hazardArea = hazardObject.AddComponent<EnemyHazardArea>();
        hazardArea.Initialize(damagePerTick, cooldown, lifeTime, radius, color);
    }
}