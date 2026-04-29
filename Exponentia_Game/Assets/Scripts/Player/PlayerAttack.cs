/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.3.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 12:00
 * DESCRIPTION: Handles manual projectile attacks driven by input.
 */

using Exponentia.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMechanics playerMechanics;

    [Header("Laser Attack")]
    [SerializeField] private float laserManaCost = 0f;
    [SerializeField] private float laserLifetime = 1.5f;
    [SerializeField] private float spawnOffset = 0.6f;

    private float nextFireTime;

    private void Reset()
    {
        playerStats = GetComponent<PlayerStats>();
        playerMechanics = GetComponent<PlayerMechanics>();
    }

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerMechanics == null)
        {
            playerMechanics = GetComponent<PlayerMechanics>();
        }
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            return;
        }

        TryFireLaser();
    }

    private bool TryFireLaser()
    {
        // Turkish: Saldırı yapmadan önce null/ölü/mana/cooldown kontrollerini tek noktada tamamlıyoruz.
        if (playerStats == null || playerMechanics == null || !playerMechanics.Yasiyor)
        {
            return false;
        }

        if (Time.time < nextFireTime)
        {
            return false;
        }

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return false;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = Mathf.Abs(activeCamera.transform.position.z - transform.position.z);
        Vector3 mouseWorldPosition = activeCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 direction = (Vector2)(mouseWorldPosition - transform.position);

        if (direction.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        if (!playerMechanics.HarcaMana(laserManaCost))
        {
            return false;
        }

        float fireInterval = playerStats.AttackSpeed > 0f ? 1f / playerStats.AttackSpeed : 1f;
        nextFireTime = Time.time + fireInterval;

        GameObject projectileObject = new GameObject("PlayerLaser");
        projectileObject.transform.position = transform.position + (Vector3)(direction.normalized * spawnOffset);

        PlayerProjectile projectile = projectileObject.AddComponent<PlayerProjectile>();
        projectile.Initialize(playerMechanics, direction, playerStats.ProjectileSpeed, laserLifetime);
        return true;
    }
}
