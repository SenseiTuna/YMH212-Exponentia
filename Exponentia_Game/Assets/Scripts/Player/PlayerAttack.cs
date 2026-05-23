/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 18:45
 * DESCRIPTION: Handles projectile attacks driven by centralized or fallback input.
 */

using Exponentia.InputSystem;
using Exponentia.Player;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerAimIndicator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMechanics playerMechanics;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInputReader inputReader;

    [Header("Skill System")]
    [SerializeField] private GodSkillBase equippedSkill; // Inspector'dan degisebilecek aktif skill
    [SerializeField] private UnityEngine.UI.Image skillIconUI; // UI ustunde gosterilecek resim

    public GodSkillBase EquippedSkill => equippedSkill;

    [Header("Laser Attack")]
    [SerializeField] private float laserManaCost = 0f;
    [SerializeField] private float laserLifetime = 1.5f;
    [SerializeField] private float spawnOffset = 0.6f;
    [SerializeField] private float gamepadAimDeadzone = 0.12f;
    [SerializeField] private bool prioritizeControllerRightStick = true;
    [SerializeField] private bool useCentralInput = true;
    [SerializeField] private bool fallbackToMoveDirection = true;

    private float nextFireTime;
    private Vector2 lastAimDirection = Vector2.right;

    private void Reset()
    {
        playerStats = GetComponent<PlayerStats>();
        playerMechanics = GetComponent<PlayerMechanics>();
        playerMovement = GetComponent<PlayerMovement>();
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (playerMechanics == null)
            playerMechanics = GetComponent<PlayerMechanics>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
            if (inputReader == null && useCentralInput)
            {
                inputReader = gameObject.AddComponent<PlayerInputReader>();
            }
        }

        // OTOMATİK SKİLL BULMA (Eğer inspector üzerinden boş bırakıldıysa)
        if (equippedSkill == null)
        {
            equippedSkill = GetComponentInChildren<GodSkillBase>(); 
        }

        // OTOMATİK ARAYÜZ BULMA (Eğer inspector üzerinden boş bırakıldıysa)
        if (skillIconUI == null)
        {
            GameObject uiObj = GameObject.Find("SkillIconUI");
            if (uiObj != null)
            {
                skillIconUI = uiObj.GetComponent<UnityEngine.UI.Image>();
            }
        }
    }

    private void Update()
    {
        UpdateSkillUI();

        if (TryResolveAttackDirection(out Vector2 resolvedDirection))
        {
            lastAimDirection = resolvedDirection.normalized;
        }

        HandleSkillInput();

        if (!IsAttackInputHeld())
        {
            return;
        }

        TryFireLaser();
    }

    private void HandleSkillInput()
    {
        bool useSkill = false;
        
        // Skill1 tusu (Gamepad/Touch) veya direkt Space tusu
        if (inputReader != null && inputReader.ConsumeSkill1PressedThisFrame())
        {
            useSkill = true;
        }
        else if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            useSkill = true;
        }

        if (useSkill && equippedSkill != null)
        {
            if (equippedSkill.CanActivate())
            {
                equippedSkill.TryActivate();
            }
        }
    }

    private void UpdateSkillUI()
    {
        if (skillIconUI == null)
        {
            // Tembel arama (Lazy Find): Eğer UI sistemi Player'dan daha sonra oluşuyorsa Update içinde tekrar arıyoruz.
            GameObject uiObj = GameObject.Find("SkillIconUI");
            if (uiObj != null)
            {
                skillIconUI = uiObj.GetComponent<UnityEngine.UI.Image>();
            }

            if (skillIconUI == null) return;
        }

        // Eğer hala otomatik skill bulmadıysa burada tekrar deneyelim
        if (equippedSkill == null)
        {
            equippedSkill = GetComponentInChildren<GodSkillBase>(); 
        }

        if (equippedSkill != null && equippedSkill.SkillIcon != null)
        {
            skillIconUI.enabled = true;
            skillIconUI.sprite = equippedSkill.SkillIcon;

            // Bekleme (Cooldown) veya mana yetersizse ikon rengini yarim (gri/transparan) yap
            if (!equippedSkill.CanActivate())
            {
                skillIconUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            else
            {
                skillIconUI.color = Color.white;
            }
        }
        else
        {
            skillIconUI.enabled = false;
        }
    }

    private bool TryFireLaser()
    {
        // Turkish: Saldiri yapmadan once null, yasam durumu, mana ve cooldown kontrollerini tek noktada yapiyoruz.
        if (playerStats == null || playerMechanics == null || !playerMechanics.Yasiyor)
        {
            return false;
        }

        if (Time.time < nextFireTime)
        {
            return false;
        }

        if (!TryResolveAttackDirection(out Vector2 direction))
        {
            return false;
        }

        if (!playerMechanics.HarcaLaserMana(laserManaCost))
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

    public bool TryGetAimDirection(out Vector2 direction)
    {
        if (TryResolveAttackDirection(out direction))
        {
            lastAimDirection = direction.normalized;
            return true;
        }

        direction = lastAimDirection;
        return direction.sqrMagnitude > 0.001f;
    }

    private bool IsAttackInputHeld()
    {
        if (useCentralInput && inputReader != null)
        {
            return inputReader.AttackHeld;
        }

        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private bool TryResolveAttackDirection(out Vector2 direction)
    {
        direction = Vector2.zero;

        if (useCentralInput && inputReader != null)
        {
            // Turkish: Merkezi inputta Aim action'i gamepad icin yon, mouse icin ekran pozisyonu doner.
            if (TryResolveDirectionFromInputReader(out direction))
            {
                return true;
            }
        }

        return TryResolveDirectionFromMouse(out direction);
    }

    private bool TryResolveDirectionFromInputReader(out Vector2 direction)
    {
        direction = Vector2.zero;
        bool controllerSchemeActive = IsControllerSchemeActive();

        if (controllerSchemeActive && prioritizeControllerRightStick)
        {
            // Turkish: Controller aktifken aim'i dogrudan right stick'ten aliyoruz.
            if (TryResolveDirectionFromControllerStick(out direction))
            {
                return true;
            }
        }

        Vector2 aimValue = inputReader.AimValue;
        Camera activeCamera = Camera.main;

        // Turkish: Pointer pozisyonu geldiginde (mouse) ekran koordinatini dunya yonune ceviriyoruz.
        if (!controllerSchemeActive && activeCamera != null && aimValue.x >= 0f && aimValue.y >= 0f && aimValue.sqrMagnitude > 4f)
        {
            Vector3 screenPosition = new Vector3(
                aimValue.x,
                aimValue.y,
                Mathf.Abs(activeCamera.transform.position.z - transform.position.z));

            Vector3 worldPosition = activeCamera.ScreenToWorldPoint(screenPosition);
            Vector2 mouseDirection = (Vector2)(worldPosition - transform.position);
            if (mouseDirection.sqrMagnitude > 0.001f)
            {
                direction = mouseDirection.normalized;
                return true;
            }
        }

        // Turkish: Gamepad veya analoga benzer yon vektoru geldiginde dogrudan kullaniyoruz.
        if (!controllerSchemeActive && aimValue.sqrMagnitude >= gamepadAimDeadzone * gamepadAimDeadzone)
        {
            direction = aimValue.normalized;
            return true;
        }

        if (fallbackToMoveDirection && playerMovement != null && playerMovement.LastMoveDirection.sqrMagnitude > 0.001f)
        {
            direction = playerMovement.LastMoveDirection.normalized;
            return true;
        }

        return false;
    }

    private bool IsControllerSchemeActive()
    {
        if (playerMovement != null)
        {
            return playerMovement.CurrentControlScheme != PlayerMovement.ControlScheme.KeyboardMouse;
        }

        return Gamepad.current != null;
    }

    private bool TryResolveDirectionFromControllerStick(out Vector2 direction)
    {
        direction = Vector2.zero;

        float deadzoneSqr = gamepadAimDeadzone * gamepadAimDeadzone;
        float bestMagnitude = deadzoneSqr;
        Vector2 bestDirection = Vector2.zero;

        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            Gamepad gamepad = Gamepad.all[i];
            if (gamepad == null)
            {
                continue;
            }

            Vector2 stick = gamepad.rightStick.ReadValue();
            float sqrMagnitude = stick.sqrMagnitude;
            if (sqrMagnitude > bestMagnitude)
            {
                bestMagnitude = sqrMagnitude;
                bestDirection = stick;
            }
        }

        if (bestDirection.sqrMagnitude <= deadzoneSqr)
        {
            return false;
        }

        direction = bestDirection.normalized;
        return true;
    }

    private bool TryResolveDirectionFromMouse(out Vector2 direction)
    {
        direction = Vector2.zero;

        Camera activeCamera = Camera.main;
        if (activeCamera == null || Mouse.current == null)
        {
            return false;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = Mathf.Abs(activeCamera.transform.position.z - transform.position.z);
        Vector3 mouseWorldPosition = activeCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 mouseDirection = (Vector2)(mouseWorldPosition - transform.position);

        if (mouseDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        direction = mouseDirection.normalized;
        return true;
    }
}
