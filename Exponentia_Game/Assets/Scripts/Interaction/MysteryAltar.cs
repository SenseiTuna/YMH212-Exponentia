/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : MysteryAltar.cs
BUILD_DATE : 2026-05-26
====================================================
*/

using UnityEngine;
using UnityEngine.UI;
using Exponentia.Player;

namespace Exponentia.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class MysteryAltar : MonoBehaviour, IInteractable
    {
        [Header("Mystery Settings")]
        [SerializeField] private string altarDisplayName = "Lanetli Altar";
        [SerializeField] [TextArea(3, 5)] private string mysteryDescription = "Karanlık bir enerji etrafını sarıyor. Kadim Altar seninle konuşuyor:\n\n'Mevcut yaşam gücünün (Can) %30'unu bana feda et, karşılığında sana saf yıkım gücü (Hasar) vereyim.'";
        
        [Header("Sacrifice Outcome")]
        [SerializeField] private float hpSacrificePercentage = 0.3f; // %30 feda
        [SerializeField] private float damageReward = 5f; // +5 Hasar ödülü

        [Header("Altar Visuals")]
        [SerializeField] private SpriteRenderer altarRenderer;
        [SerializeField] private Color inactiveColor = Color.gray;

        private bool _isUsed = false;
        private GameObject _eventCanvasInstance;
        private Button _btnOptionA;
        private Button _btnOptionB;
        private PlayerStats _activePlayerStats;

        private void Awake()
        {
            if (altarRenderer == null)
            {
                altarRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            // Sahnede önceden kurulan EventUI_Canvas'ı bulmaya çalış
            GameObject canvasObj = GameObject.Find("EventUI_Canvas");
            if (canvasObj != null)
            {
                _eventCanvasInstance = canvasObj;
                
                // Çocuk butonları ve yapıyı otomatik çözümle
                Transform panel = _eventCanvasInstance.transform.Find("Event_Panel");
                if (panel != null)
                {
                    Transform btnAObj = panel.Find("OptionA_Button");
                    Transform btnBObj = panel.Find("OptionB_Button");

                    if (btnAObj != null) _btnOptionA = btnAObj.GetComponent<Button>();
                    if (btnBObj != null) _btnOptionB = btnBObj.GetComponent<Button>();
                }
            }
        }

        public Vector3 GetInteractionPoint()
        {
            return transform.position;
        }

        public string GetInteractionLabel()
        {
            if (_isUsed) return "Kullanılmış Altar";
            return $"[E] {altarDisplayName}\n<size=80%>Meydan Oku / Kaderini Seç</size>";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (_isUsed || interactor == null) return false;

            PlayerStats stats = interactor.GetComponent<PlayerStats>();
            if (stats == null)
            {
                stats = interactor.GetComponentInChildren<PlayerStats>();
            }

            return stats != null && _eventCanvasInstance != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            _activePlayerStats = interactor.GetComponent<PlayerStats>();
            if (_activePlayerStats == null)
            {
                _activePlayerStats = interactor.GetComponentInChildren<PlayerStats>();
            }

            if (_activePlayerStats != null && _eventCanvasInstance != null)
            {
                // UI Paneli aktifleştir ve metinleri doldur
                OpenEventUI();
            }
        }

        private void OpenEventUI()
        {
            _eventCanvasInstance.SetActive(true);

            // Metin alanını bulup doldur
            Transform panel = _eventCanvasInstance.transform.Find("Event_Panel");
            if (panel != null)
            {
                Text descText = panel.GetComponentInChildren<Text>();
                if (descText != null)
                {
                    descText.text = mysteryDescription;
                }
            }

            // Buton dinleyicilerini sıfırla ve yeni olayları kod içinden pürüzsüzce bağla
            if (_btnOptionA != null)
            {
                _btnOptionA.onClick.RemoveAllListeners();
                _btnOptionA.onClick.AddListener(OnChooseSacrifice);
                
                // Seçenek A etiketini doldur
                Text btnAText = _btnOptionA.GetComponentInChildren<Text>();
                if (btnAText != null)
                {
                    btnAText.text = $"%{(hpSacrificePercentage * 100f):F0} Can Feda Et (+{damageReward} Hasar)";
                }
            }

            if (_btnOptionB != null)
            {
                _btnOptionB.onClick.RemoveAllListeners();
                _btnOptionB.onClick.AddListener(OnChooseLeave);

                // Seçenek B etiketini doldur
                Text btnBText = _btnOptionB.GetComponentInChildren<Text>();
                if (btnBText != null)
                {
                    btnBText.text = "Altarı Görmezden Gel ve Ayrıl";
                }
            }

            // Oyuncu hareketini/kontrollerini isterseniz dondurabilirsiniz (Burada basitlik için sadece UI odaklanması sağlanıyor)
            Time.timeScale = 0f; // Seçim anında oyunu durdur (Klasik Roguelite tarzı)
        }

        private void CloseEventUI()
        {
            Time.timeScale = 1f; // Zamanı geri al
            if (_eventCanvasInstance != null)
            {
                _eventCanvasInstance.SetActive(false);
            }
        }

        private void OnChooseSacrifice()
        {
            if (_activePlayerStats != null)
            {
                _isUsed = true;

                // 1. Cezayı uygula: Mevcut canın %30'unu düş (Mevcut canı 1'in altına indirme ki feda yüzünden direkt ölmesin)
                float hpLoss = _activePlayerStats.CurrentHealth * hpSacrificePercentage;
                _activePlayerStats.CurrentHealth = Mathf.Max(1f, _activePlayerStats.CurrentHealth - hpLoss);

                // 2. Ödülü uygula: Kalıcı hasar artışı
                _activePlayerStats.Damage += damageReward;

                Debug.Log($"[MysteryEvent] Lanetli Altar fedakarlığı kabul etti! Oyuncu can feda etti: {hpLoss}, Kalıcı Hasar Artışı: {damageReward}");

                // Floating text bildirimi (Altın / Kırmızı karışımı feedback)
                FloatingCombatText.Create($"-{Mathf.CeilToInt(hpLoss)} Can / +{damageReward} Hasar!", transform.position + Vector3.up * 1.5f, Color.red);

                // Altarı görsel olarak etkisizleştir/karart
                if (altarRenderer != null)
                {
                    altarRenderer.color = inactiveColor;
                }
            }

            CloseEventUI();
        }

        private void OnChooseLeave()
        {
            Debug.Log("[MysteryEvent] Oyuncu altarın teklifini reddetti ve odadan ayrılmayı seçti.");
            CloseEventUI();
        }
    }
}
