using System.Collections;
using UnityEngine;

// Simple damage-over-time debuff that calls TakeDamage on an IDamageable target
public class DoTDebuff : MonoBehaviour
{
    private IDamageable target;
    private float damagePerSecond;
    private float duration;

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
                target.TakeDamage(damagePerSecond * Time.deltaTime);
            }

            yield return null;
        }

        Destroy(this);
    }
}
