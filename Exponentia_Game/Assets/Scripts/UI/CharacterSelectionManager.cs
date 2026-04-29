/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.3.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 12:00
 * DESCRIPTION: Binds character data to UI slots and handles scene transition on selection.
 */

using Exponentia.Core;
using Exponentia.Data;
using UnityEngine;
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
        [SerializeField] private string dungeonSceneName = "Test_Dungeon";
        [SerializeField] private bool loadSceneImmediately = true;

        private void Start()
        {
            BindSlots();
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
                    slot.Setup(playableCharacters[i], HandleCharacterSelected);
                }
            }
        }

        private void HandleCharacterSelected(CharacterSelectionSlot selectedSlot, CharacterData characterData)
        {
            // Turkish: Seçilen karakteri sahneler arası taşıyıcıya yazıyoruz.
            if (characterData == null)
            {
                Debug.LogWarning("CharacterSelectionManager: Selected character is null.");
                return;
            }

            SelectedCharacterHolder.SelectedCharacter = characterData;
            UpdateSelectionVisual(selectedSlot);

            // Turkish: Seçimden sonra hedef sahneye geçişi burada merkezi olarak yapıyoruz.
            if (loadSceneImmediately)
            {
                SceneManager.LoadScene(dungeonSceneName);
            }
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
    }
}
