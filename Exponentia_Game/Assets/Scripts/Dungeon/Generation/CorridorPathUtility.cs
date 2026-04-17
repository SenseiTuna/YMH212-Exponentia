/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.1
FILE       : CorridorPathUtility.cs
BUILD_DATE : 2026-04-17
====================================================
Açıklama:
İki yerleşmiş oda arasında corridor hücrelerini üretir.
Bu sürüm debug corridor üretimi içindir.
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

public static class CorridorPathUtility
{
    public static DoorDirection FromVector(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return DoorDirection.Up;
        if (dir == Vector2Int.right) return DoorDirection.Right;
        if (dir == Vector2Int.down) return DoorDirection.Down;
        return DoorDirection.Left;
    }

    public static DoorDirection GetOpposite(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.Up: return DoorDirection.Down;
            case DoorDirection.Right: return DoorDirection.Left;
            case DoorDirection.Down: return DoorDirection.Up;
            default: return DoorDirection.Right;
        }
    }

    public static List<Vector2Int> BuildCorridorCells(
        DebugPlacedRoom roomA,
        DebugPlacedRoom roomB,
        Vector2Int directionFromAtoB
    )
    {
        if (directionFromAtoB == Vector2Int.right || directionFromAtoB == Vector2Int.left)
        {
            return BuildHorizontalConnection(roomA, roomB, directionFromAtoB);
        }

        return BuildVerticalConnection(roomA, roomB, directionFromAtoB);
    }

    private static List<Vector2Int> BuildHorizontalConnection(
        DebugPlacedRoom roomA,
        DebugPlacedRoom roomB,
        Vector2Int directionFromAtoB
    )
    {
        int aMinY = roomA.BoundsMin.y;
        int aMaxY = roomA.BoundsMax.y;
        int bMinY = roomB.BoundsMin.y;
        int bMaxY = roomB.BoundsMax.y;

        bool hasOverlap = TryGetOverlap(aMinY, aMaxY, bMinY, bMaxY, out int overlapMin, out int overlapMax);

        int startY;
        int endY;

        if (hasOverlap)
        {
            int sharedY = Mathf.RoundToInt((overlapMin + overlapMax) * 0.5f);
            startY = sharedY;
            endY = sharedY;
        }
        else
        {
            startY = Mathf.RoundToInt((roomA.BoundsMin.y + roomA.BoundsMax.y) * 0.5f);
            endY = Mathf.RoundToInt((roomB.BoundsMin.y + roomB.BoundsMax.y) * 0.5f);
        }

        int startX;
        int endX;

        if (directionFromAtoB == Vector2Int.right)
        {
            startX = roomA.BoundsMax.x + 1;
            endX = roomB.BoundsMin.x - 1;
        }
        else
        {
            startX = roomA.BoundsMin.x - 1;
            endX = roomB.BoundsMax.x + 1;
        }

        Vector2Int start = new Vector2Int(startX, startY);
        Vector2Int end = new Vector2Int(endX, endY);

        return BuildManhattanPath(start, end, horizontalFirst: true);
    }

    private static List<Vector2Int> BuildVerticalConnection(
        DebugPlacedRoom roomA,
        DebugPlacedRoom roomB,
        Vector2Int directionFromAtoB
    )
    {
        int aMinX = roomA.BoundsMin.x;
        int aMaxX = roomA.BoundsMax.x;
        int bMinX = roomB.BoundsMin.x;
        int bMaxX = roomB.BoundsMax.x;

        bool hasOverlap = TryGetOverlap(aMinX, aMaxX, bMinX, bMaxX, out int overlapMin, out int overlapMax);

        int startX;
        int endX;

        if (hasOverlap)
        {
            int sharedX = Mathf.RoundToInt((overlapMin + overlapMax) * 0.5f);
            startX = sharedX;
            endX = sharedX;
        }
        else
        {
            startX = Mathf.RoundToInt((roomA.BoundsMin.x + roomA.BoundsMax.x) * 0.5f);
            endX = Mathf.RoundToInt((roomB.BoundsMin.x + roomB.BoundsMax.x) * 0.5f);
        }

        int startY;
        int endY;

        if (directionFromAtoB == Vector2Int.up)
        {
            startY = roomA.BoundsMax.y + 1;
            endY = roomB.BoundsMin.y - 1;
        }
        else
        {
            startY = roomA.BoundsMin.y - 1;
            endY = roomB.BoundsMax.y + 1;
        }

        Vector2Int start = new Vector2Int(startX, startY);
        Vector2Int end = new Vector2Int(endX, endY);

        return BuildManhattanPath(start, end, horizontalFirst: false);
    }

    private static bool TryGetOverlap(int aMin, int aMax, int bMin, int bMax, out int overlapMin, out int overlapMax)
    {
        overlapMin = Mathf.Max(aMin, bMin);
        overlapMax = Mathf.Min(aMax, bMax);
        return overlapMin <= overlapMax;
    }

    private static List<Vector2Int> BuildManhattanPath(Vector2Int start, Vector2Int end, bool horizontalFirst)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int current = start;

        result.Add(current);

        if (horizontalFirst)
        {
            while (current.x != end.x)
            {
                current.x += (end.x > current.x) ? 1 : -1;
                result.Add(current);
            }

            while (current.y != end.y)
            {
                current.y += (end.y > current.y) ? 1 : -1;
                if (result[result.Count - 1] != current)
                    result.Add(current);
            }
        }
        else
        {
            while (current.y != end.y)
            {
                current.y += (end.y > current.y) ? 1 : -1;
                result.Add(current);
            }

            while (current.x != end.x)
            {
                current.x += (end.x > current.x) ? 1 : -1;
                if (result[result.Count - 1] != current)
                    result.Add(current);
            }
        }

        return result;
    }
}