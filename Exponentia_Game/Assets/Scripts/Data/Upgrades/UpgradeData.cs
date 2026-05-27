/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : UpgradeData.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using UnityEngine;

namespace Exponentia.Data
{
    [CreateAssetMenu(fileName = "UG_NewUpgrade", menuName = "Exponentia/Upgrades/UpgradeData", order = 1)]
    public class UpgradeData : ScriptableObject
    {
        [Header("Upgrade Identity")]
        public string upgradeId;
        public string displayName;
        [TextArea(3, 5)]
        public string description;
        public Sprite iconSprite;

        [Header("Permanent Stat Modifiers")]
        public float maxHealthBonus = 0f;
        public float damageBonus = 0f;
        public float moveSpeedBonus = 0f;
        public float attackSpeedBonus = 0f;
        public float defenseBonus = 0f;
    }
}
