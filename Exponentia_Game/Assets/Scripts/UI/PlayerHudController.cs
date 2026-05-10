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
        [SerializeField] private Slider manaSlider;
        [SerializeField] private Text healthText;
        [SerializeField] private Text manaText;
        [SerializeField] private Text skillText;
        [SerializeField] private Text weaponText;
        [SerializeField] private Text infoText;

        [Header("Defaults")]
        [SerializeField] private string activeSkillName = "Healing Area";
        [SerializeField] private string activeWeaponName = "Laser";
        private PlayerMechanics subscribedMechanics;

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
                TryResolveAndSubscribe();
            }
        }

        private void OnDisable()
        {
            if (subscribedMechanics == null)
            {
                return;
            }

            subscribedMechanics.OnCanDegisti -= HandleHealthChanged;
            subscribedMechanics.OnManaDegisti -= HandleManaChanged;
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
            HandleManaChanged(playerMechanics.MevcutMana, playerStats.Mana);
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
                    subscribedMechanics.OnManaDegisti -= HandleManaChanged;
                    subscribedMechanics.OnXpDegisti -= HandleXpChanged;
                }

                subscribedMechanics = playerMechanics;
                subscribedMechanics.OnCanDegisti += HandleHealthChanged;
                subscribedMechanics.OnManaDegisti += HandleManaChanged;
                subscribedMechanics.OnXpDegisti += HandleXpChanged;
            }

            RefreshAll();
        }

        private void TryAutoBindUiElements()
        {
            if (healthSlider == null)
            {
                healthSlider = FindOptionalSlider("HealthBar");
            }

            if (manaSlider == null)
            {
                manaSlider = FindOptionalSlider("ManaBar");
            }

            if (healthText == null)
            {
                healthText = FindOptionalText("HealthText");
            }

            if (manaText == null)
            {
                manaText = FindOptionalText("ManaText");
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

            if (healthText != null)
            {
                healthText.text = BuildGaugeText("HP", current, max);
            }
        }

        private void HandleManaChanged(float current, float max)
        {
            if (manaSlider != null)
            {
                manaSlider.minValue = 0f;
                manaSlider.maxValue = Mathf.Max(1f, max);
                manaSlider.value = Mathf.Clamp(current, 0f, manaSlider.maxValue);
            }

            if (manaText != null)
            {
                manaText.text = BuildGaugeText("MP", current, max);
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
