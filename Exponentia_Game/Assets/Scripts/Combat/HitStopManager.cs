using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    [Header("Behavior")]
    [SerializeField] private bool useHardPause;
    [SerializeField] [Range(0.01f, 0.5f)] private float softPauseTimeScale = 0.08f;
    [SerializeField] private bool debugLogs;

    private Coroutine hitStopRoutine;
    private float hitStopEndRealtime;
    private float cachedTimeScale = 1f;
    private float cachedFixedDeltaTime = 0.02f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void DoHitStop(float duration)
    {
        float safeDuration = Mathf.Max(0f, duration);
        if (safeDuration <= 0f)
        {
            return;
        }

        float end = Time.realtimeSinceStartup + safeDuration;
        hitStopEndRealtime = Mathf.Max(hitStopEndRealtime, end);

        if (hitStopRoutine == null)
        {
            hitStopRoutine = StartCoroutine(HitStopRoutine());
        }
    }

    private IEnumerator HitStopRoutine()
    {
        cachedTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        cachedFixedDeltaTime = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;

        Time.timeScale = useHardPause ? 0f : softPauseTimeScale;
        Time.fixedDeltaTime = cachedFixedDeltaTime * Time.timeScale;

        if (debugLogs)
        {
            Debug.Log($"HitStopManager: Hit stop started.", this);
        }

        while (Time.realtimeSinceStartup < hitStopEndRealtime)
        {
            yield return null;
        }

        Time.timeScale = cachedTimeScale;
        Time.fixedDeltaTime = cachedFixedDeltaTime;
        hitStopEndRealtime = 0f;
        hitStopRoutine = null;
    }
}
