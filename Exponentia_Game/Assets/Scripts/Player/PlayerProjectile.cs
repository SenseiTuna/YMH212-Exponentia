using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("Gorsel")]
    [SerializeField] private Color lazerRengi = Color.red;
    [SerializeField] private float lazerUzunlugu = 1.2f;
    [SerializeField] private float lazerKalInligi = 0.22f;
    [SerializeField] private float izSuresi = 0.12f;
    [SerializeField] private float spriteRotationOffsetDegrees = 0f;

    [Header("Sprite / Hitbox Scale")]
    [SerializeField] private float projectileScale = 1f;
    [SerializeField] private bool syncHitboxToSprite = true;
    [SerializeField] private float hitboxRadiusMultiplier = 1f;
    [SerializeField] private float hitboxRadiusPadding = 0f;

    private PlayerMechanics owner;
    private Vector2 velocity;
    private float lifeTime;
    private float damageMultiplier = 1f;
    private int remainingPierce;
    private readonly HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    private CircleCollider2D circleCollider;
    private Vector3 initialLocalScale = Vector3.one;
    private Color initialSpriteColor = Color.white;
    private bool useGeneratedLaserVisual;

    private static Sprite cachedSprite;
    private static Material cachedSpriteMaterial;

    public void Initialize(PlayerMechanics projectileOwner, Vector2 direction, float speed, float projectileLifeTime)
    {
        Initialize(projectileOwner, direction, speed, projectileLifeTime, 1f, 0);
    }

    public void Initialize(
        PlayerMechanics projectileOwner,
        Vector2 direction,
        float speed,
        float projectileLifeTime,
        float damageMultiplier,
        int pierceCount)
    {
        owner = projectileOwner;
        velocity = direction.normalized * Mathf.Max(0f, speed);
        lifeTime = Mathf.Max(0.05f, projectileLifeTime);
        this.damageMultiplier = Mathf.Max(0f, damageMultiplier);
        remainingPierce = Mathf.Max(0, pierceCount);

        if (direction.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteRotationOffsetDegrees;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
        initialLocalScale = transform.localScale;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        useGeneratedLaserVisual = spriteRenderer.sprite == null;
        if (useGeneratedLaserVisual)
        {
            spriteRenderer.sprite = GetOrCreateSprite();
            spriteRenderer.color = lazerRengi;
        }

        initialSpriteColor = spriteRenderer.color;
        spriteRenderer.material = GetOrCreateSpriteMaterial();
        if (useGeneratedLaserVisual)
        {
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        }

        spriteRenderer.sortingOrder = 10;

        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }

        trailRenderer.time = izSuresi;
        trailRenderer.startWidth = lazerKalInligi * 0.8f;
        trailRenderer.endWidth = 0.02f;
        trailRenderer.minVertexDistance = 0.02f;
        trailRenderer.shadowCastingMode = ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        trailRenderer.sortingOrder = 9;
        trailRenderer.material = GetOrCreateSpriteMaterial();
        Color trailColor = useGeneratedLaserVisual ? lazerRengi : initialSpriteColor;
        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);

        ApplyVisualState();
    }

    private void OnValidate()
    {
        ClampScaleSettings();
        CacheProjectileComponents();
        if (!Application.isPlaying && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            initialLocalScale = transform.localScale;
            initialSpriteColor = spriteRenderer.color;
            useGeneratedLaserVisual = false;
            return;
        }

        ApplyVisualState();
    }

    private void LateUpdate()
    {
        ApplyVisualState();
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null || !other.CompareTag("Enemy"))
        {
            return;
        }

        if (alreadyHit.Contains(other))
        {
            return;
        }

        AnvilGuardianEnemy anvilGuardian = other.GetComponentInParent<AnvilGuardianEnemy>();
        if (anvilGuardian != null && anvilGuardian.TryReflectPlayerProjectile(this, circleCollider))
        {
            return;
        }

        alreadyHit.Add(other);
        owner.DealDamage(other.gameObject, damageMultiplier);

        if (remainingPierce > 0)
        {
            remainingPierce -= 1;
            return;
        }

        Destroy(gameObject);
    }

    private static Sprite GetOrCreateSprite()
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

    private static Material GetOrCreateSpriteMaterial()
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

        float scale = Mathf.Max(0.01f, projectileScale);
        if (useGeneratedLaserVisual)
        {
            transform.localScale = new Vector3(
                Mathf.Max(0.01f, lazerUzunlugu) * scale,
                Mathf.Max(0.01f, lazerKalInligi) * scale,
                1f);
        }
        else
        {
            transform.localScale = new Vector3(
                initialLocalScale.x * scale,
                initialLocalScale.y * scale,
                initialLocalScale.z);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = initialSpriteColor;
            }
        }

        if (circleCollider != null)
        {
            if (syncHitboxToSprite)
            {
                float longestSide = useGeneratedLaserVisual
                    ? Mathf.Max(lazerUzunlugu * scale, lazerKalInligi * scale)
                    : Mathf.Max(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
                float desiredWorldRadius = Mathf.Max(
                    0.01f,
                    longestSide * 0.5f * hitboxRadiusMultiplier + hitboxRadiusPadding);
                circleCollider.radius = desiredWorldRadius / Mathf.Max(0.01f, longestSide);
            }
            else
            {
                circleCollider.radius = Mathf.Max(0.01f, circleCollider.radius);
            }
        }

        if (trailRenderer != null)
        {
            trailRenderer.time = Mathf.Max(0.01f, izSuresi);
            float width = useGeneratedLaserVisual
                ? lazerKalInligi * scale
                : Mathf.Min(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
            trailRenderer.startWidth = Mathf.Max(0.01f, width * 0.8f);
        }
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

        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }
    }

    private void ClampScaleSettings()
    {
        lazerUzunlugu = Mathf.Max(0.01f, lazerUzunlugu);
        lazerKalInligi = Mathf.Max(0.01f, lazerKalInligi);
        projectileScale = Mathf.Max(0.01f, projectileScale);
        hitboxRadiusMultiplier = Mathf.Max(0.01f, hitboxRadiusMultiplier);
    }
}
