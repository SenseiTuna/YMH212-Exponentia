using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Exponentia.Player;
using Exponentia.InventorySystem;

namespace Exponentia.UI
{
    [DisallowMultipleComponent]
    public class InventoryHUDController : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("General Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private float slideSpeed = 10f;

        private GameObject _canvasRoot;
        private GameObject _inventoryHUDPanel;
        
        // Left Panel (Hotbar Panel - dikey, sol kenarda)
        private RectTransform _leftTabPanel;
        private Image[] _leftSlotsIcons = new Image[5];
        private Image[] _leftSlotsCDOverlays = new Image[5];
        private TextMeshProUGUI[] _leftSlotsCDTexts = new TextMeshProUGUI[5];
        private GameObject[] _leftSlotGameObjects = new GameObject[5];
        private bool _isLeftPanelOpen = false;
        private float _targetLeftPanelX = 20f; 
        private float _currentLeftPanelX = -120f;

        // Bottom Panel (Inventory Panel - yatay, alt ortada)
        private RectTransform _bottomHotbarPanel;
        private Slider _verticalHPSlider; // Can barı
        private Image[] _bottomSlotsIcons = new Image[5];
        private Image[] _bottomSlotsCDOverlays = new Image[5];
        private TextMeshProUGUI[] _bottomSlotsCDTexts = new TextMeshProUGUI[5];
        private GameObject[] _bottomSlotGameObjects = new GameObject[5];
        private bool _isBottomHotbarOpen = false; // Başlangıçta kapalı
        private float _targetBottomPanelY = -320f; // Başlangıçta ekranın altında dışarıda
        private float _currentBottomPanelY = -320f;

        private float _updateInterval = 0.05f;
        private float _nextUpdateTime;

        private void Awake()
        {
            FindPlayerReferences();
            CreateDynamicUI();
        }

        private void Start()
        {
            FindPlayerReferences();
            RefreshHUD();

            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= RefreshHUD;
                playerInventory.OnInventoryChanged += RefreshHUD;
            }
        }

