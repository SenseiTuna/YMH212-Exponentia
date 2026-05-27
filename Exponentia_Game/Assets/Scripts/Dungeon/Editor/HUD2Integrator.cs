using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Exponentia.UI;

namespace Exponentia.Editor
{
    public static class HUD2Integrator
    {
        [MenuItem("Exponentia/HUD2/Setup Gameplay HUD")]
        public static void SetupGameplayHUD()
        {
            // 1. Sahnedeki aktif Canvas'ı bul veya sıfırdan oluştur
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Canvas existingCanvas = Object.FindFirstObjectByType<Canvas>();
                if (existingCanvas != null)
                {
                    canvasObj = existingCanvas.gameObject;
                }
            }

            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // Sahnede EventSystem varlığını kontrol et
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // 2. Can barının hemen altına hizalanmış (Anchored to Top-Left) bir ana panel oluştur veya mevcut olanı koru
            HUD2Controller existingController = Object.FindFirstObjectByType<HUD2Controller>();
            GameObject hud2PanelObj = GameObject.Find("HUD2Panel");

            if (existingController != null)
            {
                existingController.AutoBindUIComponents();
                EditorUtility.SetDirty(existingController.gameObject);
                
                EditorUtility.DisplayDialog("HUD2 Arayüzü Başarıyla Bağlandı!", 
                    "Sahnede mevcut olan kalkan/para göstergesi ('HUD2Controller') başarıyla algılandı! Özel arayüz tasarımınız tamamen korunarak kalkan ikonları ve altın metinleri otomatik olarak bağlandı.", "Harika!");
                return;
            }
            else if (hud2PanelObj != null)
            {
                existingController = hud2PanelObj.GetComponent<HUD2Controller>();
                if (existingController == null)
                {
                    existingController = hud2PanelObj.AddComponent<HUD2Controller>();
                }
                
                existingController.AutoBindUIComponents();
                EditorUtility.SetDirty(hud2PanelObj);
                
                EditorUtility.DisplayDialog("HUD2 Arayüzü Başarıyla Bağlandı!", 
                    "Sahnede mevcut olan 'HUD2Panel' başarıyla tespit edildi! Özel tasarımınız korunarak kalkan ikonları ve altın metinleri sisteme otomatik olarak bağlandı.", "Harika!");
                return;
            }

            hud2PanelObj = new GameObject("HUD2Panel");
            hud2PanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform mainRect = hud2PanelObj.AddComponent<RectTransform>();
            
            // Anchor: Sol-Üst, Can barının altına yerleşim için
            mainRect.anchorMin = new Vector2(0f, 1f);
            mainRect.anchorMax = new Vector2(0f, 1f);
            mainRect.pivot = new Vector2(0f, 1f);
            mainRect.sizeDelta = new Vector2(250f, 110f);
            mainRect.anchoredPosition = new Vector2(30f, -120f); // Can barı ~50px yüksekliğindeyse altına sığar

            // Premium Glassmorphic Background
            Image mainBg = hud2PanelObj.AddComponent<Image>();
            mainBg.color = new Color(0.06f, 0.06f, 0.08f, 0.8f);

