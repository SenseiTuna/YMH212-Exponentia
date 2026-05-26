using UnityEngine;

namespace Exponentia.UI
{
    public class MinimapFollow : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float zOffset = -10f; // 2D oyunda kameranın Z derinliği (genellikle -10)
        [SerializeField] private bool autoFindPlayer = true;

        private void LateUpdate()
        {
            // Eğer oyuncu referansı yoksa ve otomatik bulma aktifse, dinamik olarak bulmaya çalışıyoruz.
            // Bu sayede karakter seçildikten sonra spawner oyuncuyu klonladığı an kamera onu algılar.
            if (playerTransform == null && autoFindPlayer)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }

            if (playerTransform != null)
            {
                // Kamerayı X ve Y ekseninde oyuncunun üstüne taşır, Z eksenindeki mesafeyi korur
                transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, zOffset);
            }
        }
    }
}
