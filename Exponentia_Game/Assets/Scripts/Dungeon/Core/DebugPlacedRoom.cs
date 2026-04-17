/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.0
FILE       : DebugPlacedRoom.cs
BUILD_DATE : 2026-04-17
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DebugPlacedRoom
{
    public string RoomId;
    public RoomShapeType ShapeType;
    public Vector2Int Origin;
    public List<Vector2Int> WorldCells;

    public Vector2Int BoundsMin;
    public Vector2Int BoundsMax;

    public DebugPlacedRoom(string roomId, RoomShapeType shapeType, Vector2Int origin, List<Vector2Int> worldCells)
    {
        RoomId = roomId;
        ShapeType = shapeType;
        Origin = origin;
        WorldCells = worldCells;

        FootprintLibrary.GetBounds(WorldCells, out BoundsMin, out BoundsMax);
    }

    public Vector2 GetCenterMacro()
    {
        float centerX = (BoundsMin.x + BoundsMax.x) * 0.5f;
        float centerY = (BoundsMin.y + BoundsMax.y) * 0.5f;
        return new Vector2(centerX, centerY);
    }
}