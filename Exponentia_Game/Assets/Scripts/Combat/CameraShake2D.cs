using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraShake2D : MonoBehaviour
{
    public static CameraShake2D Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform shakeTarget;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 0.1f;
    [SerializeField] private float defaultStrength = 0.08f;

    private Vector3 originalLocalPosition;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveTarget();
    }

    private void OnEnable()
    {
        ResolveTarget();
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        ResolveTarget();
        if (shakeTarget == null)
        {
            Debug.LogWarning("CameraShake2D: No shake target found.", this);
            return;
        }

        float safeDuration = Mathf.Max(0f, duration);
        float safeStrength = Mathf.Max(0f, strength);
        if (safeDuration <= 0f || safeStrength <= 0f)
        {
            return;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeTarget.localPosition = originalLocalPosition;
        }

        shakeRoutine = StartCoroutine(ShakeRoutine(safeDuration, safeStrength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        if (shakeTarget == null)
        {
            yield break;
        }

        originalLocalPosition = shakeTarget.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * strength;
            shakeTarget.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        shakeTarget.localPosition = originalLocalPosition;
        shakeRoutine = null;
    }

    private void ResolveTarget()
    {
        if (shakeTarget != null)
        {
            originalLocalPosition = shakeTarget.localPosition;
            return;
        }

        Camera main = Camera.main;
        if (main != null)
        {
            shakeTarget = main.transform;
            originalLocalPosition = shakeTarget.localPosition;
        }
    }
}
