using UnityEngine;

public class AresSkill : GodSkillBase
{
    [Header("Ares Tuning")]
    [SerializeField] private float berserkDuration = 4f;
    [SerializeField] private float bonusDamageMultiplier = 1.5f;
    [SerializeField] private float bonusAttackSpeedMultiplier = 1.35f;
    [SerializeField] private float bonusMoveSpeedMultiplier = 1.2f;
    [Header("Ares Passive")]
    [SerializeField] private float bleedDps = 6f;
    [SerializeField] private float bleedDuration = 4f;

    protected override void Reset()
    {
        base.Reset();
        ConfigureSkillDefinition(
            "Ares",
            "Gecici ofansif guclendirme saglayan savas skill'i.",
            GodSkillType.Ares,
            24f,
            10f);
        name = "AresSkill";
    }

    protected override bool ActivateSkill()
    {
        if (owner == null || ownerStats == null) return false;

        float prevDamage = ownerStats.hasar;
        float prevAttackSpeed = ownerStats.saldiriHizi;
        float prevMove = ownerStats.hareketHizi;

        ownerStats.hasar = ownerStats.hasar * bonusDamageMultiplier;
        ownerStats.saldiriHizi = ownerStats.saldiriHizi * bonusAttackSpeedMultiplier;
        ownerStats.hareketHizi = ownerStats.hareketHizi * bonusMoveSpeedMultiplier;

        StartCoroutine(EndBerserkAfter(berserkDuration, prevDamage, prevAttackSpeed, prevMove));
        return true;
    }

    private System.Collections.IEnumerator EndBerserkAfter(float time, float prevD, float prevAS, float prevMove)
    {
        yield return new WaitForSeconds(time);
        if (ownerStats == null) yield break;
        ownerStats.hasar = prevD;
        ownerStats.saldiriHizi = prevAS;
        ownerStats.hareketHizi = prevMove;
    }

    // Kanli Darbe: apply AoE and bleeding
    public void DoBloodySmash()
    {
        if (owner == null) return;
        Vector2 origin = owner.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, 2f);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject go = hits[i].gameObject;
            if (go == null || go == owner.gameObject) continue;
            owner.DealDamage(go, 1.8f);
            IDamageable d = go.GetComponentInParent<IDamageable>();
            if (d != null)
            {
                DoTDebuff dot = go.AddComponent<DoTDebuff>();
                dot.Initialize(d, bleedDps, bleedDuration);
            }
        }
    }
}
