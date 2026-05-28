/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : PhysicalChoiceGroup.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using System.Collections.Generic;
using UnityEngine;

namespace Exponentia.Interaction
{
    [DisallowMultipleComponent]
    public class PhysicalChoiceGroup : MonoBehaviour
    {
        private List<PhysicalUpgradeChoice> _choices = new List<PhysicalUpgradeChoice>();

        public void RegisterChoice(PhysicalUpgradeChoice choice)
        {
            if (!_choices.Contains(choice))
            {
                _choices.Add(choice);
            }
        }

        public void MakeChoice(PhysicalUpgradeChoice chosenChoice)
        {
            // Sadece bir kere seçim yapılmasını garanti altına almak için listeyi ve etkileşimi kilitle
            foreach (var choice in _choices)
            {
                if (choice != null)
                {
                    // Diğer tüm seçimlerin etkileşimini anında kilitle ki hile yapılamasın
                    choice.DisableInteraction();
                }
            }

            // Seçilmeyen diğer 2 ödülü sahneden temizle
            foreach (var choice in _choices)
            {
                if (choice != null && choice != chosenChoice)
                {
                    choice.DestroyChoiceVisual();
                }
            }

            Debug.Log($"[ChoiceGroup] Oyuncu '{chosenChoice.UpgradeData?.displayName}' ödülünü seçti! Diğer seçenekler yok edildi.");
        }
    }
}
