using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class SkillManager : MonoBehaviour, ISkillManager // just 1 character at the moment
{
    private static int currentSkillId;

    [SerializeField] private Skill[] allSkills;

    private Dictionary<int, Skill> allSkillsById=new Dictionary<int, Skill>();
    private HashSet<Skill> unlockedSkills=new HashSet<Skill>();
    

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }
    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            currentSkillId = 0;
        }
    }

    private void Awake()
    {
        for (int i = 0; i < allSkills.Length; i++)
        {
            Skill skill = allSkills[i];
            skill.SetId(currentSkillId);            
            allSkillsById.Add(skill.Id, skill);
            currentSkillId++;
        }
    }

    public Skill GetSkillById(int skillId)
    {
        if(allSkillsById.TryGetValue(skillId, out Skill skill))
        {
            return skill;
        }

        Debug.LogWarning($"<color=red>Skill with id:{skillId} not found!</color>");
        return null;
    }

    public Skill[] GetCharacterSkills()
    {
        return allSkills;
    }

    public HashSet<Skill> GetUnlockedSkills()
    {
        // just 1 character
        return unlockedSkills;
    }

    public void RegisterSkillUnlock(Skill skill)
    {
        unlockedSkills.Add(skill);
    }
}
