using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class EnemyVortexZone : MonoBehaviour
{
    [SerializeField] private float duration = 2.5f;
    [SerializeField] private float pullStrength = 0.3f;
    [SerializeField] private float tickDamage = 3f;
    [SerializeField] private float tickInterval = 0.4f;
    [SerializeField] private float radius = 1.6f;
    [SerializeField] private Color vortexColor = new Color(0.6f, 0.25f, 0.85f, 0.75f);

    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;
    private float nextTickTime;
    private float elapsedTime;

    private static Sprite cachedSprite;
    private static Material cachedMaterial;

    public void Initialize(float lifeTime, float vortexRadius, float pull, float damagePerTick, float damageInterval, Color color)
    {
        duration = Mathf.Max(0.1f, lifeTime);
        radius = Mathf.Max(0.2f, vortexRadius);
        pullStrength = pull;
        tickDamage = Mathf.Max(0f, damagePerTick);
        tickInterval = Mathf.Max(0.05f, damageInterval);
        vortexColor = color;
        ApplyVisualState();
    }

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = GetSprite();
        spriteRenderer.color = vortexColor;
        spriteRenderer.sortingOrder = 26;

        if (spriteRenderer.sharedMaterial == null)
        {
            spriteRenderer.material = GetMaterial();
        }

        ApplyVisualState();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= duration)
        {
            Destroy(gameObject);
            return;
        }

        float pulse = 1f + Mathf.Sin(elapsedTime * 7f) * 0.08f;
        transform.localScale = Vector3.one * radius * 2f * pulse;

        float alpha = Mathf.Lerp(vortexColor.a, 0f, elapsedTime / duration);
        spriteRenderer.color = new Color(vortexColor.r, vortexColor.g, vortexColor.b, alpha);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        IDamageable damageable = EnemyMechanics.FindDamageable(other.gameObject);
        if (!(damageable is PlayerMechanics player))
        {
            return;
        }

        Vector2 pullDirection = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.ApplyExternalDisplacement(pullDirection * pullStrength * Time.deltaTime);
            playerMovement.ApplyExternalVelocity(pullDirection * pullStrength);
        }
        else
        {
            player.transform.position += (Vector3)(pullDirection * pullStrength * Time.deltaTime);
        }

        if (Time.time >= nextTickTime)
        {
            nextTickTime = Time.time + tickInterval;
            player.TakeDamage(tickDamage);
        }
    }

    private void ApplyVisualState()
    {
        if (circleCollider != null)
        {
            circleCollider.radius = radius;
        }

        transform.localScale = Vector3.one * radius * 2f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = vortexColor;
        }
    }

    private static Sprite GetSprite()
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

    private static Material GetMaterial()
    {
        if (cachedMaterial != null)
        {
            return cachedMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader != null)
        {
            cachedMaterial = new Material(shader);
        }

        return cachedMaterial;
    }
}
