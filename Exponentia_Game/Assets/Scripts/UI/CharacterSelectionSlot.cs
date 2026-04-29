/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.3.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 12:00
 * DESCRIPTION: UI slot that displays one CharacterData and emits select events.
 */

using System;
using Exponentia.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Exponentia.UI
{
    public class CharacterSelectionSlot : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image gameplayImage;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statsText;
        [SerializeField] private Button selectButton;

        [Header("Visual States")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.55f, 1f);

        private CharacterData characterData;
        private Action<CharacterSelectionSlot, CharacterData> onSelect;

        public CharacterData BoundCharacterData => characterData;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(NotifySelected);
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(NotifySelected);
            }
        }

        public void Setup(CharacterData data, Action<CharacterSelectionSlot, CharacterData> onSelectCallback)
        {
            characterData = data;
            onSelect = onSelectCallback;
            RefreshView();
            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            if (cardBackground == null)
            {
                return;
            }

            cardBackground.color = isSelected ? selectedColor : normalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            NotifySelected();
        }

        private void NotifySelected()
        {
            // Turkish: Slot içinde data yoksa tıklamayı sessizce yutmak yerine açık uyarı veriyoruz.
            if (characterData == null)
            {
                Debug.LogWarning("CharacterSelectionSlot: characterData is null.");
                return;
            }

            onSelect?.Invoke(this, characterData);
        }

        private void RefreshView()
        {
            if (characterData == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrWhiteSpace(characterData.characterName)
                    ? characterData.characterId
                    : characterData.characterName;
            }

            if (gameplayImage != null)
            {
                gameplayImage.sprite = characterData.gameplaySprite != null
                    ? characterData.gameplaySprite
                    : characterData.portrait;
                gameplayImage.color = characterData.characterColor;
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = characterData.portrait != null
                    ? characterData.portrait
                    : characterData.gameplaySprite;
                portraitImage.color = Color.white;
            }

            if (statsText != null)
            {
                if (characterData.baseStats == null)
                {
                    statsText.text = "Stat verisi yok";
                }
                else
                {
                    StatBlock s = characterData.baseStats;
                    statsText.text = $"HP: {s.maxHealth:0}  DMG: {s.damage:0}\nSPD: {s.moveSpeed:0.0}  ASPD: {s.attackSpeed:0.0}";
                }
            }
        }
    }
}
