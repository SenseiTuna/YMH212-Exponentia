/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.2.0
FILE       : ManualRoomCombatTrigger.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ManualRoomCombatTrigger : MonoBehaviour
{
    [Header("Bariyer Kapılar")]
    [Tooltip("Bu odaya girildiğinde kilitlenecek kapıların listesi. (Boş bırakırsanız otomatik bulunacaktır).")]
    [SerializeField] private List<DungeonDoor> doors = new List<DungeonDoor>();

    [Header("Düşman Ayarları")]
    [Tooltip("Doğacak düşmanların prefab listesi.")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("Düşmanların doğacağı yerler (Boş bırakırsanız otomatik oluşturulacaktır).")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Ödül Ayarları")]
    [Tooltip("Oda temizlendiğinde verilecek tekli ödül prefab'i (Seçim sistemi yoksa kullanılır).")]
    [SerializeField] private GameObject rewardPrefab;
    [Tooltip("Ödülün doğacağı konum (Boş bırakırsanız otomatik oluşturulacaktır).")]
    [SerializeField] private Transform rewardSpawnPoint;
    [Tooltip("Kalıcı 3'lü seçim ödül sistemini tetikleyecek spawner (Atanmazsa yerel objeden otomatik aranır).")]
    [SerializeField] private Exponentia.Dungeon.DungeonRewardSpawner rewardSpawner;

    [Header("Tetikleme Ayarları")]
    [Tooltip("Oyun başladıktan sonra yanlış tetiklemeleri önlemek için beklenecek süre (saniye).")]
    [SerializeField] private float activationDelay = 0.5f;

    private List<EnemyMechanics> _spawnedEnemies = new List<EnemyMechanics>();
    private bool _hasTriggered = false;
    private bool _isCombatActive = false;
    private float _levelLoadTime;

    private void Awake()
    {
        // Çarpışma alanının tetikleyici olduğundan emin olalım
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Seçim ödülü spawner'ını otomatik bulmaya çalış
        if (rewardSpawner == null)
        {
            rewardSpawner = GetComponent<Exponentia.Dungeon.DungeonRewardSpawner>();
        }
        if (rewardSpawner == null)
        {
            rewardSpawner = GetComponentInChildren<Exponentia.Dungeon.DungeonRewardSpawner>();
        }
        if (rewardSpawner == null)
        {
            rewardSpawner = gameObject.AddComponent<Exponentia.Dungeon.DungeonRewardSpawner>();
        }

        // Eğer Inspector'da spawn noktaları listede var ama içi "None (null)" ise veya tamamen boşsa runtime'da otomatik kur
        if (!HasAnyValidSpawnPoint() || rewardSpawnPoint == null)
        {
            RuntimeAutoSetup();
        }
    }

    private void Start()
    {
        _levelLoadTime = Time.time;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TriggerCheck(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TriggerCheck(other);
    }

    private void TriggerCheck(Collider2D other)
    {
        if (_hasTriggered) return;

        // Oyunun ilk anlarındaki yüklenme çakışmalarını önle
        if (Time.time - _levelLoadTime < activationDelay) return;

        // Oyuncu kontrolü (Collider, attachedRigidbody veya root tag'i "Player" ise kabul et - Süper Güvenli)
        bool isPlayer = other.CompareTag("Player") || 
                        (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) || 
                        other.transform.root.CompareTag("Player");

        if (isPlayer)
        {
            StartCombat();
        }
    }

    private void StartCombat()
    {
        _hasTriggered = true;
        _isCombatActive = true;

        int spawnCount = spawnPoints != null ? spawnPoints.Count : 0;
        int prefabCount = enemyPrefabs != null ? enemyPrefabs.Count : 0;
        Debug.Log($"[ManualCombatTrigger] Savaş tetiklendi! Kapı Sayısı: {doors.Count}, Doğma Noktası Sayısı: {spawnCount}, Canavar Prefab Sayısı: {prefabCount}");

        // 1. Kapıları Kilitle
        foreach (var door in doors)
        {
            if (door != null)
            {
                door.Lock();
            }
        }

        // 2. Düşmanları Doğur
        _spawnedEnemies.Clear();
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning("[ManualCombatTrigger] Bir doğma noktası (Spawn Point) NULL olduğu için geçildi!");
                continue;
            }
            if (prefabCount == 0)
            {
                Debug.LogWarning("[ManualCombatTrigger] Canavar Prefab listesi boş olduğu için doğurma yapılamadı!");
                continue;
            }

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            if (prefab == null)
            {
                Debug.LogWarning("[ManualCombatTrigger] Seçilen canavar prefabi NULL olduğu için geçildi!");
                continue;
            }

            GameObject enemyObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            
            // Düşman scriptini hem ana objede hem de alt objelerde (children) arayalım (Çok daha esnek ve güvenli)
            EnemyMechanics enemyMech = enemyObj.GetComponentInChildren<EnemyMechanics>();
            if (enemyMech != null)
            {
                _spawnedEnemies.Add(enemyMech);

                // Düşman doğar doğmaz oyuncuya saldırsın (A* takip hedefi)
                GameObject player = GameObject.FindWithTag("Player");
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
            else
            {
                // Hangi bileşenlerin olduğunu konsola yazdıralım ki sorunu saniyede teşhis edebilelim!
                Component[] allComponents = enemyObj.GetComponentsInChildren<Component>(true);
                System.Text.StringBuilder compList = new System.Text.StringBuilder();
                foreach (var c in allComponents)
                {
                    if (c != null)
                    {
                        compList.Append(c.GetType().Name).Append(" (").Append(c.GetType().Namespace ?? "Global").Append("), ");
                    }
                }
                Debug.LogWarning($"[ManualCombatTrigger] Doğurulan '{enemyObj.name}' objesinde 'EnemyMechanics' bulunamadı! Mevcut Bileşenler: {compList.ToString()}");
            }
        }

        // Güvenlik: Eğer hiçbir düşman doğurulamadıysa kapıları kilitleme
        if (_spawnedEnemies.Count == 0)
        {
            Debug.LogWarning("[ManualCombatTrigger] Dikkat! Odaya girildi fakat doğurulan düşmanların hiçbirinde EnemyMechanics bulunamadığı için savaş başlatılamadı. Kapılar kilitlenmiyor.");
            UnlockDoors();
        }
    }

    private void Update()
    {
        if (!_isCombatActive) return;

        // Ölü düşmanları listeden ayıkla
        _spawnedEnemies.RemoveAll(enemy => enemy == null || !enemy.IsAlive);

        // Tüm düşmanlar temizlendiyse kapıları aç
        if (_spawnedEnemies.Count == 0)
        {
            UnlockDoors();
        }
    }

    private void UnlockDoors()
    {
        _isCombatActive = false;
        Debug.Log("[ManualCombatTrigger] Tüm düşmanlar yenildi! Kapılar açılıyor ve ödül veriliyor.");

        // 1. Kapıların Kilidini Aç
        foreach (var door in doors)
        {
            if (door != null)
            {
                door.Unlock();
            }
        }

        // 2. Ödülü Doğur (Seçim spawner'ı varsa 3'lü seçim doğurur, yoksa tekil prefab doğurur)
        if (rewardSpawner != null && rewardSpawnPoint != null)
        {
            rewardSpawner.SpawnRewardChoices(rewardSpawnPoint.position);
        }
        else if (rewardPrefab != null && rewardSpawnPoint != null)
        {
            GameObject reward = Instantiate(rewardPrefab, rewardSpawnPoint.position, rewardSpawnPoint.rotation);
            reward.transform.localScale = Vector3.zero;
            StartCoroutine(AnimateRewardScale(reward, Vector3.one, 0.5f));
        }
    }

    private System.Collections.IEnumerator AnimateRewardScale(GameObject target, Vector3 targetScale, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = target.transform.localScale;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float scaleVal = Mathf.Sin(percent * Mathf.PI * 0.5f);
            target.transform.localScale = Vector3.Lerp(startScale, targetScale, scaleVal);
            yield return null;
        }
        if (target != null)
            target.transform.localScale = targetScale;
    }

    /// <summary>
    /// Listede en az bir tane geçerli (null olmayan) spawn noktası olup olmadığını kontrol eder.
    /// </summary>
    private bool HasAnyValidSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return false;
        foreach (var sp in spawnPoints)
        {
            if (sp != null) return true;
        }
        return false;
    }

    /// <summary>
    /// Oyun başlarken eksik yapılandırmaları otomatik olarak tamamlar (Sıfır Konfigürasyon Modu).
    /// </summary>
    private void RuntimeAutoSetup()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Vector3 center = col != null ? col.bounds.center : transform.position;
        float width = col != null ? col.size.x : 4f;
        float height = col != null ? col.size.y : 4f;

        // 1. Doğma Noktaları yoksa veya hepsi null ise otomatik oluştur
        if (!HasAnyValidSpawnPoint())
        {
            // Eski içi boş veya bozuk alt grupları temizle
            Transform oldGroup = transform.Find("Runtime_Spawns");
            if (oldGroup != null) Destroy(oldGroup.gameObject);

            spawnPoints = new List<Transform>();
            GameObject spawnsGroup = new GameObject("Runtime_Spawns");
            spawnsGroup.transform.SetParent(transform, false);

            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-width * 0.35f, -height * 0.35f, 0f),
                new Vector3(width * 0.35f, -height * 0.35f, 0f),
                new Vector3(-width * 0.35f, height * 0.35f, 0f),
                new Vector3(width * 0.35f, height * 0.35f, 0f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject sp = new GameObject($"SpawnPoint_{i + 1}");
                sp.transform.SetParent(spawnsGroup.transform, false);
                sp.transform.localPosition = offsets[i];
                spawnPoints.Add(sp.transform);
            }
        }

        // 2. Ödül Noktası yoksa otomatik oluştur
        if (rewardSpawnPoint == null)
        {
            GameObject rewardPtObj = new GameObject("Runtime_RewardPoint");
            rewardPtObj.transform.SetParent(transform, false);
            rewardPtObj.transform.localPosition = Vector3.zero;
            rewardSpawnPoint = rewardPtObj.transform;
        }

        // 3. Yakındaki kapıları otomatik bul
        if (doors == null || doors.Count == 0)
        {
            doors = new List<DungeonDoor>();
            DungeonDoor[] allDoors = Object.FindObjectsByType<DungeonDoor>(FindObjectsSortMode.None);
            foreach (var door in allDoors)
            {
                if (Vector3.Distance(center, door.transform.position) < 30f)
                {
                    doors.Add(door);
                }
            }
        }
    }

    /// <summary>
    /// Unity Editor'da sağ tıklayıp "Auto Setup Room Trigger" diyerek objeleri sanki siz oluşturmuş gibi üretir!
    /// </summary>
    [ContextMenu("Auto Setup Room Trigger")]
    public void EditorAutoSetup()
    {
        // 1. BoxCollider2D kontrolü ve otomatik büyüklük ayarı
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
        
        // Eğer collider boyutu çok küçükse varsayılan bir oda boyutu verelim (Örn: 8x8)
        if (col.size == Vector2.one || col.size.magnitude < 2f)
        {
            col.size = new Vector2(8f, 8f);
        }

        float width = col.size.x;
        float height = col.size.y;

        // 2. Eski oluşturulmuş noktaları temizle
        Transform oldSpawns = transform.Find("SpawnPoints_Group");
        if (oldSpawns != null)
        {
            DestroyImmediate(oldSpawns.gameObject);
        }

        Transform oldReward = transform.Find("RewardPoint_Group");
        if (oldReward != null)
        {
            DestroyImmediate(oldReward.gameObject);
        }

        // 3. Yeni doğma noktaları grubunu oluştur
        GameObject spawnsGroup = new GameObject("SpawnPoints_Group");
        spawnsGroup.transform.SetParent(transform, false);

        spawnPoints = new List<Transform>();
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(-width * 0.35f, -height * 0.35f, 0f), // Sol Alt
            new Vector3(width * 0.35f, -height * 0.35f, 0f),  // Sağ Alt
            new Vector3(-width * 0.35f, height * 0.35f, 0f),  // Sol Üst
            new Vector3(width * 0.35f, height * 0.35f, 0f)   // Sağ Üst
        };

        string[] spawnNames = new string[] { "Spawn_SolAlt", "Spawn_SagAlt", "Spawn_SolUst", "Spawn_SagUst" };

        for (int i = 0; i < 4; i++)
        {
            GameObject sp = new GameObject(spawnNames[i]);
            sp.transform.SetParent(spawnsGroup.transform, false);
            sp.transform.localPosition = offsets[i];
            spawnPoints.Add(sp.transform);
        }

        // 4. Yeni ödül noktası oluştur
        GameObject rewardPtObj = new GameObject("RewardPoint_Group");
        rewardPtObj.transform.SetParent(transform, false);
        rewardPtObj.transform.localPosition = Vector3.zero; // Tam oda merkezi
        rewardSpawnPoint = rewardPtObj.transform;

        // 5. Etraftaki kapıları (30 birim yarıçapta) otomatik bul ve bağla
        doors = new List<DungeonDoor>();
        DungeonDoor[] allDoors = Object.FindObjectsByType<DungeonDoor>(FindObjectsSortMode.None);
        foreach (var door in allDoors)
        {
            if (Vector3.Distance(transform.position, door.transform.position) < 30f)
            {
                doors.Add(door);
            }
        }

        Debug.Log($"[ManualRoomCombatTrigger] Editor Kurulumu Başarılı! 4 adet doğma noktası ({spawnsGroup.name}) ve 1 adet ödül noktası oluşturuldu. Yakındaki {doors.Count} kapı bağlandı.");
    }
}