            // İnce Kenarlık (Mavi Çelik)
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(hud2PanelObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(2f, 2f);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(0.2f, 0.4f, 0.7f, 0.35f);

            // Dikey Düzen (Vertical Layout Group)
            VerticalLayoutGroup mainVLayout = hud2PanelObj.AddComponent<VerticalLayoutGroup>();
            mainVLayout.padding = new RectOffset(12, 12, 10, 10);
            mainVLayout.spacing = 10f;
            mainVLayout.childAlignment = TextAnchor.MiddleLeft;
            mainVLayout.childControlHeight = false;
            mainVLayout.childControlWidth = false;
            mainVLayout.childForceExpandHeight = false;
            mainVLayout.childForceExpandWidth = false;

            // 3. KALKAN PANELİ (Shield Panel - Yan yana kalkanlar)
            GameObject shieldPanelObj = new GameObject("ShieldPanel");
            shieldPanelObj.transform.SetParent(hud2PanelObj.transform, false);
            RectTransform shieldPanelRect = shieldPanelObj.AddComponent<RectTransform>();
            shieldPanelRect.sizeDelta = new Vector2(220f, 32f);

            HorizontalLayoutGroup shieldHLayout = shieldPanelObj.AddComponent<HorizontalLayoutGroup>();
            shieldHLayout.spacing = 8f;
            shieldHLayout.childAlignment = TextAnchor.MiddleLeft;
            shieldHLayout.childControlHeight = false;
            shieldHLayout.childControlWidth = false;
            shieldHLayout.childForceExpandHeight = false;
            shieldHLayout.childForceExpandWidth = false;

            // 3 Adet Kalkan İkonu
            Image[] shieldImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject shieldIconObj = new GameObject($"ShieldIcon_{i}");
                shieldIconObj.transform.SetParent(shieldPanelObj.transform, false);
                RectTransform sIconRect = shieldIconObj.AddComponent<RectTransform>();
                sIconRect.sizeDelta = new Vector2(32f, 32f);

                Image sImg = shieldIconObj.AddComponent<Image>();
                sImg.color = new Color(0.2f, 0.85f, 1f, 1f);
                sImg.type = Image.Type.Simple;

                shieldImages[i] = sImg;
            }

            // 4. PARA PANELİ (Currency Panel - Yan yana İkon ve Text)
            GameObject currencyPanelObj = new GameObject("CurrencyPanel");
            currencyPanelObj.transform.SetParent(hud2PanelObj.transform, false);
            RectTransform currencyPanelRect = currencyPanelObj.AddComponent<RectTransform>();
            currencyPanelRect.sizeDelta = new Vector2(220f, 26f);

            HorizontalLayoutGroup currencyHLayout = currencyPanelObj.AddComponent<HorizontalLayoutGroup>();
            currencyHLayout.spacing = 8f;
            currencyHLayout.childAlignment = TextAnchor.MiddleLeft;
            currencyHLayout.childControlHeight = false;
            currencyHLayout.childControlWidth = false;
            currencyHLayout.childForceExpandHeight = false;
            currencyHLayout.childForceExpandWidth = false;

            // Para İkon Alanı
            GameObject goldIconObj = new GameObject("GoldIcon");
            goldIconObj.transform.SetParent(currencyPanelObj.transform, false);
            RectTransform goldIconRect = goldIconObj.AddComponent<RectTransform>();
            goldIconRect.sizeDelta = new Vector2(24f, 24f);
            Image goldIconImg = goldIconObj.AddComponent<Image>();
            goldIconImg.color = new Color(1f, 0.85f, 0.2f, 0.9f); // Altın sarısı ikon rengi

            // Para Göstergesi Yazısı
            GameObject goldTextObj = new GameObject("GoldText");
            goldTextObj.transform.SetParent(currencyPanelObj.transform, false);
            RectTransform goldTextRect = goldTextObj.AddComponent<RectTransform>();
            goldTextRect.sizeDelta = new Vector2(100f, 26f);

            TextMeshProUGUI goldText = goldTextObj.AddComponent<TextMeshProUGUI>();
            goldText.text = "000";
            goldText.fontSize = 18;
            goldText.fontStyle = FontStyles.Bold;
            goldText.color = new Color(1f, 0.85f, 0.2f, 1f); // Altın sarısı metin
            goldText.alignment = TextAlignmentOptions.MidlineLeft;

            // 5. Script enjeksiyonu ve referans bağlama
            HUD2Controller controller = hud2PanelObj.AddComponent<HUD2Controller>();
            controller.shieldIcons = shieldImages;
            controller.currencyText = goldText;

            // Değişiklikleri kaydet ve geri alabilmek için sahnede kaydet
            Undo.RegisterCreatedObjectUndo(hud2PanelObj, "Setup Gameplay HUD");
            EditorUtility.SetDirty(hud2PanelObj);
            
            // Editör uyarısı göster
            EditorUtility.DisplayDialog("HUD2 Kurulumu Başarılı!", 
                "Health Bar altına dinamik Kalkan Göstergesi ve Para Sayacı (HUD2) başarıyla yerleştirildi ve tüm referanslar kodla otomatik bağlandı!", "Harika!");
        }
    }
}
