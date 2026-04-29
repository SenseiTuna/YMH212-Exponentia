using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("Gorsel")]
    [SerializeField] private Color lazerRengi = Color.red;
    [SerializeField] private float lazerUzunlugu = 1.2f;
    [SerializeField] private float lazerKalInligi = 0.22f;
    [SerializeField] private float izSuresi = 0.12f;

    private PlayerMechanics owner;
    private Vector2 velocity;
    private float lifeTime;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;

    private static Sprite cachedSprite;
    private static Material cachedSpriteMaterial;

    public void Initialize(PlayerMechanics projectileOwner, Vector2 direction, float speed, float projectileLifeTime)
    {
        owner = projectileOwner;
        velocity = direction.normalized * Mathf.Max(0f, speed);
        lifeTime = Mathf.Max(0.05f, projectileLifeTime);

        transform.right = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.12f;

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
        transform.localScale = new Vector3(lazerUzunlugu, lazerKalInligi, 1f);

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

        owner.DealDamage(other.gameObject);
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
}
