using System.Collections.Generic;
using Exponentia.Player;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SkillVortexZone : MonoBehaviour
{
    [SerializeField] private float duration = 3f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private float pullStrength = 3f;
    [SerializeField] private float tickDamage = 8f;
    [SerializeField] private float tickInterval = 0.35f;

    private readonly Dictionary<EnemyMechanics, float> nextTickByEnemy = new Dictionary<EnemyMechanics, float>();
    private CircleCollider2D circleCollider;
    private Rigidbody2D rb;
    private PlayerMechanics owner;
    private PlayerStats ownerStats;
    private float elapsedTime;

    public void Initialize(PlayerMechanics skillOwner, float lifeTime, float vortexRadius, float pull, float damagePerTick, float damageInterval)
    {
        owner = skillOwner;
        ownerStats = owner != null ? owner.GetComponent<PlayerStats>() : null;
        duration = Mathf.Max(0.1f, lifeTime);
        radius = Mathf.Max(0.2f, vortexRadius);
        pullStrength = Mathf.Max(0f, pull);
        tickDamage = Mathf.Max(0f, damagePerTick);
        tickInterval = Mathf.Max(0.05f, damageInterval);
        ApplyColliderState();
    }

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;

        ApplyColliderState();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (owner == null || other == null)
        {
            return;
        }

        EnemyMechanics enemy = other.GetComponentInParent<EnemyMechanics>();
        if (enemy == null || !enemy.IsAlive)
        {
            return;
        }

        PullEnemy(enemy);
        TickDamage(enemy);
    }

    private void PullEnemy(EnemyMechanics enemy)
    {
        Vector2 toCenter = (Vector2)transform.position - (Vector2)enemy.transform.position;
        if (toCenter.sqrMagnitude <= 0.0025f)
        {
            return;
        }

        Vector2 pullDirection = toCenter.normalized;
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        Vector2 displacement = pullDirection * pullStrength * Time.deltaTime;
        if (rb != null)
        {
            rb.MovePosition(rb.position + displacement);
            return;
        }

        enemy.transform.position += (Vector3)displacement;
    }

    private void TickDamage(EnemyMechanics enemy)
    {
        if (tickDamage <= 0f)
        {
            return;
        }

        if (!nextTickByEnemy.TryGetValue(enemy, out float nextTickTime))
        {
            nextTickByEnemy[enemy] = Time.time + tickInterval;
            return;
        }

        if (Time.time < nextTickTime)
        {
            return;
        }

        nextTickByEnemy[enemy] = Time.time + tickInterval;
        owner.DealDamage(enemy.gameObject, ResolveDamageMultiplier(tickDamage));
    }

    private float ResolveDamageMultiplier(float absoluteDamage)
    {
        float baseDamage = ownerStats != null && ownerStats.Damage > 0f ? ownerStats.Damage : 1f;
        return Mathf.Max(0f, absoluteDamage) / baseDamage;
    }

    private void ApplyColliderState()
    {
        if (circleCollider != null)
        {
            circleCollider.radius = radius;
        }
    }
}
