using UnityEngine;

public class EnemyBurstMine : MonoBehaviour
{
    [SerializeField] private float armDuration = 0.85f;
    [SerializeField] private float burstDamage = 10f;
    [SerializeField] private float burstProjectileSpeed = 8f;
    [SerializeField] private float burstProjectileLifeTime = 2.5f;
    [SerializeField] private float burstProjectileSize = 0.2f;
    [SerializeField] private float burstAngle = 90f;
    [SerializeField] private Color burstColor = new Color(1f, 0.9f, 0.25f);
    [SerializeField] private float pulseScale = 0.18f;
    [SerializeField] private float pulseSpeed = 7f;

    private EnemyMechanics owner;
    private EnemyProjectile projectilePrefab;
    private Vector2 forwardDirection;
    private float elapsedTime;
    private SpriteRenderer spriteRenderer;

    private static Sprite cachedSprite;
    private static Material cachedMaterial;

    public void Initialize(
        EnemyMechanics mineOwner,
        EnemyProjectile prefab,
        Vector2 direction,
        float armingTime,
        float damage,
        float projectileSpeed,
        float projectileLifeTime,
        float projectileSize,
        Color color)
    {
        owner = mineOwner;
        projectilePrefab = prefab;
        forwardDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        armDuration = Mathf.Max(0.05f, armingTime);
        burstDamage = Mathf.Max(0f, damage);
        burstProjectileSpeed = Mathf.Max(0f, projectileSpeed);
        burstProjectileLifeTime = Mathf.Max(0.1f, projectileLifeTime);
        burstProjectileSize = Mathf.Max(0.05f, projectileSize);
        burstColor = color;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = burstColor;
        }
    }

    private void Awake()
    {
        SpriteRenderer existingRenderer = GetComponent<SpriteRenderer>();
        if (existingRenderer == null)
        {
            existingRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer = existingRenderer;
        spriteRenderer.sprite = GetSprite();
        if (spriteRenderer.sharedMaterial == null)
        {
            spriteRenderer.material = GetMaterial();
        }

        transform.localScale = Vector3.one * 0.3f;
    }

    public void ApplyVisualSprite(Sprite visualSprite)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (visualSprite != null)
        {
            spriteRenderer.sprite = visualSprite;
        }
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        float pulse = 1f + Mathf.Sin(elapsedTime * pulseSpeed) * pulseScale;
        transform.localScale = Vector3.one * 0.3f * pulse;

        if (elapsedTime >= armDuration)
        {
            Burst();
        }
    }

    private void Burst()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 leftDirection = Quaternion.Euler(0f, 0f, burstAngle) * forwardDirection;
        Vector2 rightDirection = Quaternion.Euler(0f, 0f, -burstAngle) * forwardDirection;

        SpawnBurstProjectile("BurstMineLeftShot", leftDirection);
        SpawnBurstProjectile("BurstMineRightShot", rightDirection);

        Destroy(gameObject);
    }

    private void SpawnBurstProjectile(string projectileName, Vector2 direction)
    {
        EnemyProjectile projectile;
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            projectile.gameObject.name = projectileName;
        }
        else
        {
            GameObject projectileObject = new GameObject(projectileName);
            projectileObject.transform.position = transform.position;
            projectile = projectileObject.AddComponent<EnemyProjectile>();
        }

        projectile.Initialize(owner, direction, burstProjectileSpeed, burstDamage, burstProjectileLifeTime, burstColor, burstProjectileSize);
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
