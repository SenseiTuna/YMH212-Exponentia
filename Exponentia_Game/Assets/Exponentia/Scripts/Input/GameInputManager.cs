/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 18:30
 * DESCRIPTION: Centralized runtime input router and rebinding backend.
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Exponentia.InputSystem
{
    public sealed class GameInputManager : MonoBehaviour
    {
        public const string DefaultBindingsPlayerPrefsKey = "Exponentia.InputBindings";
        private const string DefaultActionsResourcePath = "Input/GameInputActions";

        public static GameInputManager Instance { get; private set; }

        [Header("Asset Source")]
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private string inputActionsResourcePath = DefaultActionsResourcePath;

        [Header("Lifecycle")]
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private bool enableActionMapsOnAwake = true;
        [SerializeField] private bool autoLoadSavedBindings = true;
        [SerializeField] private string playerPrefsBindingsKey = DefaultBindingsPlayerPrefsKey;

        [Header("Action Maps")]
        [SerializeField] private string gameplayMapName = "Gameplay";
        [SerializeField] private string uiMapName = "UI";

        private InputAction moveAction;
        private InputAction aimAction;
        private InputAction attackAction;
        private InputAction secondaryAttackAction;
        private InputAction dashAction;
        private InputAction skill1Action;
        private InputAction skill2Action;
        private InputAction interactAction;
        private InputAction openMapAction;
        private InputAction gameplayPauseAction;
        private InputAction uiPauseAction;
        private InputAction uiBackAction;

        private InputActionRebindingExtensions.RebindingOperation activeRebindOperation;

        private int attackPressedFrame = int.MinValue;
        private int secondaryAttackPressedFrame = int.MinValue;
        private int dashPressedFrame = int.MinValue;
        private int skill1PressedFrame = int.MinValue;
        private int skill2PressedFrame = int.MinValue;
        private int interactPressedFrame = int.MinValue;
        private int openMapPressedFrame = int.MinValue;
        private int pausePressedFrame = int.MinValue;
        private int backPressedFrame = int.MinValue;

        public Vector2 MoveValue => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 AimValue => aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;

        public bool AttackHeld => attackAction != null && attackAction.IsPressed();
        public bool SecondaryAttackHeld => secondaryAttackAction != null && secondaryAttackAction.IsPressed();
        public bool DashHeld => dashAction != null && dashAction.IsPressed();
        public bool Skill1Held => skill1Action != null && skill1Action.IsPressed();
        public bool Skill2Held => skill2Action != null && skill2Action.IsPressed();
        public bool InteractHeld => interactAction != null && interactAction.IsPressed();
        public bool OpenMapHeld => openMapAction != null && openMapAction.IsPressed();

        public bool AttackPressedThisFrame => attackPressedFrame == Time.frameCount;
        public bool SecondaryAttackPressedThisFrame => secondaryAttackPressedFrame == Time.frameCount;
        public bool DashPressedThisFrame => dashPressedFrame == Time.frameCount;
        public bool Skill1PressedThisFrame => skill1PressedFrame == Time.frameCount;
        public bool Skill2PressedThisFrame => skill2PressedFrame == Time.frameCount;
        public bool InteractPressedThisFrame => interactPressedFrame == Time.frameCount;
        public bool OpenMapPressedThisFrame => openMapPressedFrame == Time.frameCount;
        public bool PausePressedThisFrame => pausePressedFrame == Time.frameCount;
        public bool BackPressedThisFrame => backPressedFrame == Time.frameCount;

        public event Action OnAttackPerformed;
        public event Action OnSecondaryAttackPerformed;
        public event Action OnDashPerformed;
        public event Action OnSkill1Performed;
        public event Action OnSkill2Performed;
        public event Action OnInteractPerformed;
        public event Action OnOpenMapPerformed;
        public event Action OnPausePerformed;
        public event Action OnBackPerformed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            // Turkish: Sahne gecislerinde input kaybolmamasi icin manager'i otomatik olusturuyoruz.
            var go = new GameObject("GameInputManager");
            go.AddComponent<GameInputManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureInputAssetLoaded();
            CacheActions();
            RegisterActionCallbacks();

            if (autoLoadSavedBindings)
            {
                LoadBindings();
            }

            if (enableActionMapsOnAwake)
            {
                EnableAllActionMaps();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            CancelActiveRebind();
            UnregisterActionCallbacks();
        }

        public bool ConsumeAttackPressedThisFrame()
        {
            return ConsumeFrameFlag(ref attackPressedFrame);
        }

        public bool ConsumeSecondaryAttackPressedThisFrame()
        {
            return ConsumeFrameFlag(ref secondaryAttackPressedFrame);
        }

        public bool ConsumeDashPressedThisFrame()
        {
            return ConsumeFrameFlag(ref dashPressedFrame);
        }

        public bool ConsumeSkill1PressedThisFrame()
        {
            return ConsumeFrameFlag(ref skill1PressedFrame);
        }

        public bool ConsumeSkill2PressedThisFrame()
        {
            return ConsumeFrameFlag(ref skill2PressedFrame);
        }

        public bool ConsumeInteractPressedThisFrame()
        {
            return ConsumeFrameFlag(ref interactPressedFrame);
        }

        public bool ConsumeOpenMapPressedThisFrame()
        {
            return ConsumeFrameFlag(ref openMapPressedFrame);
        }

        public bool ConsumePausePressedThisFrame()
        {
            return ConsumeFrameFlag(ref pausePressedFrame);
        }

        public bool ConsumeBackPressedThisFrame()
        {
            return ConsumeFrameFlag(ref backPressedFrame);
        }

        public void EnableAllActionMaps()
        {
            if (inputActionAsset == null)
            {
                return;
            }

            inputActionAsset.Enable();
        }

        public void DisableAllActionMaps()
        {
            if (inputActionAsset == null)
            {
                return;
            }

            inputActionAsset.Disable();
        }

        public void SaveBindings()
        {
            InputBindingManager.SaveBindings(inputActionAsset, playerPrefsBindingsKey);
        }

        public void LoadBindings()
        {
            InputBindingManager.LoadBindings(inputActionAsset, playerPrefsBindingsKey);
        }

        public void ResetBindingsToDefault()
        {
            InputBindingManager.ResetBindingsToDefault(inputActionAsset, playerPrefsBindingsKey);
        }

        public bool StartRebind(string actionName)
        {
            int bindingIndex = GetFirstRebindableBindingIndex(actionName);
            return StartRebind(actionName, bindingIndex, null, null);
        }

        public bool StartRebind(
            string actionName,
            int bindingIndex,
            Action<string> onCompleted,
            Action onCanceled)
        {
            if (activeRebindOperation != null)
            {
                Debug.LogWarning("GameInputManager.StartRebind ignored: another rebind is already running.");
                return false;
            }

            InputAction action = FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"GameInputManager.StartRebind failed: action not found ({actionName}).");
                return false;
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                Debug.LogWarning($"GameInputManager.StartRebind failed: invalid binding index {bindingIndex} for {action.name}.");
                return false;
            }

            if (action.bindings[bindingIndex].isComposite)
            {
                Debug.LogWarning($"GameInputManager.StartRebind failed: binding index {bindingIndex} is a composite root.");
                return false;
            }

            // Turkish: Rebind sirasinda yanlis input yakalamayi onlemek icin tum action map'leri gecici kapatiyoruz.
            DisableAllActionMaps();

            activeRebindOperation = action
                .PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .OnCancel(op =>
                {
                    CleanupRebindOperation();
                    EnableAllActionMaps();
                    onCanceled?.Invoke();
                })
                .OnComplete(op =>
                {
                    // Turkish: Ayni tusun baska aksiyonla cakisma durumunu yakalayip override'i geri aliyoruz.
                    if (InputBindingManager.TryFindBindingConflict(inputActionAsset, action, bindingIndex, out string conflictPath))
                    {
                        action.RemoveBindingOverride(bindingIndex);
                        Debug.LogWarning($"Rebind conflict: {action.name} conflicts with {conflictPath}. Override reverted.");
                    }
                    else
                    {
                        SaveBindings();
                    }

                    string bindingDisplay = action.GetBindingDisplayString(bindingIndex);
                    CleanupRebindOperation();
                    EnableAllActionMaps();
                    onCompleted?.Invoke(bindingDisplay);
                });

            activeRebindOperation.Start();
            return true;
        }

        public void CancelActiveRebind()
        {
            if (activeRebindOperation == null)
            {
                return;
            }

            activeRebindOperation.Cancel();
            CleanupRebindOperation();
            EnableAllActionMaps();
        }

        public string GetBindingDisplayString(string actionName, int bindingIndex = -1)
        {
            InputAction action = FindAction(actionName);
            if (action == null)
            {
                return string.Empty;
            }

            if (bindingIndex < 0)
            {
                bindingIndex = GetFirstRebindableBindingIndex(actionName);
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return string.Empty;
            }

            return action.GetBindingDisplayString(bindingIndex);
        }

        public InputActionAsset GetInputActionAsset()
        {
            return inputActionAsset;
        }

        private void EnsureInputAssetLoaded()
        {
            if (inputActionAsset != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(inputActionsResourcePath))
            {
                inputActionsResourcePath = DefaultActionsResourcePath;
            }

            inputActionAsset = Resources.Load<InputActionAsset>(inputActionsResourcePath);
            if (inputActionAsset == null)
            {
                Debug.LogError(
                    $"GameInputManager could not load InputActionAsset from Resources/{inputActionsResourcePath}. " +
                    "Assign an asset manually or place it under Resources.");
            }
        }

        private void CacheActions()
        {
            if (inputActionAsset == null)
            {
                return;
            }

            InputActionMap gameplayMap = inputActionAsset.FindActionMap(gameplayMapName, false);
            InputActionMap uiMap = inputActionAsset.FindActionMap(uiMapName, false);

            if (gameplayMap == null)
            {
                Debug.LogError($"GameInputManager: Gameplay action map not found ({gameplayMapName}).");
            }

            if (uiMap == null)
            {
                Debug.LogError($"GameInputManager: UI action map not found ({uiMapName}).");
            }

            moveAction = gameplayMap?.FindAction("Move", false);
            aimAction = gameplayMap?.FindAction("Aim", false);
            attackAction = gameplayMap?.FindAction("Attack", false);
            secondaryAttackAction = gameplayMap?.FindAction("SecondaryAttack", false);
            dashAction = gameplayMap?.FindAction("Dash", false);
            skill1Action = gameplayMap?.FindAction("Skill1", false);
            skill2Action = gameplayMap?.FindAction("Skill2", false);
            interactAction = gameplayMap?.FindAction("Interact", false);
            openMapAction = gameplayMap?.FindAction("OpenMap", false);
            gameplayPauseAction = gameplayMap?.FindAction("Pause", false);

            uiPauseAction = uiMap?.FindAction("Pause", false);
            uiBackAction = uiMap?.FindAction("Back", false);
        }

        private void RegisterActionCallbacks()
        {
            RegisterPerformed(attackAction, HandleAttackPerformed);
            RegisterPerformed(secondaryAttackAction, HandleSecondaryAttackPerformed);
            RegisterPerformed(dashAction, HandleDashPerformed);
            RegisterPerformed(skill1Action, HandleSkill1Performed);
            RegisterPerformed(skill2Action, HandleSkill2Performed);
            RegisterPerformed(interactAction, HandleInteractPerformed);
            RegisterPerformed(openMapAction, HandleOpenMapPerformed);
            RegisterPerformed(gameplayPauseAction, HandlePausePerformed);
            RegisterPerformed(uiPauseAction, HandlePausePerformed);
            RegisterPerformed(uiBackAction, HandleBackPerformed);
        }

        private void UnregisterActionCallbacks()
        {
            UnregisterPerformed(attackAction, HandleAttackPerformed);
            UnregisterPerformed(secondaryAttackAction, HandleSecondaryAttackPerformed);
            UnregisterPerformed(dashAction, HandleDashPerformed);
            UnregisterPerformed(skill1Action, HandleSkill1Performed);
            UnregisterPerformed(skill2Action, HandleSkill2Performed);
            UnregisterPerformed(interactAction, HandleInteractPerformed);
            UnregisterPerformed(openMapAction, HandleOpenMapPerformed);
            UnregisterPerformed(gameplayPauseAction, HandlePausePerformed);
            UnregisterPerformed(uiPauseAction, HandlePausePerformed);
            UnregisterPerformed(uiBackAction, HandleBackPerformed);
        }

        private InputAction FindAction(string actionName)
        {
            if (inputActionAsset == null || string.IsNullOrWhiteSpace(actionName))
            {
                return null;
            }

            InputAction directMatch = inputActionAsset.FindAction(actionName, false);
            if (directMatch != null)
            {
                return directMatch;
            }

            foreach (InputActionMap map in inputActionAsset.actionMaps)
            {
                foreach (InputAction action in map.actions)
                {
                    if (string.Equals(action.name, actionName, StringComparison.OrdinalIgnoreCase))
                    {
                        return action;
                    }
                }
            }

            return null;
        }

        private int GetFirstRebindableBindingIndex(string actionName)
        {
            InputAction action = FindAction(actionName);
            if (action == null)
            {
                return -1;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.path))
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        private static void RegisterPerformed(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
            {
                action.performed += callback;
            }
        }

        private static void UnregisterPerformed(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
            {
                action.performed -= callback;
            }
        }

        private void HandleAttackPerformed(InputAction.CallbackContext ctx)
        {
            attackPressedFrame = Time.frameCount;
            OnAttackPerformed?.Invoke();
        }

        private void HandleSecondaryAttackPerformed(InputAction.CallbackContext ctx)
        {
            secondaryAttackPressedFrame = Time.frameCount;
            OnSecondaryAttackPerformed?.Invoke();
        }

        private void HandleDashPerformed(InputAction.CallbackContext ctx)
        {
            dashPressedFrame = Time.frameCount;
            OnDashPerformed?.Invoke();
        }

        private void HandleSkill1Performed(InputAction.CallbackContext ctx)
        {
            skill1PressedFrame = Time.frameCount;
            OnSkill1Performed?.Invoke();
        }

        private void HandleSkill2Performed(InputAction.CallbackContext ctx)
        {
            skill2PressedFrame = Time.frameCount;
            OnSkill2Performed?.Invoke();
        }

        private void HandleInteractPerformed(InputAction.CallbackContext ctx)
        {
            interactPressedFrame = Time.frameCount;
            OnInteractPerformed?.Invoke();
        }

        private void HandleOpenMapPerformed(InputAction.CallbackContext ctx)
        {
            openMapPressedFrame = Time.frameCount;
            OnOpenMapPerformed?.Invoke();
        }

        private void HandlePausePerformed(InputAction.CallbackContext ctx)
        {
            pausePressedFrame = Time.frameCount;
            OnPausePerformed?.Invoke();
        }

        private void HandleBackPerformed(InputAction.CallbackContext ctx)
        {
            backPressedFrame = Time.frameCount;
            OnBackPerformed?.Invoke();
        }

        private void CleanupRebindOperation()
        {
            if (activeRebindOperation == null)
            {
                return;
            }

            activeRebindOperation.Dispose();
            activeRebindOperation = null;
        }

        private static bool ConsumeFrameFlag(ref int frameFlag)
        {
            if (frameFlag != Time.frameCount)
            {
                return false;
            }

            frameFlag = int.MinValue;
            return true;
        }
    }
}
