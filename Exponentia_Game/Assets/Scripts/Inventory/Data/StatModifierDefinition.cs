using System;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    [Serializable]
    public class StatModifierDefinition
    {
        [Tooltip("Which stat this modifier affects.")]
        public StatType statType = StatType.Damage;

        [Tooltip("Flat adds directly. Percent is applied as +X%.")]
        public ModifierType modifierType = ModifierType.Flat;

        [Tooltip("Modifier value. Percent uses 10 for +10%.")]
        public float value = 1f;
    }
}
