using UnityEngine;

public class PoseidonSkill : GodSkillBase
{
    [Header("Poseidon Tuning")]
    [SerializeField] private float waveDamage = 22f;
    [SerializeField] private float waveRange = 5f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float slowDuration = 1.5f;
    [Header("Poseidon Passive")]
    [SerializeField] private float passiveKnockbackInterval = 10f;
    [SerializeField] private float passiveSlowAmount = 0.25f;

    private float lastPassiveKnockback;

    protected override void Reset()
    {
        base.Reset();
        ConfigureSkillDefinition(
            "Poseidon",
            "Dalga ve itme temelli alan baskisi skill'i.",
            GodSkillType.Poseidon,
            28f,
            6.5f);
        name = "PoseidonSkill";
    }

    protected override bool ActivateSkill()
    {
        if (owner == null) return false;

        Vector2 dir = Vector2.right;
        PlayerMovement pm = owner.GetComponent<PlayerMovement>();
        if (pm != null) dir = pm.LastMoveDirection;

        Vector2 origin = (Vector2)owner.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin + dir.normalized * (waveRange * 0.5f), waveRange);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject go = hits[i].gameObject;
            if (go == null || go == owner.gameObject) continue;

            // apply damage
            owner.DealDamage(go, SafeMultiplier(waveDamage));

            // knockback via rigidbody if available
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 push = ((Vector2)go.transform.position - origin).normalized;
                rb.AddForce(push * knockbackForce * 60f);
            }
        }

        lastPassiveKnockback = Time.time;
        return true;
    }

    private float SafeMultiplier(float absoluteDamage)
    {
        float baseD = ownerStats != null && ownerStats.Damage > 0f ? ownerStats.Damage : 1f;
        return absoluteDamage / baseD;
    }

    private void Start()
    {
        if (owner != null)
        {
            owner.OnDealtDamage += HandleDealtDamage;
        }
    }

    private void OnDestroy()
    {
        if (owner != null)
            owner.OnDealtDamage -= HandleDealtDamage;
    }

    private void HandleDealtDamage(GameObject target, float applied)
    {
        // apply small slow effect by attaching a DoT-like placeholder that doesn't damage but marks the hit
        // since enemies don't expose movement setter, we only apply a brief DoT as placeholder for wet effect
        IDamageable dmg = target.GetComponentInParent<IDamageable>();
        if (dmg == null) return;

        DoTDebuff dot = target.GetComponent<DoTDebuff>();
        if (dot == null)
        {
            // reuse DoT as non-lethal slow indicator by using tiny DPS
            DoTDebuff newDot = target.AddComponent<DoTDebuff>();
            newDot.Initialize(dmg, 0.1f, slowDuration);
        }
    }
}
