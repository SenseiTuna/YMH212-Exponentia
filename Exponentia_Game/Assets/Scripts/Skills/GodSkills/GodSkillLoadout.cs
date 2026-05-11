using System.Collections.Generic;
using UnityEngine;

public class GodSkillLoadout : MonoBehaviour
{
    [SerializeField] private List<GodSkillBase> equippedSkills = new List<GodSkillBase>();

    public IReadOnlyList<GodSkillBase> EquippedSkills => equippedSkills;

    private void Reset()
    {
        RefreshSkillsFromObject();
    }

    private void Awake()
    {
        if (equippedSkills.Count == 0)
        {
            RefreshSkillsFromObject();
        }
    }

    public GodSkillBase GetSkill(GodSkillType skillType)
    {
        for (int i = 0; i < equippedSkills.Count; i++)
        {
            GodSkillBase skill = equippedSkills[i];
            if (skill != null && skill.SkillType == skillType)
            {
                return skill;
            }
        }

        return null;
    }

    public void RefreshSkillsFromObject()
    {
        equippedSkills.Clear();
        GetComponents(equippedSkills);
    }
}
