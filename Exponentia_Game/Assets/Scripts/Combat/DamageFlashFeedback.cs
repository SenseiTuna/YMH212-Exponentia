using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DamageFlashFeedback : MonoBehaviour
{
    [Header("Renderer Discovery")]
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

    [Header("Defaults")]
    [SerializeField] private Color defaultFlashColor = Color.red;
    [SerializeField] private float defaultFlashDuration = 0.12f;

    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private Coroutine flashRoutine;
    private Coroutine blinkRoutine;

    private void Awake()
    {
        CacheRenderers();
        CaptureOriginalColors(true);
    }

    private void OnEnable()
    {
        CaptureOriginalColors(true);
    }

    public void Flash()
    {
        Flash(defaultFlashColor, defaultFlashDuration);
    }

    public void Flash(Color color, float duration)
    {
        if (spriteRenderers.Count == 0)
        {
            CacheRenderers();
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine(color, Mathf.Max(0.01f, duration)));
    }

    public void StartBlink(Color blinkColor, float totalDuration, float interval, bool useAlphaBlink = false)
    {
        if (spriteRenderers.Count == 0)
        {
            CacheRenderers();
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
        }

        blinkRoutine = StartCoroutine(BlinkRoutine(
            blinkColor,
            Mathf.Max(0.01f, totalDuration),
            Mathf.Max(0.01f, interval),
            useAlphaBlink));
    }

    public void StopBlinkAndRestore()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        RestoreOriginalColors();
    }

    private IEnumerator FlashRoutine(Color flashColor, float duration)
    {
        CaptureOriginalColors(false);
        ApplyColor(flashColor, false);
        yield return new WaitForSeconds(duration);
        RestoreOriginalColors();
        flashRoutine = null;
    }

    private IEnumerator BlinkRoutine(Color blinkColor, float totalDuration, float interval, bool useAlphaBlink)
    {
        CaptureOriginalColors(false);
        float endTime = Time.time + totalDuration;
        bool toggle = false;

        while (Time.time < endTime)
        {
            if (toggle)
            {
                RestoreOriginalColors();
            }
            else
            {
                ApplyColor(blinkColor, useAlphaBlink);
            }

            toggle = !toggle;
            yield return new WaitForSeconds(interval);
        }

        RestoreOriginalColors();
        blinkRoutine = null;
    }

    private void CacheRenderers()
    {
        spriteRenderers.Clear();
        if (includeChildren)
        {
            GetComponentsInChildren(true, spriteRenderers);
        }
        else
        {
            SpriteRenderer own = GetComponent<SpriteRenderer>();
            if (own != null)
            {
                spriteRenderers.Add(own);
            }
        }
    }

    private void CaptureOriginalColors(bool overwriteExisting)
    {
        if (overwriteExisting)
        {
            List<SpriteRenderer> keysToRemove = new List<SpriteRenderer>();
            foreach (KeyValuePair<SpriteRenderer, Color> pair in originalColors)
            {
                if (pair.Key == null || !spriteRenderers.Contains(pair.Key))
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                originalColors.Remove(keysToRemove[i]);
            }
        }

        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (overwriteExisting || !originalColors.ContainsKey(renderer))
            {
                originalColors[renderer] = renderer.color;
            }
        }
    }

    private void ApplyColor(Color color, bool useAlphaBlink)
    {
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (useAlphaBlink && originalColors.TryGetValue(renderer, out Color baseColor))
            {
                renderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, color.a);
            }
            else
            {
                renderer.color = color;
            }
        }
    }

    private void RestoreOriginalColors()
    {
        foreach (KeyValuePair<SpriteRenderer, Color> pair in originalColors)
        {
            if (pair.Key != null)
            {
                pair.Key.color = pair.Value;
            }
        }
    }
}
