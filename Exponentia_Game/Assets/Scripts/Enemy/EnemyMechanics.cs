using UnityEngine;

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

    [Header("Placeholder Gorunus")]
    [SerializeField] protected Color placeholderColor = Color.gray;
    [SerializeField] protected Vector2 placeholderScale = Vector2.one;
    [SerializeField] protected int sortingOrder = 5;

    [Header("Projectile Template")]
    [SerializeField] protected EnemyProjectile enemyProjectilePrefab;

    [Header("Can Yazisi")]
    [SerializeField] protected Vector3 yaziOffset = new Vector3(0f, 1.1f, 0f);
    [SerializeField] protected int yaziFontBoyutu = 32;
    [SerializeField] protected float yaziKarakterBoyutu = 0.22f;
    [SerializeField] protected Color yaziRengi = Color.yellow;

    protected float mevcutCan;
    protected Transform playerTarget;
    protected PlayerMechanics playerMechanics;

    private float nextTouchDamageTime;
    private Transform bodyVisual;
    private SpriteRenderer bodyRenderer;
    private TextMesh canTextMesh;

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
        EnsurePlaceholderBody();
        EnsureHealthText();

        maxCan = Mathf.Max(1f, maxCan);
        mevcutCan = maxCan;

        ApplyVisuals();
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
        EnsurePlaceholderBody();
        ApplyVisuals();
    }

    protected virtual void Update()
    {
        if (useChaseMovement)
        {
            MoveTowardsPlayer();
        }
    }

    protected virtual void LateUpdate()
    {
        UpdateHealthTextTransform();
    }

    public virtual float TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return 0f;
        }

        float appliedDamage = Mathf.Min(mevcutCan, amount);
        mevcutCan -= appliedDamage;
        FloatingCombatText.Create(Mathf.CeilToInt(appliedDamage).ToString(), transform.position + Vector3.up * 0.75f, Color.red);
        UpdateHealthText();

        if (!IsAlive)
        {
            Die();
        }

        return appliedDamage;
    }

    protected virtual void Die()
    {
        if (playerMechanics != null && xpReward > 0f)
        {
            playerMechanics.GainXp(xpReward);
        }

        if (canTextMesh != null)
        {
            Destroy(canTextMesh.gameObject);
        }

        Destroy(gameObject);
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (playerTarget == null)
        {
            CachePlayerReferences();
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
        if (Time.time < nextTouchDamageTime)
        {
            return;
        }

        IDamageable damageable = FindDamageable(other);
        if (damageable == null || damageable == this)
        {
            return;
        }

        if (!(damageable is PlayerMechanics))
        {
            return;
        }

        nextTouchDamageTime = Time.time + touchDamageCooldown;
        damageable.TakeDamage(touchDamage);
    }

    protected EnemyProjectile SpawnEnemyProjectile(
        string projectileName,
        Vector3 startPosition,
        Vector2 direction,
        float speed,
        float projectileDamage,
        float lifeTime,
        Color color,
        float size)
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

        projectileInstance.Initialize(this, direction, speed, projectileDamage, lifeTime, color, size);
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

    protected void CachePlayerReferences()
    {
        playerMechanics = FindAnyObjectByType<PlayerMechanics>();
        playerTarget = playerMechanics != null ? playerMechanics.transform : null;
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
