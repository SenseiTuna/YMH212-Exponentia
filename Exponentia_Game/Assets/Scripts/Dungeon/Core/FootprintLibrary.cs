/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.0
FILE       : FootprintLibrary.cs
BUILD_DATE : 2026-04-17
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

public static class FootprintLibrary
{
    public static List<Vector2Int> GetShape(RoomShapeType shapeType)
    {
        switch (shapeType)
        {
            case RoomShapeType.OneByOne:
                return new List<Vector2Int>
                {
                    new Vector2Int(0, 0)
                };

            case RoomShapeType.TwoByTwo:
                return new List<Vector2Int>
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                };

            case RoomShapeType.ThreeByThree:
                return new List<Vector2Int>
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                    new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
                };

            case RoomShapeType.LShape:
                return new List<Vector2Int>
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1)
                };

            default:
                return new List<Vector2Int>
                {
                    new Vector2Int(0, 0)
                };
        }
    }

    public static List<Vector2Int> ToWorldCells(List<Vector2Int> localCells, Vector2Int origin)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int i = 0; i < localCells.Count; i++)
        {
            result.Add(origin + localCells[i]);
        }

        return result;
    }

    public static void GetBounds(List<Vector2Int> cells, out Vector2Int min, out Vector2Int max)
    {
        if (cells == null || cells.Count == 0)
        {
            min = Vector2Int.zero;
            max = Vector2Int.zero;
            return;
        }

        int minX = cells[0].x;
        int minY = cells[0].y;
        int maxX = cells[0].x;
        int maxY = cells[0].y;

        for (int i = 1; i < cells.Count; i++)
        {
            if (cells[i].x < minX) minX = cells[i].x;
            if (cells[i].y < minY) minY = cells[i].y;
            if (cells[i].x > maxX) maxX = cells[i].x;
            if (cells[i].y > maxY) maxY = cells[i].y;
        }

        min = new Vector2Int(minX, minY);
        max = new Vector2Int(maxX, maxY);
    }
}