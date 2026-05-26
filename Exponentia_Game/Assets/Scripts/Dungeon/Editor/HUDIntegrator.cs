using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exponentia.Editor
{
    public static class HUDIntegrator
    {
        [MenuItem("Exponentia/HUD/Integrate Esma HUD")]
        public static void IntegrateHUD()
        {
            string esmaScenePath = "Assets/Scenes/TestScenes/EsmaSampleScene.unity";
            
            // 1. EsmaScene'in varlığını kontrol et
            if (AssetDatabase.LoadMainAssetAtPath(esmaScenePath) == null)
            {
                EditorUtility.DisplayDialog("Hata", "EsmaSampleScene.unity bulunamadı. Lütfen geçici sahne dosyasının kopyalandığından emin olun.", "Tamam");
                return;
            }

            // 2. Mevcut aktif sahneyi al
            Scene activeScene = SceneManager.GetActiveScene();

            // 3. Mevcut sahnede çakışabilecek eski HUD elemanlarını temizle (Canvas, EventSystem, MinimapCamera)
            GameObject oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null)
            {
                Undo.DestroyObjectImmediate(oldCanvas);
            }
            
            GameObject oldEventSystem = GameObject.Find("EventSystem");
            if (oldEventSystem != null)
            {
                Undo.DestroyObjectImmediate(oldEventSystem);
            }

            GameObject oldMinimapCam = GameObject.Find("MinimapCamera");
            if (oldMinimapCam != null)
            {
                Undo.DestroyObjectImmediate(oldMinimapCam);
            }

            // 4. Esma'nın sahnesini katkısal (additive) olarak yükle
            Scene esmaScene = EditorSceneManager.OpenScene(esmaScenePath, OpenSceneMode.Additive);
            if (!esmaScene.IsValid())
            {
                EditorUtility.DisplayDialog("Hata", "Esma'nın sahnesi yüklenemedi.", "Tamam");
                return;
            }

            // 5. Esma'nın sahnesindeki kök objeleri tara ve aktif sahneye taşı
            GameObject[] rootObjects = esmaScene.GetRootGameObjects();
            int movedCount = 0;

            foreach (var obj in rootObjects)
            {
                if (obj.name == "Canvas" || obj.name == "EventSystem" || obj.name == "MinimapCamera")
                {
                    // Objeyi aktif sahneye taşı
                    Undo.MoveGameObjectToScene(obj, activeScene, "Move HUD Object");
                    movedCount++;
                }
            }

            // 6. Katkısal sahneyi kapat/kaldır (Kalan boş sahneyi temizle)
            EditorSceneManager.CloseScene(esmaScene, true);

            // 7. Esma'nın geçici sahne dosyasını diskten sil
            AssetDatabase.DeleteAsset(esmaScenePath);
            AssetDatabase.Refresh();

            // 8. Sahneyi kirli (dirty) olarak işaretle ki değişiklikler kaydedilebilsin
            EditorSceneManager.MarkSceneDirty(activeScene);

            EditorUtility.DisplayDialog("HUD Entegrasyonu Başarılı!", 
                $"ESMA'nın sahnesindeki Canvas, EventSystem ve MinimapCamera başarıyla senin aktif test sahnen üzerine taşındı!\n\n" +
                $"Sahnede daha önce oluşturduğun testler, odalar ve kaideler aynen korundu.", "Harika!");
        }
    }
}
