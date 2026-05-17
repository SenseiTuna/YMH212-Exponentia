using UnityEngine;

public class HarpyEnemy : EnemyMechanics
{
    [Header("Harpy Dash")]
    [SerializeField] private float dashTriggerRange = 4f;
    [SerializeField] private float dashSpeed = 9f;
    [SerializeField] private float dashDuration = 0.32f;
    [SerializeField] private float dashWindup = 0.2f;
    [SerializeField] private float dashCooldown = 1.6f;

    private bool isWindingUp;
    private bool isDashing;
    private float windupEndTime;
    private float dashEndTime;
    private float nextDashTime;
    private Vector2 dashDirection;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Harpy",
            22f,
            3.8f,
            12f,
            10f,
            true,
            0.15f,
            new Color(0.85f, 0.88f, 0.95f),
            new Vector2(0.8f, 0.8f));
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        // Force cache player reference before any checks
        if (PlayerTarget == null)
        {
            CachePlayerReferences();
        }

        if (!IsAlive || PlayerTarget == null)
        {
            return;
        }

        if (isDashing)
        {
            transform.position += (Vector3)(dashDirection * dashSpeed * Time.deltaTime);
            if (Time.time >= dashEndTime)
            {
                isDashing = false;
                if (aiAgent != null) { aiAgent.isStopped = false; }
            }
            return;
        }

        if (isWindingUp)
        {
            if (Time.time >= windupEndTime)
            {
                isWindingUp = false;
                isDashing = true;
                dashEndTime = Time.time + dashDuration;
            }
            return;
        }

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

        isWindingUp = true;
        if (animator != null) { animator.SetTrigger("Attack"); }
        if (aiAgent != null) { aiAgent.isStopped = true; } // Dash boyunca pathfinding'i durdur
        windupEndTime = Time.time + dashWindup;
        nextDashTime = Time.time + dashCooldown;
    }
}
