using System.Collections.Generic;
using Exponentia.InventorySystem;
using UnityEngine;

public class ConfiguredGodSkill : GodSkillBase
{
    private enum RuntimeEffect
    {
        None,
        AreaAtCursor,
        AreaAroundOwner,
        MeleeStrike,
        VortexTrap,
        ForwardWave,
        ForwardDash,
        TimeShift,
        TemporaryBuff,
        Shield,
        Projectile,
        PassiveKillAttackSpeed,
        PassiveWrath,
        PassiveWet,
        PassiveTideKnockback,
        PassiveDeathTouch,
        PassiveSoulArmor,
        PassiveLowHealthDamage,
        PassiveMissingHealthDamage,
        PassiveMoveStack,
        PassiveMoveDamage,
        PassiveProtectiveAura,
        PassiveWisdom
    }

    [Header("Configured Runtime")]
    [SerializeField] private string sourceItemId;
    [SerializeField] private RuntimeEffect runtimeEffect;
    [SerializeField] private UnityEngine.Object visualEffectPrefab;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private float tickInterval = 0.35f;
    [SerializeField] private float force = 260f;
    [SerializeField] private float range = 5f;
    [SerializeField] private float buffDamageMultiplier = 1.25f;
    [SerializeField] private float buffAttackSpeedMultiplier = 1.25f;
    [SerializeField] private float buffMoveSpeedMultiplier = 1.25f;
    [SerializeField] private float shieldAmount = 25f;
    [SerializeField] private float bleedDps = 6f;
    [SerializeField] private float bleedDuration = 4f;
    [SerializeField] private float statusDuration = 3f;
    [SerializeField] private float passiveMoveSpeedPerStack = 0.08f;
    [SerializeField] private float maxPassiveMoveSpeed;
    [SerializeField] private float slowMultiplier = 0.1f;
    [SerializeField] private float vfxLifetime;
    [SerializeField] private int maxStacks = 5;
    [SerializeField] private int sortingOrder = 500;

    [Header("Runtime Debug")]
    [SerializeField] private int passiveStacks;
    [SerializeField] private float passiveMoveSpeed;

    private float baseAttackSpeed;
    private float baseMoveSpeed;
    private float baseDamage;
    private PlayerMovement ownerMovement;
    private bool subscribed;
    private bool wrath60Triggered;
    private bool wrath30Triggered;
    private bool applyingScentOfBloodBonus;
    private float nextPassiveProcTime;
    private float nextMoveStackTime;

    public override bool IsPassiveSkill => IsPassiveRuntimeEffect(runtimeEffect);

    public static ConfiguredGodSkill CreateFromDefinition(GameObject ownerObject, SkillDefinition definition)
    {
        if (ownerObject == null || definition == null || definition.linkedGodSkillType == GodSkillType.None)
        {
            return null;
        }

        ConfiguredGodSkill[] configuredSkills = ownerObject.GetComponents<ConfiguredGodSkill>();
        for (int i = 0; i < configuredSkills.Length; i++)
        {
            if (configuredSkills[i] != null && configuredSkills[i].SkillType == definition.linkedGodSkillType)
            {
                configuredSkills[i].Configure(definition);
                return configuredSkills[i];
            }
        }

        ConfiguredGodSkill skill = ownerObject.AddComponent<ConfiguredGodSkill>();
        skill.Configure(definition);
        return skill;
    }

    public void Configure(SkillDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        sourceItemId = definition.itemId;
        visualEffectPrefab = definition.visualEffectPrefab;
        duration = Mathf.Max(0.1f, definition.duration);
        ConfigureRuntimeEffect(definition);
        ApplyRuntimeTuningOverrides(definition);
        ConfigureSkillDefinition(
            definition.displayName,
            definition.description,
            definition.linkedGodSkillType,
            0f,
            Mathf.Max(0f, definition.cooldown),
            false);
        SetSkillIcon(definition.icon);

        CacheBaseStats();
        SubscribePassiveEvents();
    }

    protected override void Awake()
    {
        base.Awake();
        CacheBaseStats();
        CacheOwnerMovement();
        SubscribePassiveEvents();
    }

    private void Update()
    {
        if (!IsUnlocked)
        {
            return;
        }

        if (runtimeEffect == RuntimeEffect.PassiveMoveStack)
        {
            UpdateMoveSpeedStackPassive();
        }
    }

    private void OnDestroy()
    {
        ResetMoveSpeedPassive();

        if (!subscribed || owner == null)
        {
            return;
        }

        owner.OnEnemyKilled -= HandleEnemyKilled;
        owner.OnCanDegisti -= HandleHealthChanged;
        owner.OnDealtDamage -= HandleDealtDamage;
        subscribed = false;
    }