        private void OnDestroy()
        {
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= RefreshHUD;
            }
        }

        private void Update()
        {
            // Tab tuşuyla tüm envanter ekranını (sol panel + alt hotbar) aç/kapat ve oyunu duraklat
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleInventoryScreen(!_isBottomHotbarOpen);
            }

            // Sol panel (Hotbar) her zaman açık kalacak (Gizlenmeyecek, hep gözükecek)
            _targetLeftPanelX = 20f;
            _currentLeftPanelX = Mathf.Lerp(_currentLeftPanelX, _targetLeftPanelX, Time.unscaledDeltaTime * slideSpeed);
            if (_leftTabPanel != null)
            {
                _leftTabPanel.anchoredPosition = new Vector2(_currentLeftPanelX, _leftTabPanel.anchoredPosition.y);
                _leftTabPanel.sizeDelta = new Vector2(_leftTabPanel.sizeDelta.x, 460f);
            }

            // Alt panelin (Inventory) pürüzsüz yukarı/aşağı kayma animasyonu (Slide up/down) - Time.unscaledDeltaTime ile
            _targetBottomPanelY = _isBottomHotbarOpen ? 30f : -320f;
            _currentBottomPanelY = Mathf.Lerp(_currentBottomPanelY, _targetBottomPanelY, Time.unscaledDeltaTime * slideSpeed);
            if (_bottomHotbarPanel != null)
            {
                _bottomHotbarPanel.anchoredPosition = new Vector2(_bottomHotbarPanel.anchoredPosition.x, _currentBottomPanelY);
                _bottomHotbarPanel.sizeDelta = new Vector2(_bottomHotbarPanel.sizeDelta.x, 160f);

                // Tüm yuvaları aktif tutuyoruz çünkü yatay envanterde 5 kutu olarak gözükmeli
                for (int i = 0; i < 5; i++)
                {
                    if (i < _bottomSlotGameObjects.Length && _bottomSlotGameObjects[i] != null)
                    {
                        if (!_bottomSlotGameObjects[i].activeSelf)
                        {
                            _bottomSlotGameObjects[i].SetActive(true);
                        }
                    }
                }
            }

            // Dinamik verileri güncelle (Zaman durakladığında bile çalışması için unscaledTime kullanıyoruz)
            if (Time.unscaledTime >= _nextUpdateTime)
            {
                _nextUpdateTime = Time.unscaledTime + _updateInterval;
                UpdateRealtimeData();
            }
        }

        public bool IsLeftPanelOpen => _isLeftPanelOpen;
        public bool IsBottomHotbarOpen => _isBottomHotbarOpen;

        public void ToggleLeftInventory(bool isOpen)
        {
            ToggleInventoryScreen(isOpen);
        }

        public void ToggleBottomHotbar(bool isOpen)
        {
            _isBottomHotbarOpen = isOpen;
            if (_isBottomHotbarOpen)
            {
                RefreshHUD();
            }
        }

        public void ToggleInventoryScreen(bool isOpen)
        {
            _isLeftPanelOpen = isOpen;
            _isBottomHotbarOpen = isOpen;
            
            // Zamanı duraklat / geri al (Roguelite tarzı envanter yönetim modu)
            Time.timeScale = isOpen ? 0f : 1f;

            if (isOpen)
            {
                RefreshHUD();
            }
        }

        private void FindPlayerReferences()
        {
            if (!autoFindPlayer) return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
                if (playerInventory == null)
                {
                    playerInventory = player.GetComponent<PlayerInventory>();
                    if (playerInventory != null)
                    {
                        playerInventory.OnInventoryChanged -= RefreshHUD;
                        playerInventory.OnInventoryChanged += RefreshHUD;
                    }
                }
                if (playerAttack == null) playerAttack = player.GetComponent<PlayerAttack>();
            }
        }

        private void CreateDynamicUI()
        {
            _canvasRoot = GameObject.Find("Canvas");
            if (_canvasRoot == null)
            {
                _canvasRoot = FindFirstObjectByType<Canvas>()?.gameObject;
            }

            if (_canvasRoot == null)
            {
                Debug.LogError("[InventoryHUD] Sahnede Canvas bulunamadı! Dinamik arayüz oluşturulamıyor.");
                return;
            }

            // Ana HUD Boş Objesi
            _inventoryHUDPanel = new GameObject("InventoryHUDPanel");
            _inventoryHUDPanel.transform.SetParent(_canvasRoot.transform, false);
            RectTransform hudRect = _inventoryHUDPanel.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;
            hudRect.anchoredPosition = Vector2.zero;

            // 1. SOL DIKEY PANEL (HOTBAR)
            CreateLeftTabPanel();

            // 2. ALT ORTA PANEL (ENWANTER)
            CreateBottomHotbarPanel();
        }

        private void CreateLeftTabPanel()
        {
            GameObject leftPanelObj = new GameObject("LeftTabPanel");
            leftPanelObj.transform.SetParent(_inventoryHUDPanel.transform, false);
            _leftTabPanel = leftPanelObj.AddComponent<RectTransform>();
            
            // Anchor: Sol-Orta
            _leftTabPanel.anchorMin = new Vector2(0f, 0.5f);
            _leftTabPanel.anchorMax = new Vector2(0f, 0.5f);
            _leftTabPanel.pivot = new Vector2(0f, 0.5f);
            _leftTabPanel.sizeDelta = new Vector2(85f, 460f);
            _leftTabPanel.anchoredPosition = new Vector2(_currentLeftPanelX, 0f);

            // Premium Glassmorphic Arka Plan
            Image bgImage = leftPanelObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.11f, 0.94f);
            
            // Kenarlık (Sarı/Altın Neon Işığı)
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(leftPanelObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(4f, 4f);
            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.color = new Color(1f, 0.78f, 0.2f, 0.65f); // Neon Gold

            // Dikey Düzen Grubu (Vertical Layout Group)
            VerticalLayoutGroup vLayout = leftPanelObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(10, 10, 15, 15);
            vLayout.spacing = 15f;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = false;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = false;

            // 5 DIKEY HOTBAR YUVASI
            for (int i = 0; i < 5; i++)
            {
                string slotName = i == 0 ? "WeaponSlot" : (i == 1 ? "ActiveItemSlot" : $"HotbarPassiveSlot_{i}");
                GameObject slotObj = new GameObject(slotName);
                slotObj.transform.SetParent(leftPanelObj.transform, false);
                RectTransform slotRect = slotObj.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(65f, 65f);

                HUDSlot slotComp = slotObj.AddComponent<HUDSlot>();
                slotComp.panelType = PanelType.Left;
                slotComp.slotIndex = i;
                slotComp.hudController = this;

                // Yuva Arka Planı (Karanlık roguelite karesi)
                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.color = new Color(0.04f, 0.04f, 0.06f, 0.85f);
                
                // Kenarlık
                GameObject slotBorder = new GameObject("Border");
                slotBorder.transform.SetParent(slotObj.transform, false);
                RectTransform sBorderRect = slotBorder.AddComponent<RectTransform>();
                sBorderRect.anchorMin = Vector2.zero;
                sBorderRect.anchorMax = Vector2.one;
                sBorderRect.sizeDelta = new Vector2(2f, 2f);
                Image sBorderImg = slotBorder.AddComponent<Image>();
                
                // Aktif slotlara neon sarı, pasiflere hafif çelik mavi
                sBorderImg.color = (i == 0 || i == 1) ? new Color(1f, 0.78f, 0.2f, 0.7f) : new Color(0.2f, 0.2f, 0.25f, 0.5f);

                // İkon Kutusu
                GameObject iconBox = new GameObject("Icon");
                iconBox.transform.SetParent(slotObj.transform, false);
                RectTransform iconRect = iconBox.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-10f, -10f); // Kenarlardan 5px içeride
                
                _leftSlotsIcons[i] = iconBox.AddComponent<Image>();
                _leftSlotsIcons[i].enabled = false;

                // Cooldown Overlay (Dairesel Dolgu)
                GameObject cdOverlay = new GameObject("CooldownOverlay");
                cdOverlay.transform.SetParent(iconBox.transform, false);
                RectTransform cdRect = cdOverlay.AddComponent<RectTransform>();
                cdRect.anchorMin = Vector2.zero;
                cdRect.anchorMax = Vector2.one;
                cdRect.sizeDelta = Vector2.zero;
                
                _leftSlotsCDOverlays[i] = cdOverlay.AddComponent<Image>();
                _leftSlotsCDOverlays[i].color = new Color(0f, 0f, 0f, 0.65f);
                _leftSlotsCDOverlays[i].type = Image.Type.Filled;
                _leftSlotsCDOverlays[i].fillMethod = Image.FillMethod.Radial360;
                _leftSlotsCDOverlays[i].fillOrigin = (int)Image.Origin360.Top;
                _leftSlotsCDOverlays[i].enabled = false;

                // Cooldown Yazısı
                GameObject cdTextObj = new GameObject("CooldownText");
                cdTextObj.transform.SetParent(slotObj.transform, false);
                RectTransform cdTxtRect = cdTextObj.AddComponent<RectTransform>();
                cdTxtRect.anchorMin = Vector2.zero;
                cdTxtRect.anchorMax = Vector2.one;
                cdTxtRect.sizeDelta = Vector2.zero;
                
                _leftSlotsCDTexts[i] = cdTextObj.AddComponent<TextMeshProUGUI>();
                _leftSlotsCDTexts[i].fontSize = 14;
                _leftSlotsCDTexts[i].fontStyle = FontStyles.Bold;
                _leftSlotsCDTexts[i].color = Color.white;
                _leftSlotsCDTexts[i].alignment = TextAlignmentOptions.Center;

                _leftSlotGameObjects[i] = slotObj;
            }
        }

        private void CreateBottomHotbarPanel()
        {
            GameObject hotbarPanelObj = new GameObject("BottomHotbarPanel");
            hotbarPanelObj.transform.SetParent(_inventoryHUDPanel.transform, false);
            _bottomHotbarPanel = hotbarPanelObj.AddComponent<RectTransform>();

            // Anchor: Alt-Orta
            _bottomHotbarPanel.anchorMin = new Vector2(0.5f, 0f);
            _bottomHotbarPanel.anchorMax = new Vector2(0.5f, 0f);
            _bottomHotbarPanel.pivot = new Vector2(0.5f, 0f);
            _bottomHotbarPanel.sizeDelta = new Vector2(540f, 160f);
            _bottomHotbarPanel.anchoredPosition = new Vector2(0f, _currentBottomPanelY); // Başlangıçta ekranın dışında

            // Premium Cam Tasarımı
            Image bgImage = hotbarPanelObj.AddComponent<Image>();
            bgImage.color = new Color(0.06f, 0.06f, 0.08f, 0.88f);

            // İnce kenarlık (Çelik rengi)
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(hotbarPanelObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(4f, 4f);
            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.color = new Color(0.2f, 0.5f, 1f, 0.5f); // Neon Blue

            // Ana Dikey Düzen Grubu (Vertical Layout Group) - HP Slider ve Yuvaları üst üste dizmek için
            VerticalLayoutGroup mainVLayout = hotbarPanelObj.AddComponent<VerticalLayoutGroup>();
            mainVLayout.padding = new RectOffset(15, 15, 15, 15);
            mainVLayout.spacing = 15f;
            mainVLayout.childAlignment = TextAnchor.MiddleCenter;
            mainVLayout.childControlHeight = false;
            mainVLayout.childControlWidth = false;
            mainVLayout.childForceExpandHeight = false;
            mainVLayout.childForceExpandWidth = false;

            // DIKEY CAN BARI (Pürüzsüz Visual HP Slider - Yazısız)
            GameObject hpObj = new GameObject("HPBarSlider");
            hpObj.transform.SetParent(hotbarPanelObj.transform, false);
            RectTransform hpRect = hpObj.AddComponent<RectTransform>();
            hpRect.sizeDelta = new Vector2(510f, 16f);

            _verticalHPSlider = hpObj.AddComponent<Slider>();
            
            // HP Bar Background
            GameObject hpBgObj = new GameObject("Background");
            hpBgObj.transform.SetParent(hpObj.transform, false);
            RectTransform bgRect = hpBgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image hpBgImg = hpBgObj.AddComponent<Image>();
            hpBgImg.color = new Color(0.18f, 0.1f, 0.1f, 1f);

            // HP Bar Fill Area
            GameObject hpFillArea = new GameObject("Fill Area");
            hpFillArea.transform.SetParent(hpObj.transform, false);
            RectTransform fillAreaRect = hpFillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            GameObject hpFill = new GameObject("Fill");
            hpFill.transform.SetParent(hpFillArea.transform, false);
            RectTransform fillRect = hpFill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.sizeDelta = Vector2.zero;
            Image hpFillImg = hpFill.AddComponent<Image>();
            hpFillImg.color = new Color(0.2f, 0.85f, 0.3f, 1f); // Canlı yeşil

            _verticalHPSlider.fillRect = fillRect;
            _verticalHPSlider.minValue = 0f;
            _verticalHPSlider.maxValue = 100f;
            _verticalHPSlider.value = 100f;

            // YUVALARIN BULUNDUĞU SATIR (Slots Row)
            GameObject slotsRowObj = new GameObject("SlotsRow");
            slotsRowObj.transform.SetParent(hotbarPanelObj.transform, false);
            RectTransform rowRect = slotsRowObj.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(510f, 65f);

            // Yatay Düzen Grubu (Horizontal Layout Group)
            HorizontalLayoutGroup hLayout = slotsRowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 15f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlHeight = false;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childForceExpandWidth = false;

            // 5 ADET YATAY ENVANTER YUVASI (Zeus Yeteneği + 4 Pasif)
            for (int i = 0; i < 5; i++)
            {
                string slotName = i == 0 ? "ZeusActiveSlot" : $"InventoryPassiveSlot_{i}";
                GameObject slotObj = new GameObject(slotName);
                slotObj.transform.SetParent(slotsRowObj.transform, false);
                RectTransform slotRect = slotObj.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(65f, 65f);

                HUDSlot slotComp = slotObj.AddComponent<HUDSlot>();
                slotComp.panelType = PanelType.Bottom;
                slotComp.slotIndex = i;
                slotComp.hudController = this;

                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.color = new Color(0.03f, 0.03f, 0.04f, 0.9f);

                // Slot Kenarlığı
                GameObject slotBorder = new GameObject("Border");
                slotBorder.transform.SetParent(slotObj.transform, false);
                RectTransform sBorderRect = slotBorder.AddComponent<RectTransform>();
                sBorderRect.anchorMin = Vector2.zero;
                sBorderRect.anchorMax = Vector2.one;
                sBorderRect.sizeDelta = new Vector2(2f, 2f);
                Image sBorderImg = slotBorder.AddComponent<Image>();
                
                // Zeus yeteneğine mor neon, diğerlerine mavi/çelik neon kenarlık
                sBorderImg.color = i == 0 ? new Color(0.6f, 0.2f, 0.8f, 0.6f) : new Color(0.2f, 0.4f, 0.6f, 0.4f);

                // İkon
                GameObject iconBox = new GameObject("Icon");
                iconBox.transform.SetParent(slotObj.transform, false);
                RectTransform iconRect = iconBox.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-10f, -10f); // Kenarlardan 5px içeride
                
                _bottomSlotsIcons[i] = iconBox.AddComponent<Image>();
                _bottomSlotsIcons[i].enabled = false;

                // Cooldown Dairesel Overlay
                GameObject cdOverlay = new GameObject("CooldownOverlay");
                cdOverlay.transform.SetParent(iconBox.transform, false);
                RectTransform cdRect = cdOverlay.AddComponent<RectTransform>();
                cdRect.anchorMin = Vector2.zero;
                cdRect.anchorMax = Vector2.one;
                cdRect.sizeDelta = Vector2.zero;

                _bottomSlotsCDOverlays[i] = cdOverlay.AddComponent<Image>();
                _bottomSlotsCDOverlays[i].color = new Color(0f, 0f, 0f, 0.7f);
                _bottomSlotsCDOverlays[i].type = Image.Type.Filled;
                _bottomSlotsCDOverlays[i].fillMethod = Image.FillMethod.Radial360;
                _bottomSlotsCDOverlays[i].fillOrigin = (int)Image.Origin360.Top;
                _bottomSlotsCDOverlays[i].enabled = false;

                // Cooldown Metni (Ortada)
                GameObject cdTextObj = new GameObject("CooldownText");
                cdTextObj.transform.SetParent(slotObj.transform, false);
                RectTransform cdTxtRect = cdTextObj.AddComponent<RectTransform>();
                cdTxtRect.anchorMin = Vector2.zero;
                cdTxtRect.anchorMax = Vector2.one;
                cdTxtRect.sizeDelta = Vector2.zero;

                _bottomSlotsCDTexts[i] = cdTextObj.AddComponent<TextMeshProUGUI>();
                _bottomSlotsCDTexts[i].fontSize = 16;
                _bottomSlotsCDTexts[i].fontStyle = FontStyles.Bold;
                _bottomSlotsCDTexts[i].color = Color.white;
                _bottomSlotsCDTexts[i].alignment = TextAlignmentOptions.Center;

                _bottomSlotGameObjects[i] = slotObj;
            }
        }

        private void RefreshHUD()
        {
            if (playerStats == null || playerInventory == null) return;

            // 1. Can Barı Güncellemesi (Yazısız slider)
            if (_verticalHPSlider != null)
            {
                _verticalHPSlider.maxValue = playerStats.MaxHealth;
                _verticalHPSlider.value = playerStats.CurrentHealth;
            }

            // 2. Tanrı Yeteneği Güncellemesi (Yatay Alt Yuva 0)
            SkillDefinition activeSkill = playerInventory.EquippedSkill;
            if (_bottomSlotsIcons[0] != null)
            {
                _bottomSlotsIcons[0].sprite = activeSkill != null ? activeSkill.icon : null;
                _bottomSlotsIcons[0].enabled = activeSkill != null && activeSkill.icon != null;
            }

            // 3. Pasifler Güncellemesi (Yatay Alt Yuvalar 1 - 4)
            IReadOnlyList<PlayerInventory.PassiveItemStackInfo> passiveStacks = playerInventory.GetPassiveStacks();
            for (int i = 1; i < 5; i++)
            {
                int stackIndex = i - 1;
                if (stackIndex < passiveStacks.Count)
                {
                    PlayerInventory.PassiveItemStackInfo info = passiveStacks[stackIndex];
                    if (_bottomSlotsIcons[i] != null && info.passiveItem != null)
                    {
                        _bottomSlotsIcons[i].sprite = info.passiveItem.icon;
                        _bottomSlotsIcons[i].enabled = info.passiveItem.icon != null;
                    }
                }
                else
                {
                    if (_bottomSlotsIcons[i] != null) _bottomSlotsIcons[i].enabled = false;
                }
            }

            // 4. Hotbar - Silah Slotu (Dikey Sol Yuva 0)
            WeaponDefinition activeWeapon = playerInventory.ActiveWeapon;
            if (_leftSlotsIcons[0] != null)
            {
                _leftSlotsIcons[0].sprite = activeWeapon != null ? activeWeapon.icon : null;
                _leftSlotsIcons[0].enabled = activeWeapon != null && activeWeapon.icon != null;
            }

            // 5. Hotbar - Aktif Eşya Slotu (Dikey Sol Yuva 1)
            ActiveItemDefinition activeItem = playerInventory.ActiveItem;
            if (_leftSlotsIcons[1] != null)
            {
                _leftSlotsIcons[1].sprite = activeItem != null ? activeItem.icon : null;
                _leftSlotsIcons[1].enabled = activeItem != null && activeItem.icon != null;
            }

            // 6. Hotbar - Pasif Yuvalar (Dikey Sol Yuvalar 2, 3, 4)
            for (int i = 2; i < 5; i++)
            {
                int passiveIndex = i - 2;
                if (passiveIndex < passiveStacks.Count)
                {
                    PlayerInventory.PassiveItemStackInfo info = passiveStacks[passiveIndex];
                    if (_leftSlotsIcons[i] != null && info.passiveItem != null)
                    {
                        _leftSlotsIcons[i].sprite = info.passiveItem.icon;
                        _leftSlotsIcons[i].enabled = info.passiveItem.icon != null;
                    }
                }
                else
                {
                    if (_leftSlotsIcons[i] != null) _leftSlotsIcons[i].enabled = false;
                }
            }
        }

        private void UpdateRealtimeData()
        {
            if (playerStats == null || playerInventory == null) return;

            // Can güncelleme (Slider)
            if (_verticalHPSlider != null)
            {
                _verticalHPSlider.value = playerStats.CurrentHealth;
            }

            // 1. Tanrı Yeteneği Cooldown (Yatay Alt Yuva 0)
            if (playerAttack != null)
            {
                float skillCD = playerAttack.GetEquippedSkillRemainingCooldown();
                GodSkillBase skill = playerAttack.EquippedSkill;
                float totalSkillCD = skill != null ? skill.Cooldown : 1f;

                if (_bottomSlotsCDOverlays[0] != null)
                {
                    _bottomSlotsCDOverlays[0].enabled = skillCD > 0f;
                    _bottomSlotsCDOverlays[0].fillAmount = skillCD / Mathf.Max(0.1f, totalSkillCD);
                }
                if (_bottomSlotsCDTexts[0] != null)
                {
                    _bottomSlotsCDTexts[0].text = skillCD > 0f ? $"{skillCD:F1}s" : "";
                }
            }

            // 3. Aktif Eşya Cooldown (Dikey Sol Yuva 1)
            float activeCDNorm = playerInventory.GetActiveItemCooldownNormalized();
            float activeCDRem = playerInventory.GetActiveItemCooldownRemaining();

            if (_leftSlotsCDOverlays[1] != null)
            {
                _leftSlotsCDOverlays[1].enabled = activeCDRem > 0f;
                _leftSlotsCDOverlays[1].fillAmount = activeCDNorm;
            }
            if (_leftSlotsCDTexts[1] != null)
            {
                _leftSlotsCDTexts[1].text = activeCDRem > 0f ? $"{activeCDRem:F1}s" : "";
            }
        }

        public void HandleItemSwap(HUDSlot source, HUDSlot dest)
        {
            if (playerInventory == null) return;

            // Kategori kontrolü: Her iki slot da pasif slotu olmalı
            // Left Panel (Hotbar): Pasif yuvalar >= 2 (2, 3, 4)
            // Bottom Panel (Inventory): Pasif yuvalar >= 1 (1, 2, 3, 4)
            bool isSourcePassive = (source.panelType == PanelType.Left && source.slotIndex >= 2) ||
                                  (source.panelType == PanelType.Bottom && source.slotIndex >= 1);
            bool isDestPassive = (dest.panelType == PanelType.Left && dest.slotIndex >= 2) ||
                                (dest.panelType == PanelType.Bottom && dest.slotIndex >= 1);

            if (isSourcePassive && isDestPassive)
            {
                int sourceIdx = source.panelType == PanelType.Left ? source.slotIndex - 2 : source.slotIndex - 1;
                int destIdx = dest.panelType == PanelType.Left ? dest.slotIndex - 2 : dest.slotIndex - 1;

                var passives = playerInventory.GetPassiveStacks();
                if (sourceIdx >= 0 && sourceIdx < passives.Count && destIdx >= 0 && destIdx < passives.Count)
                {
                    playerInventory.SwapPassivePositions(sourceIdx, destIdx);
                    FloatingCombatText.Create("Eşya Düzenlendi!", dest.transform.position, Color.yellow);
                    RefreshHUD();
                }
            }
            else
            {
                FloatingCombatText.Create("Kategori Uyuşmazlığı!", dest.transform.position, Color.red);
            }
        }
    }

    public enum PanelType { Left, Bottom }

    public class HUDSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public PanelType panelType;
        public int slotIndex;
        public InventoryHUDController hudController;

        private GameObject _dragInstance;

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Only allow dragging if the inventory screen (bottom panel) is active
            if (hudController == null || !hudController.IsBottomHotbarOpen)
                return;

            Image iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage == null || !iconImage.enabled || iconImage.sprite == null)
                return;

            _dragInstance = new GameObject("DragDropIcon");
            _dragInstance.transform.SetParent(hudController.transform.parent, false);
            
            RectTransform rect = _dragInstance.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(50f, 50f);
            
            Image img = _dragInstance.AddComponent<Image>();
            img.sprite = iconImage.sprite;
            img.color = new Color(1f, 1f, 1f, 0.75f);
            
            CanvasGroup group = _dragInstance.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            _dragInstance.transform.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragInstance != null)
            {
                _dragInstance.transform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragInstance != null)
            {
                Destroy(_dragInstance);
                _dragInstance = null;
            }

            if (eventData.hovered != null)
            {
                foreach (var obj in eventData.hovered)
                {
                    HUDSlot destSlot = obj.GetComponent<HUDSlot>();
                    if (destSlot == null)
                    {
                        destSlot = obj.GetComponentInParent<HUDSlot>();
                    }

                    if (destSlot != null && destSlot != this)
                    {
                        hudController.HandleItemSwap(this, destSlot);
                        break;
                    }
                }
            }
        }
    }
}
