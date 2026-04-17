/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.0
FILE       : DebugConnectionView.cs
BUILD_DATE : 2026-04-17
====================================================
*/

using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DebugConnectionView : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    public void Initialize(Vector3 start, Vector3 end, Color color, float width)
    {
        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();

        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = width;
        _lineRenderer.endWidth = width;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.numCapVertices = 4;
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
        _lineRenderer.sortingOrder = 10;
    }
}