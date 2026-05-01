/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 18:30
 * DESCRIPTION: Lightweight bridge for player systems to consume centralized input.
 */

using UnityEngine;

namespace Exponentia.InputSystem
{
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Runtime Source")]
        [SerializeField] private GameInputManager inputManager;
        [SerializeField] private bool autoResolveManager = true;
        private bool missingManagerWarningLogged;

        public Vector2 MoveValue => inputManager != null ? inputManager.MoveValue : Vector2.zero;
        public Vector2 AimValue => inputManager != null ? inputManager.AimValue : Vector2.zero;

        public bool AttackHeld => inputManager != null && inputManager.AttackHeld;
        public bool SecondaryAttackHeld => inputManager != null && inputManager.SecondaryAttackHeld;
        public bool DashHeld => inputManager != null && inputManager.DashHeld;
        public bool Skill1Held => inputManager != null && inputManager.Skill1Held;
        public bool Skill2Held => inputManager != null && inputManager.Skill2Held;
        public bool InteractHeld => inputManager != null && inputManager.InteractHeld;
        public bool OpenMapHeld => inputManager != null && inputManager.OpenMapHeld;

        private void Awake()
        {
            ResolveManagerIfNeeded();
        }

        private void Update()
        {
            if (autoResolveManager && inputManager == null)
            {
                ResolveManagerIfNeeded();
            }
        }

        public bool ConsumeAttackPressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumeAttackPressedThisFrame();
        }

        public bool ConsumeSecondaryAttackPressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumeSecondaryAttackPressedThisFrame();
        }

        public bool ConsumeDashPressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumeDashPressedThisFrame();
        }

        public bool ConsumeSkill1PressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumeSkill1PressedThisFrame();
        }

        public bool ConsumeSkill2PressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumeSkill2PressedThisFrame();
        }

        public bool ConsumeInteractPressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumeInteractPressedThisFrame();
        }

        public bool ConsumeOpenMapPressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumeOpenMapPressedThisFrame();
        }

        public bool ConsumePausePressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumePausePressedThisFrame();
        }

        public bool ConsumeBackPressedThisFrame()
        {
            return inputManager != null && inputManager.ConsumeBackPressedThisFrame();
        }

        public void SetInputManager(GameInputManager manager)
        {
            inputManager = manager;
        }

        private void ResolveManagerIfNeeded()
        {
            if (inputManager != null)
            {
                missingManagerWarningLogged = false;
                return;
            }

            inputManager = GameInputManager.Instance;
            if (inputManager == null && autoResolveManager && !missingManagerWarningLogged)
            {
                Debug.LogWarning("PlayerInputReader could not resolve GameInputManager instance.");
                missingManagerWarningLogged = true;
            }
        }
    }
}
