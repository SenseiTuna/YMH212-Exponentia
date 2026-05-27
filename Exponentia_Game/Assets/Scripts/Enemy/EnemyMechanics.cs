using System.Collections;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Collider2D))]
public class EnemyMechanics : MonoBehaviour, IDamageable
{
    [Header("Kimlik")]
    [SerializeField] protected string mobDisplayName = "Generic Enemy";

    [Header("Temel Statlar")]
    [SerializeField] protected float maxCan = 50f;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float touchDamage = 10f;
    [SerializeField] protected float xpReward = 10f;

    [Header("Hareket")]
    [SerializeField] protected bool useChaseMovement = true;
    [SerializeField] protected float stopDistance = 0.2f;
    [SerializeField] protected float touchDamageCooldown = 0.5f;

    [Header("Takılma Kurtarma (Stuck Detection)")]
    [SerializeField] protected bool checkStuck = true;
    [SerializeField] protected float stuckCheckInterval = 0.5f;
    [SerializeField] protected float minMoveDistance = 0.05f;
    [SerializeField] protected float unstuckPushForce = 2f;
    [SerializeField] protected float unstuckDuration = 0.3f;

    [Header("Görünüm ve Animasyon")]
    [SerializeField] protected bool usePlaceholderVisuals = false; // Gerçek assetleri kullanacaksanız bunu kapatın
    [SerializeField] protected Color placeholderColor = Color.gray;
    [SerializeField] protected Vector2 placeholderScale = Vector2.one;
    [SerializeField] protected int sortingOrder = 5;

    protected Animator animator;
    protected SpriteRenderer mainSpriteRenderer;
    private bool isFacingRight = true;

    [Header("Projectile Template")]
    [SerializeField] protected EnemyProjectile enemyProjectilePrefab;

    [Header("Can Yazisi")]
    [SerializeField] protected Vector3 yaziOffset = new Vector3(0f, 1.1f, 0f);
    [SerializeField] protected int yaziFontBoyutu = 32;
    [SerializeField] protected float yaziKarakterBoyutu = 0.22f;
    [SerializeField] protected Color yaziRengi = Color.yellow;

    [Header("Olum Akisi")]
    [SerializeField] protected float fallbackDeathDespawnDelay = 0.85f;
    [SerializeField] [Range(0.1f, 0.98f)] protected float deathNormalizedDestroyPoint = 0.92f;

    [Header("Hit Feedback")]
    [SerializeField] protected CombatFeedbackController combatFeedback;
    [SerializeField] protected bool autoAddFeedbackComponents = true;
    [SerializeField] protected bool disableKnockback = false;

    protected float mevcutCan;
    protected Transform playerTarget;
    protected PlayerMechanics playerMechanics;

    private float nextTouchDamageTime;
    private Transform bodyVisual;
    private SpriteRenderer bodyRenderer;
    private TextMesh canTextMesh;
    
    // A* Referansı
    protected IAstarAI aiAgent;

    // Takılma Kurtarma Durumları
    private Vector3 lastTickPosition;
    private float stuckCheckTimer;
    private bool isUnstucking = false;
    private float unstuckTimer = 0f;
    protected Rigidbody2D rb2d;
    protected bool isDying;
    private Coroutine deathRoutine;
    private DamageFlashFeedback damageFlashFeedback;
    private KnockbackReceiver2D knockbackReceiver;
    private Coroutine timeShiftMoveSpeedRoutine;
    private bool timeShiftMoveSpeedActive;
    private float timeShiftOriginalMoveSpeed;

    private static Sprite cachedSquareSprite;
    private static Material cachedSpriteMaterial;

    public float CurrentHealth => mevcutCan;
    public float MaxHealth => maxCan;
    public bool IsAlive => mevcutCan > 0f;
    protected Transform PlayerTarget => playerTarget;
    protected PlayerMechanics PlayerMechanics => playerMechanics;

