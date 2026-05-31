using System.Collections;
using UnityEngine;

public class LykaonBossEnemy : EnemyMechanics
{
    private enum LykaonAction
    {
        None,
        TwinClawSlash,
        HuntersLeap,
        BloodFangDash,
        RendingHowl,
        MoonlitSlash,
        BloodMoonHowl
    }

    [Header("Lykaon Boss")]
    [SerializeField] private float phaseTwoHealthPercent = 0.5f;
    [SerializeField] private float phaseTwoMoveSpeedMultiplier = 1.25f;
    [SerializeField] private float phaseTwoDamageMultiplier = 1.2f;
    [SerializeField] private float phaseTwoCooldownMultiplier = 0.8f;

    [Header("Twin Claw Slash")]
    [SerializeField] private float twinClawRange = 1.65f;
    [SerializeField] private float twinClawAngle = 105f;
    [SerializeField] private float twinClawDamage = 14f;
    [SerializeField] private float twinClawCooldown = 2.2f;
    [SerializeField] private float twinClawWindup = 0.12f;
    [SerializeField] private float twinClawBetweenSlashes = 0.18f;
    [SerializeField] private float twinClawKnockback = 3f;

    [Header("Hunter's Leap")]
    [SerializeField] private float huntersLeapRange = 7.5f;
    [SerializeField] private float huntersLeapCooldown = 5f;
    [SerializeField] private float huntersLeapWindup = 0.45f;
    [SerializeField] private float huntersLeapTravelTime = 0.35f;
    [SerializeField] private float huntersLeapImpactRadius = 1.45f;
    [SerializeField] private float huntersLeapDamage = 22f;
    [SerializeField] private float huntersLeapKnockback = 5f;

    [Header("Blood Fang Dash")]
    [SerializeField] private float bloodFangDashRange = 8f;
    [SerializeField] private float bloodFangDashCooldown = 4.2f;
    [SerializeField] private float bloodFangDashWindup = 0.35f;
    [SerializeField] private float bloodFangDashDuration = 0.42f;
    [SerializeField] private float bloodFangDashSpeed = 12f;
    [SerializeField] private float bloodFangDashHitRadius = 0.8f;
    [SerializeField] private float bloodFangDashDamage = 24f;
    [SerializeField] private float bloodFangDashKnockback = 6f;

    [Header("Rending Howl")]
    [SerializeField] private float rendingHowlRange = 4.5f;
    [SerializeField] private float rendingHowlAngle = 75f;
    [SerializeField] private float rendingHowlDamage = 12f;
    [SerializeField] private float rendingHowlKnockback = 6f;
    [SerializeField] private float rendingHowlCooldown = 6f;
    [SerializeField] private float rendingHowlWindup = 0.45f;
    [SerializeField] private bool rendingHowlUseCone = true;

    [Header("Moonlit Slash")]
    [SerializeField] private float moonlitSlashMinRange = 3.3f;
    [SerializeField] private float moonlitSlashCooldown = 3.2f;
    [SerializeField] private float moonlitSlashWindup = 0.18f;
    [SerializeField] private float moonlitSlashProjectileSpeed = 7.5f;
    [SerializeField] private float moonlitSlashProjectileDamage = 18f;
    [SerializeField] private float moonlitSlashProjectileLifeTime = 2.4f;
    [SerializeField] private float moonlitSlashProjectileSize = 0.55f;
    [SerializeField] private float moonlitSlashSpawnOffset = 0.9f;
    [SerializeField] private Color moonlitSlashColor = new Color(0.65f, 0.85f, 1f);
    [SerializeField] private Sprite moonlitSlashSprite;

    [Header("Blood Moon Howl")]
    [SerializeField] private float bloodMoonHowlDuration = 1.4f;
    [SerializeField] private float bloodMoonHowlDamage = 16f;
    [SerializeField] private float bloodMoonHowlRadius = 3.2f;
    [SerializeField] private float bloodMoonHowlKnockback = 7f;

    private LykaonAction currentAction;
    private bool phaseTwoStarted;
    private bool playerDamagedDuringDash;
    private float nextTwinClawTime;
    private float nextLeapTime;
    private float nextDashTime;
    private float nextHowlTime;
    private float nextMoonlitSlashTime;
    private float baseMoveSpeed;
    private Vector2 facingDirection = Vector2.right;

