using UnityEngine;

public class ArkeHarbingerOfRainbowsEnemy : EnemyMechanics
{
    [Header("Arke Prism Burst")]
    [SerializeField] private float attackCooldown = 3.4f;
    [SerializeField] private float shootRange = 9f;
    [SerializeField] private float projectileLifeTime = 4.5f;
    [SerializeField] private float projectileSize = 0.22f;
    [SerializeField] private float spawnOffset = 0.9f;
    [SerializeField] private float effectRadius = 2f;
    [SerializeField] private float buffDuration = 2.8f;
    [SerializeField] private float coreBeamDamage = 20f;
    [SerializeField] private float coreBeamSpeed = 9.5f;
    [SerializeField] private float coreBeamSize = 0.34f;
    [SerializeField] private Color coreBeamColor = new Color(1f, 0.95f, 0.8f);
    [SerializeField] private Sprite prismDamageSprite;
    [SerializeField] private Sprite prismHealSprite;
    [SerializeField] private Sprite prismMoveSpeedSprite;
    [SerializeField] private Sprite prismBuffDamageSprite;
    [SerializeField] private Sprite prismVortexSprite;

    private float nextAttackTime;

    private readonly Color[] prismColors = new Color[]
    {
        new Color(1f, 0.2f, 0.2f),
        new Color(1f, 0.5f, 0.1f),
        new Color(1f, 0.9f, 0.2f),
        new Color(0.2f, 1f, 0.3f),
        new Color(0.2f, 0.75f, 1f),
        new Color(0.45f, 0.45f, 1f),
        new Color(0.7f, 0.3f, 0.95f)
    };

    private readonly float[] prismSpeeds = new float[] { 8f, 7.2f, 6.5f, 5.8f, 5f, 4.4f, 3.8f };
    private readonly float[] prismDamages = new float[] { 13f, 13f, 8f, 0f, 0f, 7f, 6f };
    private readonly ArkePrismProjectile.PrismEffect[] prismEffects = new ArkePrismProjectile.PrismEffect[]
    {
        ArkePrismProjectile.PrismEffect.Damage,
        ArkePrismProjectile.PrismEffect.Damage,
        ArkePrismProjectile.PrismEffect.BuffMoveSpeed,
        ArkePrismProjectile.PrismEffect.HealEnemies,
        ArkePrismProjectile.PrismEffect.BuffDamage,
        ArkePrismProjectile.PrismEffect.Damage,
        ArkePrismProjectile.PrismEffect.Vortex
    };

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Arke, Harbinger Of Rainbows",
            70f,
            1.1f,
            10f,
            30f,
            true,
            6.2f,
            new Color(0.95f, 0.85f, 1f),
            new Vector2(1.1f, 1.1f));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive || PlayerTarget == null || Time.time < nextAttackTime)
        {
            return;
        }

        if (GetDistanceToPlayer() > shootRange)
        {
            return;
        }

        Vector2 centerDirection = GetDirectionToPlayer();
        if (centerDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        FirePrismBurst(centerDirection);
    }

    private void FirePrismBurst(Vector2 centerDirection)
    {
        int count = prismColors.Length;
        float totalSpread = 72f;

        for (int i = 0; i < count; i++)
        {
            float lerp = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-totalSpread * 0.5f, totalSpread * 0.5f, lerp);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * centerDirection;

            GameObject projectileObject = new GameObject($"ArkePrism_{i}");
            projectileObject.transform.position = transform.position + (Vector3)(direction.normalized * spawnOffset);
            ArkePrismProjectile projectile = projectileObject.AddComponent<ArkePrismProjectile>();
            projectile.Initialize(
                this,
                direction,
                prismSpeeds[i],
                prismDamages[i],
                projectileLifeTime,
                effectRadius,
                buffDuration,
                prismEffects[i],
                prismColors[i],
                projectileSize,
                GetPrismVisualSprite(prismEffects[i]));
        }

        SpawnEnemyProjectile(
            "ArkeCoreBeam",
            transform.position + (Vector3)(centerDirection.normalized * spawnOffset),
            centerDirection,
            coreBeamSpeed,
            coreBeamDamage,
            projectileLifeTime,
            coreBeamColor,
            coreBeamSize);
    }

    private Sprite GetPrismVisualSprite(ArkePrismProjectile.PrismEffect prismEffect)
    {
        return prismEffect switch
        {
            ArkePrismProjectile.PrismEffect.Damage => prismDamageSprite,
            ArkePrismProjectile.PrismEffect.HealEnemies => prismHealSprite,
            ArkePrismProjectile.PrismEffect.BuffMoveSpeed => prismMoveSpeedSprite,
            ArkePrismProjectile.PrismEffect.BuffDamage => prismBuffDamageSprite,
            ArkePrismProjectile.PrismEffect.Vortex => prismVortexSprite,
            _ => prismDamageSprite
        };
    }
}
