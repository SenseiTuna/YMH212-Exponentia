/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
====================================================
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class UiMinimap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleDungeonGenerator dungeonGenerator;
    [SerializeField] private VisibleMacroGrid visibleGrid;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private RectTransform playerIndicator;

    [Header("Fog of War Settings")]
    [SerializeField] private bool useFogOfWar = true;
    [SerializeField] private Color fogColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    [SerializeField] [Range(0.0f, 1.0f)] private float connectedRoomOpacity = 0.5f;

    private RectTransform _rectTransform;
    private HashSet<string> _visitedRoomIds = new HashSet<string>();
    private string _currentRoomId = "";
    private HashSet<string> _connectedRoomIds = new HashSet<string>();

    // Maps RoomId -> list of UI Fog Overlays
    private Dictionary<string, List<Image>> _fogOverlays = new Dictionary<string, List<Image>>();

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (dungeonGenerator == null)
            dungeonGenerator = FindAnyObjectByType<SimpleDungeonGenerator>();

        if (visibleGrid == null)
            visibleGrid = FindAnyObjectByType<VisibleMacroGrid>();
    }

    private void Start()
    {
        // Automatically create the Player Indicator if it wasn't assigned
        if (playerIndicator == null)
        {
            CreateDefaultPlayerIndicator();
        }

        // Initialize Fog of War
        if (useFogOfWar)
        {
            CreateFogOverlays();
        }

        // Start room is discovered by default
        DiscoverRoom("START_00");
    }

    private void Update()
    {
        // Try to find the player dynamically at runtime
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                return;
        }

        UpdatePlayerIndicatorPosition();
        DetectPlayerRoom();
    }

    private void UpdatePlayerIndicatorPosition()
    {
        if (playerIndicator == null || visibleGrid == null) return;

        Vector3 playerPos = playerTransform.position;

        // Total world dimensions of the grid
        float worldWidth = visibleGrid.Width * visibleGrid.CellWorldSize;
        float worldHeight = visibleGrid.Height * visibleGrid.CellWorldSize;

        // Map world X/Y coordinates to local RectTransform space (pivot centered)
        float localX = (playerPos.x / worldWidth) * _rectTransform.rect.width;
        float localY = (playerPos.y / worldHeight) * _rectTransform.rect.height;

        playerIndicator.anchoredPosition = new Vector2(localX, localY);
    }

    private void DetectPlayerRoom()
    {
        if (visibleGrid == null) return;

        Vector2Int playerMacro = visibleGrid.WorldToMacro(playerTransform.position);
        string newRoomId = "";

        if (visibleGrid.Grid != null)
        {
            var cell = visibleGrid.Grid.GetCell(playerMacro);
            if (cell != null && cell.IsOccupied)
            {
                newRoomId = cell.RoomId;
            }
        }

        // If player entered a new room/corridor
        if (!string.IsNullOrEmpty(newRoomId) && newRoomId != _currentRoomId)
        {
            _currentRoomId = newRoomId;

            if (!_visitedRoomIds.Contains(newRoomId))
            {
                DiscoverRoom(newRoomId);
            }
        }
    }

    private void DiscoverRoom(string roomId)
    {
        _visitedRoomIds.Add(roomId);
        Debug.Log($"[MinimapUI] Discovered: {roomId}");

        UpdateConnectedRooms();
        UpdateFogOverlays();
    }

    private void UpdateConnectedRooms()
    {
        _connectedRoomIds.Clear();
        if (dungeonGenerator == null) return;

        foreach (var conn in dungeonGenerator.RoomConnections)
        {
            bool isAVisited = _visitedRoomIds.Contains(conn.RoomIdA);
            bool isBVisited = _visitedRoomIds.Contains(conn.RoomIdB);

            if (isAVisited && !isBVisited)
            {
                _connectedRoomIds.Add(conn.RoomIdB);
            }
            else if (isBVisited && !isAVisited)
            {
                _connectedRoomIds.Add(conn.RoomIdA);
            }
        }
    }

    private void CreateFogOverlays()
    {
        if (dungeonGenerator == null || visibleGrid == null) return;

        // Create a root parent for Fog of War overlays to keep UI organized
        GameObject fogRootObj = new GameObject("FogOfWarRoot");
        RectTransform fogRoot = fogRootObj.AddComponent<RectTransform>();
        fogRoot.SetParent(_rectTransform, false);
        fogRoot.anchorMin = Vector2.zero;
        fogRoot.anchorMax = Vector2.one;
        fogRoot.offsetMin = Vector2.zero;
        fogRoot.offsetMax = Vector2.zero;
        fogRoot.SetAsFirstSibling(); // Draw underneath the Player Indicator but on top of the Map Image

        float worldWidth = visibleGrid.Width * visibleGrid.CellWorldSize;
        float worldHeight = visibleGrid.Height * visibleGrid.CellWorldSize;

        float xOffset = (visibleGrid.Width - 1) * 0.5f;
        float yOffset = (visibleGrid.Height - 1) * 0.5f;

        // Procedurally spawn fog overlays for each placed room
        foreach (var room in dungeonGenerator.PlacedRooms)
        {
            List<Image> overlays = new List<Image>();

            // For multi-cell rooms (like LShape or TwoByTwo), we can place an overlay for each cell,
            // or a single bounding box overlay. Placing per-cell is highly robust!
            foreach (var cell in room.WorldCells)
            {
                Image cellFog = CreateSingleCellFogOverlay(fogRoot, cell, xOffset, yOffset, worldWidth, worldHeight);
                if (cellFog != null)
                {
                    overlays.Add(cellFog);
                }
            }

            _fogOverlays.Add(room.RoomId, overlays);
        }

        // Procedurally spawn fog overlays for corridors
        // Since corridors are represented as individual occupied cells in visibleGrid.Grid,
        // let's scan all cells in the grid to find corridors.
        if (visibleGrid.Grid != null)
        {
            for (int x = 0; x < visibleGrid.Width; x++)
            {
                for (int y = 0; y < visibleGrid.Height; y++)
                {
                    var cell = visibleGrid.Grid.GetCell(new Vector2Int(x, y));
                    if (cell != null && cell.IsOccupied && cell.RoomId.StartsWith("CORRIDOR"))
                    {
                        Image corrFog = CreateSingleCellFogOverlay(fogRoot, new Vector2Int(x, y), xOffset, yOffset, worldWidth, worldHeight);
                        if (corrFog != null)
                        {
                            if (!_fogOverlays.ContainsKey(cell.RoomId))
                            {
                                _fogOverlays[cell.RoomId] = new List<Image>();
                            }
                            _fogOverlays[cell.RoomId].Add(corrFog);
                        }
                    }
                }
            }
        }
    }

    private Image CreateSingleCellFogOverlay(Transform parent, Vector2Int cell, float xOffset, float yOffset, float worldWidth, float worldHeight)
    {
        Vector2 centerMacro = new Vector2(cell.x, cell.y);

        // Convert macro grid to world position
        float worldX = (centerMacro.x - xOffset) * visibleGrid.CellWorldSize;
        float worldY = (centerMacro.y - yOffset) * visibleGrid.CellWorldSize;

        // Convert world position to local UI position
        float localX = (worldX / worldWidth) * _rectTransform.rect.width;
        float localY = (worldY / worldHeight) * _rectTransform.rect.height;

        // Cell visual scale in UI
        float uiCellWidth = (visibleGrid.CellWorldSize / worldWidth) * _rectTransform.rect.width;
        float uiCellHeight = (visibleGrid.CellWorldSize / worldHeight) * _rectTransform.rect.height;

        GameObject fogCell = new GameObject($"Fog_{cell.x}_{cell.y}");
        RectTransform rt = fogCell.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchoredPosition = new Vector2(localX, localY);
        rt.sizeDelta = new Vector2(uiCellWidth * 1.05f, uiCellHeight * 1.05f); // 1.05x scale to prevent gaps

        Image img = fogCell.AddComponent<Image>();
        img.color = fogColor;
        
        return img;
    }

    private void UpdateFogOverlays()
    {
        if (!useFogOfWar) return;

        foreach (var kvp in _fogOverlays)
        {
            string roomId = kvp.Key;
            List<Image> overlays = kvp.Value;

            float targetAlpha = 1.0f;

            if (_visitedRoomIds.Contains(roomId))
            {
                // Fully visited -> Reveal completely (make overlay fully transparent)
                targetAlpha = 0f;
            }
            else if (_connectedRoomIds.Contains(roomId))
            {
                // Connected unvisited -> Partially reveal (semi-transparent Fog of War hint)
                targetAlpha = 1.0f - connectedRoomOpacity;
            }
            else
            {
                // Unvisited & Not connected -> Completely dark
                targetAlpha = 1.0f;
            }

            foreach (var img in overlays)
            {
                if (img != null)
                {
                    // Update alpha channel smoothly or instantly
                    Color c = img.color;
                    c.a = targetAlpha;
                    img.color = c;
                    
                    // Deactivate GameObject if fully revealed to save drawcalls
                    img.gameObject.SetActive(targetAlpha > 0.01f);
                }
            }
        }
    }

    private void CreateDefaultPlayerIndicator()
    {
        GameObject indicatorObj = new GameObject("PlayerIndicator");
        playerIndicator = indicatorObj.AddComponent<RectTransform>();
        playerIndicator.SetParent(_rectTransform, false);
        playerIndicator.sizeDelta = new Vector2(16, 16);

        Image img = indicatorObj.AddComponent<Image>();
        
        // Let's create a beautiful red circle/dot for the player
        img.color = Color.red;
        
        // Find a default circle sprite if possible, otherwise use a default square colored red
        Sprite defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        if (defaultSprite != null)
        {
            img.sprite = defaultSprite;
        }

        playerIndicator.SetAsLastSibling(); // Draw on top of everything
    }

    [ContextMenu("Reset Minimap Discovery")]
    public void ResetDiscovery()
    {
        _visitedRoomIds.Clear();
        _currentRoomId = "";
        _connectedRoomIds.Clear();
        _visitedRoomIds.Add("START_00");
        UpdateConnectedRooms();
        UpdateFogOverlays();
    }
}
