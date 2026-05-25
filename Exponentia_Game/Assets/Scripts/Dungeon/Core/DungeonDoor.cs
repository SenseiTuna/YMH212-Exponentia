/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.1.0
FILE       : DungeonDoor.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DungeonDoor : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 1f); // Renk filtresini beyaz yaparak orijinal pixel art renklerini koruyoruz
    [SerializeField] private Color unlockedColor = new Color(1f, 1f, 1f, 0f); // Tamamen Şeffaf

    [Header("Animasyon Ayarları")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private float pulseSpeed = 5.0f;
    [SerializeField] private bool enablePulseEffect = false; // Pixel art kapıda titreme istemeyebiliriz, varsayılan kapalı yapalım

    [Header("Sprite Kare Animasyonu")]
    [Tooltip("Kapının açılış animasyon karelerini sırasıyla buraya sürükleyin (0: Tam Kapalı, 4: Tam Açık).")]
    [SerializeField] private List<Sprite> animationFrames = new List<Sprite>();

    private BoxCollider2D _collider;
    private Vector3 _originalScale;
    private bool _isLocked = false;
    private Coroutine _activeTransition;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _originalScale = transform.localScale;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Eğer hala SpriteRenderer yoksa, otomatik olarak ekleyelim ve temiz beyaz bir kare tanımlayalım
        if (spriteRenderer == null)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            spriteRenderer = visual.AddComponent<SpriteRenderer>();
            
            // 2x2 beyaz kare doku
            Texture2D tex = new Texture2D(2, 2);
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }

        // Eğer bir SpriteRenderer var ama sprite görseli atanmamışsa (None) ve animasyon kareleri de yoksa otomatik varsayılan bir beyaz kare ekle
        if (spriteRenderer != null && spriteRenderer.sprite == null && (animationFrames == null || animationFrames.Count == 0))
        {
            Texture2D tex = new Texture2D(2, 2);
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }

        // Başlangıçta kapı açık ve görünmez
        _collider.enabled = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = unlockedColor;
            // Katman (Sorting Order) sırasını 15 yaparak haritanın arkasında kalmasını engelliyoruz
            spriteRenderer.sortingOrder = 15;
            
            // Başlangıçta eğer animasyon karesi varsa son kareyi (tam açık kemer) göster ve rengi tam opak yap
            if (animationFrames != null && animationFrames.Count > 0)
            {
                spriteRenderer.sprite = animationFrames[animationFrames.Count - 1]; // Tam açık kemer
                spriteRenderer.color = Color.white; // Tam görünür taş rengi
            }
        }
        
        // Eğer görsel kare animasyonu kullanıyorsak scale'i sıfırlamaya gerek yok
        if (animationFrames == null || animationFrames.Count == 0)
        {
            transform.localScale = new Vector3(_originalScale.x, 0f, _originalScale.z);
        }
    }

    private void Update()
    {
        if (_isLocked && enablePulseEffect && spriteRenderer != null)
        {
            // Neon titreme / parıldama dalgası (Micro-animation)
            float lerpVal = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(0.55f, 0.95f, lerpVal);
            spriteRenderer.color = c;
        }
    }

    [ContextMenu("Lock Door")]
    public void Lock()
    {
        if (_isLocked) return;
        _isLocked = true;

        _collider.enabled = true;

        if (_activeTransition != null) StopCoroutine(_activeTransition);
        _activeTransition = StartCoroutine(AnimateDoor(true));
    }

    [ContextMenu("Unlock Door")]
    public void Unlock()
    {
        if (!_isLocked) return;
        _isLocked = false;

        _collider.enabled = false;

        if (_activeTransition != null) StopCoroutine(_activeTransition);
        _activeTransition = StartCoroutine(AnimateDoor(false));
    }

    private System.Collections.IEnumerator AnimateDoor(bool isLocking)
    {
        float elapsed = 0f;
        int frameCount = animationFrames != null ? animationFrames.Count : 0;

        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        // Eğer kare animasyonu varsa rengin hep opak (alpha=1) kalmasını istiyoruz, sadece içindeki parmaklık sprite'ı değişecek
        Color targetColor = isLocking ? lockedColor : (frameCount > 0 ? Color.white : unlockedColor);

        Vector3 startScale = transform.localScale;
        Vector3 targetScale = isLocking ? _originalScale : new Vector3(_originalScale.x, 0f, _originalScale.z);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / transitionDuration);
            
            // Yumuşak açılış/kapanış eğrisi (Smooth Step)
            float t = percent * percent * (3f - 2f * percent);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
                
                // EĞER kare animasyonu varsa, kareleri zamanla değiştirerek açılış/kapanış oynat!
                if (frameCount > 0)
                {
                    if (isLocking)
                    {
                        // KAPANIRKEN: Sondan başa doğru (Açık -> Kapalı)
                        int frameIndex = Mathf.Clamp(Mathf.FloorToInt((1f - t) * (frameCount - 1)), 0, frameCount - 1);
                        spriteRenderer.sprite = animationFrames[frameIndex];
                    }
                    else
                    {
                        // AÇILIRKEN: Baştan sona doğru (Kapalı -> Açık)
                        int frameIndex = Mathf.Clamp(Mathf.FloorToInt(t * (frameCount - 1)), 0, frameCount - 1);
                        spriteRenderer.sprite = animationFrames[frameIndex];
                    }
                }
            }

            // Kare animasyonu yoksa klasik scale (ölçek) animasyonunu kullan
            if (frameCount == 0)
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            }
            
            yield return null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetColor;
            if (frameCount > 0)
            {
                spriteRenderer.sprite = isLocking ? animationFrames[0] : animationFrames[frameCount - 1];
            }
        }
        
        if (frameCount == 0)
        {
            transform.localScale = targetScale;
        }

        // EĞER kare animasyonu YOKSA kilit açıldığında objeyi yok et (fallback modu için)
        // EĞER kare animasyonu VARSA objeyi yok etme, sahneden "açık kemer" olarak kalsın!
        if (!isLocking && frameCount == 0)
        {
            Destroy(gameObject);
        }
    }
}
