using UnityEngine;

public class RGBBackground : MonoBehaviour
{
    [SerializeField] private float hueSpeed = 0.05f;
    [SerializeField] private float saturationSpeed = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Camera targetCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            targetCamera = Camera.main;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float hue = Mathf.Repeat(Time.time * hueSpeed, 1f);
        float saturation = Mathf.PingPong(Time.time * saturationSpeed, 1f);
        Color current = Color.HSVToRGB(hue, saturation, 1f);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = current;
        }
        else if (targetCamera != null)
        {
            targetCamera.backgroundColor = current;
        }
    }
}
