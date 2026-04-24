using UnityEngine;

public class EnemyMechanics : MonoBehaviour, IDamageable
{
    [Header("Can")]
    [SerializeField] private float maxCan = 50f;
    [SerializeField] private float mevcutCan;

    [Header("Can Yazisi")]
    [SerializeField] private Vector3 yaziOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private int yaziFontBoyutu = 6;
    [SerializeField] private Color yaziRengi = Color.white;

    private TextMesh canTextMesh;

    private void Awake()
    {
        if (!CompareTag("Enemy"))
        {
            gameObject.tag = "Enemy";
        }

        maxCan = Mathf.Max(1f, maxCan);
        mevcutCan = maxCan;

        CreateHealthText();
        UpdateHealthText();
    }

    private void LateUpdate()
    {
        if (canTextMesh == null)
        {
            return;
        }

        canTextMesh.transform.position = transform.position + yaziOffset;

        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            canTextMesh.transform.rotation = activeCamera.transform.rotation;
        }
    }

    public float TakeDamage(float amount)
    {
        if (amount <= 0f || mevcutCan <= 0f)
        {
            return 0f;
        }

        float uygulananHasar = Mathf.Min(mevcutCan, amount);
        mevcutCan -= uygulananHasar;
        UpdateHealthText();

        if (mevcutCan <= 0f)
        {
            Die();
        }

        return uygulananHasar;
    }

    private void Die()
    {
        if (canTextMesh != null)
        {
            Destroy(canTextMesh.gameObject);
        }

        Destroy(gameObject);
    }

    private void CreateHealthText()
    {
        GameObject textObject = new GameObject("EnemyHealthText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = yaziOffset;

        canTextMesh = textObject.AddComponent<TextMesh>();
        canTextMesh.anchor = TextAnchor.MiddleCenter;
        canTextMesh.alignment = TextAlignment.Center;
        canTextMesh.fontSize = yaziFontBoyutu;
        canTextMesh.characterSize = 0.15f;
        canTextMesh.color = yaziRengi;
    }

    private void UpdateHealthText()
    {
        if (canTextMesh == null)
        {
            return;
        }

        canTextMesh.text = Mathf.CeilToInt(mevcutCan).ToString();
    }
}
