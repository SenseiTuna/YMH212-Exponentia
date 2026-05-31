/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.3.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 12:00
 * DESCRIPTION: Applies selected CharacterData to the runtime player object.
 */

using Exponentia.Data;
using Exponentia.InventorySystem;
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
        private PlayerInventory playerInventory;

        private void Awake()
        {
            ResolveReferences();
        }

        public void ApplyCharacter(CharacterData characterData)
        {
            // Turkish: Prefab pasifken Awake calismayabilir; referanslari burada da cozuyoruz.
            ResolveReferences();

            // Turkish: Null seçimde sistemi patlatmak yerine güvenli uyarı verip işlemi durduruyoruz.
            if (characterData == null)
            {
                Debug.LogWarning("PlayerCharacterApplier: CharacterData is null.");
                return;
            }

            ApplyVisual(characterData);
            ApplyStats(characterData);
            ApplyStartingLoadout(characterData);

            Debug.Log("Applied character: " + characterData.characterName);
        }

        private void ResolveReferences()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
                }
            }

            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }

            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
            }
        }

        private void ApplyVisual(CharacterData characterData)
        {
            if (spriteRenderer == null)
            {
                Debug.LogError("PlayerCharacterApplier: No SpriteRenderer on this object or children.", this);
                return;
            }

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
            if (playerStats == null)
            {
                Debug.LogError("PlayerCharacterApplier: PlayerStats is missing.", this);
                return;
            }

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

        private void ApplyStartingLoadout(CharacterData characterData)
        {
            if (characterData.startingWeapon == null)
            {
                return;
            }

            if (playerInventory == null)
            {
                Debug.LogWarning("PlayerCharacterApplier: PlayerInventory is missing, starting weapon cannot be equipped.", this);
                return;
            }

            playerInventory.AddWeapon(characterData.startingWeapon);
        }
    }
}
