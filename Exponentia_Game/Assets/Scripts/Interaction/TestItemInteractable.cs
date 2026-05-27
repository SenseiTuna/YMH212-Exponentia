using UnityEngine;
using Exponentia.UI;

namespace Exponentia.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class TestItemInteractable : MonoBehaviour, IInteractable
    {
        [Header("Visual & Juice")]
        [SerializeField] private Color itemColor = new Color(1f, 0.78f, 0.2f, 1f); // Amber / Altın rengi
        [SerializeField] private float bounceSpeed = 2.5f;
        [SerializeField] private float bounceHeight = 0.15f;

        private Vector3 _startPosition;
        private SpriteRenderer _spriteRenderer;
        private Transform _auraTransform;

        private void Awake()
        {
            _startPosition = transform.position;

            // SpriteRenderer kur (yoksa ekle)
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

#if UNITY_EDITOR
            _spriteRenderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Circle.png");
#endif
            _spriteRenderer.color = itemColor;
            _spriteRenderer.sortingOrder = 10;

            // Collider2D tetikleyici ayarı
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Çarpıcı Parlama Aurası
            EnsureAura();
        }

        private void Update()
        {
            // Hafif dikey dalgalanma animasyonu (bobbing)
            float newY = _startPosition.y + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);

            // Aura animasyonu
            if (_auraTransform != null)
            {
                float pulse = 1.3f + Mathf.Sin(Time.time * 4f) * 0.15f;
                _auraTransform.localScale = new Vector3(pulse, pulse, 1f);
                _auraTransform.Rotate(Vector3.forward * (Time.deltaTime * 30f));
            }
        }

        private void EnsureAura()
        {
            _auraTransform = transform.Find("TestItemAura");
            if (_auraTransform == null)
            {
                GameObject auraObj = new GameObject("TestItemAura");
                auraObj.transform.SetParent(transform, false);
                auraObj.transform.localPosition = Vector3.zero;
                auraObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

                SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
                auraSr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Circle.png");
#endif
                auraSr.sortingOrder = 9; // İkonun hemen arkasında
                auraSr.color = new Color(itemColor.r, itemColor.g, itemColor.b, 0.28f); // Yarı şeffaf altın sarısı aura
                _auraTransform = auraObj.transform;
            }
        }

        public Vector3 GetInteractionPoint()
        {
            return transform.position;
        }

        public string GetInteractionLabel()
        {
            return "[E] Gizemli Sandık / Envanteri Göster";
        }

        public bool CanInteract(GameObject interactor)
        {
            return interactor != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            // UI'daki akıllı envanter kontrolcüsünü bul ve envanteri aç/kapat
            InventoryHUDController inventoryUI = Object.FindAnyObjectByType<InventoryHUDController>();
            if (inventoryUI != null)
            {
                bool newState = !inventoryUI.IsLeftPanelOpen;
                inventoryUI.ToggleLeftInventory(newState); // Envanteri aç/kapat
                string msg = newState ? "Envanter Açıldı!" : "Envanter Kapatıldı!";
                FloatingCombatText.Create(msg, transform.position + Vector3.up * 1.2f, Color.yellow);
                Debug.Log("[TestItem] Oyuncu gizemli sandık ile etkileşime girdi. Envanter HUD durumu: " + newState);
            }
            else
            {
                Debug.LogWarning("[TestItem] Sahnede aktif InventoryHUDController bulunamadı!");
            }
        }
    }
}
