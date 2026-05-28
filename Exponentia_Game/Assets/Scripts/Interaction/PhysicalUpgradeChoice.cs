/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : PhysicalUpgradeChoice.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using UnityEngine;
using Exponentia.Data;
using Exponentia.Player;

namespace Exponentia.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class PhysicalUpgradeChoice : MonoBehaviour, IInteractable
    {
        [Header("Upgrade Configuration")]
        [SerializeField] private UpgradeData upgradeData;
        [SerializeField] private SpriteRenderer iconRenderer;
        
        [Header("Juice / Animations")]
        [SerializeField] private float bounceSpeed = 3.0f;
        [SerializeField] private float bounceHeight = 0.15f;

        private PhysicalChoiceGroup _parentGroup;
        private bool _isInteractable = true;
        private bool _isReadyToHover = false;
        private Vector3 _startPosition;
        private TextMesh _infoTextMesh;

        public UpgradeData UpgradeData => upgradeData;

        public void Initialize(UpgradeData data, PhysicalChoiceGroup group)
        {
            upgradeData = data;
            _parentGroup = group;
            _isReadyToHover = false;

            if (_parentGroup != null)
            {
                _parentGroup.RegisterChoice(this);
            }

            // Orijinal ikon görselini sprite renderer'a ata
            if (iconRenderer == null)
            {
                iconRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (iconRenderer != null && upgradeData != null && upgradeData.iconSprite != null)
            {
                iconRenderer.sprite = upgradeData.iconSprite;
            }

            // Collider2D'nin tetikleyici (trigger) olduğundan emin olalım
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Stat bilgi yazısını oluştur
            EnsureInfoText();
        }

        public void StartHovering(Vector3 floorPosition)
        {
            _startPosition = floorPosition;
            _isReadyToHover = true;
        }

        private void EnsureInfoText()
        {
            if (_infoTextMesh != null) return;

            GameObject textObj = new GameObject("UpgradeInfoText");
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = new Vector3(0f, 1.1f, 0f); // Objeden biraz yukarıda

            _infoTextMesh = textObj.AddComponent<TextMesh>();
            _infoTextMesh.anchor = TextAnchor.MiddleCenter;
            _infoTextMesh.alignment = TextAlignment.Center;
            _infoTextMesh.fontSize = 24;
            _infoTextMesh.characterSize = 0.1f;
            _infoTextMesh.color = Color.white;
            _infoTextMesh.richText = true;

            MeshRenderer textRenderer = _infoTextMesh.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingOrder = 20; // En üstte net gözükmesi için
            }

            _infoTextMesh.text = BuildStatBonusText();
        }

        private string BuildStatBonusText()
        {
            if (upgradeData == null) return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // İlk satır: Güçlendirme İsmi (Gold/Yellow)
            sb.AppendLine($"<color=#FFD54F><b>{upgradeData.displayName}</b></color>");

            // İkinci satır: Stat Değişimi (Soft Green)
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

        private void Update()
        {
            // Hades/Dead Cells tarzı hafif havada asılı kalma / dalgalanma efekti (Micro-animation)
            if (_isInteractable && _isReadyToHover)
            {
                float newY = _startPosition.y + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
                transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
            }
        }

        public Vector3 GetInteractionPoint()
        {
            return transform.position;
        }

        public string GetInteractionLabel()
        {
            if (upgradeData == null) return "Güçlendirme Al";
            return $"[E] {upgradeData.displayName}\n<size=80%>{upgradeData.description}</size>";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (!_isInteractable || interactor == null) return false;

            // Oyuncuda PlayerStats bulunup bulunmadığını kontrol edelim
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
                // Kalıcı stat güçlendirmelerini uygulayalım
                stats.MaxHealth += upgradeData.maxHealthBonus;
                // Can artırıldığında mevcut canı da o kadar iyileştirelim
                if (upgradeData.maxHealthBonus > 0f)
                {
                    stats.CurrentHealth += upgradeData.maxHealthBonus;
                }
                
                stats.Damage += upgradeData.damageBonus;
                stats.MoveSpeed += upgradeData.moveSpeedBonus;
                stats.AttackSpeed += upgradeData.attackSpeedBonus;
                stats.Defense += upgradeData.defenseBonus;

                Debug.Log($"[Upgrade] '{upgradeData.displayName}' kalıcı güçlendirmesi uygulandı!");
            }

            // Gruba bu seçimin yapıldığını bildir, diğer 2 seçimi silsin
            if (_parentGroup != null)
            {
                _parentGroup.MakeChoice(this);
            }

            // Kendini yok etmeden önce şık bir kaybolma efekti oynat
            DisableInteraction();
            StartCoroutine(AnimatePickupAndDestroy());
        }

        public void DisableInteraction()
        {
            _isInteractable = false;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        public void DestroyChoiceVisual()
        {
            DisableInteraction();
            // Diğer seçilmeyen objelerin toz olup yok olması (Dead Cells tarzı pürüzsüz sönme)
            StartCoroutine(AnimateFadeOutAndDestroy());
        }

        private System.Collections.IEnumerator AnimatePickupAndDestroy()
        {
            float elapsed = 0f;
            float duration = 0.45f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = startScale * 1.4f; // Hafif büyüyerek patlama hissi

            Color startColor = iconRenderer != null ? iconRenderer.color : Color.white;
            Color textStartColor = _infoTextMesh != null ? _infoTextMesh.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;
                float t = percent * percent; // Hızlanan eğri

                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                
                if (iconRenderer != null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, t);
                    iconRenderer.color = c;
                }

                if (_infoTextMesh != null)
                {
                    Color tc = textStartColor;
                    tc.a = Mathf.Lerp(textStartColor.a, 0f, t);
                    _infoTextMesh.color = tc;
                }

                // Havaya doğru yükselme animasyonu
                transform.position += Vector3.up * (Time.deltaTime * 1.5f);

                yield return null;
            }

            Destroy(gameObject);
        }

        private System.Collections.IEnumerator AnimateFadeOutAndDestroy()
        {
            float elapsed = 0f;
            float duration = 0.35f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = Vector3.zero; // Küçülerek yok olma

            Color startColor = iconRenderer != null ? iconRenderer.color : Color.white;
            Color textStartColor = _infoTextMesh != null ? _infoTextMesh.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;
                float t = percent * percent;

                transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                if (iconRenderer != null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, t);
                    iconRenderer.color = c;
                }

                if (_infoTextMesh != null)
                {
                    Color tc = textStartColor;
                    tc.a = Mathf.Lerp(textStartColor.a, 0f, t);
                    _infoTextMesh.color = tc;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
