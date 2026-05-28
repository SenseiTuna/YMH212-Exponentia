using System.Collections;
using UnityEngine;

public class CurseOfTheBlackbeardEnemy : EnemyMechanics
{
    [Header("Blackbeard Cannon")]
    [SerializeField] private float battleRange = 13f;
    [SerializeField] private float cannonVolleyCooldown = 4.2f;
    [SerializeField] private float cannonVolleyWindup = 0.85f;
    [SerializeField] private float cannonSpawnHeight = 8f;
    [SerializeField] private int cannonVolleyShots = 3;
    [SerializeField] private float cannonVolleySpread = 2.2f;
    [SerializeField] private float cannonProjectileSpeed = 14f;
    [SerializeField] private float cannonImpactDamage = 18f;
    [SerializeField] private float cannonProjectileLifeTime = 1.2f;
    [SerializeField] private float cannonProjectileSize = 0.28f;
    [SerializeField] private float cannonImpactRadius = 1.8f;
    [SerializeField] private float splashDamage = 10f;
    [SerializeField] private float splashDuration = 1.15f;
    [SerializeField] private float splashTickCooldown = 0.35f;
    [SerializeField] private float broadsideCooldown = 2.6f;
    [SerializeField] private int broadsideProjectileCount = 5;
    [SerializeField] private float broadsideSpreadAngle = 30f;
    [SerializeField] private float broadsideProjectileSpeed = 7.5f;
    [SerializeField] private float broadsideProjectileDamage = 12f;
    [SerializeField] private float broadsideProjectileLifeTime = 4f;
    [SerializeField] private float broadsideProjectileSize = 0.22f;
    [SerializeField] private float broadsideSpawnOffset = 1.1f;
    [SerializeField] private Color cannonColor = new Color(0.35f, 0.35f, 0.38f);
    [SerializeField] private Color splashColor = new Color(0.2f, 0.55f, 1f, 0.75f);
    [SerializeField] private Sprite cannonBallVisualSprite;

    private float nextAttackTime;
    private int attackIndex;

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Curse Of The Blackbeard",
            170f,
            1.1f,
            14f,
            55f,
            true,
            8f,
            new Color(0.3f, 0.25f, 0.22f),
            new Vector2(1.35f, 1.35f));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive || PlayerTarget == null)
        {
            return;
        }

        float distance = GetDistanceToPlayer();
        if (distance > battleRange || Time.time < nextAttackTime)
        {
            return;
        }

        Vector2 directionToPlayer = GetDirectionToPlayer();
        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            directionToPlayer = Vector2.right;
        }

        if (attackIndex == 0)
        {
            TriggerAttackAnimation("cannonball attack");
            StartCoroutine(FireCannonBarrage(PlayerTarget.position));
            nextAttackTime = Time.time + cannonVolleyCooldown;
        }
        else
        {
            TriggerAttackAnimation("parrot attack");
            FireBroadside(directionToPlayer);
            nextAttackTime = Time.time + broadsideCooldown;
        }

        attackIndex = (attackIndex + 1) % 2;
    }

    private IEnumerator FireCannonBarrage(Vector3 targetPosition)
    {
        Vector2 centerPoint = targetPosition;
        float halfSpread = cannonVolleyShots <= 1 ? 0f : cannonVolleySpread * 0.5f;

        for (int i = 0; i < cannonVolleyShots; i++)
        {
            float t = cannonVolleyShots == 1 ? 0.5f : i / (float)(cannonVolleyShots - 1);
            float offset = Mathf.Lerp(-halfSpread, halfSpread, t);
            Vector2 impactPoint = centerPoint + new Vector2(offset, 0f);

            SpawnTelegraph(impactPoint, cannonImpactRadius, cannonVolleyWindup);
            yield return new WaitForSeconds(cannonVolleyWindup);

            Vector3 spawnPosition = impactPoint + Vector2.up * cannonSpawnHeight;
            float travelTime = Mathf.Max(0.1f, cannonVolleyWindup * 0.9f);
            float speed = Mathf.Max(cannonProjectileSpeed, cannonSpawnHeight / travelTime);

            SpawnEnemyProjectile(
                $"BlackbeardCannonball_{i}",
                spawnPosition,
                Vector2.down,
                speed,
                cannonImpactDamage,
                cannonProjectileLifeTime,
                cannonColor,
                cannonProjectileSize,
                cannonBallVisualSprite);

            SpawnSplashArea(impactPoint, cannonImpactRadius, splashDamage, splashTickCooldown, splashDuration, splashColor);
        }
    }

    private void FireBroadside(Vector2 centerDirection)
    {
        int count = Mathf.Max(1, broadsideProjectileCount);
        float startAngle = -broadsideSpreadAngle * 0.5f;
        float step = count == 1 ? 0f : broadsideSpreadAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * centerDirection.normalized;
            SpawnEnemyProjectile(
                $"BlackbeardBroadside_{i}",
                transform.position + (Vector3)(direction.normalized * broadsideSpawnOffset),
                direction,
                broadsideProjectileSpeed,
                broadsideProjectileDamage,
                broadsideProjectileLifeTime,
                cannonColor,
                broadsideProjectileSize,
                cannonBallVisualSprite);
        }
    }

    private void SpawnTelegraph(Vector3 centerPosition, float radius, float lifeTime)
    {
        SpawnHazardArea(
            "BlackbeardTelegraph",
            centerPosition,
            0f,
            0.5f,
            lifeTime,
            radius,
            new Color(1f, 0.88f, 0.25f, 0.35f));
    }

    private void SpawnSplashArea(Vector3 centerPosition, float radius, float damage, float tickCooldown, float lifeTime, Color color)
    {
        SpawnHazardArea(
            "BlackbeardSplash",
            centerPosition,
            damage,
            tickCooldown,
            lifeTime,
            radius,
            color);
    }

    private void SpawnHazardArea(string objectName, Vector3 position, float damagePerTick, float cooldown, float lifeTime, float radius, Color color)
    {
        GameObject hazardObject = new GameObject(objectName);
        hazardObject.transform.position = position;
        EnemyHazardArea hazardArea = hazardObject.AddComponent<EnemyHazardArea>();
        hazardArea.Initialize(damagePerTick, cooldown, lifeTime, radius, color);
    }
}
