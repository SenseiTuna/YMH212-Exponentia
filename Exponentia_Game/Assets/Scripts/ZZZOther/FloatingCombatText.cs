using UnityEngine;

public class FloatingCombatText : MonoBehaviour
{
    [SerializeField] private float yasamSuresi = 0.8f;
    [SerializeField] private float yukselmeHizi = 1.5f;
    [SerializeField] private float yataySacilma = 0.35f;
    [SerializeField] private int sortingOrder = 30;
    [SerializeField] private int fontBoyutu = 24;
    [SerializeField] private float karakterBoyutu = 0.22f;

    private TextMesh textMesh;
    private Color baseColor;
    private float elapsedTime;
    private Vector3 moveDirection;

    public static void Create(string text, Vector3 worldPosition, Color color)
    {
        GameObject textObject = new GameObject("FloatingCombatText");
        textObject.transform.position = worldPosition;

        FloatingCombatText floatingText = textObject.AddComponent<FloatingCombatText>();
        floatingText.Initialize(text, color);
    }

    private void Initialize(string text, Color color)
    {
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = fontBoyutu;
        textMesh.characterSize = karakterBoyutu;
        textMesh.color = color;

        MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
        meshRenderer.sortingOrder = sortingOrder;

        baseColor = color;
        float randomX = Random.Range(-yataySacilma, yataySacilma);
        moveDirection = new Vector3(randomX, 1f, 0f).normalized;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        transform.position += moveDirection * yukselmeHizi * Time.deltaTime;

        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            transform.rotation = activeCamera.transform.rotation;
        }

        if (textMesh != null)
        {
            float alpha = Mathf.Clamp01(1f - (elapsedTime / yasamSuresi));
            textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        if (elapsedTime >= yasamSuresi)
        {
            Destroy(gameObject);
        }
    }
}
