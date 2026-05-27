using UnityEngine;
using TMPro; // TextMeshPro desteği ekledik!
using Exponentia.Player;
using Exponentia.InventorySystem;

namespace Exponentia.UI
{
    public class PlayerStatsDisplay : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerStatController statController;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("UI Text Elements (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI damageText;          // Saldırı Gücü (Hasar)
        [SerializeField] private TextMeshProUGUI moveSpeedText;       // Hareket Hızı
        [SerializeField] private TextMeshProUGUI attackSpeedText;     // Saldırı Hızı
        [SerializeField] private TextMeshProUGUI critChanceText;      // Kritik İhtimali
        [SerializeField] private TextMeshProUGUI luckText;            // Şans
        [SerializeField] private TextMeshProUGUI projectileCountText; // Mermi Sayısı

        [Header("Mermi Sayısı Ayarları")]
        [Tooltip("İşaretlenirse ekranda 'Sınırsız' yazar. İşaretlenmezse oyuncunun gerçek mermi/atış sayısını gösterir.")]
        [SerializeField] private bool unlimitedProjectiles = false;

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.2f; // Saniyede 5 kez güncelleme (performans dostu)
        private float nextUpdateTime;

        private void Update()
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + updateInterval;

            // Eğer oyuncu referansı henüz yoksa, dinamik olarak sahneden çek
            if (playerStats == null && autoFindPlayer)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                    statController = player.GetComponent<PlayerStatController>();
                }
            }

            if (playerStats != null)
            {
                UpdateStatsUI();
            }
        }

        private void UpdateStatsUI()
        {
            // 1. Saldırı Gücü (Damage)
            if (damageText != null)
            {
                damageText.text = $"Saldırı Gücü: {playerStats.Damage:F1}";
            }

            // 2. Hareket Hızı (Move Speed)
            if (moveSpeedText != null)
            {
                moveSpeedText.text = $"Hareket Hızı: {playerStats.MoveSpeed:F1}";
            }

            // 3. Saldırı Hızı (Attack Speed)
            if (attackSpeedText != null)
            {
                attackSpeedText.text = $"Saldırı Hızı: {playerStats.AttackSpeed:F2}";
            }

            // 4. Kritik İhtimali (Crit Chance)
            if (critChanceText != null)
            {
                float critChance = statController != null ? statController.CritChance : 0.05f; // Varsayılan %5
                critChanceText.text = $"Kritik Şansı: %{(critChance * 100f):F0}";
            }

            // 5. Şans (Luck)
            if (luckText != null)
            {
                luckText.text = $"Şans: {playerStats.Luck:F1}";
            }

            // 6. Mermi Sayısı (Projectile Count)
            if (projectileCountText != null)
            {
                if (unlimitedProjectiles)
                {
                    projectileCountText.text = "Mermi Sayısı: Sınırsız";
                }
                else
                {
                    int projCount = statController != null ? statController.ProjectileCount : 1; // Varsayılan 1
                    projectileCountText.text = $"Mermi Sayısı: {projCount}";
                }
            }
        }
    }
}
