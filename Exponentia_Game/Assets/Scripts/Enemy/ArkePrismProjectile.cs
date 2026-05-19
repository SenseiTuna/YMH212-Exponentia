using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class ArkePrismProjectile : MonoBehaviour
{
    public enum PrismEffect
    {
        Damage,
        HealEnemies,
        BuffMoveSpeed,
        BuffDamage,
        Vortex
    }

    private EnemyMechanics owner;
    private PlayerMechanics reflectedByPlayer;
    private Vector2 velocity;
    private float damage;
    private float lifeTime;
    private float effectRadius;
    private float effectDuration;
    private PrismEffect effect;
    private Color projectileColor;
    private bool resolved;
    private float elapsedTime;
    private bool resolvesOnExpiry;
    private bool reflectedToEnemies;
    private const float CollisionArmDelay = 0.08f;

    private static Sprite cachedSprite;
    private static Material cachedMaterial;

    public void Initialize(
        EnemyMechanics projectileOwner,
        Vector2 direction,
        float speed,
        float projectileDamage,
        float projectileLifeTime,
        float radius,
        float duration,
        PrismEffect prismEffect,
        Color color,
        float size)
    {
        owner = projectileOwner;
        velocity = direction.normalized * Mathf.Max(0f, speed);
        damage = Mathf.Max(0f, projectileDamage);
        lifeTime = Mathf.Max(0.1f, projectileLifeTime);
        effectRadius = Mathf.Max(0.2f, radius);
        effectDuration = Mathf.Max(0.1f, duration);
        effect = prismEffect;
        projectileColor = color;
        resolvesOnExpiry = effect != PrismEffect.Damage;

        transform.right = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        transform.localScale = Vector3.one * Mathf.Max(0.08f, size);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.color = projectileColor;
    }

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        if (sr.sharedMaterial == null)
        {
            sr.material = GetMaterial();
        }
        sr.sortingOrder = 12;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (!resolved && elapsedTime >= lifeTime)
        {
            ResolveEffect(null);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved)
        {
            return;
        }

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
            resolved = true;
            Destroy(gameObject);
            return;
        }

        if (elapsedTime < CollisionArmDelay)
        {
            return;
        }

        if (owner != null && other.GetComponentInParent<EnemyMechanics>() == owner)
        {
            return;
        }

        if (other.GetComponent<ArkePrismProjectile>() != null || other.GetComponent<EnemyProjectile>() != null)
        {
            return;
        }

        if (effect == PrismEffect.Damage)
        {
            IDamageable damageable = EnemyMechanics.FindDamageable(other.gameObject);
            if (!(damageable is PlayerMechanics))
            {
                return;
            }

            PlayerMechanics player = damageable as PlayerMechanics;
            AthenaSkill athenaSkill = player != null ? player.GetComponent<AthenaSkill>() : null;
            if (athenaSkill != null && athenaSkill.TryReflectProjectile(player, this))
            {
                return;
            }
        }
        else if (!resolvesOnExpiry)
        {
            return;
        }

        ResolveEffect(other.gameObject);
    }

    private void ResolveEffect(GameObject target)
    {
        resolved = true;

        switch (effect)
        {
            case PrismEffect.HealEnemies:
                AffectEnemies(heal: 10f, moveBuff: 1f, touchBuff: 1f);
                break;
            case PrismEffect.BuffMoveSpeed:
                AffectEnemies(heal: 0f, moveBuff: 1.35f, touchBuff: 1f);
                break;
            case PrismEffect.BuffDamage:
                AffectEnemies(heal: 0f, moveBuff: 1f, touchBuff: 1.35f);
                break;
            case PrismEffect.Vortex:
                SpawnVortex();
                break;
            default:
                if (target != null)
                {
                    IDamageable damageable = EnemyMechanics.FindDamageable(target);
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
                    }
                }
                break;
        }

        Destroy(gameObject);
    }

    public void Reflect(PlayerMechanics reflector, float speedMultiplier, float damageMultiplier)
    {
        if (effect != PrismEffect.Damage)
        {
            return;
        }

        reflectedByPlayer = reflector;
        reflectedToEnemies = true;
        owner = null;
        damage = Mathf.Max(0f, damage * Mathf.Max(0f, damageMultiplier));
        velocity = -velocity.normalized * (velocity.magnitude * Mathf.Max(0.01f, speedMultiplier));
        transform.right = velocity.sqrMagnitude > 0.001f ? velocity.normalized : Vector2.right;
        projectileColor = Color.cyan;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = projectileColor;
        }
    }

    private void AffectEnemies(float heal, float moveBuff, float touchBuff)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, effectRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyMechanics enemy = hits[i].GetComponentInParent<EnemyMechanics>();
            if (enemy == null || enemy == owner)
            {
                continue;
            }

            if (heal > 0f)
            {
                enemy.Heal(heal);
            }

            if (moveBuff != 1f)
            {
                enemy.ApplyTemporaryMoveSpeedMultiplier(moveBuff, effectDuration);
            }

            if (touchBuff != 1f)
            {
                enemy.ApplyTemporaryTouchDamageMultiplier(touchBuff, effectDuration);
            }
        }
    }

    private void SpawnVortex()
    {
        GameObject vortex = new GameObject("ArkeVortex");
        vortex.transform.position = transform.position;
        EnemyVortexZone zone = vortex.AddComponent<EnemyVortexZone>();
        zone.Initialize(effectDuration, effectRadius, 0.3f, damage * 0.2f, 0.35f, projectileColor);
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
