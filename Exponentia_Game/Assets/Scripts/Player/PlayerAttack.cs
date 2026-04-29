using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMechanics playerMechanics;

    [Header("Lazer Saldirisi")]
    [SerializeField] private float lazerManaMaliyeti = 10f;
    [SerializeField] private float lazerYasamSuresi = 1.5f;
    [SerializeField] private float spawnOffset = 0.6f;

    private float sonrakiAtesZamani;

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
        if (playerStats == null || playerMechanics == null || !playerMechanics.Yasiyor)
        {
            return false;
        }

        if (Time.time < sonrakiAtesZamani)
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

        if (!playerMechanics.HarcaMana(lazerManaMaliyeti))
        {
            return false;
        }

        float atesAraligi = playerStats.saldiriHizi > 0f ? 1f / playerStats.saldiriHizi : 1f;
        sonrakiAtesZamani = Time.time + atesAraligi;

        GameObject projectileObject = new GameObject("PlayerLaser");
        projectileObject.transform.position = transform.position + (Vector3)(direction.normalized * spawnOffset);

        PlayerProjectile projectile = projectileObject.AddComponent<PlayerProjectile>();
        projectile.Initialize(playerMechanics, direction, playerStats.projectileHizi, lazerYasamSuresi);
        return true;
    }
}
