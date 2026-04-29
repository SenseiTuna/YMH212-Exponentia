using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private Color projectileColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] private float projectileSize = 0.22f;

    private Vector2 velocity;
    private float damage;
    private EnemyMechanics owner;
    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;

    private static Sprite cachedSprite;
    private static Material cachedSpriteMaterial;

    public void Initialize(EnemyMechanics projectileOwner, Vector2 direction, float speed, float projectileDamage, float lifeTime, Color color, float size)
    {
        owner = projectileOwner;
        velocity = direction.normalized * Mathf.Max(0f, speed);
        damage = Mathf.Max(0f, projectileDamage);
        projectileColor = color;
        projectileSize = Mathf.Max(0.05f, size);

        transform.right = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        ApplyVisualState();
        Destroy(gameObject, Mathf.Max(0.1f, lifeTime));
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
        spriteRenderer.sprite = GetSquareSprite();
        spriteRenderer.material = GetSpriteMaterial();
        spriteRenderer.sortingOrder = 8;

        ApplyVisualState();
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null)
        {
            return;
        }

        IDamageable damageable = EnemyMechanics.FindDamageable(other.gameObject);
        if (!(damageable is PlayerMechanics))
        {
            return;
        }

        damageable.TakeDamage(damage);
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
        if (spriteRenderer != null)
        {
            spriteRenderer.color = projectileColor;
        }

        if (circleCollider != null)
        {
            circleCollider.radius = projectileSize * 0.5f;
        }

        transform.localScale = Vector3.one * projectileSize;
    }
}
