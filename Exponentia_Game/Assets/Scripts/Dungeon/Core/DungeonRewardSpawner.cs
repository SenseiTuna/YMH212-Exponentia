/*
====================================================
PROJ_NAME  : Exponentia Game
PROJ_ID    : Dungeon
VERSION    : 1.0.0
FILE       : DungeonRewardSpawner.cs
BUILD_DATE : 2026-05-25
====================================================
*/

using System.Collections.Generic;
using UnityEngine;
using Exponentia.Data;
using Exponentia.Interaction;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Exponentia.Dungeon
{
    [DisallowMultipleComponent]
    public class DungeonRewardSpawner : MonoBehaviour
    {
        [Header("Prefab Configuration")]
        [Tooltip("Etkileşime girilebilir ödül kutusu/küresi prefabi (PhysicalUpgradeChoice taşımalıdır).")]
        [SerializeField] private GameObject choicePrefab;

        [Header("Upgrade Database")]
        [Tooltip("Oyuncuya sunulabilecek tüm kalıcı güçlendirme veri dosyaları.")]
        [SerializeField] private List<UpgradeData> allAvailableUpgrades = new List<UpgradeData>();

        [Header("Spawn Settings")]
        [Tooltip("Ödüllerin yan yana dururken aralarındaki mesafe (Unity Birimi).")]
        [SerializeField] private float spacing = 1.4f;
        [Tooltip("Yerden ne kadar yukarıda spawn olacakları (Hades tarzı havadan süzülerek düşüş için).")]
        [SerializeField] private float spawnHeightOffset = 3.0f;

        private void Awake()
        {
            SelfHeal();
        }

        private void SelfHeal()
        {
#if UNITY_EDITOR
            // 1. Editor'deysek eksik prefab ve verileri projeden bulmaya çalışalım
            if (choicePrefab == null)
            {
                string[] guids = AssetDatabase.FindAssets("UpgradeChoice t:GameObject");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    choicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (choicePrefab != null)
                    {
                        Debug.Log($"[RewardSpawner] Auto-resolved choicePrefab from project: '{choicePrefab.name}'");
                    }
                }
            }

            if (allAvailableUpgrades == null || allAvailableUpgrades.Count == 0)
            {
                string[] guids = AssetDatabase.FindAssets("t:UpgradeData");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    UpgradeData data = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
                    if (data != null && !allAvailableUpgrades.Contains(data))
                    {
                        allAvailableUpgrades.Add(data);
                    }
                }
                if (allAvailableUpgrades.Count > 0)
                {
                    Debug.Log($"[RewardSpawner] Auto-resolved {allAvailableUpgrades.Count} UpgradeData assets from project.");
                }
            }
#endif

            // 2. Hala veri havuzu boşsa, RAM'de geçici güçlendirme verileri oluştur
            if (allAvailableUpgrades == null || allAvailableUpgrades.Count == 0)
            {
                allAvailableUpgrades = new List<UpgradeData>();

                UpgradeData hpData = ScriptableObject.CreateInstance<UpgradeData>();
                hpData.upgradeId = "hp_up_temp";
                hpData.displayName = "Kutsal Can (Geçici)";
                hpData.description = "Maksimum caninizi kalici olarak 25 artirir.";
                hpData.maxHealthBonus = 25f;

                UpgradeData dmgData = ScriptableObject.CreateInstance<UpgradeData>();
                dmgData.upgradeId = "dmg_up_temp";
                dmgData.displayName = "Dev Gücü (Geçici)";
                dmgData.description = "Saldiri hasarinizi kalici olarak 5 artirir.";
                dmgData.damageBonus = 5f;

                UpgradeData speedData = ScriptableObject.CreateInstance<UpgradeData>();
                speedData.upgradeId = "speed_up_temp";
                speedData.displayName = "Rüzgar Çizmeleri (Geçici)";
                speedData.description = "Hareket hizinizi kalici olarak 1.5 artirir.";
                speedData.moveSpeedBonus = 1.5f;

                allAvailableUpgrades.Add(hpData);
                allAvailableUpgrades.Add(dmgData);
                allAvailableUpgrades.Add(speedData);

                Debug.Log("[RewardSpawner] Projede UpgradeData bulunamadı, RAM'de 3 adet geçici ödül verisi oluşturuldu.");
            }

            // 3. Hala prefab yoksa, RAM'de dinamik bir fallback prefab taslağı oluştur
            if (choicePrefab == null)
            {
                GameObject tempChoice = new GameObject("UpgradeChoice_Fallback_Prefab");
                tempChoice.SetActive(false);
                tempChoice.transform.SetParent(transform);

                BoxCollider2D col = tempChoice.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.8f, 0.8f);

                tempChoice.AddComponent<PhysicalUpgradeChoice>();

                GameObject visualObj = new GameObject("Visual");
                visualObj.transform.SetParent(tempChoice.transform, false);
                SpriteRenderer sr = visualObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 16;

                // 2x2 sarı renkli geçici doku
                Texture2D tex = new Texture2D(2, 2);
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        tex.SetPixel(x, y, Color.yellow);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);

                choicePrefab = tempChoice;
                Debug.Log("[RewardSpawner] Projede UpgradeChoice prefabi bulunamadı, RAM'de geçici bir görsel prefab oluşturuldu.");
            }
        }

        /// <summary>
        /// Belirtilen dünya pozisyonunda yan yana 3 adet rastgele kalıcı ödül seçeneği oluşturur.
        /// </summary>
        public void SpawnRewardChoices(Vector3 spawnPosition)
        {
            if (choicePrefab == null)
            {
                Debug.LogError("[RewardSpawner] choicePrefab atanmamış! Ödül doğurulamıyor.");
                return;
            }

            if (allAvailableUpgrades == null || allAvailableUpgrades.Count == 0)
            {
                Debug.LogError("[RewardSpawner] allAvailableUpgrades veri havuzu boş! Ödül seçeneği doğurulamıyor.");
                return;
            }

            // 1. Ödülleri koordine edecek olan boş bir Grup Yöneticisi oluştur
            GameObject groupObj = new GameObject("PhysicalUpgradeChoiceGroup");
            groupObj.transform.position = spawnPosition;
            PhysicalChoiceGroup choiceGroup = groupObj.AddComponent<PhysicalChoiceGroup>();

            // 2. Havuzdan birbirinden farklı 3 adet rastgele ödül seç
            List<UpgradeData> pickedUpgrades = PickThreeRandomUpgrades();

            // 3. Seçilen ödülleri yan yana spawn et
            for (int i = 0; i < pickedUpgrades.Count; i++)
            {
                // Sol, merkez ve sağ hizalama hesaplama (i = 0 ise sol, 1 ise orta, 2 ise sağ)
                float xOffset = (i - 1) * spacing;
                Vector3 targetFloorPos = spawnPosition + new Vector3(xOffset, 0f, 0f);
                
                // Havadan süzülerek düşme efekti için başlangıç yüksekliği ekle
                Vector3 spawnAirPos = targetFloorPos + new Vector3(0f, spawnHeightOffset, 0f);

                GameObject choiceObj = Instantiate(choicePrefab, spawnAirPos, Quaternion.identity, groupObj.transform);
                choiceObj.name = $"UpgradeChoice_{pickedUpgrades[i].upgradeId}";
                choiceObj.SetActive(true); // Kesinlikle aktif et

                PhysicalUpgradeChoice choiceComponent = choiceObj.GetComponent<PhysicalUpgradeChoice>();
                if (choiceComponent == null)
                {
                    choiceComponent = choiceObj.GetComponentInChildren<PhysicalUpgradeChoice>();
                }

                if (choiceComponent != null)
                {
                    choiceComponent.Initialize(pickedUpgrades[i], choiceGroup);
                    
                    // Fiziksel süzülerek yere inme (drop/juice) animasyonunu tetikle
                    StartCoroutine(AnimateDropToFloor(choiceObj.transform, targetFloorPos, 0.6f + (i * 0.1f), choiceComponent));
                }
                else
                {
                    Debug.LogWarning($"[RewardSpawner] Doğurulan '{choiceObj.name}' objesinde 'PhysicalUpgradeChoice' bileşeni bulunamadı!");
                }
            }

            Debug.Log($"[RewardSpawner] {pickedUpgrades.Count} adet fiziksel ödül seçimi başarıyla doğuruldu!");
        }

        /// <summary>
        /// Veri havuzundan birbirinden farklı 3 adet rastgele UpgradeData seçer.
        /// </summary>
        private List<UpgradeData> PickThreeRandomUpgrades()
        {
            List<UpgradeData> result = new List<UpgradeData>();
            List<UpgradeData> tempPool = new List<UpgradeData>(allAvailableUpgrades);

            // Havuzda en az 3 adet ödül olmalıdır, yoksa tekrarlı seçime izin veririz
            int countToPick = Mathf.Min(3, allAvailableUpgrades.Count);
            if (countToPick <= 0) return result;

            for (int i = 0; i < 3; i++)
            {
                if (tempPool.Count == 0)
                {
                    // Havuz bittiyse orijinal havuzdan tekrar ekleyip kopyalara izin ver
                    tempPool = new List<UpgradeData>(allAvailableUpgrades);
                }

                int randomIndex = Random.Range(0, tempPool.Count);
                result.Add(tempPool[randomIndex]);
                tempPool.RemoveAt(randomIndex); // Aynı seçim turunda aynı ödülün tekrar çıkmasını engelle
            }

            return result;
        }

        /// <summary>
        /// Ödül nesnesini havadan yere yumuşakça düşürür (Elastic bounce effect).
        /// </summary>
        private System.Collections.IEnumerator AnimateDropToFloor(Transform target, Vector3 floorPos, float duration, PhysicalUpgradeChoice choiceComponent)
        {
            float elapsed = 0f;
            Vector3 startPos = target.position;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.deltaTime;
                float percent = Mathf.Clamp01(elapsed / duration);
                
                // Zıplama eğrisi (Elastic ease out)
                float t = Mathf.Sin(percent * Mathf.PI * 0.5f);
                target.position = Vector3.Lerp(startPos, floorPos, t);
                yield return null;
            }

            if (target != null)
            {
                target.position = floorPos;
                if (choiceComponent != null)
                {
                    choiceComponent.StartHovering(floorPos);
                }
            }
        }
    }
}
