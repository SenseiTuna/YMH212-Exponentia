using UnityEngine;

public class ZeusSkill : GodSkillBase
{
    [Header("Zeus Tuning")]
    [SerializeField] private float lightningDamage = 30f;
    [SerializeField] private float chainRadius = 3f;
    [SerializeField] private int maxChainCount = 3;

    [Header("Lightning Strike Animation")]
    [SerializeField] private RuntimeAnimatorController lightningStrikeController;
    [SerializeField] private Sprite lightningStrikeFirstFrame;
    [SerializeField] private float animationLifetime = 0.85f;
    [SerializeField] private Vector2 animationOffset = new Vector2(0f, 2.6f);
    [SerializeField] private float animationScale = 1.6f;
    [SerializeField] private int animationSortingOrder = 500;

    [Header("Zeus Passive")]
    [SerializeField] private float killAttackSpeedPerStack = 0.15f;
    [SerializeField] private int maxKillStacks = 5;
    [SerializeField] private float killStackDuration = 4f;
    [SerializeField] private float wrathCooldown = 10f;

    private readonly System.Collections.Generic.List<float> stackExpiry = new System.Collections.Generic.List<float>();
    private int currentKillStacks;
    private float baseAttackSpeed;
    private float lastWrathTime = -999f;
    private bool stackRoutineRunning;

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

        Vector2 center = ResolveCastCenter();
        SpawnLightningStrikeAnimation(center);
        DamageTargets(center);

        return true;
    }

    private void DamageTargets(Vector2 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, chainRadius);

        int chained = 0;
        for (int i = 0; i < hits.Length && chained < maxChainCount; i++)
        {
            GameObject target = hits[i].gameObject;
            if (target == null || target == owner.gameObject) continue;

            owner.DealDamage(target, SafeMultiplier(lightningDamage));

            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(((Vector2)target.transform.position - center).normalized * 200f);
            }

            chained++;
        }
    }

    private float SafeMultiplier(float absoluteDamage)
    {
        float baseDamage = ownerStats != null && ownerStats.Damage > 0f ? ownerStats.Damage : 1f;
        return absoluteDamage / baseDamage;
    }

    private Vector2 ResolveCastCenter()
    {
        if (owner == null)
        {
            return Vector2.zero;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null || UnityEngine.InputSystem.Mouse.current == null)
        {
            return owner.transform.position;
        }

        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 screenPosition = new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Mathf.Abs(mainCamera.transform.position.z - owner.transform.position.z));

        return mainCamera.ScreenToWorldPoint(screenPosition);
    }

    private void SpawnLightningStrikeAnimation(Vector2 center)
    {
        if (lightningStrikeController == null && lightningStrikeFirstFrame == null)
        {
            return;
        }

        GameObject root = new GameObject("ZeusLightningStrikeAnimation");
        root.layer = owner != null ? owner.gameObject.layer : 0;
        root.transform.position = new Vector3(
            center.x + animationOffset.x,
            center.y + animationOffset.y,
            0f);
        root.transform.localScale = Vector3.one * Mathf.Max(0.01f, animationScale);

        GameObject visual = new GameObject("Visual");
        visual.layer = root.layer;
        visual.transform.SetParent(root.transform, false);

        SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = lightningStrikeFirstFrame;
        spriteRenderer.sortingOrder = animationSortingOrder;
        spriteRenderer.enabled = true;

        Animator animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = lightningStrikeController;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.enabled = lightningStrikeController != null;

        if (lightningStrikeController != null)
        {
            animator.Rebind();
            PlayFirstAnimationClip(animator, lightningStrikeController);
            animator.Update(0f);
        }

        Destroy(root, ResolveAnimationLifetime());
    }

    private void PlayFirstAnimationClip(Animator animator, RuntimeAnimatorController controller)
    {
        if (controller.animationClips == null || controller.animationClips.Length == 0)
        {
            return;
        }

        AnimationClip clip = controller.animationClips[0];
        if (clip != null)
        {
            animator.Play(clip.name, 0, 0f);
        }
    }

    private float ResolveAnimationLifetime()
    {
        float lifetime = Mathf.Max(0.1f, animationLifetime);
        if (lightningStrikeController == null || lightningStrikeController.animationClips == null)
        {
            return lifetime;
        }

        for (int i = 0; i < lightningStrikeController.animationClips.Length; i++)
        {
            AnimationClip clip = lightningStrikeController.animationClips[i];
            if (clip != null)
            {
                lifetime = Mathf.Max(lifetime, clip.length + 0.1f);
            }
        }

        return lifetime;
    }

    private void Start()
    {
        if (ownerStats != null)
        {
            baseAttackSpeed = ownerStats.AttackSpeed;
        }

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
        if (IsUnlocked)
        {
            AddKillStack();
        }
    }

    private void AddKillStack()
    {
        if (currentKillStacks < maxKillStacks)
        {
            currentKillStacks++;
            UpdateAttackSpeed();
        }

        stackExpiry.Add(Time.time + killStackDuration);

        if (!stackRoutineRunning)
        {
            StartCoroutine(StackDecayRoutine());
        }
    }

    private System.Collections.IEnumerator StackDecayRoutine()
    {
        stackRoutineRunning = true;
        while (stackExpiry.Count > 0)
        {
            float now = Time.time;
            stackExpiry.RemoveAll(expiry => expiry <= now);

            int previousStacks = currentKillStacks;
            currentKillStacks = Mathf.Min(maxKillStacks, stackExpiry.Count);
            if (currentKillStacks != previousStacks)
            {
                UpdateAttackSpeed();
            }

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
        if (!IsUnlocked || owner == null)
        {
            return;
        }

        float ratio = max > 0f ? current / max : 1f;
        if (Time.time - lastWrathTime < wrathCooldown)
        {
            return;
        }

        if (ratio <= 0.6f)
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
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject target = hits[i].gameObject;
            if (target == null || target == owner.gameObject) continue;

            owner.DealDamage(target, SafeMultiplier(lightningDamage * 0.6f));
        }

        owner.SetTemporaryInvulnerable(2f);
    }
}
