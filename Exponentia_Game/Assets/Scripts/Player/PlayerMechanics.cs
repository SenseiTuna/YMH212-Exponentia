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

    [Header("Health Text")]
    [SerializeField] private Vector3 healthTextOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private Color healthTextColor = Color.green;
    [SerializeField] private int healthTextFontSize = 32;
    [SerializeField] private float healthTextCharacterSize = 0.22f;

    public float MevcutCan { get; private set; }
    public float MevcutMana { get; private set; }
    public float MevcutKalkan { get; private set; }
    public float MevcutLaserMana { get; private set; }
    public float MaxLaserMana { get; private set; } = 100f;
    [SerializeField] private float laserManaRegenRate = 15f;

    [Header("Kill Resource Bonus")]
    [SerializeField] private int killsNeededForSkillMana = 5;
    private int currentKillCountForMana = 0;

    private float nextAttackTime;
    private float nextDamageTime;
    private TextMesh healthTextMesh;

    public bool Yasiyor => MevcutCan > 0f;

    public event System.Action<float, float> OnCanDegisti;
    public event System.Action<float, float> OnManaDegisti;
    public event System.Action<float, float> OnLaserManaDegisti;
    public event System.Action<int> OnLevelAtlandi;
    public event System.Action<float, float> OnXpDegisti;
    public event System.Action OnOldu;
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

        EnsureHealthText();
        MevcutCan = Mathf.Max(0f, playerStats != null ? playerStats.MaxHealth : 0f);
        MevcutMana = Mathf.Max(0f, playerStats != null ? playerStats.Mana : 0f);
        MevcutKalkan = Mathf.Max(0f, playerStats != null ? playerStats.Shield : 0f);
        MevcutLaserMana = MaxLaserMana;
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnAttackPressed += HandleAttackPressed;
        }

        OnEnemyKilled += HandleEnemyKilledForMana;
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

        OnEnemyKilled -= HandleEnemyKilledForMana;
    }

    private void Update()
    {
        if (MevcutLaserMana < MaxLaserMana)
        {
            MevcutLaserMana = Mathf.Min(MaxLaserMana, MevcutLaserMana + laserManaRegenRate * Time.deltaTime);
            OnLaserManaDegisti?.Invoke(MevcutLaserMana, MaxLaserMana);
        }
    }

    private void LateUpdate()
    {
        UpdateHealthTextTransform();
    }

    public float TakeDamage(float amount)
    {
        if (!Yasiyor || amount <= 0f || Time.time < nextDamageTime || isInvulnerable)
        {
            return 0f;
        }

        nextDamageTime = Time.time + damageCooldown;
        float remainingDamage = amount;

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
        UpdateHealthText();

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


        float appliedDamage = damageable.TakeDamage(totalDamage);

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

    public void SetTemporaryInvulnerable(float duration)
    {
        if (duration <= 0f)
            return;

        if (isInvulnerable)
        {
            StopCoroutine("InvulRoutine");
        }

        StartCoroutine("InvulRoutine", duration);
    }

    private System.Collections.IEnumerator InvulRoutine(object arg)
    {
        float duration = (float)arg;
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
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

    public bool HarcaLaserMana(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (MevcutLaserMana < amount)
        {
            return false;
        }

        MevcutLaserMana -= amount;
        OnLaserManaDegisti?.Invoke(MevcutLaserMana, MaxLaserMana);
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
        OnLaserManaDegisti?.Invoke(MevcutLaserMana, MaxLaserMana);
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
        MevcutLaserMana = MaxLaserMana;
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

    private void HandleEnemyKilledForMana(GameObject enemy)
    {
        currentKillCountForMana++;
        if (currentKillCountForMana >= killsNeededForSkillMana)
        {
            currentKillCountForMana = 0;

            float manaToRestore = 30f; // Default fallback (Zeus cost)
            PlayerAttack attackComp = GetComponent<PlayerAttack>();
            if (attackComp != null && attackComp.EquippedSkill != null)
            {
                manaToRestore = attackComp.EquippedSkill.ManaCost;
            }

            ManaYenile(manaToRestore);
            FloatingCombatText.Create("+Skill Mana!", transform.position + Vector3.up * 1.5f, Color.cyan);
            Debug.Log($"Düşman öldürme ödülü! {killsNeededForSkillMana} düşman öldü, 1 skillik mana ({manaToRestore}) kazanıldı!");
        }
    }
}
