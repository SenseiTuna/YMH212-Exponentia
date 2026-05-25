/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : DungeonState.cs
BUILD_DATE : 2026-05-25
====================================================
*/

namespace Exponentia.Dungeon
{
    /// <summary>
    /// Roguelite zindan akışının anlık durumlarını temsil eder.
    /// </summary>
    public enum DungeonState
    {
        NormalRoom,       // Normal savaş veya koridor odaları
        BossRoom,         // Aktif Boss savaşı devrede
        RewardRoom,       // Ödül/Hazine odasına girildi
        FloorTransition   // Kat geçiş ekranı aktif (Kat temizlendi)
    }
}
