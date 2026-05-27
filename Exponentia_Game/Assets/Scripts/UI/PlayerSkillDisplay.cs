using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Exponentia.Player;

namespace Exponentia.UI
{
    public class PlayerSkillDisplay : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("UI Text Elements (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI skillNameText; // Yetenek İsmi (Örn: "ŞİFA ALANI")
        [SerializeField] private TextMeshProUGUI cooldownText;  // Saniye cinsinden bekleme süresi (Örn: "3.4s")

        [Header("UI Image Elements")]
        [SerializeField] private Image skillIconImage;        // Yetenek İkonu
        [SerializeField] private Image cooldownOverlayImage; // Bekleme süresini dairesel kapatacak yarı saydam gölge resim
        [SerializeField] private Sprite defaultSkillIcon;     // Eğer yetenek yoksa gösterilecek varsayılan görsel

        private void Update()
        {
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
                UpdateSkillUI();
            }
        }

        private void UpdateSkillUI()
        {
            GodSkillBase skill = playerAttack.EquippedSkill;

            if (skill != null)
            {
                // 1. Yetenek İsmi
                if (skillNameText != null)
                {
                    skillNameText.text = skill.SkillName.ToUpper();
                }

                // 2. Yetenek İkonu
                if (skillIconImage != null)
                {
                    Sprite icon = skill.SkillIcon != null ? skill.SkillIcon : defaultSkillIcon;
                    if (icon != null)
                    {
                        skillIconImage.enabled = true;
                        skillIconImage.sprite = icon;
                    }
                    else
                    {
                        skillIconImage.enabled = false;
                    }
                }

                // 3. Bekleme Süresi (Cooldown) Hesaplamaları
                float remainingCdr = skill.RemainingCooldown;
                float totalCdr = skill.Cooldown;

                // Cooldown metin gösterimi
                if (cooldownText != null)
                {
                    if (remainingCdr > 0f)
                    {
                        cooldownText.enabled = true;
                        cooldownText.text = $"{remainingCdr:F1}s";
                    }
                    else
                    {
                        cooldownText.enabled = false; // Cooldown bittiyse yazıyı gizle
                    }
                }

                // Dairesel saat yönünde gölge (Radial Fill) gösterimi
                if (cooldownOverlayImage != null)
                {
                    if (remainingCdr > 0f && totalCdr > 0f)
                    {
                        cooldownOverlayImage.enabled = true;
                        cooldownOverlayImage.fillAmount = remainingCdr / totalCdr; // Oransal doldurma
                    }
                    else
                    {
                        cooldownOverlayImage.enabled = false; // Cooldown yoksa gölgeyi gizle
                    }
                }
            }
            else
            {
                // Eğer kuşanılmış yetenek yoksa her şeyi temizle
                if (skillNameText != null) skillNameText.text = "YETENEK: YOK";
                if (cooldownText != null) cooldownText.enabled = false;
                if (cooldownOverlayImage != null) cooldownOverlayImage.enabled = false;
                if (skillIconImage != null)
                {
                    if (defaultSkillIcon != null)
                    {
                        skillIconImage.enabled = true;
                        skillIconImage.sprite = defaultSkillIcon;
                    }
                    else
                    {
                        skillIconImage.enabled = false;
                    }
                }
            }
        }
    }
}
