using UnityEngine;

public class BlackHoleEnemy : EnemyMechanics
{
    [Header("Black Hole Pull")]
    [SerializeField] private float pullRadius = 8f;
    [SerializeField] private float pullStrength = 0.3f;
    [SerializeField] private float centerDamage = 8f;
    [SerializeField] private float centerDamageRadius = 1.3f;
    [SerializeField] private float centerDamageCooldown = 0.45f;
    [SerializeField] private bool invulnerableWhileOthersLive = true;
    [SerializeField] private bool pullOtherEnemies = true;
    [SerializeField] private float enemyPullStrengthMultiplier = 0.55f;

    private float nextCenterDamageTime;

    protected override void Awake()
    {
        maxCan = 300f;
        useChaseMovement = false;
        moveSpeed = 0f;
        touchDamage = 0f;
        stopDistance = 0f;
        base.Awake();
    }

    private void Reset()
    {
        ApplyDefaultSetup(
            "Black Hole",
            300f,
            0f,
            0f,
            0f,
            false,
            0f,
            new Color(0.1f, 0.05f, 0.15f),
            new Vector2(1.2f, 1.2f));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive)
        {
            return;
        }

        if (PlayerTarget == null || PlayerMechanics == null)
        {
            CachePlayerReferences();
        }

        if (PlayerTarget == null || PlayerMechanics == null)
        {
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();
        if (distanceToPlayer > pullRadius)
        {
            PullOtherEnemiesTowardsCenter();
            return;
        }

        Vector2 pullDirection = ((Vector2)transform.position - (Vector2)PlayerTarget.position).normalized;
        ApplyPullToPlayer(pullDirection);
        PullOtherEnemiesTowardsCenter();

        if (distanceToPlayer <= centerDamageRadius && Time.time >= nextCenterDamageTime && PlayerMechanics != null)
        {
            nextCenterDamageTime = Time.time + centerDamageCooldown;
            Vector2 direction = ((Vector2)PlayerMechanics.transform.position - (Vector2)transform.position).normalized;
            DamageInfo info = new DamageInfo(centerDamage, transform.position, direction, gameObject);
            PlayerMechanics.TakeDamage(info);
        }
    }

    public override float TakeDamage(float amount)
    {
        if (invulnerableWhileOthersLive && HasOtherLivingEnemies())
        {
            return 0f;
        }

        return base.TakeDamage(amount);
    }

    public override float TakeDamage(DamageInfo damageInfo)
    {
        if (invulnerableWhileOthersLive && HasOtherLivingEnemies())
        {
            return 0f;
        }

        return base.TakeDamage(damageInfo);
    }

    private void ApplyPullToPlayer(Vector2 pullDirection)
    {
        if (PlayerMechanics == null)
        {
            return;
        }

        PlayerMovement playerMovement = PlayerMechanics.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            // Black hole pull now matches the vortex feel instead of overpowering movement.
            playerMovement.ApplyExternalDisplacement(pullDirection * pullStrength * Time.deltaTime);
            playerMovement.ApplyExternalVelocity(pullDirection * pullStrength);
            return;
        }

        PlayerTarget.position += (Vector3)(pullDirection * pullStrength * Time.deltaTime);
    }

    private void PullOtherEnemiesTowardsCenter()
    {
        if (!pullOtherEnemies)
        {
            return;
        }

        EnemyMechanics[] allEnemies = FindObjectsByType<EnemyMechanics>(FindObjectsSortMode.None);
        for (int i = 0; i < allEnemies.Length; i++)
        {
            EnemyMechanics enemy = allEnemies[i];
            if (enemy == null || enemy == this || !enemy.IsAlive)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance > pullRadius || distance <= 0.05f)
            {
                continue;
            }

            Vector2 pullDirection = ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
            enemy.transform.position += (Vector3)(pullDirection * pullStrength * enemyPullStrengthMultiplier * Time.deltaTime);
        }
    }

    private bool HasOtherLivingEnemies()
    {
        EnemyMechanics[] allEnemies = FindObjectsByType<EnemyMechanics>(FindObjectsSortMode.None);
        for (int i = 0; i < allEnemies.Length; i++)
        {
            EnemyMechanics enemy = allEnemies[i];
            if (enemy != null && enemy != this && enemy.IsAlive)
            {
                return true;
            }
        }

        return false;
    }
}
