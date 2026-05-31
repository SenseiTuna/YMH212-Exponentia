/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 19:40
 * DESCRIPTION: Draws a runtime aim direction indicator for mouse and controller aiming.
 */

using Exponentia.InventorySystem;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAimIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Header("Visual")]
    [SerializeField] private Color indicatorColor = new Color(1f, 0.86f, 0.2f, 0.95f);
    [SerializeField] private float indicatorStartOffset = 0.2f;
    [SerializeField] private float indicatorLength = 1.35f;
    [SerializeField] private float indicatorWidth = 0.06f;
    [SerializeField] private int sortingOrder = 30;

    [Header("Render Order")]
    [SerializeField] private bool renderBehindPlayerSprite = true;
    [SerializeField] private int sortingOrderOffsetFromPlayer = -1;

    [Header("Weapon Sprite Indicator")]
    [SerializeField] private bool useWeaponSpriteIndicator = true;
    [SerializeField] private float weaponSpriteDistance = 0.8f;
    [SerializeField] private float weaponSpriteScale = 0.55f;
    [SerializeField] private float weaponSpriteRotationOffsetDegrees = 0f;

    [Header("Behavior")]
    [SerializeField] private bool showWhenIdle = true;
    [SerializeField] private bool useMoveDirectionFallback = true;

    private LineRenderer lineRenderer;
    private SpriteRenderer weaponSpriteRenderer;
    private Vector2 lastDirection = Vector2.right;
    private Sprite activeWeaponSprite;
    private WeaponDefinition activeWeapon;

    private void Reset()
    {
        playerAttack = GetComponent<PlayerAttack>();
        playerMovement = GetComponent<PlayerMovement>();
        playerInventory = GetComponent<PlayerInventory>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        ResolveReferences();

        EnsureLineRenderer();
        EnsureWeaponSpriteRenderer();
        ApplyLineRendererStyle();
        ApplyIndicatorSorting();
        RefreshWeaponSprite();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (playerInventory != null)
        {
            playerInventory.OnWeaponChanged += HandleWeaponChanged;
        }
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnWeaponChanged -= HandleWeaponChanged;
        }
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
            SetWeaponSpriteVisible(false);
            return;
        }

        if (!hasDirection)
        {
            direction = lastDirection;
        }

        lastDirection = direction.normalized;
        ApplyIndicatorSorting();
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
        bool showWeaponSprite = ShouldShowWeaponSpriteIndicator();
        lineRenderer.enabled = !showWeaponSprite;
        SetWeaponSpriteVisible(showWeaponSprite);

        if (showWeaponSprite)
        {
            DrawWeaponSpriteIndicator(direction);
            return;
        }

        Vector3 origin = transform.position;
        Vector3 start = origin + (Vector3)(direction * Mathf.Max(0f, indicatorStartOffset));
        Vector3 end = start + (Vector3)(direction * Mathf.Max(0.01f, indicatorLength));

        start.z = origin.z;
        end.z = origin.z;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void DrawWeaponSpriteIndicator(Vector2 direction)
    {
        Vector3 origin = transform.position;
        float distance = activeWeapon != null ? activeWeapon.aimIndicatorDistance : weaponSpriteDistance;
        float scale = activeWeapon != null ? activeWeapon.aimIndicatorScale : weaponSpriteScale;
        float rotationOffset = activeWeapon != null ? activeWeapon.aimIndicatorRotationOffsetDegrees : weaponSpriteRotationOffsetDegrees;

        Vector3 position = origin + (Vector3)(direction * Mathf.Max(0f, distance));
        position.z = origin.z;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
        weaponSpriteRenderer.transform.position = position;
        weaponSpriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        weaponSpriteRenderer.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    private void ResolveReferences()
    {
        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (playerInventory == null)
        {
            playerInventory = GetComponent<PlayerInventory>();
        }

        if (playerSpriteRenderer == null)
        {
            playerSpriteRenderer = GetComponent<SpriteRenderer>();
        }
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
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.generateLightingData = false;
        ApplyIndicatorSorting();

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

    private void EnsureWeaponSpriteRenderer()
    {
        if (weaponSpriteRenderer != null)
        {
            return;
        }

        const string indicatorName = "AimIndicatorWeapon";
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

        weaponSpriteRenderer = indicatorObject.GetComponent<SpriteRenderer>();
        if (weaponSpriteRenderer == null)
        {
            weaponSpriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        }

        weaponSpriteRenderer.color = Color.white;
        weaponSpriteRenderer.enabled = false;
        ApplyIndicatorSorting();
    }

    private void ApplyIndicatorSorting()
    {
        int targetSortingLayerId = 0;
        int targetSortingOrder = sortingOrder;

        if (renderBehindPlayerSprite)
        {
            if (playerSpriteRenderer == null)
            {
                playerSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (playerSpriteRenderer != null)
            {
                targetSortingLayerId = playerSpriteRenderer.sortingLayerID;
                targetSortingOrder = playerSpriteRenderer.sortingOrder + sortingOrderOffsetFromPlayer;
            }
        }

        if (lineRenderer != null)
        {
            lineRenderer.sortingLayerID = targetSortingLayerId;
            lineRenderer.sortingOrder = targetSortingOrder;
        }

        if (weaponSpriteRenderer != null)
        {
            weaponSpriteRenderer.sortingLayerID = targetSortingLayerId;
            weaponSpriteRenderer.sortingOrder = targetSortingOrder;
        }
    }

    private void HandleWeaponChanged(WeaponDefinition weapon)
    {
        activeWeapon = weapon;
        activeWeaponSprite = ResolveWeaponIndicatorSprite(weapon);
        RefreshWeaponSprite();
    }

    private void RefreshWeaponSprite()
    {
        if (weaponSpriteRenderer == null)
        {
            return;
        }

        if (playerInventory != null && playerInventory.ActiveWeapon != null)
        {
            activeWeapon = playerInventory.ActiveWeapon;
            activeWeaponSprite = ResolveWeaponIndicatorSprite(activeWeapon);
        }

        weaponSpriteRenderer.sprite = activeWeaponSprite;
        SetWeaponSpriteVisible(false);
    }

    private Sprite ResolveWeaponIndicatorSprite(WeaponDefinition weapon)
    {
        if (weapon == null)
        {
            return null;
        }

        if (weapon.aimIndicatorSprite != null)
        {
            return weapon.aimIndicatorSprite;
        }

        if (weapon.icon != null)
        {
            return weapon.icon;
        }

        if (weapon.projectilePrefab != null && weapon.projectilePrefab.TryGetComponent(out SpriteRenderer rootSpriteRenderer))
        {
            return rootSpriteRenderer.sprite;
        }

        SpriteRenderer childSpriteRenderer = weapon.projectilePrefab != null
            ? weapon.projectilePrefab.GetComponentInChildren<SpriteRenderer>()
            : null;

        return childSpriteRenderer != null ? childSpriteRenderer.sprite : null;
    }

    private bool ShouldShowWeaponSpriteIndicator()
    {
        if (!useWeaponSpriteIndicator || activeWeaponSprite == null || weaponSpriteRenderer == null)
        {
            return false;
        }

        return activeWeapon == null || activeWeapon.useIconAsAimIndicator;
    }

    private void SetWeaponSpriteVisible(bool visible)
    {
        if (weaponSpriteRenderer != null)
        {
            weaponSpriteRenderer.enabled = visible;
        }
    }
}
