using UnityEngine;

public class CosmicWandererEnemy : EnemyMechanics
{
    [Header("Mimic Logic")]
    [SerializeField] private float mimicCooldown = 2.4f;
    [SerializeField] private float mimicRange = 8f;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float projectileLifeTime = 4f;
    [SerializeField] private float projectileSize = 0.24f;
    [SerializeField] private float spawnOffset = 0.85f;

    [Header("Observed Skill")]
    [SerializeField] private GodSkillType observedSkill = GodSkillType.Zeus;

    private float nextMimicTime;

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Cosmic Wanderer",
            72f,
            2.4f,
            8f,
            28f,
            true,
            4.5f,
            new Color(0.55f, 0.45f, 0.9f),
            new Vector2(1f, 1f));
    }

    public void RecordObservedSkill(GodSkillType skillType)
    {
        if (skillType != GodSkillType.None)
        {
            observedSkill = skillType;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAlive || PlayerTarget == null || Time.time < nextMimicTime)
        {
            return;
        }

        if (GetDistanceToPlayer() > mimicRange)
        {
            return;
        }

        nextMimicTime = Time.time + mimicCooldown;
        FireMimickedPattern();
    }

    private void FireMimickedPattern()
    {
        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        switch (observedSkill)
        {
            case GodSkillType.Poseidon:
                FireFan(direction, 3, 14f, 10f, new Color(0.2f, 0.8f, 1f));
                break;
            case GodSkillType.Hades:
                FireFan(direction, 5, 8f, 8f, new Color(0.55f, 0.2f, 0.8f));
                break;
            case GodSkillType.Ares:
                FireNova(8, 7f, 9f, new Color(0.9f, 0.2f, 0.2f));
                break;
            case GodSkillType.Hermes:
                FireFan(direction, 2, 10f, 7f, new Color(1f, 0.95f, 0.4f));
                break;
            case GodSkillType.Athena:
                FireFan(direction, 1, 16f, 15f, new Color(0.9f, 0.9f, 1f));
                break;
            default:
                FireFan(direction, 4, 12f, 12f, new Color(0.7f, 0.8f, 1f));
                break;
        }
    }

    private void FireFan(Vector2 centerDirection, int count, float damage, float spreadAngle, Color color)
    {
        if (count <= 1)
        {
            FireShot(centerDirection, damage, color);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float lerp = i / (float)(count - 1);
            float angle = Mathf.Lerp(-spreadAngle, spreadAngle, lerp);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * centerDirection;
            FireShot(direction, damage, color);
        }
    }

    private void FireNova(int count, float damage, float speedMultiplier, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            SpawnEnemyProjectile(
                "CosmicNovaShot",
                transform.position + (Vector3)(direction * spawnOffset),
                direction,
                projectileSpeed * speedMultiplier / 5f,
                damage,
                projectileLifeTime,
                color,
                projectileSize);
        }
    }

    private void FireShot(Vector2 direction, float damage, Color color)
    {
        SpawnEnemyProjectile(
            "CosmicMimicShot",
            transform.position + (Vector3)(direction.normalized * spawnOffset),
            direction,
            projectileSpeed,
            damage,
            projectileLifeTime,
            color,
            projectileSize);
    }
}