    protected virtual void Awake()
    {
        EnsureEnemyTag();
        RenameGameObject();
        CachePlayerReferences();
        
        animator = GetComponentInChildren<Animator>();
        mainSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (usePlaceholderVisuals && animator == null)
        {
            EnsurePlaceholderBody();
        }

        EnsureHealthText();
        
        aiAgent = GetComponent<IAstarAI>();
        rb2d = GetComponent<Rigidbody2D>();

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
        
        // ZORUNLU KILIT (Awake'te bir kere yapilir): 
        // AILerp veya AIPath kullaniyorsa rotasyonu bozmasini tamamen engelliyoruz!
        if (aiAgent is Pathfinding.AIBase pathAgent)
        {
            pathAgent.updateRotation = false; 
            pathAgent.enableRotation = false;
        }

        if (rb2d != null)
        {
            rb2d.freezeRotation = true; // Fiziksel donmeyi engelle
        }
        
        lastTickPosition = transform.position;

        maxCan = Mathf.Max(1f, maxCan);
        mevcutCan = maxCan;

        if (usePlaceholderVisuals && animator == null)
        {
            ApplyVisuals();
        }
        UpdateHealthText();
    }

    protected virtual void Reset()
    {
        EnsureEnemyTag();
        EnsurePlaceholderBody();
        ApplyVisuals();
    }

