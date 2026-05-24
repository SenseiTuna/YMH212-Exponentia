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

    private HashSet<string> _visitedRoomIds = new HashSet<string>();
    private string _currentRoomId = "";
    private HashSet<string> _connectedRoomIds = new HashSet<string>();

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

    [ContextMenu("Reset Map Discovery")]
    public void ResetDiscovery()
    {
        _visitedRoomIds.Clear();
        _currentRoomId = "";
        _connectedRoomIds.Clear();
        _visitedRoomIds.Add("START_00");
        UpdateConnectedRooms();
        UpdateMapVisuals();
    }
}
