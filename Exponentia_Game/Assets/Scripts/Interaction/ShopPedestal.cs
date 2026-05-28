/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : ShopPedestal.cs
BUILD_DATE : 2026-05-26
====================================================
*/

using UnityEngine;
using Exponentia.Data;
using Exponentia.Player;

namespace Exponentia.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class ShopPedestal : MonoBehaviour, IInteractable
    {
        [Header("Upgrade Configuration")]
        [SerializeField] private UpgradeData upgradeData;
        [SerializeField] private int price = 50;
        [SerializeField] private SpriteRenderer iconRenderer;

        [Header("Juice & Bobbing")]
        [SerializeField] private float bounceSpeed = 3.0f;
        [SerializeField] private float bounceHeight = 0.12f;

        private TextMesh _priceTextMesh;
        private Vector3 _startPosition;
        private bool _isPurchased = false;
        private Vector3 _itemStartPosition;
        private Transform _itemVisualGroup;

        private void SelfHeal()
        {
            if (upgradeData == null)
            {
                upgradeData = ScriptableObject.CreateInstance<UpgradeData>();
                upgradeData.upgradeId = "shop_hp_temp";
                upgradeData.displayName = "Can İksiri";
                upgradeData.description = "Maksimum caninizi kalici olarak 15 artirir.";
                upgradeData.maxHealthBonus = 15f;

#if UNITY_EDITOR
                upgradeData.iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Circle.png");
#endif
            }
        }

        public void Initialize(UpgradeData data, int priceVal)
        {
            upgradeData = data;
            price = priceVal;
            _isPurchased = false;

            // Eksik veri varsa mock veri oluştur (Kendi kendini iyileştirme)
            SelfHeal();

            // Görsel grubunu bul veya oluştur
            _itemVisualGroup = transform.Find("ItemVisual");
            if (_itemVisualGroup == null)
            {
                GameObject visualGroupObj = new GameObject("ItemVisual");
                visualGroupObj.transform.SetParent(transform, false);
                visualGroupObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                _itemVisualGroup = visualGroupObj.transform;
            }

            _itemStartPosition = _itemVisualGroup.localPosition;

            // İkon renderer'ı ayarla
            if (iconRenderer == null)
            {
                iconRenderer = _itemVisualGroup.GetComponent<SpriteRenderer>();
                if (iconRenderer == null)
                {
                    iconRenderer = _itemVisualGroup.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (iconRenderer != null && upgradeData != null && upgradeData.iconSprite != null)
            {
                iconRenderer.sprite = upgradeData.iconSprite;
                iconRenderer.sortingOrder = 18;
            }

            // Değerli eşyaların arkasına şık parlayan aura ekle
            EnsureAura();

            // Çift Collider Kurulumu (Katı Engel + Geniş Görünmez Tetikleyici)
            EnsureColliders();

            // Fiyat ve stat bilgi etiketini oluştur
            EnsurePriceText();
        }

        private void Start()
        {
            _startPosition = transform.position;
            
            // Kendi kendini iyileştirme (Self-Healing) - RAM'de kaybolan veya atanmamış verileri kur
            SelfHeal();

            // Görsel grubunu bul veya oluştur
            _itemVisualGroup = transform.Find("ItemVisual");
            if (_itemVisualGroup == null)
            {
                GameObject visualGroupObj = new GameObject("ItemVisual");
                visualGroupObj.transform.SetParent(transform, false);
                visualGroupObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                _itemVisualGroup = visualGroupObj.transform;
            }

            _itemStartPosition = _itemVisualGroup.localPosition;

            // İkon renderer bileşenini bul veya ekle
            if (iconRenderer == null)
            {
                iconRenderer = _itemVisualGroup.GetComponent<SpriteRenderer>();
                if (iconRenderer == null)
                {
                    iconRenderer = _itemVisualGroup.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (iconRenderer != null && upgradeData != null && upgradeData.iconSprite != null)
            {
                iconRenderer.sprite = upgradeData.iconSprite;
                iconRenderer.sortingOrder = 18;
            }

            // Değerli eşyaların arkasına şık parlayan aura ekle
            EnsureAura();

            // Çift Collider Kurulumu (Katı Engel + Geniş Görünmez Tetikleyici)
            EnsureColliders();

            // Fiyat ve stat etiketini kur
            EnsurePriceText();
        }

        private void Update()
        {
            if (_isPurchased) return;

            // Eşyayı kaide üzerinde yavaşça dalgalandır (bobbing)
            if (_itemVisualGroup != null)
            {
                float newY = _itemStartPosition.y + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
                _itemVisualGroup.localPosition = new Vector3(_itemStartPosition.x, newY, _itemStartPosition.z);

                // Aura nabız (heartbeat pulse) ve yavaş dönüş animasyonu
                Transform aura = _itemVisualGroup.Find("AuraGlow");
                if (aura != null)
                {
                    float pulse = 1.25f + Mathf.Sin(Time.time * 4.0f) * 0.12f;
                    aura.localScale = new Vector3(pulse, pulse, 1f);
                    aura.Rotate(Vector3.forward * (Time.deltaTime * 25.0f));
                }
            }
        }

        public Vector3 GetInteractionPoint()
        {
            return transform.position;
        }

        public string GetInteractionLabel()
        {
            if (upgradeData == null) return "Eşya Satın Al";
            return $"[E] Satın Al: {upgradeData.displayName}\n<size=80%>{price} Altın</size>";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (_isPurchased || interactor == null) return false;

            PlayerStats stats = interactor.GetComponent<PlayerStats>();
            if (stats == null)
            {
                stats = interactor.GetComponentInChildren<PlayerStats>();
            }

            return stats != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            PlayerStats stats = interactor.GetComponent<PlayerStats>();
            if (stats == null)
            {
                stats = interactor.GetComponentInChildren<PlayerStats>();
            }

            if (stats != null && upgradeData != null)
            {
                // Altın kontrolü
                if (stats.Gold >= price)
                {
                    _isPurchased = true;
                    stats.Gold -= price;

                    // Kalıcı statları uygula
                    stats.MaxHealth += upgradeData.maxHealthBonus;
                    if (upgradeData.maxHealthBonus > 0f)
                    {
                        stats.CurrentHealth += upgradeData.maxHealthBonus;
                    }
                    stats.Damage += upgradeData.damageBonus;
                    stats.MoveSpeed += upgradeData.moveSpeedBonus;
                    stats.AttackSpeed += upgradeData.attackSpeedBonus;
                    stats.Defense += upgradeData.defenseBonus;

                    Debug.Log($"[Shop] '{upgradeData.displayName}' {price} Altın karşılığında satın alındı!");

                    // Floating text bildirimi (Yeşil)
                    FloatingCombatText.Create("Satın Alındı!", transform.position + Vector3.up * 1.5f, Color.green);

                    // Pürüzsüz kaybolma animasyonu
                    StartCoroutine(AnimatePurchaseAndDestroy());
                }
                else
                {
                    Debug.LogWarning($"[Shop] Yetersiz Altın! Gereken: {price}, Oyuncuda Olan: {stats.Gold}");
                    
                    // Floating text bildirimi (Kırmızı)
                    FloatingCombatText.Create("Yetersiz Altın!", transform.position + Vector3.up * 1.5f, Color.red);
                }
            }
        }

        private void EnsurePriceText()
        {
            if (_priceTextMesh != null) return;

            Transform existingText = transform.Find("ShopPriceText");
            GameObject textObj;
            if (existingText != null)
            {
                textObj = existingText.gameObject;
            }
            else
            {
                textObj = new GameObject("ShopPriceText");
                textObj.transform.SetParent(transform, false);
                textObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            }

            _priceTextMesh = textObj.GetComponent<TextMesh>();
            if (_priceTextMesh == null)
            {
                _priceTextMesh = textObj.AddComponent<TextMesh>();
            }

            _priceTextMesh.anchor = TextAnchor.MiddleCenter;
            _priceTextMesh.alignment = TextAlignment.Center;
            _priceTextMesh.fontSize = 24;
            _priceTextMesh.characterSize = 0.08f;
            _priceTextMesh.color = Color.white;
            _priceTextMesh.richText = true;

            MeshRenderer textRenderer = _priceTextMesh.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingOrder = 20;
            }

            _priceTextMesh.text = BuildPriceLabel();
        }

        private void EnsureAura()
        {
            if (_itemVisualGroup == null) return;

            Transform auraTrans = _itemVisualGroup.Find("AuraGlow");
            if (auraTrans == null)
            {
                GameObject auraObj = new GameObject("AuraGlow");
                auraObj.transform.SetParent(_itemVisualGroup, false);
                auraObj.transform.localPosition = Vector3.zero;
                auraObj.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

                SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();

                // Projedeki Circle.png sprite'ını yükle, editördeyse AssetDatabase kullan
                Sprite circleSprite = null;
#if UNITY_EDITOR
                circleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Circle.png");
#endif
                if (circleSprite == null && upgradeData != null)
                {
                    circleSprite = upgradeData.iconSprite;
                }

                auraSr.sprite = circleSprite;
                auraSr.sortingOrder = 17; // İkonun (18) hemen arkasında

                // Premium altın sarısı şeffaf neon ışığı aurası
                auraSr.color = new Color(1.0f, 0.78f, 0.2f, 0.24f);
            }
        }

        private void EnsureColliders()
        {
            // 1. Ana collider'ı katı engel (solid) yap
            Collider2D mainCol = GetComponent<Collider2D>();
            if (mainCol == null)
            {
                mainCol = gameObject.AddComponent<BoxCollider2D>();
            }
            mainCol.isTrigger = false; // Katı engel! Oyuncu içinden geçemez.
            
            // Eğer BoxCollider2D ise, boyutunu taş kaideye uygun şekilde ayarlayalım
            if (mainCol is BoxCollider2D boxCol)
            {
                boxCol.size = new Vector2(0.9f, 0.9f);
                boxCol.offset = Vector2.zero;
            }

            // 2. Çevreleyen görünmez tetikleyici (Trigger) alanını çocuk obje olarak otomatik kur
            Transform triggerTrans = transform.Find("InteractionTrigger");
            GameObject triggerObj;
            if (triggerTrans != null)
            {
                triggerObj = triggerTrans.gameObject;
            }
            else
            {
                triggerObj = new GameObject("InteractionTrigger");
                triggerObj.transform.SetParent(transform, false);
                triggerObj.transform.localPosition = Vector3.zero;
            }

            // Çocuk objenin katmanını ebeveynle aynı yap ki PlayerInteractor katman filtrelemesinden geçsin
            triggerObj.layer = gameObject.layer;

            BoxCollider2D triggerCol = triggerObj.GetComponent<BoxCollider2D>();
            if (triggerCol == null)
            {
                triggerCol = triggerObj.AddComponent<BoxCollider2D>();
            }
            triggerCol.isTrigger = true; // Görünmez tetikleyici!
            triggerCol.size = new Vector2(1.8f, 1.8f); // Daha geniş etkileşim alanı
            triggerCol.offset = Vector2.zero;
        }

        private string BuildPriceLabel()
        {
            if (upgradeData == null) return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // Üst Satır: Eşya İsmi (Sarı)
            sb.AppendLine($"<color=#FFD54F><b>{upgradeData.displayName}</b></color>");

            // Orta Satır: Fiyat (Turuncu)
            sb.AppendLine($"<color=#FF8A65>{price} Altın</color>");

            // Alt Satır: Kazandırdığı Statlar (Soft Yeşil)
            sb.Append("<color=#A5D6A7>");
            if (upgradeData.maxHealthBonus > 0f)
                sb.Append($"+{upgradeData.maxHealthBonus} Maks Can");
            else if (upgradeData.damageBonus > 0f)
                sb.Append($"+{upgradeData.damageBonus} Hasar");
            else if (upgradeData.moveSpeedBonus > 0f)
                sb.Append($"+{upgradeData.moveSpeedBonus} Hız");
            else if (upgradeData.attackSpeedBonus > 0f)
                sb.Append($"+{upgradeData.attackSpeedBonus * 100f:F0}% Sal. Hızı");
            else if (upgradeData.defenseBonus > 0f)
                sb.Append($"+{upgradeData.defenseBonus} Savunma");
            sb.Append("</color>");

            return sb.ToString();
        }

        private System.Collections.IEnumerator AnimatePurchaseAndDestroy()
        {
            float elapsed = 0f;
            float duration = 0.4f;
            Vector3 startScale = _itemVisualGroup != null ? _itemVisualGroup.localScale : Vector3.one;

            Color textStartColor = _priceTextMesh != null ? _priceTextMesh.color : Color.white;
            Color iconStartColor = iconRenderer != null ? iconRenderer.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scaleT = t * t; // Hızlanan küçülme

                if (_itemVisualGroup != null)
                {
                    _itemVisualGroup.localScale = Vector3.Lerp(startScale, Vector3.zero, scaleT);
                    _itemVisualGroup.localPosition += Vector3.up * (Time.deltaTime * 1.5f);
                }

                if (_priceTextMesh != null)
                {
                    Color tc = textStartColor;
                    tc.a = Mathf.Lerp(textStartColor.a, 0f, scaleT);
                    _priceTextMesh.color = tc;
                }

                if (iconRenderer != null)
                {
                    Color ic = iconStartColor;
                    ic.a = Mathf.Lerp(iconStartColor.a, 0f, scaleT);
                    iconRenderer.color = ic;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
