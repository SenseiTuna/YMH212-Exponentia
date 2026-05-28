/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 18:45
 * DESCRIPTION: Handles projectile attacks driven by centralized or fallback input.
 */

using Exponentia.InputSystem;
using Exponentia.InventorySystem;
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
    [SerializeField] private PlayerStatController statController;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Skill System")]
    [SerializeField] private GodSkillBase equippedSkill; // Inspector'dan degisebilecek aktif skill
    [SerializeField] private UnityEngine.UI.Image skillIconUI; // UI ustunde gosterilecek resim
    [SerializeField] private UnityEngine.UI.Image skillCooldownFillUI;

    [Header("Weapon System")]
    [SerializeField] private WeaponDefinition equippedWeaponDefinition;
    [SerializeField] private float defaultDamageMultiplier = 1f;

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
        statController = GetComponent<PlayerStatController>();
        playerInventory = GetComponent<PlayerInventory>();
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

        if (statController == null)
        {
            statController = GetComponent<PlayerStatController>();
        }

        if (playerInventory == null)
        {
            playerInventory = GetComponent<PlayerInventory>();
        }

        ResolveSkillUiReferences();

        if (equippedWeaponDefinition != null)
        {
            ApplyWeaponDefinition(equippedWeaponDefinition);
        }
    }

    private void Start()
    {
        ResolveEquippedSkillSource();
    }

    private void Update()
    {
        if (equippedSkill == null)
        {
            ResolveEquippedSkillSource();
        }

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

        if (useSkill && equippedSkill != null && !equippedSkill.IsPassiveSkill)
        {
            if (equippedSkill.CanActivate())
            {
                equippedSkill.TryActivate();
            }
        }
    }

    private void UpdateSkillUI()
    {
        ResolveSkillUiReferences();

        if (skillIconUI == null)
        {
            return;
        }

        bool hasSkillIcon = equippedSkill != null && equippedSkill.SkillIcon != null;
        if (hasSkillIcon)
        {
            skillIconUI.enabled = true;
            skillIconUI.sprite = equippedSkill.SkillIcon;
            skillIconUI.color = ResolveSkillIconColor();

            float cooldownNormalized = GetEquippedSkillCooldownNormalized();
            if (skillCooldownFillUI != null)
            {
                bool showCooldown = cooldownNormalized > 0.001f;
                skillCooldownFillUI.enabled = showCooldown;
                skillCooldownFillUI.sprite = equippedSkill.SkillIcon;
                skillCooldownFillUI.type = UnityEngine.UI.Image.Type.Filled;
                skillCooldownFillUI.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
                skillCooldownFillUI.fillOrigin = 2;
                skillCooldownFillUI.fillClockwise = false;
                skillCooldownFillUI.fillAmount = cooldownNormalized;
                skillCooldownFillUI.color = new Color(0f, 0f, 0f, 0.55f);
            }
        }
        else
        {
            skillIconUI.enabled = false;
            if (skillCooldownFillUI != null)
            {
                skillCooldownFillUI.enabled = false;
            }
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

        if (!playerMechanics.HarcaMana(laserManaCost))
        {
            return false;
        }

        float fireRate = ResolveFireRate();
        float fireInterval = fireRate > 0f ? 1f / fireRate : 1f;
        nextFireTime = Time.time + fireInterval;

        int projectileCount = ResolveProjectileCount();
        float spreadAngle = equippedWeaponDefinition != null ? Mathf.Max(0f, equippedWeaponDefinition.spreadAngle) : 0f;
        float projectileSpeed = ResolveProjectileSpeed();
        float projectileLifetime = ResolveProjectileLifetime();
        int pierceCount = equippedWeaponDefinition != null ? Mathf.Max(0, equippedWeaponDefinition.pierceCount) : 0;
        float damageMultiplier = ResolveDamageMultiplier();

        if (projectileCount <= 1)
        {
            SpawnProjectile(direction, projectileSpeed, projectileLifetime, damageMultiplier, pierceCount);
            return true;
        }

        float startAngle = -spreadAngle * 0.5f;
        float step = projectileCount > 1 ? spreadAngle / (projectileCount - 1) : 0f;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = startAngle + (step * i);
            Vector2 spreadDirection = Quaternion.Euler(0f, 0f, angle) * direction.normalized;
            SpawnProjectile(spreadDirection, projectileSpeed, projectileLifetime, damageMultiplier, pierceCount);
        }

        return true;
    }

    public void ApplyWeaponDefinition(WeaponDefinition weapon)
    {
        equippedWeaponDefinition = weapon;
    }

    public void SetEquippedSkill(GodSkillBase skill)
    {
        if (equippedSkill == skill)
        {
            return;
        }

        equippedSkill = skill;
    }

    public bool TryEquipSkillByDefinition(SkillDefinition skillDefinition)
    {
        if (skillDefinition == null)
        {
            return false;
        }

        GodSkillBase[] availableSkills = GetComponents<GodSkillBase>();
        GodSkillBase matchedSkill = null;

        if (IsConfiguredRuntimeSkillType(skillDefinition.linkedGodSkillType))
        {
            if (availableSkills != null)
            {
                for (int i = 0; i < availableSkills.Length; i++)
                {
                    if (availableSkills[i] is ConfiguredGodSkill configuredSkill &&
                        configuredSkill.SkillType == skillDefinition.linkedGodSkillType)
                    {
                        matchedSkill = configuredSkill;
                        break;
                    }
                }
            }

            matchedSkill = matchedSkill != null
                ? matchedSkill
                : ConfiguredGodSkill.CreateFromDefinition(gameObject, skillDefinition);
        }
        else
        {
            if (skillDefinition.linkedGodSkillType != GodSkillType.None && availableSkills != null)
            {
                for (int i = 0; i < availableSkills.Length; i++)
                {
                    if (availableSkills[i] != null && availableSkills[i].SkillType == skillDefinition.linkedGodSkillType)
                    {
                        matchedSkill = availableSkills[i];
                        break;
                    }
                }
            }

            if (matchedSkill == null && skillDefinition.linkedGodSkillType == GodSkillType.None && availableSkills != null)
            {
                for (int i = 0; i < availableSkills.Length; i++)
                {
                    GodSkillBase candidate = availableSkills[i];
                    if (candidate != null && MatchesSkillDefinition(candidate, skillDefinition))
                    {
                        matchedSkill = candidate;
                        break;
                    }
                }
            }
        }

        if (matchedSkill == null)
        {
            matchedSkill = ConfiguredGodSkill.CreateFromDefinition(gameObject, skillDefinition);
            if (matchedSkill == null)
            {
                return false;
            }
        }
        else if (matchedSkill is ConfiguredGodSkill configuredSkill)
        {
            configuredSkill.Configure(skillDefinition);
        }
        else
        {
            matchedSkill.ApplyInventoryDefinition(skillDefinition);
        }

        UpdateSkillUnlockStates(GetComponents<GodSkillBase>(), matchedSkill);
        SetEquippedSkill(matchedSkill);
        return true;
    }

    public string GetCurrentWeaponDisplayName()
    {
        return equippedWeaponDefinition != null ? equippedWeaponDefinition.displayName : "Laser";
    }

    public float GetEquippedSkillRemainingCooldown()
    {
        return equippedSkill != null ? equippedSkill.RemainingCooldown : 0f;
    }

    public float GetEquippedSkillCooldownNormalized()
    {
        if (equippedSkill == null || equippedSkill.Cooldown <= 0.001f)
        {
            return 0f;
        }

        return Mathf.Clamp01(equippedSkill.RemainingCooldown / equippedSkill.Cooldown);
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

    private static bool MatchesSkillDefinition(GodSkillBase candidate, SkillDefinition skillDefinition)
    {
        string candidateName = NormalizeSkillText(candidate.SkillName);
        string candidateType = NormalizeSkillText(candidate.SkillType.ToString());
        string definitionName = NormalizeSkillText(skillDefinition.displayName);
        string definitionId = NormalizeSkillText(skillDefinition.itemId);

        return
            (!string.IsNullOrEmpty(definitionName) &&
             (definitionName.Contains(candidateName) || candidateName.Contains(definitionName) ||
              definitionName.Contains(candidateType) || candidateType.Contains(definitionName))) ||
            (!string.IsNullOrEmpty(definitionId) &&
             (definitionId.Contains(candidateName) || definitionId.Contains(candidateType)));
    }

    private static bool IsConfiguredRuntimeSkillType(GodSkillType skillType)
    {
        return skillType >= GodSkillType.ZeusLightningStrike;
    }

    private void ResolveSkillUiReferences()
    {
        if (skillIconUI == null)
        {
            GameObject uiObj = GameObject.Find("SkillIconUI");
            if (uiObj != null)
            {
                skillIconUI = uiObj.GetComponent<UnityEngine.UI.Image>();
            }
        }

        if (skillCooldownFillUI != null || skillIconUI == null)
        {
            return;
        }

        GameObject fillObject = GameObject.Find("SkillCooldownFill");
        if (fillObject != null)
        {
            skillCooldownFillUI = fillObject.GetComponent<UnityEngine.UI.Image>();
        }

        if (skillCooldownFillUI == null)
        {
            Transform existingFill = skillIconUI.transform.Find("SkillCooldownFill");
            if (existingFill != null)
            {
                skillCooldownFillUI = existingFill.GetComponent<UnityEngine.UI.Image>();
            }
        }

        if (skillCooldownFillUI == null)
        {
            GameObject runtimeFill = new GameObject("SkillCooldownFill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            RectTransform fillRect = runtimeFill.GetComponent<RectTransform>();
            fillRect.SetParent(skillIconUI.transform, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            skillCooldownFillUI = runtimeFill.GetComponent<UnityEngine.UI.Image>();
            skillCooldownFillUI.raycastTarget = false;
            skillCooldownFillUI.enabled = false;
        }
    }

    private Color ResolveSkillIconColor()
    {
        if (equippedSkill == null)
        {
            return Color.white;
        }

        float cooldownNormalized = GetEquippedSkillCooldownNormalized();
        if (cooldownNormalized > 0.001f)
        {
            return new Color(1f, 1f, 1f, 0.45f);
        }

        if (!equippedSkill.IsPassiveSkill && !equippedSkill.CanActivate())
        {
            return new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        return Color.white;
    }

    private static string NormalizeSkillText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    private static void UpdateSkillUnlockStates(GodSkillBase[] availableSkills, GodSkillBase activeSkill)
    {
        if (availableSkills == null)
        {
            return;
        }

        for (int i = 0; i < availableSkills.Length; i++)
        {
            if (availableSkills[i] != null)
            {
                availableSkills[i].SetUnlocked(availableSkills[i] == activeSkill);
            }
        }
    }

    private void ResolveEquippedSkillSource()
    {
        if (playerInventory != null && playerInventory.EquippedSkill != null)
        {
            if (TryEquipSkillByDefinition(playerInventory.EquippedSkill))
            {
                return;
            }
        }

        if (equippedSkill == null)
        {
            equippedSkill = GetComponent<ZeusSkill>();
            if (equippedSkill == null)
            {
                equippedSkill = GetComponent<GodSkillBase>();
            }

            if (equippedSkill != null)
            {
                UpdateSkillUnlockStates(GetComponents<GodSkillBase>(), equippedSkill);
            }
        }
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

    private void SpawnProjectile(Vector2 direction, float speed, float lifeTime, float damageMultiplier, int pierceCount)
    {
        GameObject projectileObject;

        if (equippedWeaponDefinition != null && equippedWeaponDefinition.projectilePrefab != null)
        {
            projectileObject = Instantiate(
                equippedWeaponDefinition.projectilePrefab,
                transform.position + (Vector3)(direction.normalized * spawnOffset),
                Quaternion.identity);
        }
        else
        {
            projectileObject = new GameObject("PlayerLaser");
            projectileObject.transform.position = transform.position + (Vector3)(direction.normalized * spawnOffset);
        }

        PlayerProjectile projectile = projectileObject.GetComponent<PlayerProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<PlayerProjectile>();
        }

        projectile.Initialize(playerMechanics, direction, speed, lifeTime, damageMultiplier, pierceCount);
    }

    private float ResolveFireRate()
    {
        float weaponFireRate = equippedWeaponDefinition != null ? Mathf.Max(0.01f, equippedWeaponDefinition.fireRate) : 1f;
        float statFireRate = playerStats != null ? Mathf.Max(0.01f, playerStats.AttackSpeed) : 1f;
        return weaponFireRate * statFireRate;
    }

    private int ResolveProjectileCount()
    {
        int weaponCount = equippedWeaponDefinition != null ? Mathf.Max(1, equippedWeaponDefinition.projectileCount) : 1;
        int bonusCount = statController != null ? Mathf.Max(1, statController.ProjectileCount) : 1;
        return Mathf.Max(1, weaponCount * bonusCount);
    }

    private float ResolveProjectileSpeed()
    {
        if (equippedWeaponDefinition != null && equippedWeaponDefinition.projectileSpeed > 0f)
        {
            return equippedWeaponDefinition.projectileSpeed;
        }

        return playerStats != null ? Mathf.Max(0f, playerStats.ProjectileSpeed) : 0f;
    }

    private float ResolveProjectileLifetime()
    {
        if (equippedWeaponDefinition != null && equippedWeaponDefinition.projectileLifetime > 0f)
        {
            return equippedWeaponDefinition.projectileLifetime;
        }

        return laserLifetime;
    }

    private float ResolveDamageMultiplier()
    {
        if (equippedWeaponDefinition == null)
        {
            return Mathf.Max(0f, defaultDamageMultiplier);
        }

        return Mathf.Max(0f, equippedWeaponDefinition.damage);
    }
}
