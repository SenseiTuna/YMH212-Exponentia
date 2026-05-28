/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : DungeonPrototypesGenerator.cs
BUILD_DATE : 2026-05-26
====================================================
*/

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Exponentia.Dungeon;
using Exponentia.Interaction;
using Exponentia.Data;

namespace Exponentia.Editor
{
    public static class DungeonPrototypesGenerator
    {
        [MenuItem("Exponentia/Rooms/Generate Shop Room Setup")]
        public static void GenerateShopSetup()
        {
            // 1. Ana Mağaza Boş Objesini Oluştur
            GameObject shopRoomObj = new GameObject("ShopRoom_Setup");
            Undo.RegisterCreatedObjectUndo(shopRoomObj, "Generate Shop Room Setup");

            // Projeden mevcut örnek UpgradeData dosyalarını bul
            System.Collections.Generic.List<UpgradeData> upgrades = new System.Collections.Generic.List<UpgradeData>();
            string[] guids = AssetDatabase.FindAssets("t:UpgradeData");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UpgradeData data = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
                if (data != null)
                {
                    upgrades.Add(data);
                }
            }

            // 3 adet kaideyi yan yana oluştur
            float spacing = 1.8f;
            int[] prices = { 35, 50, 75 };

            for (int i = 0; i < 3; i++)
            {
                // Fiziksel Kaide (2D Sprite-based)
                GameObject pedestalObj = new GameObject($"ShopPedestal_{i + 1}");
                pedestalObj.transform.SetParent(shopRoomObj.transform, false);
                pedestalObj.transform.localPosition = new Vector3((i - 1) * spacing, 0f, 0f);
                pedestalObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

                SpriteRenderer baseSr = pedestalObj.AddComponent<SpriteRenderer>();
                baseSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Square.png");
                baseSr.sortingOrder = 5;
                baseSr.color = new Color(0.18f, 0.18f, 0.22f, 1f); // Taş kaide rengi

                BoxCollider2D col2d = pedestalObj.AddComponent<BoxCollider2D>();
                col2d.isTrigger = true;
                col2d.size = new Vector2(1.2f, 1.2f);

                // ShopPedestal scriptini ekle
                ShopPedestal shopPed = pedestalObj.AddComponent<ShopPedestal>();

                // Eşya Görsel Grubu
                GameObject visualGroup = new GameObject("ItemVisual");
                visualGroup.transform.SetParent(pedestalObj.transform, false);
                visualGroup.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                visualGroup.transform.localScale = Vector3.one;

                SpriteRenderer sr = visualGroup.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 18;

                // Veri varsa bağla
                UpgradeData dataToBind = upgrades.Count > i ? upgrades[i] : null;
                shopPed.Initialize(dataToBind, prices[i]);

                Undo.RegisterCreatedObjectUndo(pedestalObj, $"Create Pedestal {i + 1}");
            }

            Selection.activeGameObject = shopRoomObj;
            EditorUtility.SetDirty(shopRoomObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Mağaza Odası Kurulumu", 
                "Mağaza Odası Prototipleri başarıyla oluşturuldu!\n\n" +
                "1. Sahnede 'ShopRoom_Setup' objesi altında yan yana 3 kaide kuruldu.\n" +
                "2. Kaideler otomatik olarak projedeki UpgradeData verileriyle donatıldı.\n" +
                "3. Buton veya elle ayarlama gerekmeden direkt Play modunda satın alabilirsiniz!", "Harika!");
        }

