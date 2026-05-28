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

        transform.right = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
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

        spriteRenderer.sprite = GetOrCreateSprite();
        spriteRenderer.color = lazerRengi;
        spriteRenderer.material = GetOrCreateSpriteMaterial();
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
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
        trailRenderer.startColor = lazerRengi;
        trailRenderer.endColor = new Color(lazerRengi.r, lazerRengi.g, lazerRengi.b, 0f);

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
        transform.localScale = new Vector3(
            Mathf.Max(0.01f, lazerUzunlugu) * scale,
            Mathf.Max(0.01f, lazerKalInligi) * scale,
            1f);

        if (circleCollider != null)
        {
            if (syncHitboxToSprite)
            {
                float longestSide = Mathf.Max(lazerUzunlugu, lazerKalInligi);
                float desiredWorldRadius = Mathf.Max(
                    0.01f,
                    longestSide * scale * 0.5f * hitboxRadiusMultiplier + hitboxRadiusPadding);
                circleCollider.radius = desiredWorldRadius / Mathf.Max(0.01f, longestSide * scale);
            }
            else
            {
                circleCollider.radius = Mathf.Max(0.01f, circleCollider.radius);
            }
        }

        if (trailRenderer != null)
        {
            trailRenderer.time = Mathf.Max(0.01f, izSuresi);
            trailRenderer.startWidth = Mathf.Max(0.01f, lazerKalInligi * scale * 0.8f);
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
