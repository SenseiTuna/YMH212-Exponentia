using UnityEngine;

public class SparkingSpriteEnemy : EnemyMechanics
{
    [Header("Death Hazard")]
    [SerializeField] private float hazardDamagePerTick = 5f;
    [SerializeField] private float hazardTickCooldown = 0.45f;
    [SerializeField] private float hazardDuration = 3f;
    [SerializeField] private float hazardRadius = 1f;
    [SerializeField] private Color hazardColor = new Color(1f, 0.5f, 0.1f, 0.65f);

    protected override void Reset()
    {
        ApplyDefaultSetup(
            "Sparking Sprite",
            18f,
            4.4f,
            8f,
            10f,
            true,
            0.1f,
            new Color(1f, 0.8f, 0.25f),
            new Vector2(0.7f, 0.7f));
    }

    protected override void Die()
    {
        SpawnHazardArea();
        base.Die();
    }

    private void SpawnHazardArea()
    {
        GameObject hazardObject = new GameObject("SparkingFireArea");
        hazardObject.transform.position = transform.position;

        EnemyHazardArea hazardArea = hazardObject.AddComponent<EnemyHazardArea>();
        hazardArea.Initialize(
            hazardDamagePerTick,
            hazardTickCooldown,
            hazardDuration,
            hazardRadius,
            hazardColor);
    }
}