    protected override void Awake()
    {
        maxCan = Mathf.Max(maxCan, 450f);
        moveSpeed = Mathf.Max(moveSpeed, 2.2f);
        touchDamage = Mathf.Max(touchDamage, 18f);
        xpReward = Mathf.Max(xpReward, 150f);
        stopDistance = Mathf.Max(stopDistance, 1.25f);
        base.Awake();
        baseMoveSpeed = moveSpeed;
    }

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Lykaon",
            450f,
            2.2f,
            18f,
            150f,
            true,
            1.25f,
            new Color(0.45f, 0.36f, 0.3f),
            new Vector2(1.65f, 1.65f));
    }

    protected override void Update()
    {
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

        Vector2 directionToPlayer = GetDirectionToPlayer();
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            facingDirection = directionToPlayer;
        }

        if (!phaseTwoStarted && CurrentHealth <= MaxHealth * phaseTwoHealthPercent)
        {
            StartCoroutine(BloodMoonHowlRoutine());
            return;
        }

        if (currentAction != LykaonAction.None)
        {
            StopAgentMovementCompletely();
            SetAnimatorBoolIfPresent("isWalking", false);
            return;
        }

        RestoreAgentMovement();
        base.Update();
        TryStartAttack();
    }

    private void TryStartAttack()
    {
        float distance = GetDistanceToPlayer();

        if (distance <= twinClawRange && Time.time >= nextTwinClawTime)
        {
            StartCoroutine(TwinClawSlashRoutine());
            return;
        }

        if (distance >= moonlitSlashMinRange && Time.time >= nextMoonlitSlashTime)
        {
            StartCoroutine(MoonlitSlashRoutine());
            return;
        }

        if (distance <= rendingHowlRange && Time.time >= nextHowlTime)
        {
            StartCoroutine(RendingHowlRoutine());
            return;
        }

        if (distance <= bloodFangDashRange && Time.time >= nextDashTime)
        {
            StartCoroutine(BloodFangDashRoutine());
            return;
        }

        if (distance <= huntersLeapRange && Time.time >= nextLeapTime)
        {
            StartCoroutine(HuntersLeapRoutine());
        }
    }

    private IEnumerator TwinClawSlashRoutine()
    {
        BeginAction(LykaonAction.TwinClawSlash);
        nextTwinClawTime = Time.time + ScaledCooldown(twinClawCooldown);
        TriggerAttackAnimation("twin claw slash", "claw slash", "attack");

        yield return new WaitForSeconds(twinClawWindup);
        DamagePlayerInCone(twinClawDamage, twinClawRange, twinClawAngle, twinClawKnockback);

        yield return new WaitForSeconds(twinClawBetweenSlashes);
        DamagePlayerInCone(twinClawDamage, twinClawRange, twinClawAngle, twinClawKnockback);

        EndAction();
    }

    private IEnumerator HuntersLeapRoutine()
    {
        BeginAction(LykaonAction.HuntersLeap);
        nextLeapTime = Time.time + ScaledCooldown(huntersLeapCooldown);
        TriggerAttackAnimation("hunter's leap", "hunters leap", "leap", "attack");

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = PlayerTarget.position;

        yield return new WaitForSeconds(huntersLeapWindup);

        float elapsed = 0f;
        while (elapsed < huntersLeapTravelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / huntersLeapTravelTime);
            float arc = Mathf.Sin(t * Mathf.PI) * 0.7f;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t) + Vector3.up * arc;
            yield return null;
        }

        transform.position = targetPosition;
        DamagePlayerInCircle(huntersLeapDamage, huntersLeapImpactRadius, huntersLeapKnockback);
        EndAction();
    }

    private IEnumerator BloodFangDashRoutine()
    {
        BeginAction(LykaonAction.BloodFangDash);
        nextDashTime = Time.time + ScaledCooldown(bloodFangDashCooldown);
        playerDamagedDuringDash = false;
        facingDirection = GetDirectionToPlayer();
        if (facingDirection.sqrMagnitude <= 0.001f)
        {
            facingDirection = Vector2.right;
        }

        TriggerAttackAnimation("blood fang dash", "dash attack", "dash", "attack");
        yield return new WaitForSeconds(bloodFangDashWindup);

        float endTime = Time.time + bloodFangDashDuration;
        while (Time.time < endTime)
        {
            transform.position += (Vector3)(facingDirection.normalized * bloodFangDashSpeed * Time.deltaTime);
            if (!playerDamagedDuringDash && IsPlayerInCircle(transform.position, bloodFangDashHitRadius))
            {
                playerDamagedDuringDash = true;
                DamagePlayer(bloodFangDashDamage, transform.position, bloodFangDashKnockback);
            }

            yield return null;
        }

        EndAction();
    }

    private IEnumerator RendingHowlRoutine()
    {
        BeginAction(LykaonAction.RendingHowl);
        nextHowlTime = Time.time + ScaledCooldown(rendingHowlCooldown);
        TriggerAttackAnimation("rending howl", "howl", "attack");

        yield return new WaitForSeconds(rendingHowlWindup);

        if (rendingHowlUseCone)
        {
            DamagePlayerInCone(rendingHowlDamage, rendingHowlRange, rendingHowlAngle, rendingHowlKnockback);
        }
        else
        {
            DamagePlayerInCircle(rendingHowlDamage, rendingHowlRange, rendingHowlKnockback);
        }

        EndAction();
    }

    private IEnumerator MoonlitSlashRoutine()
    {
        BeginAction(LykaonAction.MoonlitSlash);
        nextMoonlitSlashTime = Time.time + ScaledCooldown(moonlitSlashCooldown);
        TriggerAttackAnimation("moonlit slash", "slash projectile", "attack");

        yield return new WaitForSeconds(moonlitSlashWindup);

        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = facingDirection;
        }

        SpawnEnemyProjectile(
            "LykaonMoonlitSlash",
            transform.position + (Vector3)(direction.normalized * moonlitSlashSpawnOffset),
            direction,
            moonlitSlashProjectileSpeed,
            ScaledDamage(moonlitSlashProjectileDamage),
            moonlitSlashProjectileLifeTime,
            moonlitSlashColor,
            moonlitSlashProjectileSize,
            moonlitSlashSprite);

        EndAction();
    }

    private IEnumerator BloodMoonHowlRoutine()
    {
        phaseTwoStarted = true;
        BeginAction(LykaonAction.BloodMoonHowl);
        TriggerAttackAnimation("blood moon howl", "howl", "attack");

        yield return new WaitForSeconds(bloodMoonHowlDuration);

        moveSpeed = baseMoveSpeed * phaseTwoMoveSpeedMultiplier;
        DamagePlayerInCircle(bloodMoonHowlDamage, bloodMoonHowlRadius, bloodMoonHowlKnockback);
        EndAction();
    }

    private void BeginAction(LykaonAction action)
    {
        currentAction = action;
        StopAgentMovementCompletely();
        SetAnimatorBoolIfPresent("isWalking", false);
    }

    private void EndAction()
    {
        currentAction = LykaonAction.None;
        RestoreAgentMovement();
    }

    private void DamagePlayerInCircle(float damage, float radius, float knockback)
    {
        if (IsPlayerInCircle(transform.position, radius))
        {
            DamagePlayer(damage, transform.position, knockback);
        }
    }

    private void DamagePlayerInCone(float damage, float range, float angle, float knockback)
    {
        if (PlayerTarget == null)
        {
            return;
        }

        Vector2 toPlayer = (Vector2)(PlayerTarget.position - transform.position);
        if (toPlayer.magnitude > range)
        {
            return;
        }

        Vector2 forward = facingDirection.sqrMagnitude > 0.001f ? facingDirection.normalized : Vector2.right;
        if (Vector2.Angle(forward, toPlayer.normalized) > angle * 0.5f)
        {
            return;
        }

        DamagePlayer(damage, transform.position, knockback);
    }

    private bool IsPlayerInCircle(Vector2 center, float radius)
    {
        if (PlayerTarget == null)
        {
            return false;
        }

        return Vector2.Distance(center, PlayerTarget.position) <= radius;
    }

    private void DamagePlayer(float damage, Vector2 hitPoint, float knockback)
    {
        if (PlayerMechanics == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)PlayerMechanics.transform.position - hitPoint).normalized;
        DamageInfo info = new DamageInfo(ScaledDamage(damage), hitPoint, direction, gameObject, false, knockback);
        PlayerMechanics.TakeDamage(info);

        PlayerMovement playerMovement = PlayerMechanics.GetComponent<PlayerMovement>();
        if (playerMovement != null && knockback > 0f)
        {
            playerMovement.ApplyExternalVelocity(direction * knockback);
        }
    }

    private float ScaledDamage(float damage)
    {
        return damage * (phaseTwoStarted ? phaseTwoDamageMultiplier : 1f);
    }

    private float ScaledCooldown(float cooldown)
    {
        return cooldown * (phaseTwoStarted ? phaseTwoCooldownMultiplier : 1f);
    }
}
