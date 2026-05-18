using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class EnemyHazardArea : MonoBehaviour
{
    [SerializeField] private float tickDamage = 4f;
    [SerializeField] private float tickCooldown = 0.5f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private float radius = 0.9f;
    [SerializeField] private Color areaColor = new Color(1f, 0.45f, 0.15f, 0.9f);
    [SerializeField] private Color outlineColor = new Color(1f, 0.95f, 0.55f, 1f);
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private float pulseAmount = 0.12f;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float outlineWidth = 0.08f;
    [SerializeField] private int outlineSegmentCount = 32;

    private float elapsedTime;
    private float nextTickTime;
    private Transform visualRoot;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private LineRenderer outlineRenderer;

    private static Sprite cachedSprite;
    private static Material cachedSpriteMaterial;

    public void Initialize(float damagePerTick, float cooldown, float lifeTime, float areaRadius, Color color)
    {
        tickDamage = Mathf.Max(0f, damagePerTick);
        tickCooldown = Mathf.Max(0.05f, cooldown);
        duration = Mathf.Max(0.1f, lifeTime);
        radius = Mathf.Max(0.1f, areaRadius);
        areaColor = color;
        ApplyVisualState();
    }

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        EnsureVisualRoot();
        spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
        }

        outlineRenderer = visualRoot.GetComponent<LineRenderer>();
        if (outlineRenderer == null)
        {
            outlineRenderer = visualRoot.gameObject.AddComponent<LineRenderer>();
        }

        spriteRenderer.sprite = GetSquareSprite();
        spriteRenderer.color = areaColor;
        spriteRenderer.sortingOrder = sortingOrder;

        if (spriteRenderer.sharedMaterial == null)
        {
            spriteRenderer.material = GetSpriteMaterial();
        }

        outlineRenderer.useWorldSpace = false;
        outlineRenderer.loop = true;
        outlineRenderer.textureMode = LineTextureMode.Stretch;
        outlineRenderer.alignment = LineAlignment.TransformZ;
        outlineRenderer.positionCount = Mathf.Max(8, outlineSegmentCount);
        outlineRenderer.startWidth = outlineWidth;
        outlineRenderer.endWidth = outlineWidth;
        outlineRenderer.sortingOrder = sortingOrder + 1;

        if (outlineRenderer.sharedMaterial == null)
        {
            outlineRenderer.material = GetSpriteMaterial();
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

        float alpha = Mathf.Lerp(areaColor.a, 0f, elapsedTime / duration);
        spriteRenderer.color = new Color(areaColor.r, areaColor.g, areaColor.b, alpha);
        outlineRenderer.startColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha);
        outlineRenderer.endColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha);
        UpdatePulseScale();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextTickTime)
        {
            return;
        }

        IDamageable damageable = EnemyMechanics.FindDamageable(other.gameObject);
        if (damageable is PlayerMechanics)
        {
            damageable.TakeDamage(tickDamage);
            nextTickTime = Time.time + tickCooldown;
        }
    }

    private void ApplyVisualState()
    {
        if (circleCollider != null)
        {
            circleCollider.radius = radius;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = areaColor;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (outlineRenderer != null)
        {
            outlineRenderer.sortingOrder = sortingOrder + 1;
            outlineRenderer.startWidth = outlineWidth;
            outlineRenderer.endWidth = outlineWidth;
            outlineRenderer.startColor = outlineColor;
            outlineRenderer.endColor = outlineColor;
            RebuildOutline();
        }

        if (visualRoot != null)
        {
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one * radius * 2f;
        }
    }

    private void EnsureVisualRoot()
    {
        if (visualRoot != null)
        {
            return;
        }

        Transform existing = transform.Find("HazardVisual");
        if (existing != null)
        {
            visualRoot = existing;
            return;
        }

        GameObject visualObject = new GameObject("HazardVisual");
        visualObject.transform.SetParent(transform, false);
        visualRoot = visualObject.transform;
    }

    private void UpdatePulseScale()
    {
        if (visualRoot == null)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(elapsedTime * pulseSpeed) * pulseAmount;
        visualRoot.localScale = Vector3.one * radius * 2f * pulse;
    }

    private void RebuildOutline()
    {
        if (outlineRenderer == null)
        {
            return;
        }

        int segmentCount = Mathf.Max(8, outlineSegmentCount);
        outlineRenderer.positionCount = segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segmentCount;
            float x = Mathf.Cos(angle) * 0.5f;
            float y = Mathf.Sin(angle) * 0.5f;
            outlineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
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
}
