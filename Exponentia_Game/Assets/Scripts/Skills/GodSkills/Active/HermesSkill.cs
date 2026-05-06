using UnityEngine;

public class HermesSkill : GodSkillBase
{
    [Header("Hermes Tuning")]
    [SerializeField] private float dashDistance = 4f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float hasteDuration = 3f;
    [SerializeField] private float cooldownReductionRatio = 0.2f;

    protected override void Reset()
    {
        base.Reset();
        ConfigureSkillDefinition(
            "Hermes",
            "Hiz, atiklik ve pozisyon alma odakli aktif skill.",
            GodSkillType.Hermes,
            20f,
            5f);
        name = "HermesSkill";
    }

    protected override bool ActivateSkill()
    {
        if (owner == null) return false;

        // Dash: teleport/lerp forward and damage enemies along path
        PlayerMovement pm = owner.GetComponent<PlayerMovement>();
        Vector2 dir = pm != null ? pm.LastMoveDirection : Vector2.right;
        Vector3 start = owner.transform.position;
        Vector3 end = start + (Vector3)(dir.normalized * dashDistance);
        // simple instant move
        owner.transform.position = end;

        // damage nearby enemies at end
        Collider2D[] hits = Physics2D.OverlapCircleAll(end, 1f);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject go = hits[i].gameObject;
            if (go == null || go == owner.gameObject) continue;
            owner.DealDamage(go, 1.0f);
        }

        // apply haste (temporary attack speed increase)
        if (ownerStats != null)
        {
            float prev = ownerStats.saldiriHizi;
            ownerStats.saldiriHizi = prev * 1.25f;
            StartCoroutine(RestoreAttackSpeedAfter(hasteDuration, prev));
        }

        return true;
    }

    private System.Collections.IEnumerator RestoreAttackSpeedAfter(float dur, float prev)
    {
        yield return new WaitForSeconds(dur);
        if (ownerStats != null)
            ownerStats.saldiriHizi = prev;
    }
}
