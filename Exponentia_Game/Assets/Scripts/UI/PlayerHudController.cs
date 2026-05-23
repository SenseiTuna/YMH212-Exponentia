using Exponentia.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Exponentia.UI
{
    public class PlayerHudController : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerMechanics playerMechanics;
        [SerializeField] private PlayerAttack playerAttack;

        [Header("HUD Elements")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;

        [Header("Skill Mana (Zeus)")]
        [SerializeField] private Slider skillManaSlider;
        [SerializeField] private Image skillManaImage;
        [SerializeField] private Sprite[] skillManaSprites;
        [SerializeField] private Text skillManaText;

        [Header("Laser Mana")]
        [SerializeField] private Slider laserManaSlider;
        [SerializeField] private Image laserManaImage;
        [SerializeField] private Image laserManaFillImage;
        [SerializeField] private Sprite[] laserManaSprites;
        [SerializeField] private Text laserManaText;

        [Header("General HUD")]
        [SerializeField] private Text skillText;
        [SerializeField] private Text weaponText;
        [SerializeField] private Text infoText;

        [Header("Defaults")]
        [SerializeField] private string activeSkillName = "Healing Area";
        [SerializeField] private string activeWeaponName = "Laser";
        [SerializeField] private float referenceRetryInterval = 0.5f;
        private PlayerMechanics subscribedMechanics;
        private float referenceRetryElapsed;

        private void Awake()
        {
            ResolvePlayerReferences();
            TryAutoBindUiElements();
        }

        private void OnEnable()
        {
            TryResolveAndSubscribe();
        }

        private void Start()
        {
            TryResolveAndSubscribe();
        }

        private void Update()
        {
            if (subscribedMechanics == null || playerStats == null)
            {
                referenceRetryElapsed += Time.unscaledDeltaTime;
                if (referenceRetryElapsed >= Mathf.Max(0.1f, referenceRetryInterval))
                {
                    referenceRetryElapsed = 0f;
                    TryResolveAndSubscribe();
                }
            }
        }

        private void OnDisable()
        {
            if (subscribedMechanics == null)
            {
                return;
            }

            subscribedMechanics.OnCanDegisti -= HandleHealthChanged;
            subscribedMechanics.OnManaDegisti -= HandleSkillManaChanged;
            subscribedMechanics.OnLaserManaDegisti -= HandleLaserManaChanged;
            subscribedMechanics.OnXpDegisti -= HandleXpChanged;
            subscribedMechanics = null;
        }

        public void RefreshAll()
        {
            if (playerStats == null || playerMechanics == null)
            {
                return;
            }

            HandleHealthChanged(playerMechanics.MevcutCan, playerStats.MaxHealth);
            HandleSkillManaChanged(playerMechanics.MevcutMana, playerStats.Mana);
            HandleLaserManaChanged(playerMechanics.MevcutLaserMana, playerMechanics.MaxLaserMana);
            HandleXpChanged(playerStats.Xp, playerStats.NextLevelXp);
            UpdateStaticFields();
        }

        private void ResolvePlayerReferences()
        {
            if (playerStats != null && playerMechanics != null)
            {
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerStats>()?.gameObject;
            }

            if (player == null)
            {
                return;
            }

            if (playerStats == null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }

            if (playerMechanics == null)
            {
                playerMechanics = player.GetComponent<PlayerMechanics>();
            }

            if (playerAttack == null)
            {
                playerAttack = player.GetComponent<PlayerAttack>();
            }
        }

        private void TryResolveAndSubscribe()
        {
            ResolvePlayerReferences();

            if (playerMechanics == null)
            {
                return;
            }

            if (subscribedMechanics != playerMechanics)
            {
                if (subscribedMechanics != null)
                {
                    subscribedMechanics.OnCanDegisti -= HandleHealthChanged;
                    subscribedMechanics.OnManaDegisti -= HandleSkillManaChanged;
                    subscribedMechanics.OnLaserManaDegisti -= HandleLaserManaChanged;
                    subscribedMechanics.OnXpDegisti -= HandleXpChanged;
                }

                subscribedMechanics = playerMechanics;
                subscribedMechanics.OnCanDegisti += HandleHealthChanged;
                subscribedMechanics.OnManaDegisti += HandleSkillManaChanged;
                subscribedMechanics.OnLaserManaDegisti += HandleLaserManaChanged;
                subscribedMechanics.OnXpDegisti += HandleXpChanged;
            }

            RefreshAll();
            referenceRetryElapsed = 0f;
        }

        private void TryAutoBindUiElements()
        {
            if (healthSlider == null)
            {
                healthSlider = FindOptionalSlider("HealthBar");
            }

            if (skillManaSlider == null)
            {
                skillManaSlider = FindOptionalSlider("SkillManaBar") ?? FindOptionalSlider("ManaBar");
            }

            if (laserManaSlider == null)
            {
                laserManaSlider = FindOptionalSlider("LaserManaBar");
            }

            if (healthText == null)
            {
                healthText = FindOptionalText("HealthText");
            }

            if (skillManaText == null)
            {
                skillManaText = FindOptionalText("SkillManaText") ?? FindOptionalText("ManaText");
            }

            if (laserManaText == null)
            {
                laserManaText = FindOptionalText("LaserManaText");
            }

            if (skillText == null)
            {
                skillText = FindOptionalText("SkillText");
            }

            if (weaponText == null)
            {
                weaponText = FindOptionalText("WeaponText");
            }

            if (infoText == null)
            {
                infoText = FindOptionalText("InfoText");
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = Mathf.Max(1f, max);
                healthSlider.value = Mathf.Clamp(current, 0f, healthSlider.maxValue);
            }

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = Mathf.Clamp01(current / Mathf.Max(1f, max));
            }

            if (healthText != null)
            {
                healthText.text = BuildGaugeText("HP", current, max);
            }
        }

        private void HandleSkillManaChanged(float current, float max)
        {
            if (skillManaSlider != null)
            {
                skillManaSlider.minValue = 0f;
                skillManaSlider.maxValue = Mathf.Max(1f, max);
                skillManaSlider.value = Mathf.Clamp(current, 0f, skillManaSlider.maxValue);
            }

            if (skillManaText != null)
            {
                skillManaText.text = BuildGaugeText("Skill MP", current, max);
            }

            if (skillManaImage != null && skillManaSprites != null && skillManaSprites.Length > 0)
            {
                float percent = Mathf.Clamp01(current / Mathf.Max(1f, max));
                int spriteIndex = Mathf.Clamp(Mathf.FloorToInt(percent * skillManaSprites.Length), 0, skillManaSprites.Length - 1);

                // Eğer aktif yetenek kuşanılmışsa, sprite seçimini pürüzsüz yüzde yerine NET SKILL HAKKI (yük) sayısına göre yap
                if (playerAttack != null && playerAttack.EquippedSkill != null)
                {
                    float cost = playerAttack.EquippedSkill.ManaCost;
                    if (cost > 0f)
                    {
                        int charges = Mathf.FloorToInt(current / cost);
                        spriteIndex = Mathf.Clamp(charges, 0, skillManaSprites.Length - 1);
                    }
                }

                skillManaImage.sprite = skillManaSprites[spriteIndex];
            }
        }

        private void HandleLaserManaChanged(float current, float max)
        {
            if (laserManaSlider != null)
            {
                laserManaSlider.minValue = 0f;
                laserManaSlider.maxValue = Mathf.Max(1f, max);
                laserManaSlider.value = Mathf.Clamp(current, 0f, laserManaSlider.maxValue);
            }

            if (laserManaText != null)
            {
                laserManaText.text = BuildGaugeText("Laser MP", current, max);
            }

            float percent = Mathf.Clamp01(current / Mathf.Max(1f, max));

            if (laserManaFillImage != null)
            {
                laserManaFillImage.fillAmount = percent;
            }

            if (laserManaImage != null && laserManaSprites != null && laserManaSprites.Length > 0)
            {
                int spriteIndex = Mathf.Clamp(Mathf.FloorToInt(percent * laserManaSprites.Length), 0, laserManaSprites.Length - 1);
                laserManaImage.sprite = laserManaSprites[spriteIndex];
            }
        }

        private void HandleXpChanged(float current, float required)
        {
            if (infoText == null || playerStats == null)
            {
                return;
            }

            infoText.text = BuildInfoText(playerStats.Level, current, required);
        }

        private void UpdateStaticFields()
        {
            if (skillText != null)
            {
                skillText.text = "Skill: " + activeSkillName;
            }

            if (weaponText != null)
            {
                string weaponLabel = playerAttack != null ? activeWeaponName : "None";
                weaponText.text = "Weapon: " + weaponLabel;
            }
        }

        private static Slider FindOptionalSlider(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            return target != null ? target.GetComponent<Slider>() : null;
        }

        private static Text FindOptionalText(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            return target != null ? target.GetComponent<Text>() : null;
        }

        public static string BuildGaugeText(string label, float current, float max)
        {
            int safeCurrent = Mathf.CeilToInt(Mathf.Max(0f, current));
            int safeMax = Mathf.CeilToInt(Mathf.Max(1f, max));
            return label + ": " + safeCurrent + "/" + safeMax;
        }

        public static string BuildInfoText(int level, float currentXp, float nextLevelXp)
        {
            int safeLevel = Mathf.Max(1, level);
            int shownXp = Mathf.FloorToInt(Mathf.Max(0f, currentXp));
            int requiredXp = Mathf.Max(1, Mathf.CeilToInt(nextLevelXp));
            return "Level: " + safeLevel + "  XP: " + shownXp + "/" + requiredXp;
        }
    }
}
