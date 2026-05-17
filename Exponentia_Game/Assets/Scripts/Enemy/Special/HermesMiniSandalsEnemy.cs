using UnityEngine;

public class HermesMiniSandalsEnemy : EnemyMechanics
{
    [Header("Hermes Dash Trail")]
    [SerializeField] private float dashTriggerRange = 5f;
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.45f;
    [SerializeField] private float dashCooldown = 2.6f;
    [SerializeField] private float mineDropInterval = 0.12f;
    [SerializeField] private float mineArmDuration = 0.8f;
    [SerializeField] private float mineBurstDamage = 10f;
    [SerializeField] private float mineProjectileSpeed = 9f;
    [SerializeField] private float mineProjectileLifeTime = 2.5f;
    [SerializeField] private float mineProjectileSize = 0.18f;
    [SerializeField] private Color mineColor = new Color(1f, 0.9f, 0.25f);

    private bool isDashing;
    private float dashEndTime;
    private float nextDashTime;
    private float nextMineDropTime;
    private Vector2 dashDirection;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Hermes Mini Sandals",
            40f,
            4.8f,
            10f,
            18f,
            true,
            0.15f,
            new Color(1f, 0.86f, 0.4f),
            new Vector2(0.9f, 0.9f));
    }

    protected override void Update()
    {
        if (!IsAlive || PlayerTarget == null)
        {
            CachePlayerReferences();
            if (PlayerTarget == null) return;
        }

        if (isDashing)
        {
            StopAgentMovementCompletely();
            transform.position += (Vector3)(dashDirection * dashSpeed * Time.deltaTime);

            if (Time.time >= nextMineDropTime)
            {
                nextMineDropTime = Time.time + mineDropInterval;
                DropMine();
            }

            if (Time.time >= dashEndTime)
            {
                isDashing = false;
                RestoreAgentMovement();
            }

            return;
        }

        RestoreAgentMovement();
        base.Update();

        if (Time.time < nextDashTime || GetDistanceToPlayer() > dashTriggerRange)
        {
            return;
        }

        dashDirection = GetDirectionToPlayer();
        if (dashDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
        nextMineDropTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    private void DropMine()
    {
        GameObject mineObject = new GameObject("HermesTrailMine");
        mineObject.transform.position = transform.position;
        EnemyBurstMine mine = mineObject.AddComponent<EnemyBurstMine>();
        mine.Initialize(this, enemyProjectilePrefab, dashDirection, mineArmDuration, mineBurstDamage, mineProjectileSpeed, mineProjectileLifeTime, mineProjectileSize, mineColor);
    }
}
