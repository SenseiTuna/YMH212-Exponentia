using System.Collections;
using UnityEngine;

// Simple damage-over-time debuff that calls TakeDamage on an IDamageable target
public class DoTDebuff : MonoBehaviour
{
    private IDamageable target;
    private float damagePerSecond;
    private float duration;
    private const float TickInterval = 0.25f;

    public void Initialize(IDamageable targetDamageable, float dps, float totalDuration)
    {
        target = targetDamageable;
        damagePerSecond = dps;
        duration = totalDuration;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float end = Time.time + duration;
        while (Time.time < end)
        {
            if (target != null)
            {
                float remaining = end - Time.time;
                float tickDuration = Mathf.Min(TickInterval, remaining);
                if (tickDuration > 0f)
                {
                    target.TakeDamage(damagePerSecond * tickDuration);
                }
            }

            yield return new WaitForSeconds(TickInterval);
        }

        Destroy(this);
    }
}