    protected virtual void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsurePlaceholderBody();
        ApplyVisuals();
    }

    protected virtual void Update()
    {
        if (!IsAlive || isDying)
        {
            return;
        }

        if (playerTarget == null || playerMechanics == null)
        {
            CachePlayerReferences();
        }

        if (useChaseMovement)
        {
            MoveTowardsPlayer();
        }

        UpdateAnimator();
    }

    protected virtual void UpdateAnimator()
    {
        if (animator == null) return;

        if (isDying)
        {
            animator.SetBool("isWalking", false);
            return;
        }

        // 1. Hareket Halinde mi?
        Vector2 velocity = Vector2.zero;
        if (aiAgent != null)
        {
            velocity = aiAgent.velocity;
        }
        else if (rb2d != null)
        {
            velocity = rb2d.linearVelocity;
        }

        bool isMoving = velocity.sqrMagnitude > 0.01f || (isUnstucking);
        animator.SetBool("isWalking", isMoving);

        // 2. Yön Dönüşü (FlipX)
        // Eğer bir velocity varsa velocity yönüne, yoksa playera doğru baksın
        Vector2 lookDirection = GetDirectionToPlayer();
        if (velocity.sqrMagnitude > 0.1f)
        {
            lookDirection = velocity.normalized;
        }

        if (lookDirection.x > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (lookDirection.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    protected virtual void Flip()
    {
        isFacingRight = !isFacingRight;
        
        if (mainSpriteRenderer != null)
        {
            mainSpriteRenderer.flipX = !isFacingRight; // Sprite sola bakıyorsa flipX = true
        }
        else
        {
            // Bazı prefablar transform Scale ile dönebilir (Eğer SpriteRenderer merkezde değilse)
            Vector3 scaler = transform.localScale;
            scaler.x *= -1;
            transform.localScale = scaler;
        }
    }

    protected virtual void LateUpdate()
    {
        if (isDying)
        {
            return;
        }

        UpdateHealthTextTransform();
    }

    public virtual float TakeDamage(float amount)
    {
        DamageInfo info = new DamageInfo(amount, transform.position, Vector2.zero, null, false, 0f);
        return TakeDamage(info);
    }

    public virtual float TakeDamage(DamageInfo damageInfo)
    {
        if (!IsAlive || damageInfo.amount <= 0f)
        {
            return 0f;
        }

        float appliedDamage = Mathf.Min(mevcutCan, damageInfo.amount);
        mevcutCan -= appliedDamage;
        FloatingCombatText.Create(Mathf.CeilToInt(appliedDamage).ToString(), transform.position + Vector3.up * 0.75f, Color.red);
        UpdateHealthText();

        bool isLethalHit = mevcutCan <= 0f;

        DamageInfo resolved = damageInfo;
        if (resolved.hitDirection.sqrMagnitude <= 0.001f && resolved.source != null)
        {
            resolved.hitDirection = ((Vector2)transform.position - (Vector2)resolved.source.transform.position).normalized;
        }

        if (combatFeedback != null)
        {
            combatFeedback.OnEnemyDamaged(this, resolved, isLethalHit, disableKnockback);
        }
        else if (damageFlashFeedback != null)
        {
            damageFlashFeedback.Flash(new Color(1f, 0.35f, 0.35f, 1f), 0.08f);
        }

        if (!IsAlive)
        {
            Die();
        }

        return appliedDamage;
    }

    public virtual void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        mevcutCan = Mathf.Min(maxCan, mevcutCan + amount);
        FloatingCombatText.Create(Mathf.CeilToInt(amount).ToString(), transform.position + Vector3.up * 0.75f, Color.green);
        UpdateHealthText();
    }

    public void ApplyTemporaryMoveSpeedMultiplier(float multiplier, float duration)
    {
        if (!IsAlive || multiplier <= 0f || duration <= 0f)
        {
            return;
        }

        StartCoroutine(TemporaryMoveSpeedRoutine(multiplier, duration));
    }

    public void ApplyTimeShiftMoveSpeedMultiplier(float multiplier, float duration)
    {
        if (!IsAlive || multiplier <= 0f || duration <= 0f)
        {
            return;
        }

        if (timeShiftMoveSpeedRoutine != null)
        {
            StopCoroutine(timeShiftMoveSpeedRoutine);
        }

        timeShiftMoveSpeedRoutine = StartCoroutine(TimeShiftMoveSpeedRoutine(multiplier, duration));
    }

    public void ApplyTemporaryTouchDamageMultiplier(float multiplier, float duration)
    {
        if (!IsAlive || multiplier <= 0f || duration <= 0f)
        {
            return;
        }

        StartCoroutine(TemporaryTouchDamageRoutine(multiplier, duration));
    }

    protected virtual void Die()
    {
        if (isDying)
        {
            return;
        }

        isDying = true;

        if (playerMechanics != null && xpReward > 0f)
        {
            playerMechanics.GainXp(xpReward);
        }

        if (canTextMesh != null)
        {
            Destroy(canTextMesh.gameObject);
        }

        // Animasyon varsa oynatıp bekleyeceğiz, yoksa anında yok et
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetBool("isWalking", false); // Kesinlikle yürümeyi durdur ki animasyon bug'a girmesin
            animator.SetTrigger("Die");
            
            // Fizikleri kapat ki ölürken itilmesin veya hasar vurmasin
            if (rb2d != null)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
                rb2d.simulated = false;
            }
            Collider2D coll = GetComponent<Collider2D>();
            if (coll != null) coll.enabled = false;
            
            StopAgentMovementCompletely();

            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
            }

            deathRoutine = StartCoroutine(DestroyAfterDeathAnimation());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (isDying)
        {
            return;
        }

        if (playerTarget == null)
        {
            CachePlayerReferences();
            return;
        }

        if (isUnstucking)
        {
            unstuckTimer -= Time.deltaTime;
            if (unstuckTimer <= 0f)
            {
                isUnstucking = false;
                if (aiAgent != null) aiAgent.isStopped = false;
            }
            return; // Kurtarma devam ediyorken normal hareketi devredışı bırak
        }

        if (aiAgent != null)
        {
            aiAgent.destination = playerTarget.position;
            aiAgent.maxSpeed = moveSpeed;
            aiAgent.isStopped = (GetDistanceToPlayer() <= stopDistance);

            if (checkStuck && !aiAgent.isStopped)
            {
                stuckCheckTimer += Time.deltaTime;
                if (stuckCheckTimer >= stuckCheckInterval)
                {
                    float distMoved = Vector3.Distance(transform.position, lastTickPosition);
                    if (distMoved < minMoveDistance)
                    {
                        // Takılma tespit edildi, geri tepme işlemini başlat
                        isUnstucking = true;
                        unstuckTimer = unstuckDuration;
                        aiAgent.isStopped = true;

                        Vector2 pushDir = (transform.position - playerTarget.position).normalized;
                        if (rb2d != null)
                        {
                            rb2d.linearVelocity = Vector2.zero;
                            rb2d.AddForce(pushDir * unstuckPushForce, ForceMode2D.Impulse);
                        }
                        else
                        {
                            transform.position += (Vector3)(pushDir * (unstuckPushForce * 0.1f));
                        }
                    }
                    
                    lastTickPosition = transform.position;
                    stuckCheckTimer = 0f;
                }
            }

            return;
        }

        Vector2 direction = GetDirectionToPlayer();
        float distance = Vector2.Distance(transform.position, playerTarget.position);
        if (distance <= stopDistance || direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    protected Vector2 GetDirectionToPlayer()
    {
        if (playerTarget == null)
        {
            return Vector2.zero;
        }

        return ((Vector2)(playerTarget.position - transform.position)).normalized;
    }

    protected float GetDistanceToPlayer()
    {
        if (playerTarget == null)
        {
            return float.MaxValue;
        }

        return Vector2.Distance(transform.position, playerTarget.position);
    }

    protected void TryDealTouchDamage(GameObject other)
    {
        if (isDying)
        {
            return;
        }

        if (Time.time < nextTouchDamageTime)
        {
            return;
        }

        IDamageable damageable = FindDamageable(other);
        if (damageable == null || ReferenceEquals(damageable, this))
        {
            return;
        }

        if (!(damageable is PlayerMechanics))
        {
            return;
        }

        nextTouchDamageTime = Time.time + touchDamageCooldown;
        Vector2 direction = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        DamageInfo info = new DamageInfo(
            touchDamage,
            transform.position,
            direction,
            gameObject,
            false,
            0f);

        if (damageable is PlayerMechanics player)
        {
            player.TakeDamage(info);
        }
        else
        {
            damageable.TakeDamage(touchDamage);
        }
        
        // Hasar verdiyse "Attack" animasyonunu tetikle
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    protected EnemyProjectile SpawnEnemyProjectile(
        string projectileName,
        Vector3 startPosition,
        Vector2 direction,
        float speed,
        float projectileDamage,
        float lifeTime,
        Color color,
        float size,
        Sprite visualSprite = null)
    {
        EnemyProjectile projectileInstance;

        if (enemyProjectilePrefab != null)
        {
            projectileInstance = Instantiate(enemyProjectilePrefab, startPosition, Quaternion.identity);
            projectileInstance.gameObject.name = projectileName;
        }
        else
        {
            GameObject projectileObject = new GameObject(projectileName);
            projectileObject.transform.position = startPosition;
            projectileObject.transform.rotation = Quaternion.identity;
            projectileInstance = projectileObject.AddComponent<EnemyProjectile>();
        }

        projectileInstance.Initialize(this, direction, speed, projectileDamage, lifeTime, color, size, visualSprite);
        return projectileInstance;
    }

    protected void ApplyDefaultSetup(
        string displayName,
        float health,
        float speed,
        float contactDamage,
        float rewardXp,
        bool chasePlayer,
        float desiredStopDistance,
        Color color,
        Vector2 scale)
    {
        mobDisplayName = displayName;
        maxCan = health;
        moveSpeed = speed;
        touchDamage = contactDamage;
        xpReward = rewardXp;
        useChaseMovement = chasePlayer;
        stopDistance = desiredStopDistance;
        placeholderColor = color;
        placeholderScale = scale;
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        TryDealTouchDamage(other.gameObject);
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        TryDealTouchDamage(collision.gameObject);
    }

    protected virtual void CachePlayerReferences()
    {
        playerMechanics = FindAnyObjectByType<PlayerMechanics>();
        playerTarget = playerMechanics != null ? playerMechanics.transform : null;
    }

    protected virtual void StopAgentMovementCompletely()
    {
        if (aiAgent != null)
        {
            aiAgent.isStopped = true;
        }

        if (aiAgent is AIBase pathAgent)
        {
            pathAgent.canMove = false;
            pathAgent.canSearch = false;
        }
    }

    protected virtual void RestoreAgentMovement()
    {
        if (isDying)
        {
            return;
        }

        if (aiAgent != null)
        {
            aiAgent.isStopped = false;
        }

        if (aiAgent is AIBase pathAgent)
        {
            pathAgent.canMove = true;
            pathAgent.canSearch = true;
        }
    }

    private IEnumerator DestroyAfterDeathAnimation()
    {
        float failSafeTime = Time.time + Mathf.Max(0.1f, fallbackDeathDespawnDelay);
        bool enteredDeathState = false;

        while (Time.time < failSafeTime)
        {
            if (animator == null)
            {
                break;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isDeathStateNow = IsDeathState(stateInfo);

            if (isDeathStateNow)
            {
                enteredDeathState = true;

                if (stateInfo.normalizedTime >= deathNormalizedDestroyPoint)
                {
                    break;
                }
            }
            else if (enteredDeathState)
            {
                break;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private static bool IsDeathState(AnimatorStateInfo stateInfo)
    {
        return
            stateInfo.IsName("death") ||
            stateInfo.IsName("Death") ||
            stateInfo.IsName("Base Layer.death") ||
            stateInfo.IsName("Base Layer.Death") ||
            stateInfo.shortNameHash == Animator.StringToHash("death") ||
            stateInfo.shortNameHash == Animator.StringToHash("Death");
    }

    private IEnumerator TemporaryMoveSpeedRoutine(float multiplier, float duration)
    {
        float previous = moveSpeed;
        moveSpeed *= multiplier;
        yield return new WaitForSeconds(duration);

        if (!isDying)
        {
            moveSpeed = previous;
        }
    }

    private IEnumerator TimeShiftMoveSpeedRoutine(float multiplier, float duration)
    {
        if (!timeShiftMoveSpeedActive)
        {
            timeShiftOriginalMoveSpeed = moveSpeed;
            timeShiftMoveSpeedActive = true;
        }

        moveSpeed = timeShiftOriginalMoveSpeed * Mathf.Clamp01(multiplier);
        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));

        if (!isDying)
        {
            moveSpeed = timeShiftOriginalMoveSpeed;
        }

        timeShiftMoveSpeedActive = false;
        timeShiftMoveSpeedRoutine = null;
    }

    private IEnumerator TemporaryTouchDamageRoutine(float multiplier, float duration)
    {
        float previous = touchDamage;
        touchDamage *= multiplier;
        yield return new WaitForSeconds(duration);

        if (!isDying)
        {
            touchDamage = previous;
        }
    }

    private void EnsureEnemyTag()
    {
        if (CompareTag("Enemy"))
        {
            return;
        }

        gameObject.tag = "Enemy";
    }

    private void RenameGameObject()
    {
        if (!string.IsNullOrWhiteSpace(mobDisplayName))
        {
            gameObject.name = mobDisplayName;
        }
    }

    private void EnsurePlaceholderBody()
    {
        if (bodyVisual == null)
        {
            Transform existingVisual = transform.Find("EnemyBodyVisual");
            if (existingVisual == null)
            {
                GameObject bodyObject = new GameObject("EnemyBodyVisual");
                bodyObject.transform.SetParent(transform, false);
                bodyVisual = bodyObject.transform;
            }
            else
            {
                bodyVisual = existingVisual;
            }
        }

        bodyRenderer = bodyVisual.GetComponent<SpriteRenderer>();
        if (bodyRenderer == null)
        {
            bodyRenderer = bodyVisual.gameObject.AddComponent<SpriteRenderer>();
        }

        bodyRenderer.sprite = GetSquareSprite();
        bodyRenderer.material = GetSpriteMaterial();
        bodyRenderer.enabled = true;
    }

    private void ApplyVisuals()
    {
        if (bodyRenderer == null)
        {
            return;
        }

        bodyRenderer.color = placeholderColor;
        bodyRenderer.sortingOrder = sortingOrder;
        bodyVisual.localPosition = Vector3.zero;
        bodyVisual.localRotation = Quaternion.identity;
        bodyVisual.localScale = new Vector3(placeholderScale.x, placeholderScale.y, 1f);
    }

    private void EnsureHealthText()
    {
        if (canTextMesh != null)
        {
            return;
        }

        GameObject textObject = new GameObject("EnemyHealthText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = yaziOffset;

        canTextMesh = textObject.AddComponent<TextMesh>();
        canTextMesh.anchor = TextAnchor.MiddleCenter;
        canTextMesh.alignment = TextAlignment.Center;
        canTextMesh.fontSize = yaziFontBoyutu;
        canTextMesh.characterSize = yaziKarakterBoyutu;
        canTextMesh.color = yaziRengi;

        MeshRenderer textRenderer = canTextMesh.GetComponent<MeshRenderer>();
        textRenderer.sortingOrder = sortingOrder + 5;
    }

    private void UpdateHealthText()
    {
        if (canTextMesh == null)
        {
            return;
        }

        canTextMesh.text = Mathf.CeilToInt(mevcutCan).ToString();
    }

    private void UpdateHealthTextTransform()
    {
        if (canTextMesh == null)
        {
            return;
        }

        canTextMesh.transform.position = transform.position + yaziOffset;

        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            canTextMesh.transform.rotation = activeCamera.transform.rotation;
        }
    }

    public static IDamageable FindDamageable(GameObject target)
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

    private static Sprite GetSquareSprite()
    {
        if (cachedSquareSprite == null)
        {
            cachedSquareSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        return cachedSquareSprite;
    }

    private static Material GetSpriteMaterial()
    {
        if (cachedSpriteMaterial != null)
        {
            return cachedSpriteMaterial;
        }

        Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (spriteShader == null)
        {
            spriteShader = Shader.Find("Sprites/Default");
        }

        if (spriteShader != null)
        {
            cachedSpriteMaterial = new Material(spriteShader);
        }

        return cachedSpriteMaterial;
    }
}
