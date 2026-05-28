/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : DungeonMapManager.cs
BUILD_DATE : 2026-05-24
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

public class DungeonMapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleDungeonGenerator dungeonGenerator;
    [SerializeField] private VisibleMacroGrid visibleGrid;
    [SerializeField] private Transform playerTransform;

    [Header("Map Visual Settings")]
    [SerializeField] private bool useFogOfWar = true;
    [SerializeField] private Color hiddenCellColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    [SerializeField] [Range(0.0f, 1.0f)] private float connectedRoomOpacity = 0.35f;
    [SerializeField] private Color currentRoomHighlightColor = Color.yellow;
    [SerializeField] private bool pulseCurrentRoom = true;
    [SerializeField] private float pulseSpeed = 4.0f;
    [Header("Dünya Alanı Savaş Sisi (World Space Fog of War)")]
    [Tooltip("Dünya alanında gerçek odaların üstünü siyah kaplayacak sis sistemi aktif olsun mu?")]
    [SerializeField] private bool useWorldSpaceFog = true;
    [Tooltip("Sis katmanının çizim sırası (Sorting Order - Her şeyin üstünde olması için yüksek olmalıdır).")]
    [SerializeField] private int fogSortingOrder = 100;
    [Tooltip("Henüz keşfedilmemiş odaların üstündeki sisin opaklığı (0: şeffaf, 1: simsiyah).")]
    [SerializeField] [Range(0f, 1f)] private float unvisitedFogOpacity = 1.0f;
    [Tooltip("Bağlantılı (komşu) odaların üstündeki sisin opaklığı.")]
    [SerializeField] [Range(0f, 1f)] private float connectedFogOpacity = 0.55f;
    [Tooltip("Sisin açılma/kapanma geçiş hızı.")]
    [SerializeField] private float fogTransitionSpeed = 3.0f;

    private HashSet<string> _visitedRoomIds = new HashSet<string>();
    private string _currentRoomId = "";
    private HashSet<string> _connectedRoomIds = new HashSet<string>();
    private Dictionary<Vector2Int, SpriteRenderer> _worldFogRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
    private Sprite _fogSprite;

    public event System.Action<string> OnRoomEntered;
    public string CurrentRoomId => _currentRoomId;
    public HashSet<string> VisitedRoomIds => _visitedRoomIds;

    private void Awake()
    {
        if (dungeonGenerator == null)
            dungeonGenerator = FindAnyObjectByType<SimpleDungeonGenerator>();
        
        if (visibleGrid == null)
            visibleGrid = FindAnyObjectByType<VisibleMacroGrid>();
    }

    private void Start()
    {
        // Start room is discovered by default
        _visitedRoomIds.Add("START_00");
        UpdateConnectedRooms();
        UpdateMapVisuals();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                return;
        }

        DetectPlayerRoom();

        if (pulseCurrentRoom && !string.IsNullOrEmpty(_currentRoomId))
        {
            ApplyCurrentRoomPulse();
        }

        // Dünya alanı savaş sisini pürüzsüzce güncelle (Yumuşak açılma/kapanma)
        if (useWorldSpaceFog)
        {
            UpdateWorldFogVisuals();
        }
    }

    private void DetectPlayerRoom()
    {
        if (dungeonGenerator == null || visibleGrid == null) return;

        Vector2Int playerMacro = visibleGrid.WorldToMacro(playerTransform.position);
        string newRoomId = "";

        // Check which macro cell the player is currently occupying
        if (visibleGrid.Grid != null)
        {
            var cell = visibleGrid.Grid.GetCell(playerMacro);
            if (cell != null && cell.IsOccupied)
            {
                newRoomId = cell.RoomId;
            }
        }

        // If player has moved to a different room/corridor
        if (!string.IsNullOrEmpty(newRoomId) && newRoomId != _currentRoomId)
        {
            _currentRoomId = newRoomId;

            // Automatically discover this room/corridor if not already visited
            if (!_visitedRoomIds.Contains(newRoomId))
            {
                _visitedRoomIds.Add(newRoomId);
                Debug.Log($"[MapManager] New area discovered: {newRoomId}");
                UpdateConnectedRooms();
            }

            UpdateMapVisuals();
            OnRoomEntered?.Invoke(newRoomId);
        }
    }

    private void UpdateConnectedRooms()
    {
        _connectedRoomIds.Clear();
        if (dungeonGenerator == null) return;

        // Look for unvisited rooms that share a direct connection with any visited room
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

    public void UpdateMapVisuals()
    {
        if (visibleGrid == null || visibleGrid.Grid == null) return;

        foreach (var kvp in visibleGrid.CellViews)
        {
            Vector2Int coord = kvp.Key;
            MacroGridCellView view = kvp.Value;
            var cell = visibleGrid.Grid.GetCell(coord);

            // If empty cell, keep it dark/hidden
            if (cell == null || !cell.IsOccupied)
            {
                view.SetColor(hiddenCellColor);
                continue;
            }

            string roomId = cell.RoomId;

            if (!useFogOfWar)
            {
                view.SetColor(GetStandardRoomColor(roomId));
                continue;
            }

            // 1. Current Room (Mevcut Konum)
            if (roomId == _currentRoomId)
            {
                view.SetColor(currentRoomHighlightColor);
            }
            // 2. Visited Rooms & Corridors (Keşfedilmiş Odalar)
            else if (_visitedRoomIds.Contains(roomId))
            {
                view.SetColor(GetStandardRoomColor(roomId));
            }
            // 3. Connected Rooms (Bağlantılı Odalar - Fog of War)
            else if (_connectedRoomIds.Contains(roomId))
            {
                Color stdColor = GetStandardRoomColor(roomId);
                // Tint the room color to represent a semi-transparent fog of war hint
                Color foggyColor = Color.Lerp(stdColor, hiddenCellColor, 1f - connectedRoomOpacity);
                view.SetColor(foggyColor);
            }
            // 4. Locked/Hidden Rooms (Keşfedilmemiş/Karanlık)
            else
            {
                view.SetColor(hiddenCellColor);
            }
        }
    }

    private void ApplyCurrentRoomPulse()
    {
        if (visibleGrid == null || visibleGrid.Grid == null) return;

        float lerpFactor = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        Color stdColor = GetStandardRoomColor(_currentRoomId);
        Color pulseColor = Color.Lerp(stdColor, currentRoomHighlightColor, lerpFactor);

        foreach (var kvp in visibleGrid.CellViews)
        {
            var cell = visibleGrid.Grid.GetCell(kvp.Key);
            if (cell != null && cell.RoomId == _currentRoomId)
            {
                kvp.Value.SetColor(pulseColor);
            }
        }
    }

    private Color GetStandardRoomColor(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return hiddenCellColor;

        if (roomId.StartsWith("CORRIDOR"))
            return new Color(0.15f, 0.75f, 1f, 1f); // Corridor Color
        if (roomId.StartsWith("START"))
            return new Color(0.2f, 0.9f, 0.2f, 1f); // Start Room Color
        if (roomId.StartsWith("TREASURE"))
            return new Color(1f, 0.85f, 0.2f, 1f); // Treasure Room Color
        if (roomId.StartsWith("BOSS"))
            return new Color(0.8f, 0.2f, 0.9f, 1f); // Boss Room Color

        return new Color(0.85f, 0.25f, 0.25f, 1f); // Combat Room Color (Default)
    }

    /// <summary>
    /// Dünya alanındaki odaların üstüne siyah fiziksel sis bloklarını yerleştirir.
    /// </summary>
    private void InitializeWorldFog()
    {
        if (visibleGrid == null || visibleGrid.Grid == null) return;

        // Eski sis objelerini temizle
        Transform oldFogRoot = transform.Find("WorldSpaceFogRoot");
        if (oldFogRoot != null)
        {
            Destroy(oldFogRoot.gameObject);
        }

        GameObject fogRootObj = new GameObject("WorldSpaceFogRoot");
        fogRootObj.transform.SetParent(transform, false);
        Transform fogRoot = fogRootObj.transform;

        _worldFogRenderers.Clear();

        // Tüm işgal edilmiş (occupied) hücreler için siyah sis sprite'ı oluştur
        for (int x = 0; x < visibleGrid.Width; x++)
        {
            for (int y = 0; y < visibleGrid.Height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                var cell = visibleGrid.Grid.GetCell(coord);
                if (cell != null && cell.IsOccupied)
                {
                    GameObject fogTile = new GameObject($"Fog_{x}_{y}");
                    fogTile.transform.SetParent(fogRoot, false);
                    fogTile.transform.position = visibleGrid.MacroToWorld(coord);
                    
                    // Grid hücresi boyutunda ölçekle
                    float cellSize = visibleGrid.CellWorldSize;
                    fogTile.transform.localScale = new Vector3(cellSize * 1.05f, cellSize * 1.05f, 1f); // Boşluk kalmaması için hafif büyük

                    SpriteRenderer sr = fogTile.AddComponent<SpriteRenderer>();
                    sr.sprite = GetFogSprite();
                    sr.color = new Color(0f, 0f, 0f, unvisitedFogOpacity);
                    sr.sortingOrder = fogSortingOrder;

                    _worldFogRenderers.Add(coord, sr);
                }
            }
        }
    }

    /// <summary>
    /// Dünya alanındaki sisin açılış/kapanış geçişlerini pürüzsüzce günceller.
    /// </summary>
    private void UpdateWorldFogVisuals()
    {
        if (visibleGrid == null || visibleGrid.Grid == null) return;

        // Eğer henüz sis oluşturulmadıysa kurulumu yap
        if (_worldFogRenderers.Count == 0)
        {
            InitializeWorldFog();
        }

        foreach (var kvp in _worldFogRenderers)
        {
            Vector2Int coord = kvp.Key;
            SpriteRenderer sr = kvp.Value;
            if (sr == null) continue;

            var cell = visibleGrid.Grid.GetCell(coord);
            if (cell == null || !cell.IsOccupied) continue;

            string roomId = cell.RoomId;
            float targetOpacity = unvisitedFogOpacity;

            // 1. Oyuncunun İçinde Bulunduğu Oda (Sis Tamamen Kalkar)
            if (roomId == _currentRoomId)
            {
                targetOpacity = 0f;
            }
            // 2. Keşfedilmiş/Ziyaret Edilmiş Odalar (Sis Tamamen Kalkar)
            else if (_visitedRoomIds.Contains(roomId))
            {
                targetOpacity = 0f;
            }
            // 3. Bağlantılı/Görünen Komşu Odalar (Yarı Şeffaf Sis)
            else if (_connectedRoomIds.Contains(roomId))
            {
                targetOpacity = connectedFogOpacity;
            }
            // 4. Keşfedilmemiş/Uzak Odalar (Karanlık Sis)
            else
            {
                targetOpacity = unvisitedFogOpacity;
            }

            // Sisin rengini hedeflenen opaklığa doğru pürüzsüzce yumuşat (Lerp)
            Color c = sr.color;
            c.a = Mathf.MoveTowards(c.a, targetOpacity, Time.deltaTime * fogTransitionSpeed);
            sr.color = c;

            // Performansı korumak için sis tamamen açıldığında renderer bileşenini kapat
            sr.enabled = (c.a > 0.01f);
        }
    }

    private Sprite GetFogSprite()
    {
        if (_fogSprite != null) return _fogSprite;
        Texture2D tex = new Texture2D(2, 2);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        _fogSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return _fogSprite;
    }

    [ContextMenu("Reset Map Discovery")]
    public void ResetDiscovery()
    {
        _visitedRoomIds.Clear();
        _currentRoomId = "";
        _connectedRoomIds.Clear();
        _visitedRoomIds.Add("START_00");
        UpdateConnectedRooms();
        UpdateMapVisuals();

        if (useWorldSpaceFog)
        {
            InitializeWorldFog();
        }
    }
}
