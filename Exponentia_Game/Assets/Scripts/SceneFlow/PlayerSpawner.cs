/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.3.0
 * BUILD_DATE: 2026-04-29
 * BUILD_TIME: 12:00
 * DESCRIPTION: Spawns PlayerBase and applies selected character data at scene start.
 */

using Exponentia.Core;
using Exponentia.Data;
using Exponentia.Player;
using UnityEngine;

namespace Exponentia.SceneFlow
{
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Spawn Setup")]
        [SerializeField] private GameObject playerBasePrefab;
        [SerializeField] private Transform spawnPoint;

        [Header("Fallback")]
        [SerializeField] private CharacterData fallbackCharacter;
        [SerializeField] private bool clearSelectionAfterSpawn;

        public GameObject SpawnedPlayer { get; private set; }

        private void Start()
        {
            SpawnPlayer();
        }

        public void SpawnPlayer()
        {
            if (SpawnedPlayer != null)
            {
                return;
            }

            if (playerBasePrefab == null)
            {
                Debug.LogError("PlayerSpawner: playerBasePrefab is not assigned.", this);
                return;
            }

            // Turkish: Önce seçimden gelen karakter, yoksa fallback karakteri kullanıyoruz.
            CharacterData selectedCharacter = SelectedCharacterHolder.SelectedCharacter;
            if (selectedCharacter == null)
            {
                selectedCharacter = fallbackCharacter;
                Debug.LogWarning("PlayerSpawner: SelectedCharacter is null, fallback character will be used.");
            }

            if (selectedCharacter == null)
            {
                Debug.LogError("PlayerSpawner: No selected or fallback character data found.", this);
                return;
            }

            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            SpawnedPlayer = Instantiate(playerBasePrefab, spawnPosition, spawnRotation);

            // Turkish: Prefab kok pasifse Awake calismadan ApplyCharacter cagrilir; referanslar null kalir.
            if (SpawnedPlayer != null && !SpawnedPlayer.activeSelf)
            {
                SpawnedPlayer.SetActive(true);
            }

            PlayerCharacterApplier applier = SpawnedPlayer.GetComponent<PlayerCharacterApplier>();
            if (applier == null)
            {
                Debug.LogWarning("PlayerSpawner: PlayerBase is missing PlayerCharacterApplier.", SpawnedPlayer);
            }
            else
            {
                // Turkish: Karakterin görseli ve statları spawn anında PlayerBase'e uygulanır.
                applier.ApplyCharacter(selectedCharacter);
            }

            if (clearSelectionAfterSpawn)
            {
                SelectedCharacterHolder.SelectedCharacter = null;
            }
        }
    }
}
