/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 18:30
 * DESCRIPTION: Saves, loads and validates input binding overrides.
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Exponentia.InputSystem
{
    public static class InputBindingManager
    {
        public static void SaveBindings(InputActionAsset inputActionAsset, string playerPrefsKey)
        {
            if (inputActionAsset == null)
            {
                Debug.LogError("InputBindingManager.SaveBindings failed: InputActionAsset is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(playerPrefsKey))
            {
                Debug.LogError("InputBindingManager.SaveBindings failed: PlayerPrefs key is empty.");
                return;
            }

            var data = new InputRebindData
            {
                schemaVersion = 1,
                bindingOverridesJson = inputActionAsset.SaveBindingOverridesAsJson(),
                savedAtUtc = DateTime.UtcNow.ToString("O")
            };

            string payload = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(playerPrefsKey, payload);
            PlayerPrefs.Save();
        }

        public static void LoadBindings(InputActionAsset inputActionAsset, string playerPrefsKey)
        {
            if (inputActionAsset == null)
            {
                Debug.LogError("InputBindingManager.LoadBindings failed: InputActionAsset is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(playerPrefsKey) || !PlayerPrefs.HasKey(playerPrefsKey))
            {
                return;
            }

            string payload = PlayerPrefs.GetString(playerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            InputRebindData data;
            try
            {
                data = JsonUtility.FromJson<InputRebindData>(payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"InputBindingManager.LoadBindings: invalid payload. {ex.Message}");
                return;
            }

            if (data == null || string.IsNullOrWhiteSpace(data.bindingOverridesJson))
            {
                return;
            }

            inputActionAsset.LoadBindingOverridesFromJson(data.bindingOverridesJson, true);
        }

        public static void ResetBindingsToDefault(InputActionAsset inputActionAsset, string playerPrefsKey)
        {
            if (inputActionAsset == null)
            {
                Debug.LogError("InputBindingManager.ResetBindingsToDefault failed: InputActionAsset is null.");
                return;
            }

            inputActionAsset.RemoveAllBindingOverrides();
            if (!string.IsNullOrWhiteSpace(playerPrefsKey))
            {
                PlayerPrefs.DeleteKey(playerPrefsKey);
                PlayerPrefs.Save();
            }
        }

        public static bool TryFindBindingConflict(
            InputActionAsset inputActionAsset,
            InputAction targetAction,
            int targetBindingIndex,
            out string conflictActionPath)
        {
            conflictActionPath = string.Empty;

            if (inputActionAsset == null || targetAction == null)
            {
                return false;
            }

            if (targetBindingIndex < 0 || targetBindingIndex >= targetAction.bindings.Count)
            {
                return false;
            }

            InputBinding targetBinding = targetAction.bindings[targetBindingIndex];
            if (targetBinding.isComposite)
            {
                return false;
            }

            string targetPath = GetEffectiveBindingPath(targetBinding);
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return false;
            }

            foreach (InputActionMap map in inputActionAsset.actionMaps)
            {
                foreach (InputAction action in map.actions)
                {
                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        if (action == targetAction && i == targetBindingIndex)
                        {
                            continue;
                        }

                        InputBinding otherBinding = action.bindings[i];
                        if (otherBinding.isComposite)
                        {
                            continue;
                        }

                        if (!BindingGroupsOverlap(targetBinding.groups, otherBinding.groups))
                        {
                            continue;
                        }

                        string otherPath = GetEffectiveBindingPath(otherBinding);
                        if (string.IsNullOrWhiteSpace(otherPath))
                        {
                            continue;
                        }

                        if (!string.Equals(targetPath, otherPath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        conflictActionPath = $"{map.name}/{action.name}";
                        return true;
                    }
                }
            }

            return false;
        }

        private static string GetEffectiveBindingPath(InputBinding binding)
        {
            if (!string.IsNullOrWhiteSpace(binding.overridePath))
            {
                return binding.overridePath;
            }

            if (!string.IsNullOrWhiteSpace(binding.effectivePath))
            {
                return binding.effectivePath;
            }

            return binding.path;
        }

        private static bool BindingGroupsOverlap(string firstGroups, string secondGroups)
        {
            if (string.IsNullOrWhiteSpace(firstGroups) || string.IsNullOrWhiteSpace(secondGroups))
            {
                return true;
            }

            string[] first = firstGroups.Split(';', StringSplitOptions.RemoveEmptyEntries);
            string[] second = secondGroups.Split(';', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < first.Length; i++)
            {
                for (int j = 0; j < second.Length; j++)
                {
                    if (string.Equals(first[i], second[j], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
