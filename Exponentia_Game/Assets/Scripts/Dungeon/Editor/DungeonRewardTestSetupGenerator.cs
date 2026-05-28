/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : DungeonRewardTestSetupGenerator.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Exponentia.Data;
using Exponentia.Interaction;
using Exponentia.Dungeon;

public static class DungeonRewardTestSetupGenerator
{
    [MenuItem("Exponentia/Upgrades/Generate Reward Test Setup")]
    public static void GenerateSetup()
    {
        // 1. Gerekli klasörleri oluştur
        string upgradesFolder = "Assets/Data/Upgrades";
        if (!Directory.Exists(upgradesFolder))
        {
            Directory.CreateDirectory(upgradesFolder);
            AssetDatabase.Refresh();
        }

        string prefabsFolder = "Assets/Prefabs";
        if (!Directory.Exists(prefabsFolder))
        {
            Directory.CreateDirectory(prefabsFolder);
            AssetDatabase.Refresh();
        }

        // 2. 3 Adet Örnek UpgradeData ScriptableObject Dosyası Oluştur
        List<UpgradeData> generatedUpgrades = new List<UpgradeData>();

        // Can Güçlendirmesi
        string hpPath = Path.Combine(upgradesFolder, "UG_Health.asset");
        UpgradeData hpData = AssetDatabase.LoadAssetAtPath<UpgradeData>(hpPath);
        if (hpData == null)
        {
            hpData = ScriptableObject.CreateInstance<UpgradeData>();
            hpData.upgradeId = "hp_up";
            hpData.displayName = "Kutsal Can";
            hpData.description = "Maksimum caninizi kalici olarak 25 artirir.";
            hpData.maxHealthBonus = 25f;
            AssetDatabase.CreateAsset(hpData, hpPath);
        }
        generatedUpgrades.Add(hpData);

        // Hasar Güçlendirmesi
        string dmgPath = Path.Combine(upgradesFolder, "UG_Damage.asset");
        UpgradeData dmgData = AssetDatabase.LoadAssetAtPath<UpgradeData>(dmgPath);
        if (dmgData == null)
        {
            dmgData = ScriptableObject.CreateInstance<UpgradeData>();
            dmgData.upgradeId = "dmg_up";
            dmgData.displayName = "Dev Gucu";
            dmgData.description = "Saldiri hasarinizi kalici olarak 5 artirir.";
            dmgData.damageBonus = 5f;
            AssetDatabase.CreateAsset(dmgData, dmgPath);
        }
        generatedUpgrades.Add(dmgData);

        // Hız Güçlendirmesi
        string speedPath = Path.Combine(upgradesFolder, "UG_Speed.asset");
        UpgradeData speedData = AssetDatabase.LoadAssetAtPath<UpgradeData>(speedPath);
        if (speedData == null)
        {
            speedData = ScriptableObject.CreateInstance<UpgradeData>();
            speedData.upgradeId = "speed_up";
            speedData.displayName = "Ruzgar Cizmeleri";
            speedData.description = "Hareket hizinizi kalici olarak 1.5 artirir.";
            speedData.moveSpeedBonus = 1.5f;
            AssetDatabase.CreateAsset(speedData, speedPath);
        }
        generatedUpgrades.Add(speedData);

        AssetDatabase.SaveAssets();

        // 3. Fiziksel Ödül Kutusu Prefab'ini Oluştur
        string prefabPath = Path.Combine(prefabsFolder, "UpgradeChoice.prefab");
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefabAsset == null)
        {
            GameObject tempChoiceObj = new GameObject("UpgradeChoice");
            BoxCollider2D col = tempChoiceObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            PhysicalUpgradeChoice choiceScript = tempChoiceObj.AddComponent<PhysicalUpgradeChoice>();

            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(tempChoiceObj.transform, false);
            SpriteRenderer sr = visualObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 16;

            // 2x2 beyaz kare doku oluştur (fallback)
            Texture2D tex = new Texture2D(2, 2);
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    tex.SetPixel(x, y, Color.yellow); // Sarı renkli çekici bir küp olsun
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);

            // Serialized properties ayarla
            SerializedObject so = new SerializedObject(choiceScript);
            so.FindProperty("iconRenderer").objectReferenceValue = sr;
            so.FindProperty("bounceSpeed").floatValue = 3.5f;
            so.FindProperty("bounceHeight").floatValue = 0.12f;
            so.ApplyModifiedProperties();

            prefabAsset = PrefabUtility.SaveAsPrefabAsset(tempChoiceObj, prefabPath);
            Object.DestroyImmediate(tempChoiceObj);
        }

        // 4. Sahnede Room1_CombatTrigger'ı Bul ve Konfigüre Et
        ManualRoomCombatTrigger combatTrigger = Object.FindAnyObjectByType<ManualRoomCombatTrigger>();
        if (combatTrigger != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(combatTrigger.gameObject, "Setup Reward Choices Test");

            DungeonRewardSpawner spawner = combatTrigger.GetComponent<DungeonRewardSpawner>();
            if (spawner == null)
            {
                spawner = combatTrigger.gameObject.AddComponent<DungeonRewardSpawner>();
            }

            // Spawner alanlarını otomatik doldur
            SerializedObject spawnerSO = new SerializedObject(spawner);
            spawnerSO.FindProperty("choicePrefab").objectReferenceValue = prefabAsset;
            spawnerSO.FindProperty("spacing").floatValue = 1.3f;
            spawnerSO.FindProperty("spawnHeightOffset").floatValue = 3.0f;

            // Upgrade listesini ata
            SerializedProperty listProp = spawnerSO.FindProperty("allAvailableUpgrades");
            listProp.ClearArray();
            for (int i = 0; i < generatedUpgrades.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = generatedUpgrades[i];
            }
            spawnerSO.ApplyModifiedProperties();

            // Tetikleyicideki spawner referansını bağla
            SerializedObject triggerSO = new SerializedObject(combatTrigger);
            triggerSO.FindProperty("rewardSpawner").objectReferenceValue = spawner;
            triggerSO.ApplyModifiedProperties();

            EditorUtility.SetDirty(combatTrigger.gameObject);
            Debug.Log($"[TestSetup] Sahnede '{combatTrigger.gameObject.name}' objesi ödül seçim sistemiyle donatıldı!");
        }
        else
        {
            Debug.LogWarning("[TestSetup] Sahnede 'ManualRoomCombatTrigger' takılı bir obje bulunamadı. Lütfen önce odayı hazırlayın.");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Ödül Seçim Sistemi Test Kurulumu", 
            "Test ortamınız başarıyla oluşturuldu!\n\n" +
            "1. Assets/Data/Upgrades klasörüne 3 adet örnek güçlendirme (Can, Hasar, Hız) kaydedildi.\n" +
            "2. Assets/Prefabs/UpgradeChoice.prefab oluşturuldu.\n" +
            "3. Sahnenizdeki oda tetikleyicisi bu yeni sistemle donatıldı.\n\n" +
            "Şimdi oyunu başlatıp odayı temizleyerek test edebilirsiniz!", "Harika!");
    }
}
