using Exponentia.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Exponentia.UI
{
    public class DebugPanelController : MonoBehaviour
    {
        private const string RuntimeCanvasName = "RuntimeDebugCanvas";

        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text debugText;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerMechanics playerMechanics;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("Behavior")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private float refreshInterval = 0.1f;
        [SerializeField] private Vector2 panelPosition = new Vector2(90f, -50f);

        private float elapsed;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            Debug.Log("[DebugPanel] Awake called");
            ResolveReferences();
            FitPanelToScreen();
            SetPanelVisible(showOnStart);
            Debug.Log($"[DebugPanel] Initialized. Panel visible: {IsPanelVisible()}");
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                Debug.Log("[DebugPanel] F3 pressed, toggling panel");
                SetPanelVisible(!IsPanelVisible());
                Debug.Log($"[DebugPanel] Panel now visible: {IsPanelVisible()}");
            }

            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            {
                FitPanelToScreen();
            }

            // Try to resolve references if they're missing (for scene transitions)
            if (playerStats == null || playerMechanics == null || playerMovement == null)
            {
                ResolveReferences();
            }

            if (!IsPanelVisible())
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < refreshInterval)
            {
                return;
            }

            elapsed = 0f;
            RefreshDebugText();
        }

        private void ResolveReferences()
        {
            if (panelRoot == null)
            {
                Transform panelTransform = transform.Find("DebugPanel");
                if (panelTransform != null)
                {
                    panelRoot = panelTransform.gameObject;
                }
            }

            if (debugText == null)
            {
                debugText = GetComponentInChildren<Text>(true);
            }

            EnsureRuntimeUiIfMissing();

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerStats>()?.gameObject;
            }

            if (player == null)
            {
                Debug.LogWarning("[DebugPanel] Could not find Player");
                return;
            }

            Debug.Log("[DebugPanel] Found player, resolving components");

            if (playerStats == null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }

            if (playerMechanics == null)
            {
                playerMechanics = player.GetComponent<PlayerMechanics>();
            }

            if (playerMovement == null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
            }
        }

        private void EnsureRuntimeUiIfMissing()
        {
            if (panelRoot != null && debugText != null)
            {
                return;
            }

            Canvas canvas = FindOrCreateCanvas();
            if (canvas == null)
            {
                return;
            }

            if (panelRoot == null)
            {
                panelRoot = CreatePanelRoot(canvas.transform);
            }

            if (debugText == null && panelRoot != null)
            {
                debugText = panelRoot.GetComponentInChildren<Text>(true);

                if (debugText == null)
                {
                    debugText = CreateDebugText(panelRoot.transform);
                }
            }
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas existing = FindFirstObjectByType<Canvas>();
            if (existing != null)
            {
                return existing;
            }

            GameObject canvasObject = new GameObject(RuntimeCanvasName);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreatePanelRoot(Transform parent)
        {
            GameObject panel = new GameObject("DebugPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(90f, -50f);
            rect.sizeDelta = new Vector2(420f, 210f);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.72f);

            return panel;
        }

        private static Text CreateDebugText(Transform parent)
        {
            GameObject textObject = new GameObject("DebugText");
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(10f, 8f);
            rect.offsetMax = new Vector2(-10f, -8f);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "Debug panel initialized.";

            return text;
        }

        public void SetPanelVisible(bool isVisible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(isVisible);
            }

            if (isVisible)
            {
                FitPanelToScreen();
                RefreshDebugText();
            }
        }

        private void FitPanelToScreen()
        {
            if (panelRoot == null)
            {
                return;
            }

            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            float maxWidth = Mathf.Max(260f, safeArea.width - 24f);
            float maxHeight = Mathf.Max(120f, safeArea.height - 24f);

            float desiredWidth = Mathf.Min(Mathf.Max(320f, screenWidth * 0.32f), maxWidth);
            float desiredHeight = Mathf.Min(Mathf.Max(170f, screenHeight * 0.24f), maxHeight);

            panelRect.sizeDelta = new Vector2(desiredWidth, desiredHeight);

            float minX = Mathf.Max(0f, safeArea.x);
            float maxX = Mathf.Max(minX, safeArea.x + safeArea.width - desiredWidth);
            float minY = -Mathf.Max(0f, screenHeight - (safeArea.y + safeArea.height));
            float maxY = -Mathf.Max(0f, safeArea.y) - desiredHeight;

            float clampedX = Mathf.Clamp(panelPosition.x, minX, maxX);
            float clampedY = Mathf.Clamp(panelPosition.y, maxY, minY);
            panelRect.anchoredPosition = new Vector2(clampedX, clampedY);

            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        public bool IsPanelVisible()
        {
            return panelRoot == null || panelRoot.activeSelf;
        }

        public void RefreshDebugText()
        {
            if (debugText == null)
            {
                return;
            }

            debugText.text = BuildDebugInfo(
                Time.unscaledDeltaTime,
                playerStats,
                playerMechanics,
                playerMovement,
                GetPlayerPosition());
        }

        private Vector3 GetPlayerPosition()
        {
            return playerStats != null ? playerStats.transform.position : Vector3.zero;
        }

        public static string BuildDebugInfo(
            float frameDeltaTime,
            PlayerStats stats,
            PlayerMechanics mechanics,
            PlayerMovement movement,
            Vector3 position)
        {
            float fps = frameDeltaTime > 0.0001f ? 1f / frameDeltaTime : 0f;
            int level = stats != null ? stats.Level : 0;
            float xp = stats != null ? stats.Xp : 0f;
            float nextXp = stats != null ? stats.NextLevelXp : 0f;
            float hp = mechanics != null ? mechanics.MevcutCan : 0f;
            float mana = mechanics != null ? mechanics.MevcutMana : 0f;
            string scheme = movement != null ? movement.CurrentControlScheme.ToString() : "Unknown";

            return string.Format(
                "FPS: {0:0}\nPOS: ({1:0.00}, {2:0.00})\nHP: {3:0}/{4:0}\nMP: {5:0}/{6:0}\nLevel: {7} XP: {8:0}/{9:0}\nInput: {10}",
                fps,
                position.x,
                position.y,
                hp,
                stats != null ? stats.MaxHealth : 0f,
                mana,
                stats != null ? stats.Mana : 0f,
                level,
                xp,
                nextXp,
                scheme);
        }
    }

    public static class DebugPanelBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDebugPanel()
        {
            Debug.Log("[DebugPanel] Bootstrap: EnsureDebugPanel called");
            if (Object.FindFirstObjectByType<DebugPanelController>() != null)
            {
                Debug.Log("[DebugPanel] DebugPanelController already exists");
                return;
            }

            Debug.Log("[DebugPanel] Creating new DebugPanelController");

            GameObject root = new GameObject("DebugPanelController_Auto");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<DebugPanelController>();
        }
    }
}
