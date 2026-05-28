/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : DungeonFlowManager.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exponentia.Dungeon
{
    [DisallowMultipleComponent]
    public class DungeonFlowManager : MonoBehaviour
    {
        [Header("State Settings")]
        [SerializeField] private DungeonState currentState = DungeonState.NormalRoom;

        // Akış durumu değiştiğinde tetiklenecek olay
        public event System.Action<DungeonState> OnStateChanged;

        public DungeonState CurrentState => currentState;

        private void Start()
        {
            SubscribeToCombatTriggers();
        }

        /// <summary>
        /// Sahnede yer alan dinamik veya manuel savaş yöneticilerini bulup olaylarına kaydolur.
        /// </summary>
        private void SubscribeToCombatTriggers()
        {
            // 1. Dinamik Zindan Savaş Yöneticisini bul ve dinle
            DungeonRoomCombatManager proceduralManager = Object.FindAnyObjectByType<DungeonRoomCombatManager>();
            if (proceduralManager != null)
            {
                proceduralManager.OnRoomEntered += HandleProceduralRoomEntered;
                proceduralManager.OnRoomCleared += HandleProceduralRoomCleared;
                Debug.Log("[DungeonFlowManager] Dinamik DungeonRoomCombatManager olaylarına başarıyla kaydolundu.");
            }

            // 2. Manuel Savaş Odası Tetikleyicilerini bul ve dinle
            ManualRoomCombatTrigger[] manualTriggers = Object.FindObjectsByType<ManualRoomCombatTrigger>(FindObjectsSortMode.None);
            foreach (var trigger in manualTriggers)
            {
                if (trigger != null)
                {
                    trigger.OnRoomEntered += HandleManualRoomEntered;
                    trigger.OnRoomCleared += HandleManualRoomCleared;
                }
            }
            if (manualTriggers.Length > 0)
            {
                Debug.Log($"[DungeonFlowManager] {manualTriggers.Length} adet manuel ManualRoomCombatTrigger olaylarına başarıyla kaydolundu.");
            }
        }

        #region Event Handlers (Procedural)

        private void HandleProceduralRoomEntered(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return;

            if (roomId.StartsWith("BOSS"))
            {
                Debug.Log($"[DungeonFlowManager] Boss odasına girildi: {roomId}. Durum -> BossRoom");
                SetState(DungeonState.BossRoom);
            }
            else if (roomId.StartsWith("TREASURE"))
            {
                Debug.Log($"[DungeonFlowManager] Ödül/Hazine odasına girildi: {roomId}. Durum -> RewardRoom");
                SetState(DungeonState.RewardRoom);
            }
            else
            {
                Debug.Log($"[DungeonFlowManager] Normal odaya girildi: {roomId}. Durum -> NormalRoom");
                SetState(DungeonState.NormalRoom);
            }
        }

        private void HandleProceduralRoomCleared(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return;

            if (roomId.StartsWith("BOSS"))
            {
                Debug.Log($"[DungeonFlowManager] Boss yenildi ve oda temizlendi! Durum -> FloorTransition");
                SetState(DungeonState.FloorTransition);
            }
            else if (roomId.StartsWith("TREASURE"))
            {
                Debug.Log($"[DungeonFlowManager] Ödül odası tamamlandı.");
            }
        }

        #endregion

        #region Event Handlers (Manual)

        private void HandleManualRoomEntered(string roomName)
        {
            // Eğer manuel oda adı "Boss" kelimesini içeriyorsa Boss odası sayalım
            if (roomName.Contains("Boss") || roomName.Contains("BOSS"))
            {
                Debug.Log($"[DungeonFlowManager] Manuel Boss odası tetiklendi: {roomName}. Durum -> BossRoom");
                SetState(DungeonState.BossRoom);
            }
            // Eğer manuel oda adı "Treasure" veya "Reward" veya "Odul" içeriyorsa
            else if (roomName.Contains("Treasure") || roomName.Contains("Reward") || roomName.Contains("Odul") || roomName.Contains("ODUL"))
            {
                Debug.Log($"[DungeonFlowManager] Manuel Ödül odası tetiklendi: {roomName}. Durum -> RewardRoom");
                SetState(DungeonState.RewardRoom);
            }
            else
            {
                Debug.Log($"[DungeonFlowManager] Manuel Normal oda tetiklendi: {roomName}. Durum -> NormalRoom");
                SetState(DungeonState.NormalRoom);
            }
        }

        private void HandleManualRoomCleared(string roomName)
        {
            if (roomName.Contains("Boss") || roomName.Contains("BOSS"))
            {
                Debug.Log($"[DungeonFlowManager] Manuel Boss yenildi! Durum -> FloorTransition");
                SetState(DungeonState.FloorTransition);
            }
        }

        #endregion

        /// <summary>
        /// Oyun/Zindan akış durumunu değiştirir ve dinleyicileri bilgilendirir.
        /// </summary>
        public void SetState(DungeonState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            Debug.Log($"[DungeonFlowManager] Zindan Durumu Değişti: {currentState}");
            OnStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// Sonraki kata geçiş komutunu tetikler. (Buton tıklama dinleyicisi buna bağlıdır)
        /// </summary>
        public void NextFloor()
        {
            Debug.Log("[DungeonFlowManager] 'Sonraki Kata Geç' butonu tıklandı! Kat yükleniyor...");
            
            // Sıfırlama ve test için mevcut aktif sahneyi yeniden yükler (Zindan yeniden oluşturulur)
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }
    }
}
