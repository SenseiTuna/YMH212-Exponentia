using Exponentia.Player;
using UnityEngine;

namespace Exponentia.UI
{
    public class DebugPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerMechanics playerMechanics;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("Behavior")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F4;
        private bool isVisible = false;
        
        private float deltaTime;

        // OYUN BASLADIGINDA HER YERDEN BAGIMSIZ OLARAK ZORLA KENDINI YARATIR (SILINEMEZ, BOZULAMAZ)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAutomatically()
        {
            GameObject debugObj = new GameObject("Runtime_Debug_Panel");
            debugObj.AddComponent<DebugPanelController>();
            DontDestroyOnLoad(debugObj);
        }

        public static string BuildDebugInfo(float deltaTime, PlayerStats stats, PlayerMechanics mechanics, PlayerMovement movement, Vector3 position)
        {
            float fps = 1.0f / Mathf.Max(deltaTime, 0.0001f);
            string info = $"FPS: {Mathf.Ceil(fps)}\nPosition: {position}\n";

            if (stats == null)
            {
                info += "Player Not Found!";
            }
            else
            {
                info += $"Health: {stats.CurrentHealth} / {stats.MaxHealth}\n";
                info += $"Mana: {stats.Mana}\n"; 
                
                if (mechanics != null)
                {
                    info += $"Alive: {mechanics.Yasiyor}\n";
                }
            }
            return info;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.F3))
            {
                isVisible = !isVisible;
            }

            if (isVisible)
            {
                deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
                
                if (playerStats == null || playerMechanics == null || playerMovement == null)
                {
                    GameObject player = GameObject.FindWithTag("Player");
                    if (player != null)
                    {
                        if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
                        if (playerMechanics == null) playerMechanics = player.GetComponent<PlayerMechanics>();
                        if (playerMovement == null) playerMovement = player.GetComponent<PlayerMovement>();
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!isVisible) 
                return;

            Vector3 pos = playerMovement != null ? playerMovement.transform.position : Vector3.zero;
            string debugText = BuildDebugInfo(deltaTime, playerStats, playerMechanics, playerMovement, pos);

            // 4K icin boyut olceklendirme carpanı (1080p baz alinarak 4K'da ekranina gore x2 buyur)
            float scale = Mathf.Max(1f, Screen.height / 1080f);

            // Stilleri (Yazi Fontlarini) 4K ekranlara gore buyutuyoruz
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(24 * scale) };
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * scale) };

            // Panel boyutunu cozunurluge gore ayarliyoruz (4K'da otomatik kalinlasip, devasa olacak)
            Rect rect = new Rect(20 * scale, 20 * scale, 450 * scale, 300 * scale);
            GUI.Box(rect, "Debug Panel", boxStyle);

            // Yazilarin baslayacagi ve hizalanacagi alan (basliktan hemen asagiya)
            GUILayout.BeginArea(new Rect(30 * scale, 30 * scale + boxStyle.fontSize * 1.5f, 430 * scale, 260 * scale));
            GUILayout.Label(debugText, labelStyle);
            GUILayout.EndArea();
        }
    }
}