        [MenuItem("Exponentia/Rooms/Generate Event Room Setup")]
        public static void GenerateEventSetup()
        {
            // 1. Ana Etkinlik Boş Objesini Oluştur
            GameObject eventRoomObj = new GameObject("EventRoom_Setup");
            Undo.RegisterCreatedObjectUndo(eventRoomObj, "Generate Event Room Setup");

            // 2. Altar Sütunu (2D Sprite-based)
            GameObject altarObj = new GameObject("MysteryAltar");
            altarObj.transform.SetParent(eventRoomObj.transform, false);
            altarObj.transform.localPosition = Vector3.zero;
            altarObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            SpriteRenderer altarSr = altarObj.AddComponent<SpriteRenderer>();
            altarSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Square.png");
            altarSr.sortingOrder = 5;
            altarSr.color = new Color(0.35f, 0.15f, 0.45f, 1f); // Egzotik lanetli mor

            BoxCollider2D col2d = altarObj.AddComponent<BoxCollider2D>();
            col2d.isTrigger = true;
            col2d.size = new Vector2(1.5f, 1.5f);

            // MysteryAltar scriptini ekle
            MysteryAltar altar = altarObj.AddComponent<MysteryAltar>();

            // 3. Event Canvas & Event Panel Arayüzünü Oluştur (Yoksa)
            GameObject existingCanvas = GameObject.Find("EventUI_Canvas");
            if (existingCanvas == null)
            {
                // Canvas oluştur
                GameObject canvasObj = new GameObject("EventUI_Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();

                // Karar Verme Paneli (Merkezde)
                GameObject panelObj = new GameObject("Event_Panel");
                panelObj.transform.SetParent(canvasObj.transform, false);

                RectTransform pRect = panelObj.AddComponent<RectTransform>();
                pRect.sizeDelta = new Vector2(750f, 480f);
                pRect.anchoredPosition = Vector2.zero;

                Image pImg = panelObj.AddComponent<Image>();
                pImg.color = new Color(0.08f, 0.08f, 0.11f, 0.96f);

                // Panel kenarlığı (Premium Neon Mor)
                GameObject borderObj = new GameObject("Border");
                borderObj.transform.SetParent(panelObj.transform, false);
                RectTransform bRect = borderObj.AddComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero;
                bRect.anchorMax = Vector2.one;
                bRect.sizeDelta = new Vector2(8f, 8f);
                Image bImg = borderObj.AddComponent<Image>();
                bImg.color = new Color(0.55f, 0.25f, 0.7f, 1f);

                // Etkinlik Başlığı
                CreateTextChild("Title_Text", panelObj.transform, "KADERİNİ SEÇ", 36, new Color(0.7f, 0.4f, 0.9f, 1f), new Vector2(0f, 160f));

                // Etkinlik Açıklaması
                GameObject descTextObj = CreateTextChild("Description_Text", panelObj.transform, "Lanetli altar seninle konuşuyor...", 22, Color.white, new Vector2(0f, 20f));
                Text descText = descTextObj.GetComponent<Text>();
                descText.rectTransform.sizeDelta = new Vector2(650f, 180f);
                descText.alignment = TextAnchor.UpperCenter;

                // Buton A (Meydan Oku / Feda Et)
                GameObject btnAObj = CreateButtonChild("OptionA_Button", panelObj.transform, "%30 Can Feda Et (+5 Hasar)", new Vector2(-170f, -140f), new Color(0.6f, 0.15f, 0.15f, 1f));
                
                // Buton B (Ayrıl)
                GameObject btnBObj = CreateButtonChild("OptionB_Button", panelObj.transform, "Ayrıl", new Vector2(170f, -140f), new Color(0.2f, 0.2f, 0.25f, 1f));

                // Canvas'ı başlangıçta deaktif et
                canvasObj.SetActive(false);
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Event UI Canvas");
            }

            // EventSystem kontrolü
            EventSystem existingEventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
            }

            // Hiyerarşik bağlar
            altarObj.transform.SetParent(eventRoomObj.transform, true);

            Selection.activeGameObject = eventRoomObj;
            EditorUtility.SetDirty(eventRoomObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Etkinlik Odası Kurulumu", 
                "Lanetli Altar Etkinlik Odası başarıyla oluşturuldu!\n\n" +
                "1. Sahnede 'MysteryAltar' sütunu ve 'EventUI_Canvas' paneli oluşturuldu.\n" +
                "2. Altar tetiklendiğinde oyun durur ve iki şıklı karar paneli ekrana gelir.\n" +
                "3. Dinleyiciler ve stat güncellemeleri arka planda tamamen kodla bağlanmıştır!", "Harika!");
        }

