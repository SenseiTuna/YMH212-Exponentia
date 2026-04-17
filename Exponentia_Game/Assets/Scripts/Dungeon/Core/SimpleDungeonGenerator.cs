/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 0.1.1
FILE       : SimpleDungeonGenerator.cs
BUILD_DATE : 2026-04-17
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

public class SimpleDungeonGenerator : MonoBehaviour
{
    [Header("Referans")]
    [SerializeField] private VisibleMacroGrid visibleGrid;

    [Header("Üretim")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private int combatRoomCount = 4;
    [SerializeField] private int treasureRoomCount = 1;
    [SerializeField, Min(0)] private int noNewConnectionsToFirstRoomsCount = 5;


    [Header("Bağlantı Mesafesi")]
    [SerializeField] private int minGapBetweenRooms = 0;
    [SerializeField] private int maxGapBetweenRooms = 2;
    [SerializeField] private bool biasTowardShortGaps = true;

    [Header("Kullanılacak Şekiller")]
    [SerializeField]
    private RoomShapeType[] regularShapes =
    {
        RoomShapeType.OneByOne,
        RoomShapeType.TwoByTwo,
        RoomShapeType.LShape
    };

    [SerializeField] private RoomShapeType startShape = RoomShapeType.OneByOne;
    [SerializeField] private RoomShapeType treasureShape = RoomShapeType.OneByOne;
    [SerializeField] private RoomShapeType bossShape = RoomShapeType.ThreeByThree;

    [Header("Debug Bağlantı Çizgisi")]
    [SerializeField] private bool showConnectionLines = false;
    [SerializeField] private Color connectionColor = new Color(0.25f, 0.75f, 1f, 1f);
    [SerializeField] private float connectionWidth = 0.12f;

    [Header("Kapı Markerları")]
    [SerializeField] private bool showDoorMarkers = true;
    [SerializeField] private Color doorMarkerColor = Color.white;
    [SerializeField] private float doorMarkerScale = 0.35f;

    private List<DebugPlacedRoom> _placedRooms = new List<DebugPlacedRoom>();
    private Transform _connectionsRoot;
    private Transform _doorMarkersRoot;
    private int _corridorCounter;

    private static Sprite _cachedDebugSprite;

    private void Start()
    {
        if (generateOnStart)
            GenerateDebugDungeon();
    }

    [ContextMenu("Generate Debug Dungeon")]
    public void GenerateDebugDungeon()
    {
        if (visibleGrid == null)
            visibleGrid = GetComponent<VisibleMacroGrid>();

        if (visibleGrid == null)
        {
            Debug.LogError("VisibleMacroGrid referansı yok.");
            return;
        }

        visibleGrid.BuildVisibleGrid();
        visibleGrid.ResetGridVisuals();

        ClearConnections();
        ClearDoorMarkers();

        _placedRooms.Clear();
        _corridorCounter = 0;

        Vector2Int startOrigin = new Vector2Int(visibleGrid.Width / 2, visibleGrid.Height / 2);

        DebugPlacedRoom startRoom = TryPlaceFixedRoom("START_00", startShape, startOrigin);
        if (startRoom == null)
        {
            Debug.LogError("Start odası yerleşemedi.");
            return;
        }

        int placedCombatCount = 0;
        int placedTreasureCount = 0;
        HashSet<string> usedCombatParentsForTreasure = new HashSet<string>();

        // Oda tipleri karisik: labirent benzeri dagilim icin tek bir hat zorunlu degil.
        List<bool> roomTypeQueue = new List<bool>();
        for (int i = 0; i < combatRoomCount; i++)
            roomTypeQueue.Add(false); // combat
        for (int i = 0; i < treasureRoomCount; i++)
            roomTypeQueue.Add(true); // treasure

        Shuffle(roomTypeQueue);

        // Yerlesim sirasi listesi: "ilk N odaya yeni baglanti yok" kurali burada uygulanir.
        List<DebugPlacedRoom> placementOrder = new List<DebugPlacedRoom> { startRoom };

        for (int i = 0; i < roomTypeQueue.Count; i++)
        {
            bool isTreasure = roomTypeQueue[i];

            string roomId = isTreasure
                ? $"TREASURE_{placedTreasureCount:00}"
                : $"COMBAT_{placedCombatCount:00}";

            RoomShapeType[] allowedShapes = isTreasure
                ? new RoomShapeType[] { treasureShape }
                : regularShapes;

            bool lockEarlyRoomsAsParents = placementOrder.Count >= noNewConnectionsToFirstRoomsCount;
            List<DebugPlacedRoom> parentCandidates = BuildParentCandidates(
                placementOrder,
                lockEarlyRoomsAsParents,
                isTreasure,
                usedCombatParentsForTreasure
            );

            bool placed = TryPlaceAdjacentToAnyParent(
                parentCandidates,
                roomId,
                allowedShapes,
                out DebugPlacedRoom selectedParent,
                out DebugPlacedRoom nextRoom,
                out Vector2Int nextDir,
                out int nextGap
            );

            if (!placed || nextRoom == null || selectedParent == null)
            {
                Debug.LogWarning($"Oda yerlesemedi: {roomId}");
                continue;
            }

            bool connectionCreated = CreateConnectionGeometry(selectedParent, nextRoom, nextDir, nextGap);
            if (!connectionCreated)
            {
                RollbackPlacedRoom(nextRoom);
                Debug.LogWarning($"Baglanti kurulamadigi icin oda geri alindi: {roomId}");
                continue;
            }

            placementOrder.Add(nextRoom);

            if (isTreasure)
            {
                if (IsCombatRoom(selectedParent))
                    usedCombatParentsForTreasure.Add(selectedParent.RoomId);
                placedTreasureCount++;
            }
            else
                placedCombatCount++;
        }

        if (placedCombatCount < combatRoomCount)
        {
            Debug.LogWarning($"Istenen combat sayisi: {combatRoomCount}, yerlesen: {placedCombatCount}");
        }

        if (placedTreasureCount < treasureRoomCount)
        {
            Debug.LogWarning($"Istenen treasure sayisi: {treasureRoomCount}, yerlesen: {placedTreasureCount}");
        }

        // Boss, tek hattin sonu yerine mevcut yapiya rastgele bir parent uzerinden eklenir.
        List<DebugPlacedRoom> bossParentCandidates = BuildParentCandidates(
            placementOrder,
            placementOrder.Count >= noNewConnectionsToFirstRoomsCount,
            false,
            usedCombatParentsForTreasure
        );

        bool bossPlaced = TryPlaceAdjacentToAnyParent(
            bossParentCandidates,
            "BOSS_00",
            new RoomShapeType[] { bossShape },
            out DebugPlacedRoom bossParent,
            out DebugPlacedRoom bossRoom,
            out Vector2Int bossDir,
            out int bossGap
        );

        if (bossPlaced && bossRoom != null && bossParent != null)
        {
            bool bossConnectionCreated = CreateConnectionGeometry(bossParent, bossRoom, bossDir, bossGap);
            if (!bossConnectionCreated)
            {
                RollbackPlacedRoom(bossRoom);
                Debug.LogWarning("Boss odasi geri alindi: baglanti kurulamadi.");
            }
            else
            {
                placementOrder.Add(bossRoom);
            }
        }
        else
        {
            Debug.LogWarning("Boss odasi yerlesemedi.");
        }

        visibleGrid.RefreshColors();

        Debug.Log("Debug dungeon üretildi. Yerleşen oda sayısı: " + _placedRooms.Count);
    }

    private DebugPlacedRoom TryPlaceAdjacentRoom(
    DebugPlacedRoom parentRoom,
    string roomId,
    RoomShapeType[] allowedShapes,
    out Vector2Int placedDirection,
    out int usedGap
    )
    {
        placedDirection = Vector2Int.zero;
        usedGap = -1;

        List<Vector2Int> directions = new List<Vector2Int>
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

        Shuffle(directions);

        List<int> lateralOffsets = new List<int> { 0, 1, -1, 2, -2 };
        Shuffle(lateralOffsets);

        List<int> gapCandidates = BuildGapCandidates();

        for (int shapeIndex = 0; shapeIndex < allowedShapes.Length; shapeIndex++)
        {
            RoomShapeType shapeType = allowedShapes[shapeIndex];
            List<Vector2Int> localCells = FootprintLibrary.GetShape(shapeType);

            for (int dirIndex = 0; dirIndex < directions.Count; dirIndex++)
            {
                Vector2Int dir = directions[dirIndex];

                for (int gapIndex = 0; gapIndex < gapCandidates.Count; gapIndex++)
                {
                    int candidateGap = gapCandidates[gapIndex];

                    for (int offsetIndex = 0; offsetIndex < lateralOffsets.Count; offsetIndex++)
                    {
                        int lateralOffset = lateralOffsets[offsetIndex];

                        Vector2Int candidateOrigin = CalculateAdjacentOrigin(
                            parentRoom,
                            localCells,
                            dir,
                            lateralOffset,
                            candidateGap
                        );

                        List<Vector2Int> worldCells = FootprintLibrary.ToWorldCells(localCells, candidateOrigin);

                        // Gap 0 ise gerçekten yan yana temas eden bir kenar olmalı.
                        // Sadece bounds yakınlığı yetmez.
                        if (candidateGap == 0 && !HasSideAdjacency(parentRoom.WorldCells, worldCells, dir))
                            continue;

                        // Aday oda, parent (gap 0) disinda mevcut odalara/koridorlara
                        // yandan temas etmesin. Bu sayede kapisiz dipdibe baglanti olusmaz.
                        if (HasUnintendedSideAdjacency(worldCells, parentRoom, candidateGap))
                            continue;

                        DebugPlacedRoom candidateRoom = new DebugPlacedRoom(roomId, shapeType, candidateOrigin, worldCells);

                        // Corridor ile bağlanacak odalar için, corridor'un gerçekten
                        // yerleşebilir olduğunu önceden doğrula.
                        if (candidateGap > 0 && !CanPlaceCorridorBetween(parentRoom, candidateRoom, dir))
                            continue;

                        bool success = visibleGrid.TryOccupyCells(worldCells, roomId);
                        if (!success)
                            continue;

                        _placedRooms.Add(candidateRoom);

                        placedDirection = dir;
                        usedGap = candidateGap;
                        return candidateRoom;
                    }
                }
            }
        }

        return null;
    }

    private bool TryPlaceAdjacentToAnyParent(
        List<DebugPlacedRoom> parentCandidates,
        string roomId,
        RoomShapeType[] allowedShapes,
        out DebugPlacedRoom selectedParent,
        out DebugPlacedRoom placedRoom,
        out Vector2Int placedDirection,
        out int usedGap
    )
    {
        selectedParent = null;
        placedRoom = null;
        placedDirection = Vector2Int.zero;
        usedGap = -1;

        if (parentCandidates == null || parentCandidates.Count == 0)
            return false;

        for (int i = 0; i < parentCandidates.Count; i++)
        {
            DebugPlacedRoom parent = parentCandidates[i];
            if (parent == null)
                continue;

            DebugPlacedRoom candidate = TryPlaceAdjacentRoom(
                parent,
                roomId,
                allowedShapes,
                out Vector2Int candidateDir,
                out int candidateGap
            );

            if (candidate == null)
                continue;

            selectedParent = parent;
            placedRoom = candidate;
            placedDirection = candidateDir;
            usedGap = candidateGap;
            return true;
        }

        return false;
    }

    private List<DebugPlacedRoom> BuildParentCandidates(
        List<DebugPlacedRoom> placementOrder,
        bool lockEarlyRoomsAsParents,
        bool childIsTreasure,
        HashSet<string> usedCombatParentsForTreasure
    )
    {
        List<DebugPlacedRoom> baseCandidates = new List<DebugPlacedRoom>();
        if (placementOrder == null || placementOrder.Count == 0)
            return baseCandidates;

        if (!lockEarlyRoomsAsParents)
        {
            baseCandidates.AddRange(placementOrder);
        }
        else
        {
            int protectedCount = Mathf.Clamp(noNewConnectionsToFirstRoomsCount, 0, placementOrder.Count);
            for (int i = protectedCount; i < placementOrder.Count; i++)
            {
                baseCandidates.Add(placementOrder[i]);
            }

            // Eger korunan odalar disinda parent kalmadiysa, place islemi tamamen kilitlenmesin.
            if (baseCandidates.Count == 0)
                baseCandidates.AddRange(placementOrder);
        }

        List<DebugPlacedRoom> result = new List<DebugPlacedRoom>();
        for (int i = 0; i < baseCandidates.Count; i++)
        {
            DebugPlacedRoom candidate = baseCandidates[i];
            if (candidate == null)
                continue;

            if (childIsTreasure)
            {
                // Treasure sadece combat parent'tan dogar:
                // START -> TREASURE ve TREASURE -> TREASURE engellenir.
                if (!IsCombatRoom(candidate))
                    continue;

                // Her treasure farkli bir combat parent kullanir.
                if (usedCombatParentsForTreasure != null &&
                    usedCombatParentsForTreasure.Contains(candidate.RoomId))
                    continue;
            }
            else
            {
                // Treasure odalari baska odalara parent olmasin.
                if (IsTreasureRoom(candidate))
                    continue;
            }

            result.Add(candidate);
        }

        Shuffle(result);
        return result;
    }

    private bool IsCombatRoom(DebugPlacedRoom room)
    {
        return room != null &&
               !string.IsNullOrEmpty(room.RoomId) &&
               room.RoomId.StartsWith("COMBAT");
    }

    private bool IsTreasureRoom(DebugPlacedRoom room)
    {
        return room != null &&
               !string.IsNullOrEmpty(room.RoomId) &&
               room.RoomId.StartsWith("TREASURE");
    }

    private Vector2Int CalculateAdjacentOrigin(
    DebugPlacedRoom parentRoom,
    List<Vector2Int> childLocalCells,
    Vector2Int direction,
    int lateralOffset,
    int gapBetweenTheseRooms
    )
    {
        Vector2Int parentMin = parentRoom.BoundsMin;
        Vector2Int parentMax = parentRoom.BoundsMax;

        FootprintLibrary.GetBounds(childLocalCells, out Vector2Int childMin, out Vector2Int childMax);

        float parentCenterX = (parentMin.x + parentMax.x) * 0.5f;
        float parentCenterY = (parentMin.y + parentMax.y) * 0.5f;

        float childCenterLocalX = (childMin.x + childMax.x) * 0.5f;
        float childCenterLocalY = (childMin.y + childMax.y) * 0.5f;

        int originX = 0;
        int originY = 0;

        if (direction == Vector2Int.right)
        {
            originX = parentMax.x + gapBetweenTheseRooms + 1 - childMin.x;
            originY = Mathf.RoundToInt(parentCenterY - childCenterLocalY) + lateralOffset;
        }
        else if (direction == Vector2Int.left)
        {
            originX = parentMin.x - gapBetweenTheseRooms - 1 - childMax.x;
            originY = Mathf.RoundToInt(parentCenterY - childCenterLocalY) + lateralOffset;
        }
        else if (direction == Vector2Int.up)
        {
            originX = Mathf.RoundToInt(parentCenterX - childCenterLocalX) + lateralOffset;
            originY = parentMax.y + gapBetweenTheseRooms + 1 - childMin.y;
        }
        else if (direction == Vector2Int.down)
        {
            originX = Mathf.RoundToInt(parentCenterX - childCenterLocalX) + lateralOffset;
            originY = parentMin.y - gapBetweenTheseRooms - 1 - childMax.y;
        }

        return new Vector2Int(originX, originY);
    }

    private bool HasSideAdjacency(List<Vector2Int> roomACells, List<Vector2Int> roomBCells, Vector2Int directionFromAtoB)
    {
        HashSet<Vector2Int> bSet = new HashSet<Vector2Int>(roomBCells);

        for (int i = 0; i < roomACells.Count; i++)
        {
            Vector2Int neighbor = roomACells[i] + directionFromAtoB;
            if (bSet.Contains(neighbor))
                return true;
        }

        return false;
    }

    private bool HasUnintendedSideAdjacency(
        List<Vector2Int> candidateCells,
        DebugPlacedRoom parentRoom,
        int candidateGap
    )
    {
        if (visibleGrid == null || visibleGrid.Grid == null)
            return false;

        HashSet<Vector2Int> candidateSet = new HashSet<Vector2Int>(candidateCells);
        HashSet<Vector2Int> parentSet = new HashSet<Vector2Int>(parentRoom.WorldCells);

        Vector2Int[] cardinalOffsets =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        for (int i = 0; i < candidateCells.Count; i++)
        {
            Vector2Int cell = candidateCells[i];

            for (int j = 0; j < cardinalOffsets.Length; j++)
            {
                Vector2Int neighbor = cell + cardinalOffsets[j];

                if (candidateSet.Contains(neighbor))
                    continue;

                MacroCellData neighborCell = visibleGrid.Grid.GetCell(neighbor);
                if (neighborCell == null || !neighborCell.IsOccupied)
                    continue;

                bool isParentNeighbor = parentSet.Contains(neighbor);
                if (candidateGap == 0 && isParentNeighbor)
                    continue;

                return true;
            }
        }

        return false;
    }

    private void CreateDirectDoorMarker(DebugPlacedRoom roomA, DebugPlacedRoom roomB, Vector2Int directionFromAtoB)
    {
        if (TryGetDirectDoorMacroPoint(roomA, roomB, directionFromAtoB, out Vector2 doorPoint))
        {
            CreateDoorMarkerAtWorld(
                visibleGrid.MacroPointToWorld(doorPoint, -0.25f),
                doorMarkerScale * 1.15f
            );
        }
    }

    private bool TryGetDirectDoorMacroPoint(
    DebugPlacedRoom roomA,
    DebugPlacedRoom roomB,
    Vector2Int directionFromAtoB,
    out Vector2 doorPoint
)
    {
        doorPoint = Vector2.zero;

        HashSet<Vector2Int> bSet = new HashSet<Vector2Int>(roomB.WorldCells);
        List<Vector2> candidatePoints = new List<Vector2>();

        for (int i = 0; i < roomA.WorldCells.Count; i++)
        {
            Vector2Int aCell = roomA.WorldCells[i];
            Vector2Int bNeighbor = aCell + directionFromAtoB;

            if (bSet.Contains(bNeighbor))
            {
                Vector2 midpoint = ((Vector2)aCell + (Vector2)bNeighbor) * 0.5f;
                candidatePoints.Add(midpoint);
            }
        }

        if (candidatePoints.Count == 0)
            return false;

        Vector2 targetCenter = (roomA.GetCenterMacro() + roomB.GetCenterMacro()) * 0.5f;

        float bestDist = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < candidatePoints.Count; i++)
        {
            float dist = Vector2.Distance(candidatePoints[i], targetCenter);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        doorPoint = candidatePoints[bestIndex];
        return true;
    }

    private void CreateDoorMarkerAtWorld(Vector3 worldPosition, float scaleMultiplier = 1f)
    {
        if (_doorMarkersRoot == null)
        {
            _doorMarkersRoot = new GameObject("DoorMarkers").transform;
            _doorMarkersRoot.SetParent(transform, false);
        }

        GameObject marker = new GameObject("DirectDoorMarker");
        marker.transform.SetParent(_doorMarkersRoot, false);

        SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
        sr.sprite = GetDebugSprite();
        sr.color = doorMarkerColor;
        sr.sortingOrder = 20;

        marker.transform.position = worldPosition;
        marker.transform.localScale = Vector3.one * (visibleGrid.CellWorldSize * doorMarkerScale * scaleMultiplier);
    }

    private bool CreateConnectionGeometry(
    DebugPlacedRoom roomA,
    DebugPlacedRoom roomB,
    Vector2Int directionFromAtoB,
    int usedGap
)
    {
        // Gap 0 ise corridor yok, direct door var
        if (usedGap == 0)
        {
            if (showDoorMarkers)
            {
                CreateDirectDoorMarker(roomA, roomB, directionFromAtoB);
            }

            if (showConnectionLines)
            {
                CreateConnection(roomA, roomB);
            }

            return true;
        }

        // Gap > 0 ise corridor üret
        List<Vector2Int> corridorCells = CorridorPathUtility.BuildCorridorCells(roomA, roomB, directionFromAtoB);

        if (corridorCells == null || corridorCells.Count == 0)
            return false;

        bool corridorPlaced = visibleGrid.TryOccupyCells(
            corridorCells,
            $"CORRIDOR_{_corridorCounter:00}"
        );

        if (!corridorPlaced)
        {
            Debug.LogWarning($"Corridor yerleşemedi: {roomA.RoomId} -> {roomB.RoomId}");
            return false;
        }

        if (showDoorMarkers)
        {
            CreateDoorMarker(corridorCells[0]);
            if (corridorCells.Count > 1)
                CreateDoorMarker(corridorCells[corridorCells.Count - 1]);
        }

        _corridorCounter++;

        if (showConnectionLines)
        {
            CreateConnection(roomA, roomB);
        }

        return true;
    }

        private DebugPlacedRoom TryPlaceFixedRoom(string roomId, RoomShapeType shapeType, Vector2Int origin)
    {
        List<Vector2Int> localCells = FootprintLibrary.GetShape(shapeType);
        List<Vector2Int> worldCells = FootprintLibrary.ToWorldCells(localCells, origin);

        bool success = visibleGrid.TryOccupyCells(worldCells, roomId);
        if (!success)
            return null;

        DebugPlacedRoom room = new DebugPlacedRoom(roomId, shapeType, origin, worldCells);
        _placedRooms.Add(room);
        return room;
    }


    private void CreateDoorMarker(Vector2Int macroCell)
    {
        CreateDoorMarkerAtWorld(
            visibleGrid.MacroToWorld(macroCell) + new Vector3(0f, 0f, -0.25f),
            1f
        );
    }

    private List<int> BuildGapCandidates()
    {
        int minGap = Mathf.Max(0, minGapBetweenRooms);
        int maxGap = Mathf.Max(minGap, maxGapBetweenRooms);

        List<int> result = new List<int>();

        for (int gap = minGap; gap <= maxGap; gap++)
        {
            int repeatCount = 1;

            if (biasTowardShortGaps)
            {
                if (gap == 0) repeatCount = 4;
                else if (gap == 1) repeatCount = 5;
                else if (gap == 2) repeatCount = 2;
                else repeatCount = 1;
            }

            for (int i = 0; i < repeatCount; i++)
            {
                result.Add(gap);
            }
        }

        Shuffle(result);
        return result;
    }

    private bool CanPlaceCorridorBetween(DebugPlacedRoom roomA, DebugPlacedRoom roomB, Vector2Int directionFromAtoB)
    {
        if (visibleGrid == null || visibleGrid.Grid == null)
            return false;

        List<Vector2Int> corridorCells = CorridorPathUtility.BuildCorridorCells(roomA, roomB, directionFromAtoB);
        if (corridorCells == null || corridorCells.Count == 0)
            return false;

        return visibleGrid.Grid.CanPlace(corridorCells);
    }

    private void RollbackPlacedRoom(DebugPlacedRoom room)
    {
        if (room == null)
            return;

        if (visibleGrid != null && visibleGrid.Grid != null)
            visibleGrid.Grid.Clear(room.WorldCells);

        _placedRooms.Remove(room);
        visibleGrid.RefreshColors();
    }

    private Sprite GetDebugSprite()
    {
        if (_cachedDebugSprite != null)
            return _cachedDebugSprite;

        _cachedDebugSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        return _cachedDebugSprite;
    }

    private void CreateConnection(DebugPlacedRoom roomA, DebugPlacedRoom roomB)
    {
        if (_connectionsRoot == null)
        {
            _connectionsRoot = new GameObject("Connections").transform;
            _connectionsRoot.SetParent(transform, false);
        }

        GameObject lineObj = new GameObject($"{roomA.RoomId}_TO_{roomB.RoomId}");
        lineObj.transform.SetParent(_connectionsRoot, false);

        DebugConnectionView connectionView = lineObj.AddComponent<DebugConnectionView>();

        Vector3 start = MacroCenterToWorld(roomA);
        Vector3 end = MacroCenterToWorld(roomB);

        connectionView.Initialize(start, end, connectionColor, connectionWidth);
    }

    private Vector3 MacroCenterToWorld(DebugPlacedRoom room)
    {
        Vector2 center = room.GetCenterMacro();

        float xOffset = (visibleGrid.Width - 1) * 0.5f;
        float yOffset = (visibleGrid.Height - 1) * 0.5f;

        float worldX = (center.x - xOffset) * visibleGrid.CellWorldSize;
        float worldY = (center.y - yOffset) * visibleGrid.CellWorldSize;

        return new Vector3(worldX, worldY, -0.2f);
    }

    private void ClearConnections()
    {
        Transform existing = transform.Find("Connections");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        _connectionsRoot = null;
    }

    private void ClearDoorMarkers()
    {
        Transform existing = transform.Find("DoorMarkers");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        _doorMarkersRoot = null;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
