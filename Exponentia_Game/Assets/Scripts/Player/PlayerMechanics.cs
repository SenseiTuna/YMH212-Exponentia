using System.Collections.Generic;
using UnityEngine;

public class PlayerMechanics : MonoBehaviour, IDamageable
{
    [Header("Referanslar")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Saldiri")]
    [SerializeField] private LayerMask saldiriKatmanlari = ~0;
    [SerializeField] private float saldiriMenzili = 1.5f;
    [SerializeField] private float saldiriYaricapi = 0.5f;
    [SerializeField] private float temelSaldiriCarpani = 1f;
    [SerializeField] private float temelSaldiriManaMaliyeti = 15f;

    [Header("Hasar Alma")]
    [SerializeField] private float hasarAlmaBeklemeSuresi = 0.2f;

    [Header("Can Yazisi")]
    [SerializeField] private Vector3 canYaziOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private Color canYaziRengi = Color.green;
    [SerializeField] private int canYaziFontBoyutu = 32;
    [SerializeField] private float canYaziKarakterBoyutu = 0.22f;

    public float MevcutCan { get; private set; }
    public float MevcutMana { get; private set; }
    public float MevcutKalkan { get; private set; }

    private float sonrakiSaldiriZamani;
    private float sonrakiHasarAlmaZamani;
    private TextMesh canTextMesh;

    public bool Yasiyor => MevcutCan > 0f;

    public event System.Action<float, float> OnCanDegisti;
    public event System.Action<float, float> OnManaDegisti;
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
        MevcutCan = Mathf.Max(0f, playerStats != null ? playerStats.can : 0f);
        MevcutMana = Mathf.Max(0f, playerStats != null ? playerStats.mana : 0f);
        MevcutKalkan = Mathf.Max(0f, playerStats != null ? playerStats.kalkan : 0f);
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
            Debug.LogError("PlayerMechanics icin PlayerStats referansi gerekli.", this);
            enabled = false;
            return;
        }

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
        if (!Yasiyor || amount <= 0f || Time.time < sonrakiHasarAlmaZamani || isInvulnerable)
        {
            return 0f;
        }

        sonrakiHasarAlmaZamani = Time.time + hasarAlmaBeklemeSuresi;

        float kalanHasar = amount;

        if (MevcutKalkan > 0f)
        {
            float emilenHasar = Mathf.Min(MevcutKalkan, kalanHasar);
            MevcutKalkan -= emilenHasar;
            kalanHasar -= emilenHasar;
        }

        float savunmaSonrasiHasar = Mathf.Max(1f, kalanHasar - playerStats.savunma);
        float uygulananHasar = kalanHasar > 0f ? savunmaSonrasiHasar : 0f;

        if (uygulananHasar <= 0f)
        {
            OnCanDegisti?.Invoke(MevcutCan, playerStats.can);
            return 0f;
        }

        MevcutCan = Mathf.Max(0f, MevcutCan - uygulananHasar);
        FloatingCombatText.Create(Mathf.CeilToInt(uygulananHasar).ToString(), transform.position + Vector3.up * 0.9f, Color.yellow);
        OnCanDegisti?.Invoke(MevcutCan, playerStats.can);
        UpdateHealthText();

        if (!Yasiyor)
        {
            OnOldu?.Invoke();
        }

        return uygulananHasar;
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

