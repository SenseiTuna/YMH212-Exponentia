/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : DungeonRoomCombatManager.cs
BUILD_DATE : 2026-05-24
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

public class DungeonRoomCombatManager : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private SimpleDungeonGenerator dungeonGenerator;
    [SerializeField] private VisibleMacroGrid visibleGrid;
    [SerializeField] private DungeonMapManager mapManager;

    [Header("Düşman Ayarları")]
    [Tooltip("Sıradan savaş odalarında doğacak düşman prefableri.")]
    [SerializeField] private List<GameObject> basicEnemyPrefabs = new List<GameObject>();
    [Tooltip("Boss odalarında doğacak düşman/boss prefableri.")]
    [SerializeField] private List<GameObject> bossEnemyPrefabs = new List<GameObject>();
    [SerializeField] private int minEnemiesPerRoom = 2;
    [SerializeField] private int maxEnemiesPerRoom = 4;

    [Header("Ödül Ayarları")]
    [Tooltip("Oda temizlendiğinde ortada belirecek ödül prefableri (silah/item vs.).")]
    [SerializeField] private List<GameObject> rewardPrefabs = new List<GameObject>();
    [Tooltip("Kalıcı 3'lü seçim ödül sistemini tetikleyecek spawner.")]
    [SerializeField] private Exponentia.Dungeon.DungeonRewardSpawner rewardSpawner;

    [Header("Kapı / Lazer Ayarları")]
    [Tooltip("Kullanılacak özel Kapı Prefab'i (DungeonDoor scriptine sahip olmalı). Boş bırakılırsa dinamik lazer kapısı kullanılır.")]
    [SerializeField] private DungeonDoor doorPrefab;
    [SerializeField] private Color laserColor = new Color(1f, 0.1f, 0.35f, 0.8f); // Neon Pembe/Kırmızı
    [SerializeField] private float laserThickness = 0.15f;
    [SerializeField] private float laserPulseSpeed = 6.0f;

    // Aktif savaş durumları
    private HashSet<string> _clearedRoomIds = new HashSet<string>();
    private string _activeCombatRoomId = "";
    private List<GameObject> _activeLaserGates = new List<GameObject>();
    private List<EnemyMechanics> _spawnedEnemies = new List<EnemyMechanics>();
    private bool _isCombatActive = false;

    private Sprite _proceduralLaserSprite;
    private GameObject _gatesRootInstance;

    // Dış sistemler için savaş akış olayları
    public event System.Action<string> OnRoomEntered;
    public event System.Action<string> OnRoomCleared;

    private void Awake()
    {
        // 1. Önce bu objenin üzerinde ara
        if (dungeonGenerator == null)
            dungeonGenerator = GetComponent<SimpleDungeonGenerator>();
        if (visibleGrid == null)
            visibleGrid = GetComponent<VisibleMacroGrid>();
        if (mapManager == null)
            mapManager = GetComponent<DungeonMapManager>();

        // 2. Bulamazsan parent'larda ara
        if (dungeonGenerator == null)
            dungeonGenerator = GetComponentInParent<SimpleDungeonGenerator>();
        if (visibleGrid == null)
            visibleGrid = GetComponentInParent<VisibleMacroGrid>();
        if (mapManager == null)
            mapManager = GetComponentInParent<DungeonMapManager>();

        // 3. Bulamazsan child'larda ara
        if (dungeonGenerator == null)
            dungeonGenerator = GetComponentInChildren<SimpleDungeonGenerator>();
        if (visibleGrid == null)
            visibleGrid = GetComponentInChildren<VisibleMacroGrid>();
        if (mapManager == null)
            mapManager = GetComponentInChildren<DungeonMapManager>();

        // 4. Hala bulamazsan sahnede ara (Fallback)
        if (dungeonGenerator == null)
            dungeonGenerator = FindAnyObjectByType<SimpleDungeonGenerator>();
        if (visibleGrid == null)
            visibleGrid = FindAnyObjectByType<VisibleMacroGrid>();
        if (mapManager == null)
            mapManager = FindAnyObjectByType<DungeonMapManager>();

        if (rewardSpawner == null)
            rewardSpawner = GetComponent<Exponentia.Dungeon.DungeonRewardSpawner>();
        if (rewardSpawner == null)
            rewardSpawner = GetComponentInChildren<Exponentia.Dungeon.DungeonRewardSpawner>();
        if (rewardSpawner == null)
            rewardSpawner = FindAnyObjectByType<Exponentia.Dungeon.DungeonRewardSpawner>();
        if (rewardSpawner == null)
            rewardSpawner = gameObject.AddComponent<Exponentia.Dungeon.DungeonRewardSpawner>();

        CreateProceduralLaserSprite();
    }

    private void OnEnable()
    {
        if (mapManager != null)
        {
            mapManager.OnRoomEntered += HandleRoomEntered;
        }
    }

    private void OnDisable()
    {
        if (mapManager != null)
        {
            mapManager.OnRoomEntered -= HandleRoomEntered;
        }
    }

    private void Update()
    {
        if (!_isCombatActive) return;

        MonitorCombatProgress();
    }

    private void HandleRoomEntered(string roomId)
    {
        // Koridorlar veya başlangıç odası savaş tetiklemez
        if (string.IsNullOrEmpty(roomId) || roomId.StartsWith("START") || roomId.StartsWith("CORRIDOR"))
        {
            return;
        }

        // Eğer oda zaten temizlendiyse veya şu an aktif olarak savaş zaten varsa çık
        if (_clearedRoomIds.Contains(roomId) || _isCombatActive)
        {
            return;
        }

        DebugPlacedRoom room = dungeonGenerator.PlacedRooms.Find(r => r.RoomId == roomId);
        if (room == null) return;

        // Savaş alanı giriş olayını tetikle
        OnRoomEntered?.Invoke(roomId);

        // Odada doğacak düşman prefab'lerini belirle
        List<GameObject> prefabsToUse = basicEnemyPrefabs;
        if (roomId.StartsWith("BOSS"))
        {
            prefabsToUse = bossEnemyPrefabs.Count > 0 ? bossEnemyPrefabs : basicEnemyPrefabs;
        }

        // Eğer düşman prefab listesi boş ise veya oda bir hazine odası (TREASURE) ise savaş başlatma
        if (prefabsToUse.Count == 0 || roomId.StartsWith("TREASURE"))
        {
            Debug.Log($"[CombatManager] {roomId} odası güvenli (düşman yok veya hazine odası). Kilitlenme tetiklenmedi.");
            _clearedRoomIds.Add(roomId);
            return;
        }

        // Düşman sayısını belirle
        int spawnCount = Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1);
        if (spawnCount <= 0)
        {
            Debug.Log($"[CombatManager] {roomId} odasında doğacak düşman sayısı 0 çıktı. Savaş tetiklenmedi.");
            _clearedRoomIds.Add(roomId);
            return;
        }

        Debug.Log($"[CombatManager] {roomId} odasına girildi. Çıkış kapıları kilitleniyor ve {spawnCount} adet düşman doğuruluyor!");
        
        _activeCombatRoomId = roomId;
        _isCombatActive = true;

        // 1. Kapıları Kilitle
        LockRoomExits(roomId);

        // 2. Düşmanları Yarat (Spawn)
        SpawnEnemiesInRoomFootprint(room, prefabsToUse, spawnCount);
    }

    private void SpawnEnemiesInRoomFootprint(DebugPlacedRoom room, List<GameObject> enemyPrefabsList, int count)
    {
        _spawnedEnemies.Clear();
        List<Vector2Int> cells = room.WorldCells;

        GameObject player = GameObject.FindWithTag("Player");

        for (int i = 0; i < count; i++)
        {
            Vector2Int randomCell = cells[Random.Range(0, cells.Count)];
            Vector3 spawnWorldPos = visibleGrid.MacroToWorld(randomCell);

            GameObject prefab = enemyPrefabsList[Random.Range(0, enemyPrefabsList.Count)];
            if (prefab == null) continue;

            // Düşmanların tam üst üste çakışmaması için küçük bir ofset
            Vector3 offset = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0f);
            GameObject enemyObj = Instantiate(prefab, spawnWorldPos + offset, Quaternion.identity);

            EnemyMechanics enemyMech = enemyObj.GetComponentInChildren<EnemyMechanics>();
            if (enemyMech != null)
            {
                _spawnedEnemies.Add(enemyMech);

                // Düşman doğar doğmaz oyuncuyu takip etmesi için A* hedefini ayarla
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

    private void LockRoomExits(string roomId)
    {
        if (dungeonGenerator == null || visibleGrid == null) return;

        DebugPlacedRoom room = dungeonGenerator.PlacedRooms.Find(r => r.RoomId == roomId);
        if (room == null) return;

        HashSet<Vector2Int> roomCells = new HashSet<Vector2Int>(room.WorldCells);
        HashSet<string> handledBoundaries = new HashSet<string>();

        _gatesRootInstance = new GameObject("ActiveLaserGates");
        Transform gatesRoot = _gatesRootInstance.transform;

        foreach (Vector2Int cell in room.WorldCells)
        {
            Vector2Int[] neighbors = {
                new Vector2Int(cell.x + 1, cell.y),
                new Vector2Int(cell.x - 1, cell.y),
                new Vector2Int(cell.x, cell.y + 1),
                new Vector2Int(cell.x, cell.y - 1)
            };

            foreach (Vector2Int n in neighbors)
            {
                // Sınırın dışındaki bir hücre koridor veya başka oda ise burası çıkış kapısıdır
                if (!roomCells.Contains(n))
                {
                    var gridCell = visibleGrid.Grid.GetCell(n);
                    if (gridCell != null && gridCell.IsOccupied)
                    {
                        // Mükemmel teklik için sınır anahtarı oluşturuyoruz
                        string boundaryKey = cell.x < n.x || (cell.x == n.x && cell.y < n.y) 
                            ? $"{cell.x}_{cell.y}_to_{n.x}_{n.y}" 
                            : $"{n.x}_{n.y}_to_{cell.x}_{cell.y}";

                        if (handledBoundaries.Contains(boundaryKey))
                            continue;

                        handledBoundaries.Add(boundaryKey);

                        // Kapının dünya koordinatlarındaki orta noktasını hesapla
                        Vector3 worldCell = visibleGrid.MacroToWorld(cell);
                        Vector3 worldNeighbor = visibleGrid.MacroToWorld(n);
                        Vector3 midpoint = (worldCell + worldNeighbor) * 0.5f;

                        bool isHorizontalTransition = (cell.y != n.y); // Dikey komşuluk ise yatay kilit
                        
                        if (doorPrefab != null)
                        {
                            CreateDoorPrefab(midpoint, isHorizontalTransition, gatesRoot);
                        }
                        else
                        {
                            CreateLaserGate(midpoint, isHorizontalTransition, gatesRoot);
                        }
                    }
                }
            }
        }
    }

    private void CreateDoorPrefab(Vector3 position, bool isHorizontal, Transform root)
    {
        DungeonDoor door = Instantiate(doorPrefab, position, Quaternion.identity, root);
        door.name = "DungeonDoorBarrier";

        float cellSize = visibleGrid.CellWorldSize;

        // Kapının yönüne göre ölçekle
        if (isHorizontal)
        {
            // Yatay geçiş (X genişliği hücre boyutu kadar)
            door.transform.localScale = new Vector3(cellSize * 1.15f, doorPrefab.transform.localScale.y, 1f);
        }
        else
        {
            // Dikey geçiş (Y yüksekliği hücre boyutu kadar)
            door.transform.localScale = new Vector3(doorPrefab.transform.localScale.x, cellSize * 1.15f, 1f);
        }

        door.Lock();
        _activeLaserGates.Add(door.gameObject);
    }

    private void CreateLaserGate(Vector3 position, bool isHorizontal, Transform root)
    {
        GameObject gate = new GameObject("LaserGateBarrier");
        gate.transform.SetParent(root, false);
        gate.transform.position = position;

        // Fiziksel Engel (Collider)
        BoxCollider2D col = gate.AddComponent<BoxCollider2D>();
        
        // Görsel Renderer
        SpriteRenderer sr = gate.AddComponent<SpriteRenderer>();
        sr.sprite = _proceduralLaserSprite;
        sr.color = laserColor;
        sr.sortingOrder = 10; // Oyuncunun ve çevrenin üstünde görünmesi için yüksek bir sorting order

        // Yönlendirme ve Ölçekleme
        float cellSize = visibleGrid.CellWorldSize;
        if (isHorizontal)
        {
            // Yatay geçişi engelleyen yatay lazer (Y ekseninde ince, X ekseninde hücre kadar geniş)
            gate.transform.localScale = new Vector3(cellSize * 1.15f, laserThickness, 1f);
            col.size = new Vector2(cellSize * 1.15f, laserThickness);
        }
        else
        {
            // Dikey geçişi engelleyen dikey lazer (X ekseninde ince, Y ekseninde hücre kadar geniş)
            gate.transform.localScale = new Vector3(laserThickness, cellSize * 1.15f, 1f);
            col.size = new Vector2(laserThickness, cellSize * 1.15f);
        }

        // Crackling/Pulsing Animasyon Bileşeni Ekle
        LaserGatePulse pulse = gate.AddComponent<LaserGatePulse>();
        pulse.sr = sr;
        pulse.pulseSpeed = laserPulseSpeed;

        _activeLaserGates.Add(gate);
    }



    private void MonitorCombatProgress()
    {
        // Ölü düşmanları listeden çıkart
        _spawnedEnemies.RemoveAll(enemy => enemy == null || !enemy.IsAlive);

        // Eğer tüm düşmanlar öldüyse savaşı bitir
        if (_spawnedEnemies.Count == 0)
        {
            EndCombatAndUnlockRoom();
        }
    }

    private void EndCombatAndUnlockRoom()
    {
        _isCombatActive = false;
        _clearedRoomIds.Add(_activeCombatRoomId);

        Debug.Log($"[CombatManager] Oda başarıyla temizlendi! Kapılar açılıyor ve ödül veriliyor: {_activeCombatRoomId}");

        // Oda temizlenme olayını tetikle
        OnRoomCleared?.Invoke(_activeCombatRoomId);

        // 1. Kapıları Yok Et veya Kilidini Aç
        foreach (var gate in _activeLaserGates)
        {
            if (gate != null)
            {
                DungeonDoor door = gate.GetComponentInChildren<DungeonDoor>();
                if (door != null)
                {
                    door.Unlock(); // Bu otomatik olarak yumuşak sönüp kendini yok edecek
                }
                else
                {
                    Destroy(gate); // Procedural lazerler için doğrudan sil
                }
            }
        }
        _activeLaserGates.Clear();

        // Kapıların root objesini doğrudan yok et (Yumuşak kapı kapanma süresi için 1 sn gecikmeyle)
        if (_gatesRootInstance != null)
        {
            Destroy(_gatesRootInstance, 1.0f);
            _gatesRootInstance = null;
        }

        // 2. Ödül Üret
        SpawnClearReward(_activeCombatRoomId);

        _activeCombatRoomId = "";
    }

    private void SpawnClearReward(string roomId)
    {
        if (dungeonGenerator == null || visibleGrid == null) return;

        DebugPlacedRoom room = dungeonGenerator.PlacedRooms.Find(r => r.RoomId == roomId);
        if (room == null) return;

        // Odanın merkezini bul
        Vector2 centerMacro = room.GetCenterMacro();
        Vector3 spawnWorldPos = visibleGrid.MacroPointToWorld(centerMacro, -0.1f);

        // Eğer 3'lü seçim ödül spawner'ı atanmışsa, onu tetikle
        if (rewardSpawner != null)
        {
            rewardSpawner.SpawnRewardChoices(spawnWorldPos);
        }
        else if (rewardPrefabs.Count > 0)
        {
            // Rastgele bir ödül prefabı seç ve doğur
            GameObject rewardPrefab = rewardPrefabs[Random.Range(0, rewardPrefabs.Count)];
            if (rewardPrefab != null)
            {
                GameObject spawnedReward = Instantiate(rewardPrefab, spawnWorldPos, Quaternion.identity);
                
                // Ödülü görsel olarak canlandırmak için ufak bir doğuş zıplaması ekleyebiliriz
                spawnedReward.transform.localScale = Vector3.zero;
                LeanTweenScale(spawnedReward, Vector3.one, 0.45f);
            }
        }
    }

    private void LeanTweenScale(GameObject target, Vector3 targetScale, float duration)
    {
        // Basit bir Coroutine ile animasyon (harici paket gereksinimi olmasın diye)
        StartCoroutine(AnimateScale(target, targetScale, duration));
    }

    private System.Collections.IEnumerator AnimateScale(GameObject target, Vector3 targetScale, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = target.transform.localScale;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            // Elastic out / soft bounce animasyonu
            float scaleVal = Mathf.Sin(percent * Mathf.PI * 0.5f);
            target.transform.localScale = Vector3.Lerp(startScale, targetScale, scaleVal);
            yield return null;
        }
        if (target != null)
            target.transform.localScale = targetScale;
    }

    private void CreateProceduralLaserSprite()
    {
        if (_proceduralLaserSprite != null) return;

        Texture2D tex = new Texture2D(2, 2);
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        // pixelsPerUnit = 2f so that 2x2 texture is exactly 1.0 unit in world space!
        _proceduralLaserSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
    }

    // Lazerlerin parıldayıp kıpırdamasını sağlayan premium animasyon motoru
    private class LaserGatePulse : MonoBehaviour
    {
        public SpriteRenderer sr;
        public float pulseSpeed = 6.0f;
        private Vector3 _originalScale;

        private void Start()
        {
            _originalScale = transform.localScale;
        }

        private void Update()
        {
            if (sr == null) return;

            float lerpVal = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

            // Parlaklık / Alpha dalgalanması
            Color c = sr.color;
            c.a = Mathf.Lerp(0.45f, 0.9f, lerpVal);
            sr.color = c;

            // Ufak bir enerji titreşim dalgalanması (Premium micro-animation)
            float scalePulse = Mathf.Lerp(0.96f, 1.04f, lerpVal);
            transform.localScale = new Vector3(_originalScale.x, _originalScale.y * scalePulse, _originalScale.z);
        }
    }
}
