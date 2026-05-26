using UnityEngine;
using UnityEngine.UI; // UI Image bileşenini kullanabilmek için ekledik!
using TMPro;
using Exponentia.Player;

namespace Exponentia.UI
{
    public class PlayerWeaponDisplay : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("UI Text Elements (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI weaponNameText; // Silah İsmi (Örn: "Lazer")
        [SerializeField] private TextMeshProUGUI ammoText;       // Mermi Göstergesi (Örn: "MERMİ: 998/1000")

        [Header("UI Image Elements (Weapon Icon)")]
        [SerializeField] private Image weaponIconImage;      // Silahın görselini göstereceğimiz UI Image
        [SerializeField] private Sprite defaultWeaponIcon;   // Silahta görsel yoksa (Lazer gibi) kullanılacak varsayılan görsel

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.05f; // Mermiyi anında yansıtması için 0.05 saniye
        private float nextUpdateTime;

        private void Update()
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + updateInterval;

            // Eğer oyuncu referansı henüz yoksa dinamik olarak sahneden çek
            if (playerAttack == null && autoFindPlayer)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerAttack = player.GetComponent<PlayerAttack>();
                }
            }

            if (playerAttack != null)
            {
                UpdateWeaponUI();
            }
        }

        private void UpdateWeaponUI()
        {
            // 1. Aktif Silahın İsmi
            if (weaponNameText != null)
            {
                string weaponName = playerAttack.GetCurrentWeaponDisplayName();
                weaponNameText.text = $"SİLAH: {weaponName.ToUpper()}";
            }

            // 2. Mermi Durumu (PlayerAttack üzerindeki gerçek mermi sayısını çeker)
            if (ammoText != null)
            {
                if (!playerAttack.UseAmmoLimit)
                {
                    ammoText.text = "MERMİ: SINIRSIZ";
                }
                else
                {
                    ammoText.text = $"MERMİ: {playerAttack.CurrentAmmo}/{playerAttack.MaxAmmo}";
                }
            }

            // 3. Silah Görseli (Icon)
            if (weaponIconImage != null)
            {
                Sprite weaponIcon = null;
                if (playerAttack.EquippedWeaponDefinition != null)
                {
                    weaponIcon = playerAttack.EquippedWeaponDefinition.icon;
                }

                // Silahta özel bir ikon yoksa varsayılan lazer görselini kullan
                if (weaponIcon == null)
                {
                    weaponIcon = defaultWeaponIcon;
                }

                if (weaponIcon != null)
                {
                    weaponIconImage.enabled = true;
                    weaponIconImage.sprite = weaponIcon;
                }
                else
                {
                    // Eğer hiçbir görsel yoksa ekranda boş beyaz kare durmasın diye kapatıyoruz
                    weaponIconImage.enabled = false;
                }
            }
        }
    }
}
