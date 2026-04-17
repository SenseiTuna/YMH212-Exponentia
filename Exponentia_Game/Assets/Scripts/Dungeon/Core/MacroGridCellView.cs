/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.0
FILE       : MacroGridCellView.cs
BUILD_DATE : 2026-04-17
====================================================
*/

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MacroGridCellView : MonoBehaviour
{
    public Vector2Int GridCoord { get; private set; }

    private SpriteRenderer _spriteRenderer;
    private static Sprite _cachedSprite;

    public void Initialize(Vector2Int gridCoord, Vector3 worldPosition, float worldSize, Color color)
    {
        GridCoord = gridCoord;

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        _spriteRenderer.sprite = GetDebugSprite();
        _spriteRenderer.color = color;

        transform.position = worldPosition;
        transform.localScale = new Vector3(worldSize, worldSize, 1f);
        gameObject.name = $"Cell_{gridCoord.x}_{gridCoord.y}";
    }

    public void SetColor(Color color)
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        _spriteRenderer.color = color;
    }

    private Sprite GetDebugSprite()
    {
        if (_cachedSprite != null)
            return _cachedSprite;

        _cachedSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        return _cachedSprite;
    }
}