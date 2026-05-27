/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : ManualRoomCombatTriggerEditor.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ManualRoomCombatTrigger))]
public class ManualRoomCombatTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Standart Inspector alanlarını çiz (Serileştirilmiş alanlar)
        DrawDefaultInspector();

        // Araya biraz boşluk bırak
        EditorGUILayout.Space(15);

        // Şık, geniş bir buton çizelim
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f, 1f); // Neon mavi arka plan
        if (GUILayout.Button("🪄 AUTO SETUP ROOM TRIGGER", GUILayout.Height(35)))
        {
            ManualRoomCombatTrigger trigger = (ManualRoomCombatTrigger)target;
            
            // Undo (Geri Al) desteği ekleyelim ki Unity hata vermesin ve geri alınabilsin
            Undo.RegisterFullObjectHierarchyUndo(trigger.gameObject, "Auto Setup Room Trigger");
            
            trigger.EditorAutoSetup();
            
            // Sahneyi kirletilmiş (dirty) olarak işaretle ki kaydedilsin
            EditorUtility.SetDirty(trigger);
            PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
        }
        GUI.backgroundColor = Color.white; // Rengi sıfırla
    }
}
