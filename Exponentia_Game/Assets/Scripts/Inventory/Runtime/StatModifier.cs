using System;

namespace Exponentia.InventorySystem
{
    [Serializable]
    public class StatModifier
    {
        public string sourceId;
        public StatType statType;
        public ModifierType modifierType;
        public float value;

        public StatModifier(string sourceId, StatType statType, ModifierType modifierType, float value)
        {
            this.sourceId = sourceId;
            this.statType = statType;
            this.modifierType = modifierType;
            this.value = value;
        }
    }
}