    public override void SetUnlocked(bool unlocked)
    {
        base.SetUnlocked(unlocked);
        if (!unlocked && runtimeEffect == RuntimeEffect.PassiveMoveStack)
        {
            ResetMoveSpeedPassive();
        }
    }

    protected override bool ActivateSkill()
    {
        if (owner == null)
        {
            return false;
        }

        switch (runtimeEffect)
        {
            case RuntimeEffect.AreaAtCursor:
                CastAreaAt(ResolveCastCenter(), damage, radius);
                return true;
            case RuntimeEffect.AreaAroundOwner:
                StartCoroutine(AreaAroundOwnerRoutine());
                return true;
            case RuntimeEffect.MeleeStrike:
                CastMeleeStrike();
                return true;
            case RuntimeEffect.VortexTrap:
                CastVortexTrap();
                return true;
            case RuntimeEffect.ForwardWave:
                CastForwardWave();
                return true;
            case RuntimeEffect.ForwardDash:
                CastDash();
                return true;
            case RuntimeEffect.TimeShift:
                StartCoroutine(TimeShiftRoutine());
                return true;
            case RuntimeEffect.TemporaryBuff:
                StartCoroutine(TemporaryBuffRoutine());
                return true;
            case RuntimeEffect.Shield:
                CastShield();
                return true;
            case RuntimeEffect.Projectile:
                CastProjectileStrike();
                return true;
            case RuntimeEffect.PassiveWisdom:
                SpawnVfx(owner.transform.position, owner.transform);
                return true;
            default:
                SpawnVfx(owner.transform.position, owner.transform);
                return true;
        }
    }

    private void ConfigureRuntimeEffect(SkillDefinition definition)
    {
        string id = Normalize(definition.itemId);
        runtimeEffect = RuntimeEffect.AreaAtCursor;
        damage = 25f;
        radius = 2.5f;
        range = 5f;
        force = 260f;
        tickInterval = 0.35f;
        bleedDps = 6f;
        bleedDuration = 4f;
        statusDuration = 3f;
        passiveMoveSpeedPerStack = 0.08f;
        maxPassiveMoveSpeed = 0f;
        slowMultiplier = 0.1f;
        vfxLifetime = 0f;

        if (id.Contains("zeuslightningstrike")) { runtimeEffect = RuntimeEffect.AreaAtCursor; damage = 34f; radius = 2.7f; }
        else if (id.Contains("zeuselectricfield")) { runtimeEffect = RuntimeEffect.AreaAroundOwner; damage = 8f; radius = 3.2f; duration = Mathf.Max(duration, 4f); }
        else if (id.Contains("zeuslikelightning")) { runtimeEffect = RuntimeEffect.PassiveKillAttackSpeed; }
        else if (id.Contains("zeuswrath")) { runtimeEffect = RuntimeEffect.PassiveWrath; damage = 22f; radius = 4f; }
        else if (id.Contains("poseidontsunamiwave")) { runtimeEffect = RuntimeEffect.ForwardWave; damage = 24f; radius = 1.4f; range = 6.5f; force = 420f; slowMultiplier = 0.6f; statusDuration = 3f; duration = Mathf.Max(duration, 0.65f); vfxLifetime = duration; }
        else if (id.Contains("poseidondepthtraps")) { runtimeEffect = RuntimeEffect.VortexTrap; damage = 8f; radius = 3f; duration = Mathf.Max(duration, 3f); tickInterval = 0.35f; force = 3f; vfxLifetime = duration; }
        else if (id.Contains("poseidontidecycle")) { runtimeEffect = RuntimeEffect.PassiveTideKnockback; }
        else if (id.Contains("poseidonweteffect")) { runtimeEffect = RuntimeEffect.PassiveWet; }
        else if (id.Contains("hadessoulharvest")) { runtimeEffect = RuntimeEffect.TemporaryBuff; buffDamageMultiplier = 1.15f; buffAttackSpeedMultiplier = 1f; duration = Mathf.Max(duration, 5f); }
        else if (id.Contains("hadesunderworldarmy")) { runtimeEffect = RuntimeEffect.AreaAroundOwner; damage = 10f; radius = 3.5f; duration = Mathf.Max(duration, 5f); }
        else if (id.Contains("hadesdeathtouch")) { runtimeEffect = RuntimeEffect.PassiveDeathTouch; }
        else if (id.Contains("hadessoularmor")) { runtimeEffect = RuntimeEffect.PassiveSoulArmor; shieldAmount = 10f; }
        else if (id.Contains("aresbattlefrenzy")) { runtimeEffect = RuntimeEffect.TemporaryBuff; duration = Mathf.Max(duration, 4f); buffDamageMultiplier = 1.45f; buffAttackSpeedMultiplier = 1.35f; buffMoveSpeedMultiplier = 1.15f; }
        else if (id.Contains("aresbloodystrike")) { runtimeEffect = RuntimeEffect.MeleeStrike; damage = 32f; radius = 2.25f; bleedDuration = 4f; vfxLifetime = 0.5f; force = 280f; }
        else if (id.Contains("aresscentofblood")) { runtimeEffect = RuntimeEffect.PassiveLowHealthDamage; damage = 0.15f; vfxLifetime = 0.35f; }
        else if (id.Contains("aresendlessrage")) { runtimeEffect = RuntimeEffect.PassiveMissingHealthDamage; }
        else if (id.Contains("hermeslightspeeddash")) { runtimeEffect = RuntimeEffect.ForwardDash; damage = 18f; range = 4.5f; }
        else if (id.Contains("hermestimeshift")) { runtimeEffect = RuntimeEffect.TimeShift; duration = Mathf.Max(duration, 3f); slowMultiplier = 0.1f; tickInterval = 0.2f; vfxLifetime = duration; }
        else if (id.Contains("hermeschildofthewind")) { runtimeEffect = RuntimeEffect.PassiveMoveStack; duration = Mathf.Max(duration, 0.6f); tickInterval = 0.45f; maxStacks = 5; passiveMoveSpeedPerStack = 0.1f; maxPassiveMoveSpeed = 10f; vfxLifetime = 0.35f; }
        else if (id.Contains("hermespowerofspeed")) { runtimeEffect = RuntimeEffect.PassiveMoveDamage; }
        else if (id.Contains("athenaholyshield")) { runtimeEffect = RuntimeEffect.Shield; shieldAmount = 35f; duration = Mathf.Max(duration, 4f); }
        else if (id.Contains("athenastrategicstrike")) { runtimeEffect = RuntimeEffect.Projectile; damage = 36f; radius = 0.75f; range = 7f; duration = Mathf.Max(duration, 0.35f); vfxLifetime = duration; }
        else if (id.Contains("athenawisdom")) { runtimeEffect = RuntimeEffect.PassiveWisdom; }
        else if (id.Contains("athenaprotectiveaura")) { runtimeEffect = RuntimeEffect.PassiveProtectiveAura; }
    }

