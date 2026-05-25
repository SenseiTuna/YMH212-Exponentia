/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
====================================================
*/

using UnityEngine;
using UnityEngine.UI;
using Exponentia.Dungeon;

namespace Exponentia.UI
{
    [DisallowMultipleComponent]
    public class FloorTransitionUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject bossRoomPanel;
        [SerializeField] private GameObject rewardRoomPanel;
        [SerializeField] private GameObject floorTransitionPanel;

        [Header("Controls")]
        [SerializeField] private Button nextFloorButton;

        private DungeonFlowManager _flowManager;

        private void Awake()
        {
            // Flow manager referansını otomatik bağla
            _flowManager = GetComponent<DungeonFlowManager>();
            if (_flowManager == null)
            {
                _flowManager = GetComponentInParent<DungeonFlowManager>();
            }
            if (_flowManager == null)
            {
                _flowManager = GetComponentInChildren<DungeonFlowManager>();
            }
            if (_flowManager == null)
            {
                _flowManager = Object.FindAnyObjectByType<DungeonFlowManager>();
            }
        }

        private void Start()
        {
            // Eksik referansları otomatik bulmaya çalış (Kendi kendini iyileştirme)
            AutoResolveReferences();

            // Buton olayını otomatik dinleyiciyle bağla (Elle sürüklemeye son!)
            if (nextFloorButton != null)
            {
                nextFloorButton.onClick.RemoveAllListeners();
                nextFloorButton.onClick.AddListener(OnNextFloorButtonClicked);
                Debug.Log("[FloorTransitionUI] 'Sonraki Kata Geç' buton dinleyicisi kod içinden başarıyla bağlandı.");
            }
            else
            {
                Debug.LogWarning("[FloorTransitionUI] UYARI: 'Sonraki Kata Geç' butonu bulunamadı!");
            }

            // Akış olayını dinle
            if (_flowManager != null)
            {
                _flowManager.OnStateChanged += UpdateUIState;
                UpdateUIState(_flowManager.CurrentState);
            }
            else
            {
                Debug.LogError("[FloorTransitionUI] HATA: Sahnede DungeonFlowManager bulunamadı!");
            }
        }

        private void OnDestroy()
        {
            if (_flowManager != null)
            {
                _flowManager.OnStateChanged -= UpdateUIState;
            }
        }

        /// <summary>
        /// Zindan durumuna göre Canvas panellerini aktifleştirir veya gizler.
        /// </summary>
        private void UpdateUIState(DungeonState state)
        {
            // Güvenli referans araması
            AutoResolveReferences();

            // Tüm panelleri başlangıçta kapat
            if (bossRoomPanel != null) bossRoomPanel.SetActive(false);
            if (rewardRoomPanel != null) rewardRoomPanel.SetActive(false);
            if (floorTransitionPanel != null) floorTransitionPanel.SetActive(false);

            // Duruma göre ilgili paneli aç (Ekranda renk panelleri kaplanmaz, oyun görünümü temiz kalır)
            switch (state)
            {
                case DungeonState.NormalRoom:
                case DungeonState.BossRoom:
                case DungeonState.RewardRoom:
                    // Normal, Boss ve Hazine odalarında ekranı kaplayan paneller açılmaz, doğal oyun akışı sürer.
                    break;
                case DungeonState.FloorTransition:
                    if (floorTransitionPanel != null) floorTransitionPanel.SetActive(true);
                    break;
            }
        }

        private void OnNextFloorButtonClicked()
        {
            if (_flowManager != null)
            {
                _flowManager.NextFloor();
            }
        }

        /// <summary>
        /// Eksik referansları hiyerarşik isimlerine göre otomatik bulup bağlar.
        /// </summary>
        private void AutoResolveReferences()
        {
            if (bossRoomPanel == null)
            {
                Transform t = transform.Find("BossRoom_Panel");
                if (t != null) bossRoomPanel = t.gameObject;
            }

            if (rewardRoomPanel == null)
            {
                Transform t = transform.Find("RewardRoom_Panel");
                if (t != null) rewardRoomPanel = t.gameObject;
            }

            if (floorTransitionPanel == null)
            {
                Transform t = transform.Find("FloorTransition_Panel");
                if (t != null) floorTransitionPanel = t.gameObject;
            }

            if (nextFloorButton == null && floorTransitionPanel != null)
            {
                nextFloorButton = floorTransitionPanel.GetComponentInChildren<Button>(true);
            }
        }
    }
}
