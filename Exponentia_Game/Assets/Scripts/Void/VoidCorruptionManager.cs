using System;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    [DisallowMultipleComponent]
    public class VoidCorruptionManager : MonoBehaviour
    {
        [SerializeField] private float corruptionValue;

        public float CorruptionValue => corruptionValue;

        public event Action<float> OnCorruptionChanged;

        public void AddCorruption(float value)
        {
            if (value <= 0f)
            {
                return;
            }

            corruptionValue = Mathf.Max(0f, corruptionValue + value);
            OnCorruptionChanged?.Invoke(corruptionValue);
        }

        public void SetCorruption(float value)
        {
            corruptionValue = Mathf.Max(0f, value);
            OnCorruptionChanged?.Invoke(corruptionValue);
        }
    }
}
