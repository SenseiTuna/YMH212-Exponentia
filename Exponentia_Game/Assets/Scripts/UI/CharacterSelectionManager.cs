/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 19:25
 * DESCRIPTION: Binds character data to UI slots and handles scene transition on selection.
 */

using Exponentia.Core;
using Exponentia.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Exponentia.UI
{
    public class CharacterSelectionManager : MonoBehaviour
    {
        [Header("Data Source")]
        [SerializeField] private CharacterData[] playableCharacters = new CharacterData[3];

        [Header("UI Slots")]
        [SerializeField] private CharacterSelectionSlot[] slots;

        [Header("Scene Flow")]
        [SerializeField] private string dungeonSceneName = "Dungeon";
        [SerializeField] private bool loadSceneImmediately = true;

        [Header("Navigation")]
        [SerializeField] private bool selectFirstSlotOnStart = true;
        [SerializeField] private bool syncSelectionFromEventSystem = true;

        private CharacterSelectionSlot highlightedSlot;

        private void Start()
        {
            BindSlots();
            if (selectFirstSlotOnStart)
            {
                SelectFirstSlotInEventSystem();
            }
        }

        private void Update()
        {
            if (!syncSelectionFromEventSystem)
            {
                return;
            }

            SyncHighlightFromEventSystem();
        }

        private void BindSlots()
        {
            if (slots == null || slots.Length == 0)
            {
                slots = GetComponentsInChildren<CharacterSelectionSlot>(true);
            }

            if (slots == null || slots.Length == 0)
            {
                Debug.LogError("CharacterSelectionManager: No CharacterSelectionSlot found.");
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                CharacterSelectionSlot slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                bool hasCharacter = i < playableCharacters.Length && playableCharacters[i] != null;
                slot.gameObject.SetActive(hasCharacter);

                if (hasCharacter)
                {
                    slot.Setup(playableCharacters[i], HandleCharacterSelected, HandleSlotFocused);
                }
            }
        }

        private void HandleCharacterSelected(CharacterSelectionSlot selectedSlot, CharacterData characterData)
        {
            // Turkish: Secilen karakteri sahneler arasi tasiyiciya yaziyoruz.
            if (characterData == null)
            {
                Debug.LogWarning("CharacterSelectionManager: Selected character is null.");
                return;
            }

            SelectedCharacterHolder.SelectedCharacter = characterData;
            UpdateSelectionVisual(selectedSlot);
            highlightedSlot = selectedSlot;

            // Turkish: Secimden sonra hedef sahneye gecisi burada merkezi olarak yapiyoruz.
            if (loadSceneImmediately)
            {
                SceneManager.LoadScene(dungeonSceneName);
            }
        }

        private void HandleSlotFocused(CharacterSelectionSlot focusedSlot)
        {
            if (focusedSlot == null)
            {
                return;
            }

            UpdateSelectionVisual(focusedSlot);
            highlightedSlot = focusedSlot;
        }

        private void UpdateSelectionVisual(CharacterSelectionSlot selectedSlot)
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                CharacterSelectionSlot slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                slot.SetSelected(slot == selectedSlot);
            }
        }

        private void SelectFirstSlotInEventSystem()
        {
            if (slots == null || slots.Length == 0)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                CharacterSelectionSlot slot = slots[i];
                if (slot == null || !slot.gameObject.activeInHierarchy || slot.BoundCharacterData == null)
                {
                    continue;
                }

                // Turkish: Controller ile menuye girildiginde bos ekran hissi olmamasi icin ilk karti secili aciyoruz.
                UpdateSelectionVisual(slot);
                highlightedSlot = slot;
                EventSystem currentEventSystem = EventSystem.current;
                if (currentEventSystem != null)
                {
                    currentEventSystem.SetSelectedGameObject(slot.SelectionTarget);
                }
                else
                {
                    Debug.LogWarning("CharacterSelectionManager: EventSystem.current is null.");
                }

                return;
            }
        }

        private void SyncHighlightFromEventSystem()
        {
            EventSystem currentEventSystem = EventSystem.current;
            if (currentEventSystem == null)
            {
                return;
            }

            GameObject currentSelectedObject = currentEventSystem.currentSelectedGameObject;
            if (currentSelectedObject == null)
            {
                return;
            }

            CharacterSelectionSlot focusedSlot = FindSlotBySelectedObject(currentSelectedObject);
            if (focusedSlot == null || focusedSlot == highlightedSlot)
            {
                return;
            }

            UpdateSelectionVisual(focusedSlot);
            highlightedSlot = focusedSlot;
        }

        private CharacterSelectionSlot FindSlotBySelectedObject(GameObject selectedObject)
        {
            if (slots == null || selectedObject == null)
            {
                return null;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                CharacterSelectionSlot slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                GameObject target = slot.SelectionTarget;
                if (target == selectedObject)
                {
                    return slot;
                }

                if (target != null && selectedObject.transform.IsChildOf(target.transform))
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