        float totalDamage = Mathf.Max(0f, playerStats.hasar * damageMultiplier);
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
            float lifeStealRatio = NormalizePercent(playerStats.canCalma);
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
            return false;
        }

        MevcutMana -= amount;
        OnManaDegisti?.Invoke(MevcutMana, playerStats.mana);
        return true;
    }

    public void ManaYenile(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        MevcutMana = Mathf.Min(playerStats.mana, MevcutMana + amount);
        OnManaDegisti?.Invoke(MevcutMana, playerStats.mana);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || !Yasiyor)
        {
            return;
        }

        MevcutCan = Mathf.Min(playerStats.can, MevcutCan + amount);
        OnCanDegisti?.Invoke(MevcutCan, playerStats.can);
        UpdateHealthText();
    }

    public void GainXp(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        playerStats.xp += amount;

        while (playerStats.xp >= playerStats.sonrakiLevelXp)
        {
            playerStats.xp -= playerStats.sonrakiLevelXp;
            LevelUp();
        }

        OnXpDegisti?.Invoke(playerStats.xp, playerStats.sonrakiLevelXp);
    }

    public void KalkanYenile(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        MevcutKalkan = Mathf.Min(playerStats.kalkan, MevcutKalkan + amount);
    }

    private void HandleAttackPressed()
    {
        TryBasicAttack();
    }

    private bool TryBasicAttack()
    {
        if (!Yasiyor || Time.time < sonrakiSaldiriZamani)
        {
            return false;
        }

        if (!HarcaMana(temelSaldiriManaMaliyeti))
        {
            return false;
        }

        float saldiriAraligi = playerStats.saldiriHizi > 0f ? 1f / playerStats.saldiriHizi : 1f;
        sonrakiSaldiriZamani = Time.time + saldiriAraligi;

        Vector2 saldiriYon = playerMovement != null ? playerMovement.LastMoveDirection : Vector2.right;
        if (saldiriYon.sqrMagnitude <= 0.001f)
        {
            saldiriYon = Vector2.right;
        }

        Vector2 merkez = (Vector2)transform.position + saldiriYon.normalized * saldiriMenzili;
        Collider2D[] hits = Physics2D.OverlapCircleAll(merkez, saldiriYaricapi, saldiriKatmanlari);
        HashSet<IDamageable> vurulanHedefler = new HashSet<IDamageable>();

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = FindDamageable(hits[i].gameObject);
            if (damageable == null || ReferenceEquals(damageable, this) || vurulanHedefler.Contains(damageable))
            {
                continue;
            }

            vurulanHedefler.Add(damageable);
            DealDamage(hits[i].gameObject, temelSaldiriCarpani);
        }

        return true;
    }

    private void LevelUp()
    {
        playerStats.level++;
        playerStats.can += playerStats.levelBasinaCanArtisi;
        playerStats.hasar += playerStats.levelBasinaHasarArtisi;
        playerStats.mana += playerStats.levelBasinaManaArtisi;
        playerStats.savunma += playerStats.levelBasinaSavunmaArtisi;
        playerStats.kalkan += playerStats.levelBasinaKalkanArtisi;
        playerStats.sonrakiLevelXp = Mathf.Max(1f, playerStats.sonrakiLevelXp * playerStats.levelXpCarpani);

        MevcutCan = playerStats.can;
        MevcutMana = playerStats.mana;
        MevcutKalkan = playerStats.kalkan;

        ApplyStatsToComponents();
        RaiseResourceEvents();
        OnLevelAtlandi?.Invoke(playerStats.level);
    }

    private void ApplyStatsToComponents()
    {
        if (playerMovement != null)
        {
            playerMovement.SetMoveSpeed(playerStats.hareketHizi);
        }
    }

    private void RaiseResourceEvents()
    {
        OnCanDegisti?.Invoke(MevcutCan, playerStats.can);
        OnManaDegisti?.Invoke(MevcutMana, playerStats.mana);
        OnXpDegisti?.Invoke(playerStats.xp, playerStats.sonrakiLevelXp);
        UpdateHealthText();
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
        Vector2 yon = Application.isPlaying && playerMovement != null
            ? playerMovement.LastMoveDirection
            : Vector2.right;

        if (yon.sqrMagnitude <= 0.001f)
        {
            yon = Vector2.right;
        }

        Gizmos.color = Color.red;
        Vector2 merkez = (Vector2)transform.position + yon.normalized * saldiriMenzili;
        Gizmos.DrawWireSphere(merkez, saldiriYaricapi);
    }

    private void EnsureHealthText()
    {
        if (canTextMesh != null)
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
            textObject.transform.localPosition = canYaziOffset;
        }

        canTextMesh = textObject.GetComponent<TextMesh>();
        if (canTextMesh == null)
        {
            canTextMesh = textObject.AddComponent<TextMesh>();
        }

        canTextMesh.anchor = TextAnchor.MiddleCenter;
        canTextMesh.alignment = TextAlignment.Center;
        canTextMesh.fontSize = canYaziFontBoyutu;
        canTextMesh.characterSize = canYaziKarakterBoyutu;
        canTextMesh.color = canYaziRengi;

        MeshRenderer textRenderer = canTextMesh.GetComponent<MeshRenderer>();
        textRenderer.sortingOrder = 20;
    }

    private void UpdateHealthText()
    {
        if (canTextMesh == null)
        {
            return;
        }

        canTextMesh.text = Mathf.CeilToInt(MevcutCan).ToString();
    }

    private void UpdateHealthTextTransform()
    {
        if (canTextMesh == null)
        {
            return;
        }

        canTextMesh.transform.position = transform.position + canYaziOffset;

        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            canTextMesh.transform.rotation = activeCamera.transform.rotation;
        }
    }
}
