/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.3.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 12:00
 * DESCRIPTION: Applies selected CharacterData to the runtime player object.
 */

using Exponentia.Data;
using UnityEngine;

namespace Exponentia.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCharacterApplier : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private PlayerStats playerStats;
        private PlayerMovement playerMovement;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerStats = GetComponent<PlayerStats>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        public void ApplyCharacter(CharacterData characterData)
        {
            // Turkish: Null seçimde sistemi patlatmak yerine güvenli uyarı verip işlemi durduruyoruz.
            if (characterData == null)
            {
                Debug.LogWarning("PlayerCharacterApplier: CharacterData is null.");
                return;
            }

            ApplyVisual(characterData);
            ApplyStats(characterData);

            Debug.Log("Applied character: " + characterData.characterName);
        }

        private void ApplyVisual(CharacterData characterData)
        {
            // Turkish: Oyun içi sprite standardı gameplaySprite; yoksa portrait fallback olarak kullanılır.
            Sprite visualSprite = characterData.gameplaySprite != null ? characterData.gameplaySprite : characterData.portrait;
            if (visualSprite != null)
            {
                spriteRenderer.sprite = visualSprite;
            }

            spriteRenderer.color = characterData.characterColor;
        }

        private void ApplyStats(CharacterData characterData)
        {
            // Turkish: Tek stat kaynağı CharacterData.baseStats'tır.
            if (characterData.baseStats == null)
            {
                Debug.LogError("PlayerCharacterApplier: baseStats is null on CharacterData.", this);
                return;
            }

            playerStats.ApplyFromStatBlock(characterData.baseStats);

            if (playerMovement != null)
            {
                playerMovement.SetMoveSpeed(playerStats.MoveSpeed);
            }

            PlayerMechanics mechanics = GetComponent<PlayerMechanics>();
            if (mechanics != null)
            {
                mechanics.SyncResourcesFromStats();
            }
        }
    }
}
