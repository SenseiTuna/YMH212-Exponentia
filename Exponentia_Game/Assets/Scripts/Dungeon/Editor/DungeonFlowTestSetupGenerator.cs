/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
====================================================
*/

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Exponentia.Dungeon;
using Exponentia.UI;

namespace Exponentia.Editor
{
    public static class DungeonFlowTestSetupGenerator
    {
        [MenuItem("Exponentia/Dungeon/Generate Flow Test Setup")]
        public static void GenerateSetup()
        {
            // 1. EventSystem kontrolü ve oluşturma
            EventSystem existingEventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
                Debug.Log("[TestSetup] Sahnede EventSystem bulunamadı, otomatik oluşturuldu.");
            }

            // 2. Canvas Oluşturma ve Ayarları
            GameObject canvasObj = new GameObject("DungeonFlow_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Dungeon Flow Canvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 3. Boss Odası Paneli Oluşturma
            GameObject bossPanelObj = CreateFullscreenPanel("BossRoom_Panel", canvasObj.transform, new Color(0.25f, 0.05f, 0.05f, 0.65f));
            CreateTextChild("Title_Text", bossPanelObj.transform, "BOSS SAVAŞI AKTİF!", 42, Color.red, new Vector2(0f, 100f));

            // 4. Ödül Odası Paneli Oluşturma
            GameObject rewardPanelObj = CreateFullscreenPanel("RewardRoom_Panel", canvasObj.transform, new Color(0.25f, 0.20f, 0.05f, 0.65f));
            CreateTextChild("Title_Text", rewardPanelObj.transform, "HAZİNE ODASINA GİRİLDİ!", 42, new Color(1f, 0.85f, 0.2f, 1f), new Vector2(0f, 100f));

            // 5. Kat Geçiş Paneli Oluşturma
            GameObject transitionPanelObj = CreateFullscreenPanel("FloorTransition_Panel", canvasObj.transform, new Color(0.08f, 0.08f, 0.1f, 0.96f));
            CreateTextChild("Title_Text", transitionPanelObj.transform, "KAT TAMAMLANDI", 54, Color.white, new Vector2(0f, 120f));

            // 6. Kat Geçiş Paneli Altına Buton Ekleme
            GameObject buttonObj = new GameObject("NextFloor_Button");
            buttonObj.transform.SetParent(transitionPanelObj.transform, false);
            
            RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(280f, 65f);
            btnRect.anchoredPosition = new Vector2(0f, -80f);

            Image btnImg = buttonObj.AddComponent<Image>();
            btnImg.color = new Color(0.85f, 0.85f, 0.9f, 1f);

            Button button = buttonObj.AddComponent<Button>();
            
            // Butonun hover ve click renklerini daha çekici yapalım
            ColorBlock cb = button.colors;
            cb.normalColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
            cb.pressedColor = new Color(0.6f, 0.6f, 0.65f, 1f);
            button.colors = cb;

            CreateTextChild("Button_Text", buttonObj.transform, "Sonraki Kata Geç", 22, new Color(0.1f, 0.1f, 0.15f, 1f), Vector2.zero);

            // 7. Scriptleri Ekle ve Referansları Bağla
            DungeonFlowManager flowManager = canvasObj.AddComponent<DungeonFlowManager>();
            FloorTransitionUI uiController = canvasObj.AddComponent<FloorTransitionUI>();

            SerializedObject so = new SerializedObject(uiController);
            so.FindProperty("bossRoomPanel").objectReferenceValue = bossPanelObj;
            so.FindProperty("rewardRoomPanel").objectReferenceValue = rewardPanelObj;
            so.FindProperty("floorTransitionPanel").objectReferenceValue = transitionPanelObj;
            so.FindProperty("nextFloorButton").objectReferenceValue = button;
            so.ApplyModifiedProperties();

            // 8. Panelleri Başlangıçta Gizle
            bossPanelObj.SetActive(false);
            rewardPanelObj.SetActive(false);
            transitionPanelObj.SetActive(false);

            // 9. Sahnede mevcut ManualRoomCombatTrigger bul ve "Reward -> Boss" sırası oluştur
            ManualRoomCombatTrigger rewardTrigger = Object.FindAnyObjectByType<ManualRoomCombatTrigger>();
            if (rewardTrigger != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(rewardTrigger.gameObject, "Setup Reward -> Boss Sequence");

                // A. Mevcut odayı "Ödül Odası" yap
                rewardTrigger.gameObject.name = "Room_Reward";
                
                // SerializedObject ile düşman listesini temizle (Böylece odaya girince savaş anında temizlenir ve ödül çıkar)
                SerializedObject rewardSO = new SerializedObject(rewardTrigger);
                rewardSO.FindProperty("enemyPrefabs").ClearArray();
                rewardSO.ApplyModifiedProperties();

                // Spawner bileşeni yoksa ekle ve bağla
                DungeonRewardSpawner spawner = rewardTrigger.GetComponent<DungeonRewardSpawner>();
                if (spawner == null)
                {
                    spawner = rewardTrigger.gameObject.AddComponent<DungeonRewardSpawner>();
                }
                
                // Editor kurulumunu çağır (kapıları vb. otomatik bulması için)
                rewardTrigger.EditorAutoSetup();

                // B. Bu odayı kopyalayarak "Boss Odası" oluştur
                // Zaten kopyası yoksa oluştur
                GameObject bossTriggerObj = GameObject.Find("Room_Boss");
                if (bossTriggerObj == null)
                {
                    bossTriggerObj = Object.Instantiate(rewardTrigger.gameObject, rewardTrigger.transform.position + Vector3.right * 18f, Quaternion.identity);
                    bossTriggerObj.name = "Room_Boss";
                    Undo.RegisterCreatedObjectUndo(bossTriggerObj, "Create Room_Boss");
                }

                ManualRoomCombatTrigger bossTrigger = bossTriggerObj.GetComponent<ManualRoomCombatTrigger>();
                if (bossTrigger != null)
                {
                    // Boss odası için düşman prefableri bulup ekleyelim
                    System.Collections.Generic.List<GameObject> enemyPrefabs = new System.Collections.Generic.List<GameObject>();
                    string[] enemyGuids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/Enemies/Basic" });
                    foreach (var guid in enemyGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (enemyPrefab != null)
                        {
                            enemyPrefabs.Add(enemyPrefab);
                        }
                    }

                    SerializedObject bossSO = new SerializedObject(bossTrigger);
                    SerializedProperty enemyProp = bossSO.FindProperty("enemyPrefabs");
                    enemyProp.ClearArray();
                    for (int i = 0; i < enemyPrefabs.Count; i++)
                    {
                        enemyProp.InsertArrayElementAtIndex(i);
                        enemyProp.GetArrayElementAtIndex(i).objectReferenceValue = enemyPrefabs[i];
                    }
                    bossSO.ApplyModifiedProperties();

                    // Editor kurulumunu çağır (kapıları vb. otomatik bulması için)
                    bossTrigger.EditorAutoSetup();
                }

                Debug.Log("[TestSetup] Sahnede 'Room_Reward' (Ödül) ve 'Room_Boss' (Boss) odaları sırasıyla kuruldu!");
            }

            // Değişiklikleri kaydet ve sahneyi kirlet
            EditorUtility.SetDirty(canvasObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Zindan Akış Sistemi Test Kurulumu", 
                "Kat Akış ve UI Sistemi başarıyla oluşturuldu!\n\n" +
                "1. DungeonFlow_Canvas ve EventSystem hiyerarşiye eklendi.\n" +
                "2. Boss, Ödül ve Geçiş panelleri Canvas altına oluşturuldu.\n" +
                "3. DungeonFlowManager ve FloorTransitionUI scriptleri otomatik bağlandı.\n" +
                "4. Mevcut odanız 'Room_Reward' yapıldı ve 18 birim sağında 'Room_Boss' odası kopyalandı!\n\n" +
                "Şimdi Play butonuna basarak akışı test edebilirsiniz!", "Harika!");
        }

        private static GameObject CreateFullscreenPanel(string name, Transform parent, Color color)
        {
            GameObject panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);

            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.one;

            Image img = panelObj.AddComponent<Image>();
            img.color = color;

            return panelObj;
        }

        private static GameObject CreateTextChild(string name, Transform parent, string textContent, int fontSize, Color color, Vector2 anchoredPos)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800f, 150f);
            rect.anchoredPosition = anchoredPos;

            Text text = textObj.AddComponent<Text>();
            text.text = textContent;
            text.fontSize = fontSize;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.supportRichText = true;

            return textObj;
        }
    }
}
