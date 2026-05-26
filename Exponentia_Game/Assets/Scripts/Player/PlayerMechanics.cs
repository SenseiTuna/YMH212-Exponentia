/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.3.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 12:00
 * DESCRIPTION: Core player combat/resource mechanics.
 */

using System.Collections.Generic;
using Exponentia.Player;
using UnityEngine;

public class PlayerMechanics : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Attack")]
    [SerializeField] private LayerMask attackLayers = ~0;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private float baseAttackMultiplier = 1f;
    [SerializeField] private float baseAttackManaCost = 0f;

    [Header("Damage Intake")]
    [SerializeField] private float damageCooldown = 0.2f;
    [SerializeField] private float invulnerabilityDuration = 0.75f;
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private bool flashDuringIFrames = true;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.12f;

    [Header("Damage Feel")]
    [SerializeField] private CombatFeedbackController combatFeedback;
    [SerializeField] private bool autoAddFeedbackComponents = true;

    [Header("Health Text")]
    [SerializeField] private Vector3 healthTextOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private Color healthTextColor = Color.green;
    [SerializeField] private int healthTextFontSize = 32;
    [SerializeField] private float healthTextCharacterSize = 0.22f;

    public float MevcutCan { get; private set; }
    public float MevcutMana { get; private set; }
    public float MevcutKalkan { get; private set; }

    private float nextAttackTime;
    private float nextDamageTime;
    private float invulnerabilityEndTime;
    private TextMesh healthTextMesh;
    private DamageFlashFeedback damageFlashFeedback;
    private KnockbackReceiver2D knockbackReceiver;
    private Coroutine invulRoutine;

    public bool Yasiyor => MevcutCan > 0f;
    public bool IsInvulnerable => isInvulnerable || Time.time < invulnerabilityEndTime;

    public event System.Action<float, float> OnCanDegisti;
    public event System.Action<float, float> OnDamaged;
    public event System.Action<float, float> OnManaDegisti;
    public event System.Action<int> OnLevelAtlandi;
    public event System.Action<float, float> OnXpDegisti;
    public event System.Action OnOldu;
    public event System.Action OnInvulnerabilityStarted;
    public event System.Action OnInvulnerabilityEnded;
    public event System.Action<GameObject, float> OnDealtDamage;
    public event System.Action<GameObject> OnEnemyKilled;

    private void Reset()
    {
        playerStats = GetComponent<PlayerStats>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (combatFeedback == null)
        {
            combatFeedback = FindFirstObjectByType<CombatFeedbackController>();
        }

        if (autoAddFeedbackComponents)
        {
            damageFlashFeedback = GetComponent<DamageFlashFeedback>();
            if (damageFlashFeedback == null)
            {
                damageFlashFeedback = gameObject.AddComponent<DamageFlashFeedback>();
            }

            knockbackReceiver = GetComponent<KnockbackReceiver2D>();
            if (knockbackReceiver == null)
            {
                knockbackReceiver = gameObject.AddComponent<KnockbackReceiver2D>();
            }
        }
        else
        {
            damageFlashFeedback = GetComponent<DamageFlashFeedback>();
            knockbackReceiver = GetComponent<KnockbackReceiver2D>();
        }

        EnsureHealthText();
        MevcutCan = Mathf.Max(0f, playerStats != null ? playerStats.MaxHealth : 0f);
        MevcutMana = Mathf.Max(0f, playerStats != null ? playerStats.Mana : 0f);
        MevcutKalkan = Mathf.Max(0f, playerStats != null ? playerStats.Shield : 0f);
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnAttackPressed += HandleAttackPressed;
        }
    }

    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerMechanics requires PlayerStats reference.", this);
            enabled = false;
            return;
        }

        SyncResourcesFromStats();
        ApplyStatsToComponents();
        RaiseResourceEvents();
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnAttackPressed -= HandleAttackPressed;
        }
    }

    private void LateUpdate()
    {
        UpdateHealthTextTransform();
    }

    public float TakeDamage(float amount)
    {
        DamageInfo info = new DamageInfo(amount, transform.position, Vector2.zero, null, false, 0f);
        return TakeDamage(info);
    }

    public float TakeDamage(int amount)
    {
        return TakeDamage((float)amount);
    }

    public float TakeDamage(DamageInfo damageInfo)
    {
        if (!Yasiyor || damageInfo.amount <= 0f)
        {
            return 0f;
        }

        if (!damageInfo.ignoreInvulnerability && !CanTakeDamage())
        {
            return 0f;
        }

        nextDamageTime = Time.time + Mathf.Max(0f, damageCooldown);
        float remainingDamage = damageInfo.amount;

        if (MevcutKalkan > 0f)
        {
            float absorbedDamage = Mathf.Min(MevcutKalkan, remainingDamage);
            MevcutKalkan -= absorbedDamage;
            remainingDamage -= absorbedDamage;
        }

        float reducedByDefense = Mathf.Max(1f, remainingDamage - playerStats.Defense);
        float appliedDamage = remainingDamage > 0f ? reducedByDefense : 0f;

        if (appliedDamage <= 0f)
        {
            OnCanDegisti?.Invoke(MevcutCan, playerStats.MaxHealth);
            return 0f;
        }

        MevcutCan = Mathf.Max(0f, MevcutCan - appliedDamage);
        FloatingCombatText.Create(Mathf.CeilToInt(appliedDamage).ToString(), transform.position + Vector3.up * 0.9f, Color.yellow);
        OnCanDegisti?.Invoke(MevcutCan, playerStats.MaxHealth);
        OnDamaged?.Invoke(appliedDamage, MevcutCan);
        UpdateHealthText();

        DamageInfo resolvedInfo = damageInfo;
        if (resolvedInfo.hitDirection.sqrMagnitude <= 0.001f && resolvedInfo.source != null)
        {
            resolvedInfo.hitDirection = ((Vector2)transform.position - (Vector2)resolvedInfo.source.transform.position).normalized;
        }

        if (combatFeedback != null)
        {
            combatFeedback.OnPlayerDamaged(
                this,
                resolvedInfo,
                invulnerabilityDuration,
                flashDuringIFrames,
                Mathf.Max(0.01f, flashInterval),
                damageFlashColor,
                damageFlashDuration);
        }
        else
        {
            if (damageFlashFeedback != null)
            {
                damageFlashFeedback.Flash(damageFlashColor, damageFlashDuration);
                if (flashDuringIFrames && invulnerabilityDuration > 0f)
                {
                    damageFlashFeedback.StartBlink(damageFlashColor, invulnerabilityDuration, Mathf.Max(0.01f, flashInterval));
                }
            }

            if (knockbackReceiver != null && resolvedInfo.hitDirection.sqrMagnitude > 0.001f)
            {
                float force = resolvedInfo.knockbackForce > 0f ? resolvedInfo.knockbackForce : 4f;
                knockbackReceiver.ApplyKnockback(resolvedInfo.hitDirection, force, 0.12f);
            }
        }

        if (!damageInfo.ignoreInvulnerability)
        {
            StartInvulnerability();
        }

        if (!Yasiyor)
        {
            OnOldu?.Invoke();
        }

        return appliedDamage;
    }

    public float DealDamage(GameObject target, float damageMultiplier = 1f)
    {
        if (!Yasiyor || target == null)
        {
            return 0f;
        }

        IDamageable damageable = FindDamageable(target);
        if (damageable == null || ReferenceEquals(damageable, this))
        {
            return 0f;
        }

        float totalDamage = Mathf.Max(0f, playerStats.Damage * damageMultiplier);
        // Try to detect enemy death for kill-based passives
        EnemyMechanics enemy = target.GetComponentInParent<EnemyMechanics>();
        bool wasAlive = enemy != null && enemy.IsAlive;

        float appliedDamage;
        if (enemy != null)
        {
            Vector2 direction = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            DamageInfo info = new DamageInfo(
                totalDamage,
                enemy.transform.position,
                direction,
                gameObject,
                false,
                0f);
            appliedDamage = enemy.TakeDamage(info);
        }
        else
        {
            appliedDamage = damageable.TakeDamage(totalDamage);
        }

        Debug.Log($"PlayerMechanics.DealDamage: Attacker={gameObject.name}, Target={target.name}, TotalDamage={totalDamage}, AppliedDamage={appliedDamage}");

        // Notify subscribers that damage was dealt
        if (appliedDamage > 0f)
        {
            OnDealtDamage?.Invoke(target, appliedDamage);
        }

        // If we tracked an enemy and it died as a result, emit kill event
        if (enemy != null && wasAlive && !enemy.IsAlive)
        {
            OnEnemyKilled?.Invoke(target);
        }

        if (appliedDamage > 0f)
        {
            float lifeStealRatio = NormalizePercent(playerStats.LifeSteal);
            if (lifeStealRatio > 0f)
            {
                Heal(appliedDamage * lifeStealRatio);
            }
        }

        return appliedDamage;
    }

    private bool isInvulnerable = false;

    public bool CanTakeDamage()
    {
        return Yasiyor && !IsInvulnerable && Time.time >= nextDamageTime;
    }

    public void StartInvulnerability()
    {
        StartInvulnerability(invulnerabilityDuration);
    }

    public void StartInvulnerability(float duration)
    {
        float safeDuration = Mathf.Max(0f, duration);
        if (safeDuration <= 0f)
        {
            return;
        }

        if (invulRoutine != null)
        {
            StopCoroutine(invulRoutine);
        }

        invulRoutine = StartCoroutine(InvulRoutine(safeDuration));
    }

    public void SetTemporaryInvulnerable(float duration)
    {
        if (duration <= 0f)
            return;

        StartInvulnerability(duration);
    }

    private System.Collections.IEnumerator InvulRoutine(float duration)
    {
        isInvulnerable = true;
        invulnerabilityEndTime = Time.time + duration;
        OnInvulnerabilityStarted?.Invoke();

        if (flashDuringIFrames && damageFlashFeedback != null && combatFeedback == null)
        {
            damageFlashFeedback.StartBlink(damageFlashColor, duration, Mathf.Max(0.01f, flashInterval));
        }

        yield return new WaitForSeconds(duration);

        isInvulnerable = false;
        invulnerabilityEndTime = 0f;
        if (damageFlashFeedback != null)
        {
            damageFlashFeedback.StopBlinkAndRestore();
        }

        OnInvulnerabilityEnded?.Invoke();
        invulRoutine = null;
    }

    public bool HarcaMana(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (MevcutMana < amount)
        {
            Debug.LogWarning("Insufficient Mana to cast skill! Required: " + amount + " Current: " + MevcutMana);
            return false;
        }

        MevcutMana -= amount;
        OnManaDegisti?.Invoke(MevcutMana, playerStats.Mana);
        return true;
    }

    public void ManaYenile(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        MevcutMana = Mathf.Min(playerStats.Mana, MevcutMana + amount);
        OnManaDegisti?.Invoke(MevcutMana, playerStats.Mana);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || !Yasiyor)
        {
            return;
        }

        MevcutCan = Mathf.Min(playerStats.MaxHealth, MevcutCan + amount);
        OnCanDegisti?.Invoke(MevcutCan, playerStats.MaxHealth);
        UpdateHealthText();
    }

    public void GainXp(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        playerStats.Xp += amount;

        while (playerStats.Xp >= playerStats.NextLevelXp)
        {
            playerStats.Xp -= playerStats.NextLevelXp;
            LevelUp();
        }

        OnXpDegisti?.Invoke(playerStats.Xp, playerStats.NextLevelXp);
    }

    public void KalkanYenile(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        MevcutKalkan = Mathf.Min(playerStats.Shield, MevcutKalkan + amount);
    }

    private void HandleAttackPressed()
    {
        TryBasicAttack();
    }

    private bool TryBasicAttack()
    {
        if (!Yasiyor || Time.time < nextAttackTime)
        {
            return false;
        }

        if (!HarcaMana(baseAttackManaCost))
        {
            return false;
        }

        float attackInterval = playerStats.AttackSpeed > 0f ? 1f / playerStats.AttackSpeed : 1f;
        nextAttackTime = Time.time + attackInterval;

        Vector2 attackDirection = playerMovement != null ? playerMovement.LastMoveDirection : Vector2.right;
        if (attackDirection.sqrMagnitude <= 0.001f)
        {
            attackDirection = Vector2.right;
        }

        Vector2 center = (Vector2)transform.position + attackDirection.normalized * attackRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRadius, attackLayers);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = FindDamageable(hits[i].gameObject);
            if (damageable == null || ReferenceEquals(damageable, this) || damagedTargets.Contains(damageable))
            {
                continue;
            }

            damagedTargets.Add(damageable);
            DealDamage(hits[i].gameObject, baseAttackMultiplier);
        }

        return true;
    }

    private void LevelUp()
    {
        playerStats.Level += 1;
        playerStats.MaxHealth += playerStats.LevelUpMaxHealthBonus;
        playerStats.Damage += playerStats.LevelUpDamageBonus;
        playerStats.Mana += playerStats.LevelUpManaBonus;
        playerStats.Defense += playerStats.LevelUpDefenseBonus;
        playerStats.Shield += playerStats.LevelUpShieldBonus;
        playerStats.NextLevelXp = Mathf.Max(1f, playerStats.NextLevelXp * playerStats.LevelXpMultiplier);

        MevcutCan = playerStats.MaxHealth;
        MevcutMana = playerStats.Mana;
        MevcutKalkan = playerStats.Shield;

        ApplyStatsToComponents();
        RaiseResourceEvents();
        OnLevelAtlandi?.Invoke(playerStats.Level);
    }

    private void ApplyStatsToComponents()
    {
        if (playerMovement != null)
        {
            // Turkish: Movement scripti korunur, sadece seçili karakter hızını dışarıdan enjekte ederiz.
            playerMovement.SetMoveSpeed(playerStats.MoveSpeed);
        }
    }

    private void RaiseResourceEvents()
    {
        OnCanDegisti?.Invoke(MevcutCan, playerStats.MaxHealth);
        OnManaDegisti?.Invoke(MevcutMana, playerStats.Mana);
        OnXpDegisti?.Invoke(playerStats.Xp, playerStats.NextLevelXp);
        UpdateHealthText();
    }

    public void SyncResourcesFromStats()
    {
        if (playerStats == null)
        {
            return;
        }

        // Turkish: CharacterData uygulanınca runtime kaynaklarını PlayerStats ile tekrar hizalıyoruz.
        MevcutCan = Mathf.Clamp(playerStats.CurrentHealth, 0f, playerStats.MaxHealth);
        MevcutMana = Mathf.Max(0f, playerStats.Mana);
        MevcutKalkan = Mathf.Max(0f, playerStats.Shield);
    }

    private static float NormalizePercent(float value)
    {
        if (value <= 0f)
        {
            return 0f;
        }

        return value > 1f ? value / 100f : value;
    }

    private static IDamageable FindDamageable(GameObject target)
    {
        MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IDamageable damageable)
            {
                return damageable;
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 direction = Application.isPlaying && playerMovement != null
            ? playerMovement.LastMoveDirection
            : Vector2.right;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        Gizmos.color = Color.red;
        Vector2 center = (Vector2)transform.position + direction.normalized * attackRange;
        Gizmos.DrawWireSphere(center, attackRadius);
    }

    private void EnsureHealthText()
    {
        if (healthTextMesh != null)
        {
            return;
        }

        Transform existingText = transform.Find("PlayerHealthText");
        GameObject textObject;
        if (existingText != null)
        {
            textObject = existingText.gameObject;
        }
        else
        {
            textObject = new GameObject("PlayerHealthText");
            textObject.transform.SetParent(transform);
            textObject.transform.localPosition = healthTextOffset;
        }

        healthTextMesh = textObject.GetComponent<TextMesh>();
        if (healthTextMesh == null)
        {
            healthTextMesh = textObject.AddComponent<TextMesh>();
        }

        healthTextMesh.anchor = TextAnchor.MiddleCenter;
        healthTextMesh.alignment = TextAlignment.Center;
        healthTextMesh.fontSize = healthTextFontSize;
        healthTextMesh.characterSize = healthTextCharacterSize;
        healthTextMesh.color = healthTextColor;

        MeshRenderer textRenderer = healthTextMesh.GetComponent<MeshRenderer>();
        textRenderer.sortingOrder = 20;
    }

    private void UpdateHealthText()
    {
        if (healthTextMesh == null)
        {
            return;
        }

        healthTextMesh.text = Mathf.CeilToInt(MevcutCan).ToString();
    }

    private void UpdateHealthTextTransform()
    {
        if (healthTextMesh == null)
        {
            return;
        }

        healthTextMesh.transform.position = transform.position + healthTextOffset;

        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            healthTextMesh.transform.rotation = activeCamera.transform.rotation;
        }
    }
}
