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
    public class CharacterSelectionSlot : MonoBehaviour, IPointerClickHandler, ISelectHandler, IPointerEnterHandler
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
        [SerializeField] private bool animateScaleOnSelect = true;
        [SerializeField] private float selectedScaleMultiplier = 1.04f;
        [SerializeField] private bool useOutlineIndicator = true;
        [SerializeField] private Color outlineSelectedColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Vector2 outlineDistance = new Vector2(4f, 4f);
        [SerializeField] private bool pulseOutline = true;
        [SerializeField] private float pulseSpeed = 6f;
        [SerializeField] private float pulseAlphaMin = 0.35f;
        [SerializeField] private float pulseAlphaMax = 1f;

        private CharacterData characterData;
        private Action<CharacterSelectionSlot, CharacterData> onSelect;
        private Action<CharacterSelectionSlot> onFocus;
        private Image resolvedSelectionImage;
        private Vector3 initialLocalScale;
        private Outline runtimeOutline;
        private bool isSelected;

        public CharacterData BoundCharacterData => characterData;
        public GameObject SelectionTarget => selectButton != null ? selectButton.gameObject : gameObject;

        private void Awake()
        {
            initialLocalScale = transform.localScale;
            EnsureOutlineIndicator();
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(NotifySelected);
            }
        }

        private void Update()
        {
            if (!pulseOutline || !isSelected || runtimeOutline == null)
            {
                return;
            }

            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Max(0.01f, pulseSpeed));
            float alpha = Mathf.Lerp(
                Mathf.Clamp01(pulseAlphaMin),
                Mathf.Clamp01(pulseAlphaMax),
                t);

            Color pulseColor = outlineSelectedColor;
            pulseColor.a = alpha;
            runtimeOutline.effectColor = pulseColor;
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(NotifySelected);
            }
        }

        public void Setup(
            CharacterData data,
            Action<CharacterSelectionSlot, CharacterData> onSelectCallback,
            Action<CharacterSelectionSlot> onFocusCallback = null)
        {
            characterData = data;
            onSelect = onSelectCallback;
            onFocus = onFocusCallback;
            RefreshView();
            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            this.isSelected = isSelected;
            Image selectionImage = ResolveSelectionImage();
            if (selectionImage != null)
            {
                selectionImage.color = isSelected ? selectedColor : normalColor;
            }

            if (animateScaleOnSelect)
            {
                float multiplier = isSelected ? Mathf.Max(1f, selectedScaleMultiplier) : 1f;
                transform.localScale = initialLocalScale * multiplier;
            }

            UpdateOutlineVisual(isSelected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            NotifySelected();
        }

        public void OnSelect(BaseEventData eventData)
        {
            onFocus?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onFocus?.Invoke(this);
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

        private Image ResolveSelectionImage()
        {
            if (resolvedSelectionImage != null)
            {
                return resolvedSelectionImage;
            }

            if (cardBackground != null)
            {
                resolvedSelectionImage = cardBackground;
                return resolvedSelectionImage;
            }

            if (selectButton != null && selectButton.targetGraphic is Image targetImage)
            {
                resolvedSelectionImage = targetImage;
                return resolvedSelectionImage;
            }

            resolvedSelectionImage = GetComponent<Image>();
            return resolvedSelectionImage;
        }

        private void EnsureOutlineIndicator()
        {
            if (!useOutlineIndicator)
            {
                return;
            }

            Image selectionImage = ResolveSelectionImage();
            if (selectionImage == null)
            {
                return;
            }

            runtimeOutline = selectionImage.GetComponent<Outline>();
            if (runtimeOutline == null)
            {
                runtimeOutline = selectionImage.gameObject.AddComponent<Outline>();
            }

            runtimeOutline.effectDistance = outlineDistance;
            UpdateOutlineVisual(false);
        }

        private void UpdateOutlineVisual(bool selected)
        {
            if (runtimeOutline == null)
            {
                return;
            }

            runtimeOutline.enabled = selected;
            if (!selected)
            {
                return;
            }

            Color outlineColor = outlineSelectedColor;
            if (!pulseOutline)
            {
                outlineColor.a = 1f;
            }

            runtimeOutline.effectColor = outlineColor;
            runtimeOutline.effectDistance = outlineDistance;
        }
    }
}
