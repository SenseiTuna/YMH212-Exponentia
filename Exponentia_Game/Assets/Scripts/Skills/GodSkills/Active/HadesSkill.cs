using UnityEngine;

public class HadesSkill : GodSkillBase
{
    [Header("Hades Tuning")]
    [SerializeField] private float soulDamage = 26f;
    [SerializeField] private float executeThreshold = 0.15f;
    [SerializeField] private float fearDuration = 1.2f;
    [SerializeField] private float lifeStealRatio = 0.25f;
    [Header("Hades Passive")]
    [SerializeField] private float dotDps = 4f;
    [SerializeField] private float dotDuration = 5f;
    [SerializeField] private int soulsPerKill = 1;
    [SerializeField] private float soulToShield = 8f;

    private int collectedSouls = 0;

    protected override void Reset()
    {
        base.Reset();
        ConfigureSkillDefinition(
            "Hades",
            "Can cekme ve ruh temali aktif skill.",
            GodSkillType.Hades,
            32f,
            8f);
        name = "HadesSkill";
    }

    protected override bool ActivateSkill()
    {
        // Example active: temporary summon of shadows is complex; for now grant lifesteal buff briefly
        if (owner == null) return false;

        if (ownerStats == null) return false;

        float prev = ownerStats.LifeSteal;
        ownerStats.LifeSteal += lifeStealRatio * 100f; // store as percent if >1
        StartCoroutine(RemoveLifeStealAfter(4f, prev));
        return true;
    }

    private System.Collections.IEnumerator RemoveLifeStealAfter(float duration, float previous)
    {
        yield return new WaitForSeconds(duration);
        if (ownerStats != null)
            ownerStats.LifeSteal = previous;
    }

    private void Start()
    {
        if (owner != null)
        {
            owner.OnDealtDamage += HandleDealtDamage;
            owner.OnEnemyKilled += HandleEnemyKilled;
        }
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.OnDealtDamage -= HandleDealtDamage;
            owner.OnEnemyKilled -= HandleEnemyKilled;
        }
    }

    private void HandleDealtDamage(GameObject target, float applied)
    {
        // Apply DoT to the damaged target
        IDamageable dmg = target.GetComponentInParent<IDamageable>();
        if (dmg == null) return;

        DoTDebuff existing = target.GetComponent<DoTDebuff>();
        if (existing == null)
        {
            DoTDebuff dot = target.AddComponent<DoTDebuff>();
            dot.Initialize(dmg, dotDps, dotDuration);
        }
    }

    private void HandleEnemyKilled(GameObject enemy)
    {
        collectedSouls += soulsPerKill;
        // convert souls to shield immediately
        if (owner != null)
        {
            owner.KalkanYenile(collectedSouls * soulToShield);
            collectedSouls = 0;
        }
    }
}
