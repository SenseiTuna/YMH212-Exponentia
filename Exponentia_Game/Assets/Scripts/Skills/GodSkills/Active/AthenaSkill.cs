using UnityEngine;

public class AthenaSkill : GodSkillBase
{
    [Header("Athena Tuning")]
    [SerializeField] private float shieldAmount = 35f;
    [SerializeField] private float shieldDuration = 4f;
    [SerializeField] private float defenseBonus = 8f;
    [SerializeField] private bool canReflectProjectiles = true;

    protected override void Reset()
    {
        base.Reset();
        ConfigureSkillDefinition(
            "Athena",
            "Kalkan ve savunma odakli koruyucu skill.",
            GodSkillType.Athena,
            26f,
            9f);
        name = "AthenaSkill";
    }

    protected override bool ActivateSkill()
    {
        if (owner == null) return false;

        // grant shield
        owner.KalkanYenile(shieldAmount);

        // temporary defense bonus
        if (ownerStats != null)
        {
            float prev = ownerStats.Defense;
            ownerStats.Defense += defenseBonus;
            StartCoroutine(RestoreDefenseAfter(shieldDuration, prev));
        }

        return true;
    }

    private System.Collections.IEnumerator RestoreDefenseAfter(float dur, float prev)
    {
        yield return new WaitForSeconds(dur);
        if (ownerStats != null)
            ownerStats.Defense = prev;
    }
}
