/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 19:40
 * DESCRIPTION: Draws a runtime aim direction indicator for mouse and controller aiming.
 */

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAimIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Visual")]
    [SerializeField] private Color indicatorColor = new Color(1f, 0.86f, 0.2f, 0.95f);
    [SerializeField] private float indicatorStartOffset = 0.2f;
    [SerializeField] private float indicatorLength = 1.35f;
    [SerializeField] private float indicatorWidth = 0.06f;
    [SerializeField] private int sortingOrder = 30;

    [Header("Behavior")]
    [SerializeField] private bool showWhenIdle = true;
    [SerializeField] private bool useMoveDirectionFallback = true;

    private LineRenderer lineRenderer;
    private Vector2 lastDirection = Vector2.right;

    private void Reset()
    {
        playerAttack = GetComponent<PlayerAttack>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Awake()
    {
        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        EnsureLineRenderer();
        ApplyLineRendererStyle();
    }

    private void LateUpdate()
    {
        if (lineRenderer == null)
        {
            return;
        }

        bool hasDirection = TryResolveDirection(out Vector2 direction);
        if (!hasDirection && !showWhenIdle)
        {
            lineRenderer.enabled = false;
            return;
        }

        if (!hasDirection)
        {
            direction = lastDirection;
        }

        lineRenderer.enabled = true;
        lastDirection = direction.normalized;
        DrawIndicator(lastDirection);
    }

    private bool TryResolveDirection(out Vector2 direction)
    {
        direction = Vector2.zero;

        if (playerAttack != null && playerAttack.TryGetAimDirection(out Vector2 attackDirection))
        {
            direction = attackDirection.normalized;
            return direction.sqrMagnitude > 0.001f;
        }

        if (useMoveDirectionFallback && playerMovement != null && playerMovement.LastMoveDirection.sqrMagnitude > 0.001f)
        {
            direction = playerMovement.LastMoveDirection.normalized;
            return true;
        }

        return false;
    }

    private void DrawIndicator(Vector2 direction)
    {
        Vector3 origin = transform.position;
        Vector3 start = origin + (Vector3)(direction * Mathf.Max(0f, indicatorStartOffset));
        Vector3 end = start + (Vector3)(direction * Mathf.Max(0.01f, indicatorLength));

        start.z = origin.z;
        end.z = origin.z;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer != null)
        {
            return;
        }

        const string indicatorName = "AimIndicatorLine";
        Transform indicatorTransform = transform.Find(indicatorName);
        GameObject indicatorObject;

        if (indicatorTransform != null)
        {
            indicatorObject = indicatorTransform.gameObject;
        }
        else
        {
            indicatorObject = new GameObject(indicatorName);
            indicatorObject.transform.SetParent(transform, false);
        }

        lineRenderer = indicatorObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = indicatorObject.AddComponent<LineRenderer>();
        }
    }

    private void ApplyLineRendererStyle()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.numCapVertices = 4;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.widthMultiplier = Mathf.Max(0.001f, indicatorWidth);
        lineRenderer.startColor = indicatorColor;
        lineRenderer.endColor = indicatorColor;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.generateLightingData = false;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                lineRenderer.sharedMaterial = new Material(spriteShader);
            }
            else
            {
                Debug.LogWarning("PlayerAimIndicator: Could not find Sprites/Default shader.");
            }
        }
    }
}
