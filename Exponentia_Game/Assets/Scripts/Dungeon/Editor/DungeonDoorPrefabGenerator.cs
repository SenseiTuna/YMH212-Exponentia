/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : DungeonDoorPrefabGenerator.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using UnityEditor;
using UnityEngine;
using System.IO;

public static class DungeonDoorPrefabGenerator
{
    [MenuItem("Exponentia/Dungeon/Generate Dungeon Door Prefab")]
    public static void GeneratePrefab()
    {
        // 1. Prefab klasörünü kontrol et, yoksa oluştur
        string prefabsFolder = "Assets/Prefabs";
        if (!Directory.Exists(prefabsFolder))
        {
            Directory.CreateDirectory(prefabsFolder);
            AssetDatabase.Refresh();
        }

        string prefabPath = Path.Combine(prefabsFolder, "DungeonDoor.prefab");

        // 2. Geçici sahne objesi yapısını oluştur
        GameObject doorObj = new GameObject("DungeonDoor");
        BoxCollider2D col = doorObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        col.isTrigger = false; // Fiziksel olarak geçişi engellemeli

        // DungeonDoor scriptini ekle
        DungeonDoor doorScript = doorObj.AddComponent<DungeonDoor>();

        // Visual (Görsel) alt objesini oluştur
        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(doorObj.transform, false);
        SpriteRenderer sr = visualObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // Görseli otomatik bulmaya çalış
        string assetPackPath = "Assets/free-2d-top-down-pixel-dungeon-asset-pack/PNG/doors_lever_chest_animation.png";
        Sprite doorSprite = null;

        // Dilimlenmiş sprite listesini yükle
        object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPackPath);
        if (assets != null && assets.Length > 0)
        {
            foreach (object asset in assets)
            {
                if (asset is Sprite sp)
                {
                    // İlk kapalı kapı sprite'ını bulmaya çalışalım
                    if (sp.name.Contains("doors_lever_chest_animation_0") || sp.name.Contains("_0") || doorSprite == null)
                    {
                        doorSprite = sp;
                    }
                }
            }
        }

        if (doorSprite != null)
        {
            sr.sprite = doorSprite;
            Debug.Log($"[PrefabGenerator] Otomatik kapı görseli atandı: {doorSprite.name}");
        }
        else
        {
            Debug.LogWarning("[PrefabGenerator] Dilimlenmiş kapı görseli bulunamadı. Lütfen önce 'doors_lever_chest_animation.png' dosyasını Sprite Editor ile dilimleyin. Şimdilik kapı varsayılan görsel ile çalışacaktır.");
        }

        // 3. DungeonDoor bileşeni ayarlarını (Serialized Fields) otomatik bağla
        SerializedObject so = new SerializedObject(doorScript);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        
        // Renkler ve değerler
        so.FindProperty("lockedColor").colorValue = new Color(1f, 0.1f, 0.35f, 0.9f);
        so.FindProperty("unlockedColor").colorValue = new Color(1f, 0.1f, 0.35f, 0f);
        so.FindProperty("transitionDuration").floatValue = 0.5f;
        so.FindProperty("pulseSpeed").floatValue = 6.0f;
        so.FindProperty("enablePulseEffect").boolValue = true;
        
        so.ApplyModifiedProperties();

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(doorObj, prefabPath);
        
        // 5. Geçici sahne objesini temizle
        Object.DestroyImmediate(doorObj);

        // Veritabanını yenile ve oluşturulan prefabı seçili yap
        AssetDatabase.Refresh();
        if (prefabAsset != null)
        {
            EditorGUIUtility.PingObject(prefabAsset);
            Debug.Log($"[PrefabGenerator] Başarılı! DungeonDoor prefab'iniz oluşturuldu ve şuraya kaydedildi: {prefabPath}");
            EditorUtility.DisplayDialog("Başarılı", "DungeonDoor Prefab'iniz başarıyla oluşturuldu ve Assets/Prefabs klasörüne kaydedildi!", "Harika");
        }
        else
        {
            Debug.LogError("[PrefabGenerator] Prefab oluşturma aşamasında bir hata meydana geldi!");
        }
    }
}
