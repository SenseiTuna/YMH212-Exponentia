/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : ChallengeSwitch.cs
BUILD_DATE : 2026-05-26
====================================================
*/

using System.Collections.Generic;
using UnityEngine;
using Exponentia.Dungeon;

namespace Exponentia.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class ChallengeSwitch : MonoBehaviour, IInteractable
    {
        [Header("Challenge Settings")]
        [SerializeField] private int totalWaves = 2;
        [SerializeField] private int enemiesPerWave = 3;
        [SerializeField] private float timeBetweenWaves = 1.5f;

        [Header("References & Spawns")]
        [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private List<DungeonDoor> doors = new List<DungeonDoor>();
        [SerializeField] private DungeonRewardSpawner rewardSpawner;

        [Header("Visual Components")]
        [SerializeField] private Transform leverHandle;
        [SerializeField] private Vector3 pulledRotation = new Vector3(45f, 0f, 0f);

        private bool _challengeActive = false;
        private bool _challengeCompleted = false;
        private int _currentWave = 0;
        private List<EnemyMechanics> _spawnedEnemies = new List<EnemyMechanics>();
        private float _waveTimer = 0f;
        private bool _waitingForNextWave = false;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Spawner referansını otomatik bağla
            if (rewardSpawner == null)
            {
                rewardSpawner = GetComponent<DungeonRewardSpawner>();
            }
            if (rewardSpawner == null)
            {
                rewardSpawner = GetComponentInChildren<DungeonRewardSpawner>();
            }
            if (rewardSpawner == null)
            {
                rewardSpawner = gameObject.AddComponent<DungeonRewardSpawner>();
            }

            // Kapıları ve spawn noktalarını otomatik bul
            if (doors == null || doors.Count == 0)
            {
                AutoFindDoors();
            }

            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                AutoSetupSpawnPoints();
            }
        }

        private void Update()
        {
            if (!_challengeActive) return;

            // Ölü düşmanları listeden çıkart
            _spawnedEnemies.RemoveAll(enemy => enemy == null || !enemy.IsAlive);

            // Mevcut dalga temizlendiyse
            if (_spawnedEnemies.Count == 0 && !_waitingForNextWave)
            {
                if (_currentWave < totalWaves)
                {
                    // Yeni dalga için bekleme süresini başlat
                    _waitingForNextWave = true;
                    _waveTimer = timeBetweenWaves;
                    Debug.Log($"[Challenge] Dalga {_currentWave} temizlendi. Dalga {_currentWave + 1} için bekleniyor...");
                }
                else
                {
                    // Meydan okuma başarıyla bitti!
                    CompleteChallenge();
                }
            }

            // Dalga bekleme zamanlayıcısını çalıştır
            if (_waitingForNextWave)
            {
                _waveTimer -= Time.deltaTime;
                if (_waveTimer <= 0f)
                {
                    _waitingForNextWave = false;
                    SpawnNextWave();
                }
            }
        }

        public Vector3 GetInteractionPoint()
        {
            return transform.position;
        }

        public string GetInteractionLabel()
        {
            if (_challengeCompleted) return "Meydan Okuma Başarıyla Tamamlandı";
            if (_challengeActive) return "Meydan Okuma Devam Ediyor...";
            return "[E] Meydan Okumayı Başlat\n<size=80%>Zorlu Canavar Dalgaları & Epik Ödül</size>";
        }

        public bool CanInteract(GameObject interactor)
        {
            return !_challengeActive && !_challengeCompleted && interactor != null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            StartChallenge();
        }

        private void StartChallenge()
        {
            _challengeActive = true;
            _currentWave = 0;
            _spawnedEnemies.Clear();

            Debug.Log("[Challenge] Meydan okuma başladı! Kapılar kilitleniyor.");
            FloatingCombatText.Create("MEYDAN OKUMA BAŞLADI!", transform.position + Vector3.up * 1.5f, Color.red);

            // Kolu görsel olarak çekilmiş yapalım
            if (leverHandle != null)
            {
                leverHandle.localRotation = Quaternion.Euler(pulledRotation);
            }

            // 1. Kapıları Kilitle
            foreach (var door in doors)
            {
                if (door != null)
                {
                    door.Lock();
                }
            }

            // 2. İlk dalgayı doğur
            SpawnNextWave();
        }

        private void SpawnNextWave()
        {
            _currentWave++;
            _waitingForNextWave = false;

            Debug.Log($"[Challenge] Dalga {_currentWave}/{totalWaves} doğuruluyor!");
            FloatingCombatText.Create($"DALGA {_currentWave}/{totalWaves}", transform.position + Vector3.up * 1.5f, new Color(1f, 0.5f, 0f));

            if (enemyPrefabs.Count == 0)
            {
                Debug.LogWarning("[Challenge] Canavar prefab listesi boş! Meydan okuma doğrudan tamamlanacak.");
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");

            for (int i = 0; i < enemiesPerWave; i++)
            {
                // Rastgele spawn noktası seç
                Transform spawnPt = spawnPoints[Random.Range(0, spawnPoints.Count)];
                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

                if (spawnPt != null && prefab != null)
                {
                    Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
                    GameObject enemyObj = Instantiate(prefab, spawnPt.position + offset, Quaternion.identity);

                    EnemyMechanics enemyMech = enemyObj.GetComponentInChildren<EnemyMechanics>();
                    if (enemyMech != null)
                    {
                        _spawnedEnemies.Add(enemyMech);

                        // A* hedefini oyuncu olarak ata
                        if (player != null)
                        {
                            var ai = enemyObj.GetComponentInChildren<Pathfinding.IAstarAI>();
                            if (ai != null)
                            {
                                ai.destination = player.transform.position;
                                ai.SearchPath();
                            }
                        }
                    }
                }
            }
        }

        private void CompleteChallenge()
        {
            _challengeActive = false;
            _challengeCompleted = true;

            Debug.Log("[Challenge] Tüm dalgalar temizlendi! Meydan okuma kazanıldı, kapılar açılıyor.");
            FloatingCombatText.Create("MEYDAN OKUMA KAZANILDI!", transform.position + Vector3.up * 1.5f, Color.green);

            // 1. Kapıları Aç
            foreach (var door in doors)
            {
                if (door != null)
                {
                    door.Unlock();
                }
            }

            // 2. Epik Ödülü merkezde doğur!
            if (rewardSpawner != null)
            {
                rewardSpawner.SpawnRewardChoices(transform.position + Vector3.back * 0.1f);
            }
        }

        private void AutoFindDoors()
        {
            doors = new List<DungeonDoor>();
            DungeonDoor[] allDoors = Object.FindObjectsByType<DungeonDoor>(FindObjectsSortMode.None);
            foreach (var door in allDoors)
            {
                if (Vector3.Distance(transform.position, door.transform.position) < 30f)
                {
                    doors.Add(door);
                }
            }
        }

        private void AutoSetupSpawnPoints()
        {
            spawnPoints = new List<Transform>();
            Transform spawnGroup = transform.Find("SpawnPoints");
            if (spawnGroup != null)
            {
                foreach (Transform child in spawnGroup)
                {
                    spawnPoints.Add(child);
                }
            }
            else
            {
                // Runtime'da geçici spawn noktaları grubu oluştur
                GameObject gp = new GameObject("SpawnPoints");
                gp.transform.SetParent(transform, false);

                Vector3[] offsets = new Vector3[]
                {
                    new Vector3(-4f, -4f, 0f),
                    new Vector3(4f, -4f, 0f),
                    new Vector3(-4f, 4f, 0f),
                    new Vector3(4f, 4f, 0f)
                };

                for (int i = 0; i < offsets.Length; i++)
                {
                    GameObject sp = new GameObject($"Spawn_{i + 1}");
                    sp.transform.SetParent(gp.transform, false);
                    sp.transform.localPosition = offsets[i];
                    spawnPoints.Add(sp.transform);
                }
            }
        }

        [ContextMenu("Auto Setup Challenge Switch")]
        public void EditorAutoSetup()
        {
            AutoFindDoors();
            AutoSetupSpawnPoints();
            Debug.Log($"[ChallengeSwitch] Editor Kurulumu Başarılı! {doors.Count} kapı bağlandı, {spawnPoints.Count} spawn noktası hazır.");
        }
    }
}
