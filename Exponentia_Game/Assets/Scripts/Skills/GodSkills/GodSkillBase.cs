using UnityEngine;
using Exponentia.Player;

public abstract class GodSkillBase : MonoBehaviour
{
    [Header("Skill Identity")]
    [SerializeField] private string skillName = "New God Skill";
    [SerializeField] [TextArea] private string skillDescription = "";
    [SerializeField] private GodSkillType godSkillType = GodSkillType.None;
    [SerializeField] private Sprite skillIcon; // Sol altta arayüzde göstereceğimiz ikon

    [Header("Skill Progression")]
    [SerializeField] private bool isUnlocked;
    [SerializeField] private int skillLevel = 1;
    [SerializeField] private int maxSkillLevel = 5;

    [Header("Resource Cost")]
    [SerializeField] private float manaCost = 25f;
    [SerializeField] private float cooldown = 5f;

    protected PlayerMechanics owner;
    protected PlayerStats ownerStats;

    private float nextUseTime;

    public string SkillName => skillName;
    public string SkillDescription => skillDescription;
    public GodSkillType SkillType => godSkillType;
    public Sprite SkillIcon => skillIcon;
    public bool IsUnlocked => isUnlocked;
    public int SkillLevel => skillLevel;
    public float ManaCost => manaCost;
    public float Cooldown => cooldown;
    public float RemainingCooldown => Mathf.Max(0f, nextUseTime - Time.time);

    protected virtual void Reset()
    {
        CacheOwnerReferences();
    }

    protected virtual void Awake()
    {
        CacheOwnerReferences();

        // Eğer isUnlocked tikini Inspector'da kapalı unuttuysan diye garantilemek için başlangıçta otomatik açıyoruz:
        isUnlocked = true; 
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
    }

    public void SetSkillLevel(int newLevel)
    {
        skillLevel = Mathf.Clamp(newLevel, 1, Mathf.Max(1, maxSkillLevel));
    }

    public bool CanActivate()
    {
        if (!isUnlocked || owner == null || !owner.Yasiyor)
        {
            return false;
        }

        if (Time.time < nextUseTime)
        {
            return false;
        }

        return owner.MevcutMana >= manaCost;
    }

    public bool TryActivate()
    {
        if (!CanActivate())
        {
            return false;
        }

        // Mana düşme denemesi
        if (!owner.HarcaMana(manaCost))
        {
            return false;
        }

        bool activated = ActivateSkill();
        if (!activated)
        {
            owner.ManaYenile(manaCost);
            return false;
        }

        // Skill basarili kullanildi, cooldown baslat
        nextUseTime = Time.time + cooldown;
        return true;
    }

    protected virtual bool ActivateSkill()
    {
        return false;
    }

    protected void ConfigureSkillDefinition(
        string newSkillName,
        string newDescription,
        GodSkillType newSkillType,
        float newManaCost,
        float newCooldown,
        bool unlockedByDefault = false)
    {
        skillName = newSkillName;
        skillDescription = newDescription;
        godSkillType = newSkillType;
        manaCost = Mathf.Max(0f, newManaCost);
        cooldown = Mathf.Max(0f, newCooldown);
        isUnlocked = unlockedByDefault;
    }

    private void CacheOwnerReferences()
    {
        owner = GetComponent<PlayerMechanics>();
        ownerStats = GetComponent<PlayerStats>();
    }
}
