using UnityEngine;

public class ZeusSkill : GodSkillBase
{
    [Header("Zeus Tuning")]
    [SerializeField] private float lightningDamage = 30f;
    [SerializeField] private float chainRadius = 3f;
    [SerializeField] private int maxChainCount = 3;
    [SerializeField] private float stunDuration = 0.6f;
    [Header("Zeus Passive")]
    [SerializeField] private float killAttackSpeedPerStack = 0.15f;
    [SerializeField] private int maxKillStacks = 5;
    [SerializeField] private float killStackDuration = 4f;
    [SerializeField] private float wrathCooldown = 10f;

    private int currentKillStacks = 0;
    private float baseAttackSpeed;
    private System.Collections.Generic.List<float> stackExpiry = new System.Collections.Generic.List<float>();
    private float lastWrathTime = -999f;
    private bool stackRoutineRunning = false;

    protected override void Reset()
    {
        base.Reset();
        ConfigureSkillDefinition(
            "Zeus",
            "Gokyuzunden cagrilan yildirim temelli aktif skill.",
            GodSkillType.Zeus,
            30f,
            7f);
        name = "ZeusSkill";
    }

    protected override bool ActivateSkill()
    {
        if (owner == null) return false;

        Vector2 center = (Vector2)owner.transform.position;

        // Fare (Mouse) pozisyonunu almak için
        if (Camera.main != null && UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            center = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, chainRadius);

        int chained = 0;
        for (int i = 0; i < hits.Length && chained < maxChainCount; i++)
        {
            GameObject go = hits[i].gameObject;
            if (go == null) continue;
            if (go == owner.gameObject) continue;

            owner.DealDamage(go, SafeMultiplier(lightningDamage));
            // simple knockback/stun approximation
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(((Vector2)go.transform.position - center).normalized * 200f);
            }

            chained++;
        }

        Debug.Log(
            $"Zeus skilli kullanildi. PatlamaNoktasi={center}, Radius={chainRadius:0.##}, " +
            $"Damage={lightningDamage:0.##}, ZincirlenenHedef={chained}/{maxChainCount}");

        return true;
    }

    private float SafeMultiplier(float absoluteDamage)
    {
        float baseD = ownerStats != null && ownerStats.Damage > 0f ? ownerStats.Damage : 1f;
        return absoluteDamage / baseD;
    }

    private void Start()
    {
        if (ownerStats != null)
            baseAttackSpeed = ownerStats.AttackSpeed;

        if (owner != null)
        {
            owner.OnEnemyKilled += HandleEnemyKilled;
            owner.OnCanDegisti += HandleHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.OnEnemyKilled -= HandleEnemyKilled;
            owner.OnCanDegisti -= HandleHealthChanged;
        }
    }

    private void HandleEnemyKilled(GameObject enemy)
    {
        if (!IsUnlocked)
        {
            return;
        }

        AddKillStack();
    }

    private void AddKillStack()
    {
        if (currentKillStacks >= maxKillStacks)
        {
            // refresh latest expiry
            stackExpiry.Add(Time.time + killStackDuration);
            Debug.Log($"Zeus pasifi stack yeniledi. Stack={currentKillStacks}/{maxKillStacks}, Sure={killStackDuration:0.##}sn");
            return;
        }

        currentKillStacks++;
        stackExpiry.Add(Time.time + killStackDuration);
        UpdateAttackSpeed();
        Debug.Log($"Zeus pasifi stack kazandi. Stack={currentKillStacks}/{maxKillStacks}, Sure={killStackDuration:0.##}sn");

        if (!stackRoutineRunning)
            StartCoroutine(StackDecayRoutine());
    }

    private System.Collections.IEnumerator StackDecayRoutine()
    {
        stackRoutineRunning = true;
        while (stackExpiry.Count > 0)
        {
            float now = Time.time;
            stackExpiry.RemoveAll(t => t <= now);
            int prevStacks = currentKillStacks;
            currentKillStacks = Mathf.Min(maxKillStacks, stackExpiry.Count);
            if (currentKillStacks != prevStacks)
                UpdateAttackSpeed();

            yield return new WaitForSeconds(0.25f);
        }

        currentKillStacks = 0;
        UpdateAttackSpeed();
        stackRoutineRunning = false;
    }

    private void UpdateAttackSpeed()
    {
        if (ownerStats == null) return;
        ownerStats.AttackSpeed = baseAttackSpeed * (1f + currentKillStacks * killAttackSpeedPerStack);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (!IsUnlocked)
        {
            return;
        }

        if (owner == null) return;
        float ratio = max > 0f ? current / max : 1f;
        if (Time.time - lastWrathTime < wrathCooldown) return;

        if (ratio <= 0.3f || ratio <= 0.6f)
        {
            TriggerWrath();
            lastWrathTime = Time.time;
        }
    }

    private void TriggerWrath()
    {
        if (owner == null) return;
        float radius = 4f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(owner.transform.position, radius);
        int affectedTargets = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject go = hits[i].gameObject;
            if (go == null || go == owner.gameObject) continue;
            owner.DealDamage(go, SafeMultiplier(lightningDamage * 0.6f));
            affectedTargets++;
        }

        owner.SetTemporaryInvulnerable(2f);
        Debug.Log($"Zeus wrath tetiklendi. Radius={radius:0.##}, HedefSayisi={affectedTargets}, Invuln=2sn");
    }
}
