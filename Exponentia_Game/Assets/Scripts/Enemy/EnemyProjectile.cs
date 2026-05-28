using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private Color projectileColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] private float projectileSize = 0.22f;

    [Header("Sprite / Hitbox Scale")]
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private bool syncHitboxToSprite = true;
    [SerializeField] private float hitboxRadiusMultiplier = 1f;
    [SerializeField] private float hitboxRadiusPadding = 0f;
    [SerializeField] private bool rotateVisualToDirection = true;

    private Vector2 velocity;
    private float damage;
    private EnemyMechanics owner;
    private PlayerMechanics reflectedByPlayer;
    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;
    private bool useCurvedPath;
    private bool useExplosion;
    private bool hasExploded;
    private bool reflectedToEnemies;
    private float timeShiftSpeedMultiplier = 1f;
    private Coroutine timeShiftSpeedRoutine;
    private float curveElapsedTime;
    private float curveDuration;
    private float explosionRadius;
    private Vector2 curveStartPoint;
    private Vector2 curveControlPoint;
    private Vector2 curveEndPoint;

    private static Sprite cachedSprite;
    private static Material cachedSpriteMaterial;

    public void Initialize(EnemyMechanics projectileOwner, Vector2 direction, float speed, float projectileDamage, float lifeTime, Color color, float size, Sprite visualSprite = null)
    {
        owner = projectileOwner;
        velocity = direction.normalized * Mathf.Max(0f, speed);
        damage = Mathf.Max(0f, projectileDamage);
        projectileColor = color;
        projectileSize = Mathf.Max(0.05f, size);

        ApplyVisualRotation(direction);
        if (visualSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = visualSprite;
        }
        ApplyVisualState();
        Destroy(gameObject, Mathf.Max(0.1f, lifeTime));
    }

    public void SetRotateVisualToDirection(bool value)
    {
        rotateVisualToDirection = value;
        ApplyVisualRotation(velocity.sqrMagnitude > 0.001f ? velocity.normalized : Vector2.right);
    }

    public void ConfigureCurvedPath(Vector2 targetPosition, float travelDuration, float curveOffset, float aoeRadius)
    {
        curveStartPoint = transform.position;
        curveEndPoint = targetPosition;
        curveDuration = Mathf.Max(0.05f, travelDuration);
        curveElapsedTime = 0f;
        useCurvedPath = true;
        useExplosion = aoeRadius > 0f;
        explosionRadius = Mathf.Max(0f, aoeRadius);

        Vector2 straightDirection = (curveEndPoint - curveStartPoint).normalized;
        if (straightDirection.sqrMagnitude <= 0.001f)
        {
            straightDirection = Vector2.right;
        }

        Vector2 perpendicular = new Vector2(-straightDirection.y, straightDirection.x);
        curveControlPoint = (curveStartPoint + curveEndPoint) * 0.5f + perpendicular * curveOffset;
        ApplyVisualRotation(straightDirection);
    }

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.5f;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GetSquareSprite();
        }

        if (spriteRenderer.sharedMaterial == null)
        {
            spriteRenderer.material = GetSpriteMaterial();
        }

        spriteRenderer.sortingOrder = 8;

        ApplyVisualState();
    }

    private void OnValidate()
    {
        ClampScaleSettings();
        CacheProjectileComponents();
        ApplyVisualState();
    }

    private void LateUpdate()
    {
        ApplyVisualState();
    }

    private void Update()
    {
        if (useCurvedPath)
        {
            UpdateCurvedPath();
            return;
        }

        transform.position += (Vector3)(velocity * timeShiftSpeedMultiplier * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (reflectedToEnemies)
        {
            EnemyMechanics enemy = other.GetComponentInParent<EnemyMechanics>();
            if (enemy == null)
            {
                return;
            }

            DamageInfo info = new DamageInfo(
                damage,
                transform.position,
                ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized,
                reflectedByPlayer != null ? reflectedByPlayer.gameObject : gameObject);
            enemy.TakeDamage(info);
            Destroy(gameObject);
            return;
        }

        if (owner == null)
        {
            return;
        }

        IDamageable damageable = EnemyMechanics.FindDamageable(other.gameObject);
        if (!(damageable is PlayerMechanics))
        {
            return;
        }

        if (useExplosion)
        {
            Explode();
            return;
        }

        if (damageable is PlayerMechanics player)
        {
            AthenaSkill athenaSkill = player.GetComponent<AthenaSkill>();
            if (athenaSkill != null && athenaSkill.TryReflectProjectile(player, this))
            {
                return;
            }

            Vector2 direction = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            DamageInfo info = new DamageInfo(damage, transform.position, direction, owner != null ? owner.gameObject : gameObject);
            player.TakeDamage(info);
        }
        else
        {
            damageable.TakeDamage(damage);
        }
        Destroy(gameObject);
    }

    public void Reflect(PlayerMechanics reflector, float speedMultiplier, float damageMultiplier)
    {
        reflectedByPlayer = reflector;
        reflectedToEnemies = true;
        owner = null;
        useCurvedPath = false;
        useExplosion = false;
        hasExploded = false;
        damage = Mathf.Max(0f, damage * Mathf.Max(0f, damageMultiplier));

        Vector2 direction = velocity.sqrMagnitude > 0.001f ? -velocity.normalized : -(Vector2)transform.right;
        float reflectedSpeed = velocity.magnitude * Mathf.Max(0.01f, speedMultiplier);
        velocity = direction.normalized * reflectedSpeed;
        ApplyVisualRotation(velocity.sqrMagnitude > 0.001f ? velocity.normalized : Vector2.right);
        projectileColor = Color.cyan;
        ApplyVisualState();
    }

    public void ApplyTimeShiftSpeedMultiplier(float multiplier, float duration)
    {
        if (multiplier <= 0f || duration <= 0f)
        {
            return;
        }

        if (timeShiftSpeedRoutine != null)
        {
            StopCoroutine(timeShiftSpeedRoutine);
        }

        timeShiftSpeedRoutine = StartCoroutine(TimeShiftSpeedRoutine(multiplier, duration));
    }

    private void UpdateCurvedPath()
    {
        curveElapsedTime += Time.deltaTime * timeShiftSpeedMultiplier;
        float t = Mathf.Clamp01(curveElapsedTime / curveDuration);

        Vector2 firstLerp = Vector2.Lerp(curveStartPoint, curveControlPoint, t);
        Vector2 secondLerp = Vector2.Lerp(curveControlPoint, curveEndPoint, t);
        Vector2 bezierPoint = Vector2.Lerp(firstLerp, secondLerp, t);

        Vector2 tangent = secondLerp - firstLerp;
        if (tangent.sqrMagnitude > 0.001f)
        {
            ApplyVisualRotation(tangent.normalized);
        }

        transform.position = bezierPoint;

        if (t >= 1f)
        {
            if (useExplosion)
            {
                Explode();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void Explode()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = EnemyMechanics.FindDamageable(hits[i].gameObject);
            if (damageable is PlayerMechanics)
            {
                PlayerMechanics player = damageable as PlayerMechanics;
                Vector2 direction = player != null
                    ? ((Vector2)player.transform.position - (Vector2)transform.position).normalized
                    : Vector2.zero;
                DamageInfo info = new DamageInfo(damage, transform.position, direction, owner != null ? owner.gameObject : gameObject);
                if (player != null)
                {
                    player.TakeDamage(info);
                }
                else
                {
                    damageable.TakeDamage(damage);
                }
                break;
            }
        }

        Destroy(gameObject);
    }

    private static Sprite GetSquareSprite()
    {
        if (cachedSprite == null)
        {
            cachedSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        return cachedSprite;
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

    private void ApplyVisualState()
    {
        ClampScaleSettings();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = projectileColor;
        }

        float visualScale = GetVisualScale();

        if (circleCollider != null)
        {
            if (syncHitboxToSprite)
            {
                float desiredWorldRadius = Mathf.Max(
                    0.01f,
                    visualScale * 0.5f * hitboxRadiusMultiplier + hitboxRadiusPadding);
                circleCollider.radius = desiredWorldRadius / visualScale;
            }
            else
            {
                circleCollider.radius = Mathf.Max(0.01f, circleCollider.radius);
            }
        }

        transform.localScale = Vector3.one * visualScale;
    }

    private void CacheProjectileComponents()
    {
        if (circleCollider == null)
        {
            circleCollider = GetComponent<CircleCollider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void ClampScaleSettings()
    {
        projectileSize = Mathf.Max(0.01f, projectileSize);
        projectileScale = Mathf.Max(0.01f, projectileScale);
        hitboxRadiusMultiplier = Mathf.Max(0.01f, hitboxRadiusMultiplier);
    }

    private float GetVisualScale()
    {
        return Mathf.Max(0.01f, projectileSize * projectileScale);
    }

    private void ApplyVisualRotation(Vector2 direction)
    {
        if (!rotateVisualToDirection)
        {
            transform.rotation = Quaternion.identity;
            return;
        }

        transform.right = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
    }

    private System.Collections.IEnumerator TimeShiftSpeedRoutine(float multiplier, float duration)
    {
        timeShiftSpeedMultiplier = Mathf.Clamp01(multiplier);
        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
        timeShiftSpeedMultiplier = 1f;
        timeShiftSpeedRoutine = null;
    }
}
