/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.0
FILE       : MacroGrid.cs
BUILD_DATE : 2026-04-17
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

public class MacroGrid
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int MacroCellTileSize { get; private set; }

    private MacroCellData[,] _cells;

    public MacroGrid(int width, int height, int macroCellTileSize)
    {
        Initialize(width, height, macroCellTileSize);
    }

    public void Initialize(int width, int height, int macroCellTileSize)
    {
        Width = width;
        Height = height;
        MacroCellTileSize = macroCellTileSize;

        _cells = new MacroCellData[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _cells[x, y] = new MacroCellData(new Vector2Int(x, y));
            }
        }
    }

    public bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < Width &&
               cell.y >= 0 && cell.y < Height;
    }

    public bool IsOccupied(Vector2Int cell)
    {
        if (!IsInside(cell))
            return true;

        return _cells[cell.x, cell.y].IsOccupied;
    }

    public MacroCellData GetCell(Vector2Int cell)
    {
        if (!IsInside(cell))
            return null;

        return _cells[cell.x, cell.y];
    }

    public bool CanPlace(List<Vector2Int> worldCells)
    {
        if (worldCells == null || worldCells.Count == 0)
            return false;

        foreach (Vector2Int cell in worldCells)
        {
            if (!IsInside(cell))
                return false;

            if (IsOccupied(cell))
                return false;
        }

        return true;
    }

    public bool Occupy(List<Vector2Int> worldCells, string roomId)
    {
        if (!CanPlace(worldCells))
            return false;

        foreach (Vector2Int cell in worldCells)
        {
            _cells[cell.x, cell.y].Occupy(roomId);
        }

        return true;
    }

    public void Clear(List<Vector2Int> worldCells)
    {
        if (worldCells == null)
            return;

        foreach (Vector2Int cell in worldCells)
        {
            if (!IsInside(cell))
                continue;

            _cells[cell.x, cell.y].Clear();
        }
    }

    public void ResetGrid()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _cells[x, y].Clear();
            }
        }
    }
}