    private void ApplyRuntimeTuningOverrides(SkillDefinition definition)
    {
        if (definition == null || !definition.overrideRuntimeTuning)
        {
            return;
        }

        damage = definition.runtimeDamage;
        radius = definition.runtimeRadius;
        range = definition.runtimeRange;
        force = definition.runtimeForce;
        tickInterval = definition.runtimeTickInterval;
        bleedDps = definition.runtimeBleedDps;
        bleedDuration = definition.runtimeBleedDuration;
        statusDuration = definition.runtimeStatusDuration;
        maxStacks = definition.runtimeMaxStacks;
        passiveMoveSpeedPerStack = definition.runtimeMoveSpeedPerStack;
        maxPassiveMoveSpeed = definition.runtimeMaxMoveSpeed;
        slowMultiplier = definition.runtimeSlowMultiplier;
        vfxLifetime = definition.runtimeVfxLifetime;
    }

    private void CacheBaseStats()
    {
        if (ownerStats == null)
        {
            return;
        }

        if (baseAttackSpeed <= 0f) baseAttackSpeed = ownerStats.AttackSpeed;
        if (baseMoveSpeed <= 0f) baseMoveSpeed = ownerStats.MoveSpeed;
        if (baseDamage <= 0f) baseDamage = ownerStats.Damage;
    }

    private void CacheOwnerMovement()
    {
        if (ownerMovement == null && owner != null)
        {
            ownerMovement = owner.GetComponent<PlayerMovement>();
        }
    }

    private void SubscribePassiveEvents()
    {
        if (subscribed || owner == null)
        {
            return;
        }

        owner.OnEnemyKilled += HandleEnemyKilled;
        owner.OnCanDegisti += HandleHealthChanged;
        owner.OnDealtDamage += HandleDealtDamage;
        subscribed = true;
    }

