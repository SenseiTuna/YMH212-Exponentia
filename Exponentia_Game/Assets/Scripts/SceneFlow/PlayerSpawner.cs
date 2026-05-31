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
using Unity.Cinemachine;
using UnityEngine;

namespace Exponentia.SceneFlow
{
    public class PlayerSpawner : MonoBehaviour
    {
        [System.Serializable]
        private class CharacterPrefabBinding
        {
            public CharacterData character = null;
            public GameObject prefab = null;
        }

        [Header("Spawn Setup")]
        [SerializeField] private GameObject playerBasePrefab;
        [SerializeField] private CharacterPrefabBinding[] characterPrefabs;
        [SerializeField] private Transform spawnPoint;

        [Header("Runtime Presentation")]
        [SerializeField] private float spawnedPlayerScale = 0.75f;
        [SerializeField] private float cameraOrthographicSize = 4f;

        [Header("Fallback")]
        [SerializeField] private CharacterData fallbackCharacter;
        [SerializeField] private bool clearSelectionAfterSpawn;

        public GameObject SpawnedPlayer { get; private set; }

        private Transform spawnedPlayerScaleRoot;

        private void Start()
        {
            SpawnPlayer();
        }

        private void LateUpdate()
        {
            ApplySpawnedPlayerScale();
        }

        public void SpawnPlayer()
        {
            if (SpawnedPlayer != null)
            {
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

            GameObject prefabToSpawn = ResolvePlayerPrefab(selectedCharacter);
            if (prefabToSpawn == null)
            {
                Debug.LogError("PlayerSpawner: No player prefab found for selected character.", this);
                return;
            }

            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            spawnedPlayerScaleRoot = CreatePlayerScaleRoot(prefabToSpawn, spawnPosition, spawnRotation);
            SpawnedPlayer = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, spawnedPlayerScaleRoot);
            SpawnedPlayer.transform.localPosition = Vector3.zero;
            SpawnedPlayer.transform.localRotation = Quaternion.identity;
            SpawnedPlayer.transform.localScale = Vector3.one;

            // Turkish: Prefab kok pasifse Awake calismadan ApplyCharacter cagrilir; referanslar null kalir.
            if (SpawnedPlayer != null && !SpawnedPlayer.activeSelf)
            {
                SpawnedPlayer.SetActive(true);
            }

            ApplyRuntimePresentation();

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

        private GameObject ResolvePlayerPrefab(CharacterData selectedCharacter)
        {
            if (selectedCharacter != null && characterPrefabs != null)
            {
                for (int i = 0; i < characterPrefabs.Length; i++)
                {
                    CharacterPrefabBinding binding = characterPrefabs[i];
                    if (binding != null && binding.character == selectedCharacter && binding.prefab != null)
                    {
                        return binding.prefab;
                    }
                }
            }

            return playerBasePrefab;
        }

        private void ApplyRuntimePresentation()
        {
            if (SpawnedPlayer == null)
            {
                return;
            }

            ApplySpawnedPlayerScale();

            if (cameraOrthographicSize <= 0f)
            {
                return;
            }

            CinemachineCamera cinemachineCamera = SpawnedPlayer.GetComponentInChildren<CinemachineCamera>(true);
            if (cinemachineCamera != null)
            {
                cinemachineCamera.Lens.OrthographicSize = cameraOrthographicSize;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.orthographic)
            {
                mainCamera.orthographicSize = cameraOrthographicSize;
            }
        }

        private Transform CreatePlayerScaleRoot(GameObject prefabToSpawn, Vector3 position, Quaternion rotation)
        {
            string rootName = prefabToSpawn != null ? prefabToSpawn.name + "_ScaleRoot" : "PlayerScaleRoot";
            GameObject root = new GameObject(rootName);
            root.transform.SetPositionAndRotation(position, rotation);
            ApplyScaleToTransform(root.transform);
            return root.transform;
        }

        private void ApplySpawnedPlayerScale()
        {
            if (spawnedPlayerScaleRoot == null || spawnedPlayerScale <= 0f)
            {
                return;
            }

            ApplyScaleToTransform(spawnedPlayerScaleRoot);
        }

        private void ApplyScaleToTransform(Transform target)
        {
            if (target == null)
            {
                return;
            }

            target.localScale = new Vector3(spawnedPlayerScale, spawnedPlayerScale, 1f);
        }
    }
}
