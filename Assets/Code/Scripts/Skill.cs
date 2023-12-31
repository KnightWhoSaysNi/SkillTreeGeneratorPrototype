using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using Unity.Collections;

[CreateAssetMenu(fileName = "Skill", menuName = "Skill")]
public class Skill : ScriptableObject
{
    public int Id { get; private set; }
    [field: SerializeField] public bool IsRootSkill { get; private set; }
    [field: SerializeField, TextArea] public string Description { get; private set; } = "Awesome skill" ;
    [field: SerializeField] public SkillType Type {get; private set; }
    [field: SerializeField] public int Cost { get; private set; } = 1;
    [field: SerializeField] public Skill[] LinkedSkills { get; private set; }
    
    public void SetId(int id)
    {
        Id = id;    
    }    
}