    private void UpdateMoveSpeedStackPassive()
    {
        if (ownerStats == null)
        {
            return;
        }

        CacheOwnerMovement();
        bool isMoving = ownerMovement != null && ownerMovement.IsMoving;
        if (isMoving)
        {
            if (Time.time >= nextMoveStackTime)
            {
                nextMoveStackTime = Time.time + Mathf.Max(0.05f, tickInterval);
                int previousStacks = passiveStacks;
                passiveStacks = Mathf.Min(maxStacks, passiveStacks + 1);
                ApplyMoveSpeedPassive();

                if (passiveStacks != previousStacks && passiveStacks == maxStacks)
                {
                    SpawnVfx(owner.transform.position, owner.transform);
                }
            }

            return;
        }

        if (passiveStacks > 0 && Time.time >= nextMoveStackTime + Mathf.Max(0.1f, duration))
        {
            passiveStacks = 0;
            ApplyMoveSpeedPassive();
        }
    }

    private void ApplyMoveSpeedPassive()
    {
        if (ownerStats == null || baseMoveSpeed <= 0f)
        {
            return;
        }

        float perStackMultiplier = Mathf.Max(0f, passiveMoveSpeedPerStack);
        passiveMoveSpeed = baseMoveSpeed * (1f + passiveStacks * perStackMultiplier);
        if (maxPassiveMoveSpeed > 0f)
        {
            passiveMoveSpeed = Mathf.Min(passiveMoveSpeed, maxPassiveMoveSpeed);
        }

        ownerStats.MoveSpeed = passiveMoveSpeed;

        CacheOwnerMovement();
        if (ownerMovement != null)
        {
            ownerMovement.SetMoveSpeed(ownerStats.MoveSpeed);
        }
    }

    private void ResetMoveSpeedPassive()
    {
        if (runtimeEffect != RuntimeEffect.PassiveMoveStack || ownerStats == null || baseMoveSpeed <= 0f)
        {
            return;
        }

        passiveStacks = 0;
        passiveMoveSpeed = baseMoveSpeed;
        ownerStats.MoveSpeed = baseMoveSpeed;
        CacheOwnerMovement();
        if (ownerMovement != null)
        {
            ownerMovement.SetMoveSpeed(ownerStats.MoveSpeed);
        }
    }

