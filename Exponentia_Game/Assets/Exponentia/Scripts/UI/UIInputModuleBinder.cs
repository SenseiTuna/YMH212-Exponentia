/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 19:15
 * DESCRIPTION: Binds InputSystemUIInputModule to GameInputActions UI map at runtime.
 */

using Exponentia.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Exponentia.UI
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class UIInputModuleBinder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputSystemUIInputModule uiInputModule;
        [SerializeField] private GameInputManager gameInputManager;

        [Header("Action Map Names")]
        [SerializeField] private string uiMapName = "UI";

        private void Awake()
        {
            ApplyBinding();
        }

        private void OnEnable()
        {
            ApplyBinding();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBindersOnSceneLoad()
        {
            InputSystemUIInputModule[] modules = FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None);
            for (int i = 0; i < modules.Length; i++)
            {
                InputSystemUIInputModule module = modules[i];
                if (module == null)
                {
                    continue;
                }

                if (module.GetComponent<UIInputModuleBinder>() == null)
                {
                    module.gameObject.AddComponent<UIInputModuleBinder>();
                }
            }
        }

        public void ApplyBinding()
        {
            if (uiInputModule == null)
            {
                uiInputModule = GetComponent<InputSystemUIInputModule>();
            }

            if (uiInputModule == null)
            {
                uiInputModule = FindFirstObjectByType<InputSystemUIInputModule>();
            }

            if (uiInputModule == null)
            {
                Debug.LogWarning("UIInputModuleBinder: InputSystemUIInputModule not found.");
                return;
            }

            if (gameInputManager == null)
            {
                gameInputManager = GameInputManager.Instance;
            }

            if (gameInputManager == null)
            {
                Debug.LogWarning("UIInputModuleBinder: GameInputManager is not available.");
                return;
            }

            InputActionAsset inputAsset = gameInputManager.GetInputActionAsset();
            if (inputAsset == null)
            {
                Debug.LogWarning("UIInputModuleBinder: InputActionAsset is null.");
                return;
            }

            InputAction point = FindAction(inputAsset, uiMapName, "Point");
            InputAction move = FindAction(inputAsset, uiMapName, "Navigate");
            InputAction submit = FindAction(inputAsset, uiMapName, "Submit");
            InputAction cancel = FindAction(inputAsset, uiMapName, "Cancel");
            InputAction click = FindAction(inputAsset, uiMapName, "Click");
            InputAction rightClick = FindAction(inputAsset, uiMapName, "RightClick");
            InputAction middleClick = FindAction(inputAsset, uiMapName, "MiddleClick");
            InputAction scrollWheel = FindAction(inputAsset, uiMapName, "ScrollWheel");
            InputAction trackedPosition = FindAction(inputAsset, uiMapName, "TrackedDevicePosition");
            InputAction trackedRotation = FindAction(inputAsset, uiMapName, "TrackedDeviceOrientation");

            if (move == null || submit == null || cancel == null)
            {
                Debug.LogError("UIInputModuleBinder: Required UI actions (Navigate/Submit/Cancel) are missing.");
                return;
            }

            if (uiInputModule.actionsAsset == inputAsset &&
                uiInputModule.move != null && uiInputModule.move.action == move &&
                uiInputModule.submit != null && uiInputModule.submit.action == submit &&
                uiInputModule.cancel != null && uiInputModule.cancel.action == cancel)
            {
                return;
            }

            // Turkish: EventSystem'in action referanslarini merkezi GameInputActions standardina cekiyoruz.
            uiInputModule.actionsAsset = inputAsset;
            uiInputModule.point = ToReference(point);
            uiInputModule.move = ToReference(move);
            uiInputModule.submit = ToReference(submit);
            uiInputModule.cancel = ToReference(cancel);
            uiInputModule.leftClick = ToReference(click);
            uiInputModule.rightClick = ToReference(rightClick);
            uiInputModule.middleClick = ToReference(middleClick);
            uiInputModule.scrollWheel = ToReference(scrollWheel);
            uiInputModule.trackedDevicePosition = ToReference(trackedPosition);
            uiInputModule.trackedDeviceOrientation = ToReference(trackedRotation);
        }

        private static InputAction FindAction(InputActionAsset asset, string mapName, string actionName)
        {
            if (asset == null || string.IsNullOrWhiteSpace(mapName) || string.IsNullOrWhiteSpace(actionName))
            {
                return null;
            }

            InputActionMap map = asset.FindActionMap(mapName, false);
            return map?.FindAction(actionName, false);
        }

        private static InputActionReference ToReference(InputAction action)
        {
            return action != null ? InputActionReference.Create(action) : null;
        }
    }
}
