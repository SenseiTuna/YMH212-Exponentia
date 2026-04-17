/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.0
FILE       : MacroCellData.cs
BUILD_DATE : 2026-04-17
====================================================
*/

using UnityEngine;

[System.Serializable]
public class MacroCellData
{
    public Vector2Int GridPosition;
    public bool IsOccupied;
    public string RoomId;

    public MacroCellData(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        IsOccupied = false;
        RoomId = string.Empty;
    }

    public void Occupy(string roomId)
    {
        IsOccupied = true;
        RoomId = roomId;
    }

    public void Clear()
    {
        IsOccupied = false;
        RoomId = string.Empty;
    }
}