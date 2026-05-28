/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.0
FILE       : VisibleMacroGrid.cs
BUILD_DATE : 2026-04-17
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

public class VisibleMacroGrid : MonoBehaviour
{
    [Header("Grid Ayarları")]
    [SerializeField] private int width = 24;
    [SerializeField] private int height = 24;
    [SerializeField] private int macroCellTileSize = 14;
    [SerializeField] private float cellWorldSize = 1f;
    [SerializeField] private float cellPaddingMultiplier = 0.92f;

    [Header("Başlangıç")]
    [SerializeField] private bool buildOnStart = true;

    [Header("Renkler")]
    [SerializeField] private Color emptyColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color startColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color combatColor = new Color(0.85f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color treasureColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color bossColor = new Color(0.8f, 0.2f, 0.9f, 1f);
    [SerializeField] private Color corridorColor = new Color(0.15f, 0.75f, 1f, 1f);

    private MacroGrid _grid;
    private Dictionary<Vector2Int, MacroGridCellView> _views = new Dictionary<Vector2Int, MacroGridCellView>();
    private Transform _cellsRoot;

    public MacroGrid Grid => _grid;
    public float CellWorldSize => cellWorldSize;
    public int Width => width;
    public int Height => height;

    private void Start()
    {
        if (buildOnStart)
            BuildVisibleGrid();
    }

    [ContextMenu("Build Visible Grid")]
    public void BuildVisibleGrid()
    {
        ClearVisuals();

        _grid = new MacroGrid(width, height, macroCellTileSize);
        _views = new Dictionary<Vector2Int, MacroGridCellView>();

        _cellsRoot = new GameObject("GridCells").transform;
        _cellsRoot.SetParent(transform, false);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);

                GameObject go = new GameObject($"Cell_{x}_{y}");
                go.transform.SetParent(_cellsRoot, false);

                MacroGridCellView cellView = go.AddComponent<MacroGridCellView>();
                Vector3 worldPos = MacroToWorld(coord);

                cellView.Initialize(
                    coord,
                    worldPos,
                    cellWorldSize * cellPaddingMultiplier,
                    emptyColor
                );

                _views.Add(coord, cellView);
            }
        }
    }

    [ContextMenu("Reset Grid Colors")]
    public void ResetGridVisuals()
    {
        if (_grid == null)
            BuildVisibleGrid();

        _grid.ResetGrid();
        RefreshColors();
    }

    public Vector3 MacroToWorld(Vector2Int macroCell)
    {
        return MacroPointToWorld(new Vector2(macroCell.x, macroCell.y), 0f);
    }

    public Vector3 MacroPointToWorld(Vector2 macroPoint, float z = 0f)
    {
        float xOffset = (width - 1) * 0.5f;
        float yOffset = (height - 1) * 0.5f;

        float worldX = (macroPoint.x - xOffset) * cellWorldSize;
        float worldY = (macroPoint.y - yOffset) * cellWorldSize;

        return new Vector3(worldX, worldY, z);
    }

    public bool TryOccupyCells(List<Vector2Int> cells, string roomId)
    {
        if (_grid == null)
            BuildVisibleGrid();

        bool success = _grid.Occupy(cells, roomId);
        RefreshColors();
        return success;
    }

    public void RefreshColors()
    {
        if (_grid == null)
            return;

        foreach (KeyValuePair<Vector2Int, MacroGridCellView> kvp in _views)
        {
            MacroCellData cellData = _grid.GetCell(kvp.Key);
            kvp.Value.SetColor(GetColorForCell(cellData));
        }
    }

    private Color GetColorForCell(MacroCellData cell)
    {
        if (cell == null || !cell.IsOccupied)
            return emptyColor;

        if (cell.RoomId.StartsWith("CORRIDOR"))
            return corridorColor;

        if (cell.RoomId.StartsWith("START"))
            return startColor;

        if (cell.RoomId.StartsWith("TREASURE"))
            return treasureColor;

        if (cell.RoomId.StartsWith("BOSS"))
            return bossColor;

        return combatColor;
    }

    private void ClearVisuals()
    {
        Transform existing = transform.Find("GridCells");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }
    }
}