using System;
using System.Collections.Generic;
using UnityEngine;


public interface ISkillManager
{
    Skill[] GetCharacterSkills();
    Skill GetSkillById(int skillId);
    HashSet<Skill> GetUnlockedSkills();
    void RegisterSkillUnlock(Skill skill);
}
