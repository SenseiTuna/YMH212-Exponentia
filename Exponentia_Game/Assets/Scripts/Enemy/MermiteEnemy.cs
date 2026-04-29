using UnityEngine;

public class MermiteEnemy : EnemyMechanics
{
    [Header("Mermite Attack")]
    [SerializeField] private float shootRange = 6f;
    [SerializeField] private float projectileSpeed = 3.2f;
    [SerializeField] private float projectileDamage = 11f;
    [SerializeField] private float projectileLifeTime = 4f;
    [SerializeField] private float projectileSize = 0.26f;
    [SerializeField] private float attackCooldown = 1.6f;
    [SerializeField] private float shootOffset = 0.75f;
    [SerializeField] private Color projectileColor = new Color(0.8f, 0.8f, 0.8f);

    private float nextShootTime;

    private void Reset()
    {
        ApplyDefaultSetup(
            "Mermite",
            24f,
            1.3f,
            6f,
            9f,
            true,
            4.2f,
            new Color(0.72f, 0.72f, 0.76f),
            new Vector2(0.8f, 0.8f));
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive || PlayerTarget == null || Time.time < nextShootTime)
        {
            return;
        }

        if (GetDistanceToPlayer() > shootRange)
        {
            return;
        }

        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        nextShootTime = Time.time + attackCooldown;

        GameObject projectileObject = SpawnSquareProjectile("MermiteStoneShot", transform.position + (Vector3)(direction * shootOffset), direction);
        EnemyProjectile projectile = projectileObject.AddComponent<EnemyProjectile>();
        projectile.Initialize(this, direction, projectileSpeed, projectileDamage, projectileLifeTime, projectileColor, projectileSize);
    }
}