    private Vector2 ResolveCastCenter()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || UnityEngine.InputSystem.Mouse.current == null || owner == null)
        {
            return owner != null ? owner.transform.position : transform.position;
        }

        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 screenPosition = new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Mathf.Abs(mainCamera.transform.position.z - owner.transform.position.z));

        return mainCamera.ScreenToWorldPoint(screenPosition);
    }

    private Vector2 ResolveAimDirection()
    {
        PlayerAttack attack = owner != null ? owner.GetComponent<PlayerAttack>() : null;
        if (attack != null && attack.TryGetAimDirection(out Vector2 direction) && direction.sqrMagnitude > 0.001f)
        {
            return direction.normalized;
        }

        PlayerMovement movement = owner != null ? owner.GetComponent<PlayerMovement>() : null;
        if (movement != null && movement.LastMoveDirection.sqrMagnitude > 0.001f)
        {
            return movement.LastMoveDirection.normalized;
        }

        return Vector2.right;
    }

    private void CastAreaAt(Vector2 center, float absoluteDamage, float effectRadius)
    {
        SpawnVfx(center, null);
        DamageInRadius(center, effectRadius, absoluteDamage, force);
    }

    private System.Collections.IEnumerator AreaAroundOwnerRoutine()
    {
        GameObject vfx = SpawnVfx(owner.transform.position, owner.transform);
        float endTime = Time.time + Mathf.Max(0.1f, duration);
        while (Time.time < endTime)
        {
            DamageInRadius(owner.transform.position, radius, damage, 0f);
            yield return new WaitForSeconds(Mathf.Max(0.05f, tickInterval));
        }

        if (vfx != null)
        {
            Destroy(vfx);
        }
    }

    private void CastMeleeStrike()
    {
        Vector2 direction = ResolveAimDirection();
        Vector2 center = (Vector2)owner.transform.position + direction * Mathf.Max(0.4f, radius * 0.55f);

        GameObject vfx = SpawnVfx(center, null);
        if (vfx != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            vfx.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, Mathf.Max(0.1f, radius));
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i] != null ? hits[i].gameObject : null;
            if (hitObject == null || owner == null || hitObject == owner.gameObject)
            {
                continue;
            }

            IDamageable damageable = hitObject.GetComponentInParent<IDamageable>();
            if (damageable == null || ReferenceEquals(damageable, owner) || damagedTargets.Contains(damageable))
            {
                continue;
            }

            damagedTargets.Add(damageable);
            Component damageComponent = damageable as Component;
            GameObject damageTarget = damageComponent != null ? damageComponent.gameObject : hitObject;
            owner.DealDamage(damageTarget, SafeMultiplier(damage));

            if (damageTarget.GetComponent<DoTDebuff>() == null)
            {
                damageTarget.AddComponent<DoTDebuff>().Initialize(damageable, bleedDps, Mathf.Max(0.1f, bleedDuration));
            }

            Rigidbody2D rb = damageTarget.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = hitObject.GetComponentInParent<Rigidbody2D>();
            }

            if (rb != null && force > 0f)
            {
                Vector2 knockbackDirection = ((Vector2)damageTarget.transform.position - (Vector2)owner.transform.position).normalized;
                if (knockbackDirection.sqrMagnitude <= 0.001f)
                {
                    knockbackDirection = direction;
                }

                rb.AddForce(knockbackDirection * force);
            }
        }
    }

    private void CastVortexTrap()
    {
        Vector2 center = ResolveCastCenter();
        GameObject zoneObject = SpawnVfx(center, null);
        if (zoneObject == null)
        {
            zoneObject = new GameObject($"{SkillName} Vortex Zone");
            zoneObject.transform.position = center;
        }

        CircleCollider2D circle = zoneObject.GetComponent<CircleCollider2D>();
        if (circle == null)
        {
            circle = zoneObject.AddComponent<CircleCollider2D>();
        }

        circle.isTrigger = true;
        circle.radius = Mathf.Max(0.2f, radius);

        SkillVortexZone zone = zoneObject.GetComponent<SkillVortexZone>();
        if (zone == null)
        {
            zone = zoneObject.AddComponent<SkillVortexZone>();
        }

        zone.Initialize(owner, duration, radius, force, damage, tickInterval);
    }

    private void CastForwardWave()
    {
        Vector2 direction = ResolveAimDirection();
        Vector2 start = (Vector2)owner.transform.position + direction * 0.65f;
        Vector2 end = start + direction * Mathf.Max(0.1f, range);

        GameObject vfx = SpawnVfx(start, null);
        if (vfx != null)
        {
            vfx.transform.rotation = ResolveDirectionalRotation(direction, 90f);
            StartCoroutine(MoveVfx(vfx.transform, start, end, Mathf.Max(0.05f, duration)));
        }

        DamageTsunamiAlongLine(start, direction, range, radius, damage);
    }

    private void DamageTsunamiAlongLine(Vector2 start, Vector2 direction, float castRange, float castRadius, float absoluteDamage)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            start,
            Mathf.Max(0.05f, castRadius),
            direction.normalized,
            Mathf.Max(0.1f, castRange));

        HashSet<EnemyMechanics> affectedEnemies = new HashSet<EnemyMechanics>();
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i].collider != null ? hits[i].collider.gameObject : null;
            if (hitObject == null || owner == null || hitObject == owner.gameObject)
            {
                continue;
            }

            EnemyMechanics enemy = hitObject.GetComponentInParent<EnemyMechanics>();
            if (enemy == null || !enemy.IsAlive || affectedEnemies.Contains(enemy))
            {
                continue;
            }

            affectedEnemies.Add(enemy);
            owner.DealDamage(enemy.gameObject, SafeMultiplier(absoluteDamage));
            enemy.ApplyTemporaryMoveSpeedMultiplier(Mathf.Clamp01(slowMultiplier), Mathf.Max(0.1f, statusDuration));

            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null && force > 0f)
            {
                rb.AddForce(direction.normalized * force);
            }
            else if (force > 0f)
            {
                enemy.transform.position += (Vector3)(direction.normalized * Mathf.Min(1.5f, force * 0.005f));
            }
        }
    }

    private void CastDash()
    {
        Vector2 direction = ResolveAimDirection();
        Vector3 start = owner.transform.position;
        Vector3 end = start + (Vector3)(direction * range);

        GameObject vfx = SpawnVfx(start, null);
        if (vfx != null)
        {
            Transform directionalRoot = CreateDirectionalVfxRoot(vfx, start, ResolveDirectionalRotation(direction, 0f));
            StartCoroutine(MoveVfx(directionalRoot, start, end, Mathf.Max(0.05f, duration)));
        }

        owner.transform.position = end;
        DamageInRadius(end, radius, damage, force, direction);
    }

    private System.Collections.IEnumerator TemporaryBuffRoutine()
    {
        if (ownerStats == null)
        {
            yield break;
        }

        float previousDamage = ownerStats.Damage;
        float previousAttackSpeed = ownerStats.AttackSpeed;
        float previousMoveSpeed = ownerStats.MoveSpeed;
        ownerStats.Damage *= Mathf.Max(0.01f, buffDamageMultiplier);
        ownerStats.AttackSpeed *= Mathf.Max(0.01f, buffAttackSpeedMultiplier);
        ownerStats.MoveSpeed *= Mathf.Max(0.01f, buffMoveSpeedMultiplier);

        GameObject vfx = SpawnVfx(owner.transform.position, owner.transform);
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));

        if (ownerStats != null)
        {
            ownerStats.Damage = previousDamage;
            ownerStats.AttackSpeed = previousAttackSpeed;
            ownerStats.MoveSpeed = previousMoveSpeed;
        }

        if (vfx != null)
        {
            Destroy(vfx);
        }
    }

    private System.Collections.IEnumerator TimeShiftRoutine()
    {
        GameObject vfx = SpawnVfx(owner.transform.position, owner.transform);
        float endTime = Time.time + Mathf.Max(0.1f, duration);
        float refreshDuration = Mathf.Max(0.15f, tickInterval + 0.1f);

        while (Time.time < endTime)
        {
            ApplyTimeShiftToEnemies(refreshDuration);
            ApplyTimeShiftToProjectiles(refreshDuration);
            yield return new WaitForSeconds(Mathf.Max(0.05f, tickInterval));
        }

        if (vfx != null)
        {
            Destroy(vfx);
        }
    }

    private void ApplyTimeShiftToEnemies(float refreshDuration)
    {
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemyObjects.Length; i++)
        {
            EnemyMechanics enemy = enemyObjects[i] != null ? enemyObjects[i].GetComponentInParent<EnemyMechanics>() : null;
            if (enemy != null && enemy.IsAlive)
            {
                enemy.ApplyTimeShiftMoveSpeedMultiplier(slowMultiplier, refreshDuration);
            }
        }
    }

    private void ApplyTimeShiftToProjectiles(float refreshDuration)
    {
        EnemyProjectile[] enemyProjectiles = FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < enemyProjectiles.Length; i++)
        {
            if (enemyProjectiles[i] != null)
            {
                enemyProjectiles[i].ApplyTimeShiftSpeedMultiplier(slowMultiplier, refreshDuration);
            }
        }

        ArkePrismProjectile[] prismProjectiles = FindObjectsByType<ArkePrismProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < prismProjectiles.Length; i++)
        {
            if (prismProjectiles[i] != null)
            {
                prismProjectiles[i].ApplyTimeShiftSpeedMultiplier(slowMultiplier, refreshDuration);
            }
        }
    }

    private void CastShield()
    {
        owner.KalkanYenile(shieldAmount);
        owner.SetTemporaryInvulnerable(duration);
        SpawnVfx(owner.transform.position, owner.transform);
    }

    private void CastProjectileStrike()
    {
        Vector2 direction = ResolveAimDirection();
        Vector2 start = (Vector2)owner.transform.position + direction * 0.55f;
        Vector2 end = start + direction * Mathf.Max(0.1f, range);

        GameObject vfx = SpawnVfx(start, null);
        if (vfx != null)
        {
            vfx.transform.rotation = ResolveDirectionalRotation(direction, -90f);
            StartCoroutine(MoveVfx(vfx.transform, start, end, Mathf.Max(0.05f, duration)));
        }

        DamageAlongLine(start, direction, range, radius, damage);
    }

    private void DamageAlongLine(Vector2 start, Vector2 direction, float castRange, float castRadius, float absoluteDamage)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            start,
            Mathf.Max(0.05f, castRadius),
            direction.normalized,
            Mathf.Max(0.1f, castRange));

        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject hitObject = hits[i].collider != null ? hits[i].collider.gameObject : null;
            if (hitObject == null || owner == null || hitObject == owner.gameObject)
            {
                continue;
            }

            IDamageable damageable = hitObject.GetComponentInParent<IDamageable>();
            if (damageable == null || ReferenceEquals(damageable, owner) || damagedTargets.Contains(damageable))
            {
                continue;
            }

            damagedTargets.Add(damageable);
            Component damageComponent = damageable as Component;
            GameObject damageTarget = damageComponent != null ? damageComponent.gameObject : hitObject;
            owner.DealDamage(damageTarget, SafeMultiplier(absoluteDamage));
        }
    }

    private System.Collections.IEnumerator MoveVfx(Transform vfxTransform, Vector3 start, Vector3 end, float travelTime)
    {
        float elapsed = 0f;
        while (vfxTransform != null && elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            vfxTransform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (vfxTransform != null)
        {
            vfxTransform.position = end;
        }
    }

    private Transform CreateDirectionalVfxRoot(GameObject vfx, Vector3 position, Quaternion rotation)
    {
        GameObject directionalRoot = new GameObject($"{SkillName} Directional VFX");
        directionalRoot.transform.position = position;
        directionalRoot.transform.rotation = rotation;

        vfx.transform.SetParent(directionalRoot.transform, false);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.identity;

        Destroy(directionalRoot, Mathf.Max(1f, duration + 0.25f));
        return directionalRoot.transform;
    }

    private static Quaternion ResolveDirectionalRotation(Vector2 direction, float spriteForwardOffset)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle + spriteForwardOffset);
    }

    private void DamageInRadius(Vector2 center, float effectRadius, float absoluteDamage, float knockback, Vector2? preferredDirection = null)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, Mathf.Max(0.1f, effectRadius));
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject target = hits[i] != null ? hits[i].gameObject : null;
            if (target == null || owner == null || target == owner.gameObject)
            {
                continue;
            }

            float multiplier = SafeMultiplier(absoluteDamage);
            float passiveMultiplier = ResolvePassiveDamageMultiplier(target);
            owner.DealDamage(target, multiplier * passiveMultiplier);

            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null && knockback > 0f)
            {
                Vector2 direction = preferredDirection.HasValue
                    ? preferredDirection.Value.normalized
                    : ((Vector2)target.transform.position - center).normalized;
                rb.AddForce(direction * knockback);
            }
        }
    }

    private float SafeMultiplier(float absoluteDamage)
    {
        float baseDamage = ownerStats != null && ownerStats.Damage > 0f ? ownerStats.Damage : 1f;
        return Mathf.Max(0f, absoluteDamage) / baseDamage;
    }

    private float ResolvePassiveDamageMultiplier(GameObject target)
    {
        if (runtimeEffect == RuntimeEffect.PassiveMoveDamage && ownerStats != null && baseMoveSpeed > 0f)
        {
            return 1f + Mathf.Max(0f, ownerStats.MoveSpeed - baseMoveSpeed) * 0.04f;
        }

        if (runtimeEffect == RuntimeEffect.PassiveMissingHealthDamage && owner != null && ownerStats != null && ownerStats.MaxHealth > 0f)
        {
            float missingHealthRatio = 1f - Mathf.Clamp01(owner.MevcutCan / ownerStats.MaxHealth);
            return 1f + missingHealthRatio * 0.8f;
        }

        return 1f;
    }

    private GameObject SpawnVfx(Vector3 position, Transform parent)
    {
        if (visualEffectPrefab == null)
        {
            return null;
        }

        UnityEngine.Object spawnedObject;
        try
        {
            spawnedObject = Instantiate(visualEffectPrefab, position, Quaternion.identity);
        }
        catch (System.InvalidCastException exception)
        {
            Debug.LogWarning($"ConfiguredGodSkill: VFX reference for '{SkillName}' is not a spawnable prefab. {exception.Message}", this);
            return null;
        }

        GameObject instance = spawnedObject as GameObject;
        if (instance == null && spawnedObject is Component component)
        {
            instance = component.gameObject;
        }

        if (instance == null)
        {
            Destroy(spawnedObject);
            Debug.LogWarning($"ConfiguredGodSkill: VFX reference for '{SkillName}' did not create a GameObject.", this);
            return null;
        }

        if (parent != null)
        {
            instance.transform.SetParent(parent, true);
        }

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = Mathf.Max(renderers[i].sortingOrder, sortingOrder);
        }

        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
        float lifetime = vfxLifetime > 0f
            ? vfxLifetime
            : ResolveDefaultVfxLifetime();
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = true;
            animators[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animators[i].Rebind();

            RuntimeAnimatorController controller = animators[i].runtimeAnimatorController;
            if (controller != null && controller.animationClips != null && controller.animationClips.Length > 0)
            {
                AnimationClip clip = controller.animationClips[0];
                animators[i].Play(clip.name, 0, 0f);
                if (vfxLifetime <= 0f)
                {
                    lifetime = Mathf.Max(lifetime, clip.length + 0.05f);
                }
            }

            animators[i].Update(0f);
        }

        if (parent == null || runtimeEffect != RuntimeEffect.AreaAroundOwner && runtimeEffect != RuntimeEffect.TemporaryBuff)
        {
            Destroy(instance, lifetime);
        }

        return instance;
    }

    private float ResolveDefaultVfxLifetime()
    {
        return runtimeEffect == RuntimeEffect.AreaAroundOwner || runtimeEffect == RuntimeEffect.TemporaryBuff
            ? Mathf.Max(0.1f, duration)
            : Mathf.Min(Mathf.Max(0.1f, duration), 0.75f);
    }

    private void HandleEnemyKilled(GameObject enemy)
    {
        if (!IsUnlocked || ownerStats == null)
        {
            return;
        }

        if (runtimeEffect == RuntimeEffect.PassiveKillAttackSpeed)
        {
            passiveStacks = Mathf.Min(maxStacks, passiveStacks + 1);
            ownerStats.AttackSpeed = baseAttackSpeed * (1f + passiveStacks * 0.12f);
            StartCoroutine(RemoveStackAfter(duration));
            SpawnVfx(owner.transform.position, owner.transform);
        }
        else if (runtimeEffect == RuntimeEffect.PassiveSoulArmor)
        {
            owner.KalkanYenile(shieldAmount);
            SpawnVfx(owner.transform.position, owner.transform);
        }
    }

    private System.Collections.IEnumerator RemoveStackAfter(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, delay));
        passiveStacks = Mathf.Max(0, passiveStacks - 1);
        if (ownerStats != null)
        {
            ownerStats.AttackSpeed = baseAttackSpeed * (1f + passiveStacks * 0.12f);
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (!IsUnlocked || runtimeEffect != RuntimeEffect.PassiveWrath || max <= 0f)
        {
            return;
        }

        float ratio = current / max;
        bool shouldTrigger = (!wrath60Triggered && ratio <= 0.6f) || (!wrath30Triggered && ratio <= 0.3f);
        if (!shouldTrigger)
        {
            return;
        }

        if (ratio <= 0.6f) wrath60Triggered = true;
        if (ratio <= 0.3f) wrath30Triggered = true;
        CastAreaAt(owner.transform.position, damage, radius);
        owner.SetTemporaryInvulnerable(2f);
    }

    private void HandleDealtDamage(GameObject target, float appliedDamage)
    {
        if (!IsUnlocked || target == null)
        {
            return;
        }

        if (runtimeEffect == RuntimeEffect.PassiveWet)
        {
            EnemyMechanics enemy = target.GetComponentInParent<EnemyMechanics>();
            if (enemy != null)
            {
                enemy.ApplyTemporaryMoveSpeedMultiplier(0.75f, duration);
                SpawnVfx(target.transform.position, target.transform);
            }
        }
        else if (runtimeEffect == RuntimeEffect.PassiveTideKnockback && Time.time >= nextPassiveProcTime)
        {
            nextPassiveProcTime = Time.time + 6f;
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null && owner != null)
            {
                rb.AddForce(((Vector2)target.transform.position - (Vector2)owner.transform.position).normalized * force);
                SpawnVfx(target.transform.position, null);
            }
        }
        else if (runtimeEffect == RuntimeEffect.PassiveDeathTouch)
        {
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null && target.GetComponent<DoTDebuff>() == null)
            {
                target.AddComponent<DoTDebuff>().Initialize(damageable, 4f, duration);
                SpawnVfx(target.transform.position, target.transform);
            }
        }
        else if (runtimeEffect == RuntimeEffect.PassiveLowHealthDamage)
        {
            ApplyScentOfBloodBonus(target, appliedDamage);
        }
    }

    private void ApplyScentOfBloodBonus(GameObject target, float appliedDamage)
    {
        if (applyingScentOfBloodBonus || owner == null || target == null || appliedDamage <= 0f)
        {
            return;
        }

        EnemyMechanics enemy = target.GetComponentInParent<EnemyMechanics>();
        if (enemy == null || !enemy.IsAlive || enemy.MaxHealth <= 0f)
        {
            return;
        }

        float missingHealth = Mathf.Max(0f, enemy.MaxHealth - enemy.CurrentHealth);
        float bonusDamage = missingHealth * Mathf.Max(0f, damage);
        if (bonusDamage <= 0.01f)
        {
            return;
        }

        applyingScentOfBloodBonus = true;
        owner.DealDamage(enemy.gameObject, SafeMultiplier(bonusDamage));
        applyingScentOfBloodBonus = false;

        SpawnVfx(enemy.transform.position, enemy.transform);
    }

    private static bool IsPassiveRuntimeEffect(RuntimeEffect effect)
    {
        return effect == RuntimeEffect.PassiveKillAttackSpeed ||
               effect == RuntimeEffect.PassiveWrath ||
               effect == RuntimeEffect.PassiveWet ||
               effect == RuntimeEffect.PassiveTideKnockback ||
               effect == RuntimeEffect.PassiveDeathTouch ||
               effect == RuntimeEffect.PassiveSoulArmor ||
               effect == RuntimeEffect.PassiveLowHealthDamage ||
               effect == RuntimeEffect.PassiveMissingHealthDamage ||
               effect == RuntimeEffect.PassiveMoveStack ||
               effect == RuntimeEffect.PassiveMoveDamage ||
               effect == RuntimeEffect.PassiveProtectiveAura ||
               effect == RuntimeEffect.PassiveWisdom;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(".", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
    }
}