        [MenuItem("Exponentia/Rooms/Generate Challenge Room Setup")]
        public static void GenerateChallengeSetup()
        {
            // 1. Ana Meydan Okuma Boş Objesini Oluştur
            GameObject challengeRoomObj = new GameObject("ChallengeRoom_Setup");
            Undo.RegisterCreatedObjectUndo(challengeRoomObj, "Generate Challenge Room Setup");

            // 2. Şalter Kutusu (2D Sprite-based)
            GameObject baseObj = new GameObject("ChallengeSwitch");
            baseObj.transform.SetParent(challengeRoomObj.transform, false);
            baseObj.transform.localPosition = Vector3.zero;
            baseObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            SpriteRenderer baseSr = baseObj.AddComponent<SpriteRenderer>();
            baseSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Square.png");
            baseSr.sortingOrder = 5;
            baseSr.color = new Color(0.25f, 0.25f, 0.3f, 1f); // Çelik şalter kutusu

            BoxCollider2D col2d = baseObj.AddComponent<BoxCollider2D>();
            col2d.isTrigger = true;
            col2d.size = new Vector2(1.3f, 1.3f);

            // 3. Şalter Kolu (2D Sprite-based)
            GameObject leverHandle = new GameObject("LeverHandle");
            leverHandle.name = "LeverHandle";
            leverHandle.transform.SetParent(baseObj.transform, false);
            leverHandle.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            leverHandle.transform.localRotation = Quaternion.Euler(0f, 0f, -25f); // 2D rotation on Z axis
            leverHandle.transform.localScale = new Vector3(0.15f, 0.6f, 1f);

            SpriteRenderer leverSr = leverHandle.AddComponent<SpriteRenderer>();
            leverSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Square.png");
            leverSr.sortingOrder = 6;
            leverSr.color = Color.red; // Kırmızı kol

            // ChallengeSwitch scriptini ekle
            ChallengeSwitch chalSwitch = baseObj.AddComponent<ChallengeSwitch>();

            // Kol ve 2D rotasyon referansını bağla
            SerializedObject switchSO = new SerializedObject(chalSwitch);
            switchSO.FindProperty("leverHandle").objectReferenceValue = leverHandle.transform;
            switchSO.FindProperty("pulledRotation").vector3Value = new Vector3(0f, 0f, 25f);

            // Projeden düşman prefablerini bulup ekle
            System.Collections.Generic.List<GameObject> enemies = new System.Collections.Generic.List<GameObject>();
            string[] enemyGuids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/Enemies/Basic" });
            foreach (var guid in enemyGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (enemyPrefab != null)
                {
                    enemies.Add(enemyPrefab);
                }
            }

            SerializedProperty enemyProp = switchSO.FindProperty("enemyPrefabs");
            enemyProp.ClearArray();
            for (int i = 0; i < enemies.Count; i++)
            {
                enemyProp.InsertArrayElementAtIndex(i);
                enemyProp.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            }
            switchSO.ApplyModifiedProperties();

            // Spawn noktalarını oluştur ve bağla
            chalSwitch.EditorAutoSetup();

            Selection.activeGameObject = challengeRoomObj;
            EditorUtility.SetDirty(challengeRoomObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Challenge Odası Kurulumu", 
                "Challenge (Meydan Okuma) Odası başarıyla oluşturuldu!\n\n" +
                "1. Sahnede metalik 'ChallengeSwitch' şalter kolu oluşturuldu.\n" +
                "2. Sınırlarındaki zindan kapılarını ve spawn noktalarını otomatik bağladı.\n" +
                "3. Şalter indirildiğinde kapılar kilitlenir ve canavar dalgası başlar!", "Harika!");
        }

        private static GameObject CreateTextChild(string name, Transform parent, string textContent, int fontSize, Color color, Vector2 anchoredPos)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(650f, 80f);
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

        private static GameObject CreateButtonChild(string name, Transform parent, string label, Vector2 anchoredPos, Color btnColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 65f);
            rect.anchoredPosition = anchoredPos;

            Image img = btnObj.AddComponent<Image>();
            img.color = btnColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = btnColor;
            cb.highlightedColor = btnColor * 1.25f;
            cb.pressedColor = btnColor * 0.75f;
            btn.colors = cb;

            // İç metin
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(280f, 55f);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = 20;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.supportRichText = true;

            return btnObj;
        }
    }
}
