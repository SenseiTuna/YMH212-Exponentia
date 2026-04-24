using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Temel Statlar")]
    public float can = 100f;
    public float hareketHizi = 5f;
    public float savunma = 0f;
    public float hasar = 10f;
    public float saldiriHizi = 1f;
    public float projectileHizi = 14f;
    public float canCalma = 0f;
    public float kalkan = 0f;
    public float mana = 100f;

    [Header("Gelisim")]
    public int level = 1;
    public float xp = 0f;
    public float sonrakiLevelXp = 100f;
    public float levelXpCarpani = 1.35f;

    [Header("Level Up Kazanimlari")]
    public float levelBasinaCanArtisi = 15f;
    public float levelBasinaHasarArtisi = 3f;
    public float levelBasinaManaArtisi = 10f;
    public float levelBasinaSavunmaArtisi = 1f;
    public float levelBasinaKalkanArtisi = 2f;
}
