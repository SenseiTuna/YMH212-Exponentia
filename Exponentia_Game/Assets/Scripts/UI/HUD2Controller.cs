using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Exponentia.Player;

namespace Exponentia.UI
{
    [DisallowMultipleComponent]
    public class HUD2Controller : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerMechanics playerMechanics;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("UI Bindings")]
        public Image[] shieldIcons = new Image[3];
        public TextMeshProUGUI currencyText;

        private float updateInterval = 0.05f;
        private float nextUpdateTime;

        private void Awake()
        {
            FindPlayerReferences();
            AutoBindUIComponents();
        }

        private void Start()
        {
            FindPlayerReferences();
            UpdateHUD();
        }

        private void Update()
        {
            if (Time.time >= nextUpdateTime)
            {
                nextUpdateTime = Time.time + updateInterval;
                UpdateHUD();
            }
        }

        private void FindPlayerReferences()
        {
            if (!autoFindPlayer) return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
                if (playerMechanics == null) playerMechanics = player.GetComponent<PlayerMechanics>();
            }

            // Fallback: search globally if tag lookup didn't resolve references
            if (playerStats == null) playerStats = Object.FindFirstObjectByType<PlayerStats>();
            if (playerMechanics == null) playerMechanics = Object.FindFirstObjectByType<PlayerMechanics>();
        }

        public void AutoBindUIComponents()
        {
            // Find active Canvas to scope the search efficiently
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            
            // 1. Dynamic Kalkan/Shield Search
            var foundIcons = new System.Collections.Generic.List<Image>();
            Image[] candidateImages = null;

            if (canvas != null)
            {
                candidateImages = canvas.GetComponentsInChildren<Image>(true);
            }
            else
            {
                candidateImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            if (candidateImages != null)
            {
                foreach (var img in candidateImages)
                {
                    if (img == null) continue;
                    string nameLower = img.gameObject.name.ToLower();
                    if (nameLower.Contains("shield") || nameLower.Contains("kalkan") || nameLower.Contains("armor"))
                    {
                        foundIcons.Add(img);
                    }
                }
            }

            // Fallback to child-based search if global canvas scan found nothing
            if (foundIcons.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.StartsWith("ShieldIcon"))
                    {
                        var img = child.GetComponent<Image>();
                        if (img != null) foundIcons.Add(img);
                    }
                }
            }

            // Sort icons alphabetically by GameObject name so that e.g. ShieldIcon_0 is index 0
            if (foundIcons.Count > 0)
            {
                foundIcons.Sort((a, b) => string.Compare(a.gameObject.name, b.gameObject.name, System.StringComparison.OrdinalIgnoreCase));
                shieldIcons = foundIcons.ToArray();
            }

            // 2. Dynamic Gold/Para Text Search
            TextMeshProUGUI foundText = null;
            TextMeshProUGUI[] candidateTexts = null;

            if (canvas != null)
            {
                candidateTexts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            }
            else
            {
                candidateTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            if (candidateTexts != null)
            {
                foreach (var txt in candidateTexts)
                {
                    if (txt == null) continue;
                    string nameLower = txt.gameObject.name.ToLower();
                    if (nameLower.Contains("gold") || nameLower.Contains("para") || nameLower.Contains("currency") || nameLower.Contains("coin"))
                    {
                        foundText = txt;
                        // Prioritize perfect matches containing both "gold" and "text" or similar
                        if (nameLower.Contains("goldtext") || nameLower.Contains("paratext"))
                        {
                            break;
                        }
                    }
                }
            }

            if (foundText != null)
            {
                currencyText = foundText;
            }
            else if (currencyText == null)
            {
                // Fallback to local children
                currencyText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void UpdateHUD()
        {
            if (playerStats == null || playerMechanics == null)
            {
                FindPlayerReferences();
            }

            // 1. Para (Gold) Sayacı Güncellemesi (3 basamaklı: "000")
            if (currencyText != null && playerStats != null)
            {
                currencyText.text = playerStats.Gold.ToString("D3");
            }

            // 2. Kalkan (Shield) Güncellemesi (Her kalkan ikonu tam bir katmanı temsil eder)
            if (playerStats != null && playerMechanics != null && shieldIcons != null)
            {
                float currentShield = playerMechanics.MevcutKalkan;

                for (int i = 0; i < shieldIcons.Length; i++)
                {
                    if (shieldIcons[i] != null)
                    {
                        if (currentShield >= i + 1)
                        {
                            // Katman tam olarak aktif (Neon Cyan)
                            shieldIcons[i].color = new Color(0.2f, 0.85f, 1f, 1f);
                        }
                        else if (currentShield > i)
                        {
                            // Katman hasar görmüş/kısmen aktif (Yarı saydam Cyan)
                            shieldIcons[i].color = new Color(0.2f, 0.85f, 1f, 0.5f);
                        }
                        else
                        {
                            // Katman tamamen tükenmiş (Koyu çelik gri silüet)
                            shieldIcons[i].color = new Color(0.12f, 0.12f, 0.15f, 0.65f);
                        }
                    }
                }
            }
        }
    }
}
