using UnityEngine;

public class SpearedTritonEnemy : EnemyMechanics
{
    [Header("Triton Charge")]
    [SerializeField] private float chargeTriggerRange = 5f;
    [SerializeField] private float chargeWindup = 0.35f;
    [SerializeField] private float dashSpeed = 8.5f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashCooldown = 2.2f;
    [SerializeField] private float dashImpactRadius = 0.6f;
    [SerializeField] private float dashDamage = 11f;

    private bool isCharging;
    private bool isDashing;
    private float chargeEndTime;
    private float dashEndTime;
    private float nextChargeTime;
    private Vector2 dashDirection;
    private bool dashHitApplied;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Speared Triton",
            35f,
            3f,
            5f,
            13f,
            true,
            0.2f,
            new Color(0.3f, 0.78f, 0.88f),
            new Vector2(0.95f, 0.95f));
    }

    protected override void Update()
    {
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
            StopAgentMovementCompletely();

            transform.position += (Vector3)(dashDirection * dashSpeed * Time.deltaTime);
            TryDashImpact();

            if (Time.time >= dashEndTime)
            {
                isDashing = false;
                RestoreAgentMovement();
            }

            return;
        }

        if (isCharging)
        {
            StopAgentMovementCompletely();

            if (Time.time >= chargeEndTime)
            {
                isCharging = false;
                isDashing = true;
                dashHitApplied = false;
                dashEndTime = Time.time + dashDuration;
            }

            return;
        }

        RestoreAgentMovement();
        base.Update();

        if (Time.time < nextChargeTime || GetDistanceToPlayer() > chargeTriggerRange)
        {
            return;
        }

        dashDirection = GetDirectionToPlayer();
        if (dashDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        isCharging = true;
        if (animator != null) { animator.SetTrigger("Attack"); }
        StopAgentMovementCompletely();
        chargeEndTime = Time.time + chargeWindup;
        nextChargeTime = Time.time + dashCooldown;
    }

    private void TryDashImpact()
    {
        if (dashHitApplied)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, dashImpactRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = FindDamageable(hits[i].gameObject);
            if (damageable is PlayerMechanics)
            {
                damageable.TakeDamage(dashDamage);
                dashHitApplied = true;
                break;
            }
        }
    }
}
