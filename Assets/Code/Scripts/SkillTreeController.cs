using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Zenject;
using Random = UnityEngine.Random;

public class SkillTreeController : MonoBehaviour
{
    public bool shouldUseSeed = true;
    public int seed = 3;
    public SkillTreeHelper helper;
    [Space(20)]
    [Inject]
    private ISkillManager skillManager;
    [SerializeField] private SkillButton skillButtonPrefab;
    [SerializeField] private UILineRenderer linkPrefab;

    [Space(20)]
    public TMP_InputField currentSeedText;
    public TMP_InputField seedInput;

    [Header("UI")]
    [SerializeField] private Transform skillButtonParent;
    [SerializeField] private Transform linkParent;
    [Space(10)]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button unlockButton;
    [Space(20)]
    [SerializeField] private Sprite fullLinkSprite;
    [SerializeField] private float fullLinkLineThickness;
    [Header("Button Colors")]
    [SerializeField] private Color air;
    [SerializeField] private Color water;
    [SerializeField] private Color earth;
    [SerializeField] private Color fire;
    [SerializeField] private Color unlockedAir;
    [SerializeField] private Color unlockedWater;
    [SerializeField] private Color unlockedEarth;
    [SerializeField] private Color unlockedFire;

    private SkillButton activeSkillButton;
    private int activeSkillId;
    private Dictionary<int, SkillButton> allSkillButtonsWithIds = new Dictionary<int, SkillButton>();
    private Dictionary<SkillButton, List<SkillButton>> allLinkedButtons = new Dictionary<SkillButton, List<SkillButton>>();
    private Dictionary<(SkillButton, SkillButton), UILineRenderer> buttonLinks = new Dictionary<(SkillButton, SkillButton), UILineRenderer>();


    private void OnEnable()
    {
        SkillButton.Activated += OnSkillButtonActivated;
        unlockButton.onClick.AddListener(OnUnlock);
    }
    private void OnDisable()
    {
        SkillButton.Activated -= OnSkillButtonActivated;
        unlockButton.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        if (shouldUseSeed)
        {
            Random.InitState(seed);
        }
        else
        {
            seed = Random.Range(-1000000, 1000000);
            Random.InitState(seed);
        }
        currentSeedText.text = seed.ToString();
        GenerateSkillTree();
    }

    public void UseNewSeed()
    {
        if(int.TryParse(seedInput.text, out int newSeed))
        {
            ClearTheSkillTree();
            seed = newSeed;
            Random.InitState(seed);
            currentSeedText.text = newSeed.ToString();
            GenerateSkillTree();
        }
    }
    public void GenerateRandomSeed()
    {
        ClearTheSkillTree();
        seed = Random.Range(-1000000, 1000000);
        currentSeedText.text = seed.ToString();
        Random.InitState(seed);
        GenerateSkillTree();
    }

    private void OnSkillButtonActivated(SkillButton button)
    {
        if (activeSkillButton == button)
        {
            return;
        }

        if (activeSkillButton != null)
        {
            activeSkillButton.SetScale(false);
        }

        activeSkillButton = button;
        activeSkillId = GetActiveSkillId();
        activeSkillButton.SetScale(true);

        Skill skill = skillManager.GetSkillById(activeSkillId);

        descriptionText.text = skill.Description + "<br>Unlocks:<br>";

        foreach (var linkedSkill in skill.LinkedSkills)
        {
            descriptionText.text += linkedSkill.Id.ToString() + "  ";
        }
        costText.text = "Cost: " + skill.Cost.ToString();
    }
    private int GetActiveSkillId()
    {
        foreach (var entry in allSkillButtonsWithIds)
        {
            if (entry.Value == activeSkillButton)
            {
                return entry.Key;
            }
        }
        return 0;
    }

    private void GenerateSkillTree()
    {
        var skills = skillManager.GetCharacterSkills();
        HashSet<SkillButton> rootButtons = new HashSet<SkillButton>();

        foreach (Skill skill in skills)
        {
            SkillButton skillButton = ObjectPool_GetSkillButton();
            switch (skill.Type)
            {
                case SkillType.AirBending:
                    skillButton.SetColor(air); 
                    break;
                case SkillType.WaterBending:
                    skillButton.SetColor(water);
                    break;
                case SkillType.EarthBending:
                    skillButton.SetColor(earth);
                    break;
                case SkillType.FireBending:
                    skillButton.SetColor(fire);
                    break;
                case SkillType.BloodBending:
                    break;
                case SkillType.MetalBending:
                    break;
                default:
                    break;
            }
            skillButton.name = skill.Id.ToString();
            skillButton.transform.SetParent(skillButtonParent, false);
            skillButton.transform.localScale = Vector3.one;
            skillButton.transform.localPosition = new Vector3(Random.Range(-400f, 400f), Random.Range(-400f, 400f), 0);

            skillButton.SetText(skill.Id.ToString());
            skillButton.SetInteractionState(skill.IsRootSkill);

            allSkillButtonsWithIds.Add(skill.Id, skillButton);

            if (skill.IsRootSkill)
            {
                rootButtons.Add(skillButton);
                skillButton.transform.localPosition = (new Vector3(Random.Range(-1f,1f),Random.Range(-1f,1f), 0).normalized) * Random.Range(100f,150);
            }
        }
        FindAllLinkedButtons();
        helper.RegisterButtons(seed, shouldUseSeed, rootButtons, allLinkedButtons);
        GenerateLinks();
    }

    private void FindAllLinkedButtons()
    {
        List<Skill> linkedSkills = new List<Skill>();

        foreach (var entry in allSkillButtonsWithIds)
        {
            int originId = entry.Key;
            Skill originSkill = skillManager.GetSkillById(originId);
            SkillButton originSkillButton = entry.Value;
            allLinkedButtons.Add(originSkillButton, new List<SkillButton>());

            linkedSkills.Clear();
            if (originSkill.LinkedSkills?.Length > 0)
            {
                linkedSkills.AddRange(originSkill.LinkedSkills);
            }

            foreach (var linkedSkill in linkedSkills)
            {
                SkillButton targetSkillButton = allSkillButtonsWithIds[linkedSkill.Id];
                allLinkedButtons[originSkillButton].Add(targetSkillButton);
            }
        }
    }

    public void GenerateLinks()
    {
        foreach (var entry in allLinkedButtons)
        {
            foreach (var linkedButton in entry.Value)
            {
                SetLink(entry.Key, linkedButton);
            }
        }
    }
    private void SetLink(SkillButton origin, SkillButton target)
    {
        var tangents = BezierHelper.GetTangentsForBezier(origin.transform.localPosition, target.transform.localPosition);
        foreach (var entry in buttonLinks)
        {
            bool isLinkEstablished = false;
            UILineRenderer linky = null;

            if (entry.Key.Item1 == origin && entry.Key.Item2 == target)
            {
                // Link already established
               linky = buttonLinks[entry.Key];
                linky.Points[0] = origin.transform.localPosition;
                linky.Points[3] = target.transform.localPosition;
                isLinkEstablished = true;                
            }
            else if(entry.Key.Item2 == origin && entry.Key.Item1 == target)
            {
                linky = buttonLinks[(target, origin)];
                tangents = BezierHelper.GetTangentsForBezier(target.transform.localPosition, origin.transform.localPosition);
                linky.Points[0] = target.transform.localPosition;
                linky.Points[3] = origin.transform.localPosition;
                isLinkEstablished = true;
            }

            if (isLinkEstablished)
            {
                linky.Points[1] = tangents.Item1;
                linky.Points[2] = tangents.Item2;
                linky.SetAllDirty();
                return;
            }
        }

        // Initialize new link
        UILineRenderer link = ObjectPool_GetButtonLink();
        link.name = $"{origin}->{target}";
        link.Points[0] = origin.transform.localPosition;
        link.Points[1] = tangents.Item1;
        link.Points[2] = tangents.Item2;
        link.Points[3] = target.transform.localPosition;

        buttonLinks.Add((origin, target), link);
    }

    private SkillButton ObjectPool_GetSkillButton()
    {
        SkillButton skillButton = Instantiate(skillButtonPrefab);

        return skillButton;
    }
    private UILineRenderer ObjectPool_GetButtonLink()
    {
        UILineRenderer link = Instantiate(linkPrefab, linkParent, false);
        link.transform.localScale = Vector3.one;
        return link;
    }

    private void OnUnlock() // only visuals
    {
        if (activeSkillButton == null)
        {
            return;
        }

        Skill skill = skillManager.GetSkillById(activeSkillId);
        skillManager.RegisterSkillUnlock(skill); // unlock is free. no validation

        UnlockFullLink();

        var dependencies = skill.LinkedSkills;
        foreach (var subsequentSkill in dependencies)
        {
            int id = subsequentSkill.Id;
            SkillButton button = allSkillButtonsWithIds[id];

            button.SetInteractionState(true);
        }
    }
    private void UnlockFullLink()
    {
        Skill activeSkill = skillManager.GetSkillById(activeSkillId);
        List<SkillButton> originButtons = new List<SkillButton>();
        Skill targetSkill = skillManager.GetSkillById(activeSkillId);
        Color color= Color.white;

        switch (activeSkill.Type)
        {
            case SkillType.AirBending:
                activeSkillButton.SetColor(unlockedAir);
                color = unlockedAir;
                break;
            case SkillType.WaterBending:
                activeSkillButton.SetColor(unlockedWater);
                color = unlockedWater;
                break;
            case SkillType.EarthBending:
                activeSkillButton.SetColor(unlockedEarth);
                color = unlockedEarth;
                break;
            case SkillType.FireBending:
                activeSkillButton.SetColor(unlockedFire);
                color = unlockedFire;
                break;
            case SkillType.BloodBending:
                break;
            case SkillType.MetalBending:
                break;
            default:
                break;
        }

        // Looking forward
        // If linked skills are already unlocked enable set full link as well
        foreach (var linkedSkill in activeSkill.LinkedSkills)
        {
            if (skillManager.GetUnlockedSkills().Contains(linkedSkill))
            {
                // Linked skill is unlocked already
                SkillButton targetButton = allSkillButtonsWithIds[linkedSkill.Id];
                UILineRenderer link = null;

                if (buttonLinks.TryGetValue((activeSkillButton, targetButton), out link) ||
                    buttonLinks.TryGetValue((targetButton, activeSkillButton), out link))
                {
                    link.sprite = fullLinkSprite;
                    link.LineThickness = fullLinkLineThickness;                   
                    link.color = color;
                }
            }
        }

        // Looking backward
        // Get unlocked skills that can enable active skill and set full link
        foreach (var entry in allSkillButtonsWithIds)
        {
            SkillButton originButton = entry.Value;
            Skill originSkill = skillManager.GetSkillById(entry.Key);

            // If this skill is connected to the active skill and it is unlocked then enable full link
            if (skillManager.GetUnlockedSkills().Contains(originSkill) && originSkill.LinkedSkills.Contains(targetSkill))
            {
                UILineRenderer link = null;

                if (buttonLinks.TryGetValue((originButton, activeSkillButton), out link) ||
                    (buttonLinks.TryGetValue((activeSkillButton, originButton), out link)))
                {
                    link.sprite = fullLinkSprite;
                    link.LineThickness = fullLinkLineThickness;
                    link.color = color;
                }
                else
                {
                    Debug.LogError($"Buttons {originSkill} and {activeSkillButton} are linked but no link present in the dictionary!");
                }
            }
        }
    }

    private void ClearTheSkillTree()
    {
        helper.ResetHelper();

        var allLinks=buttonLinks.Values.ToList();
        foreach (var link in allLinks)
        {
            Destroy(link.gameObject);
        }

        var allSkillButtons=allSkillButtonsWithIds.Values.ToList();
        for (int i = 0; i < allSkillButtons.Count; i++)
        {
            Destroy(allSkillButtons[i].gameObject);
        }

        activeSkillButton = null;
        activeSkillId = 0;

        allSkillButtonsWithIds.Clear();
        allLinkedButtons.Clear();
        buttonLinks.Clear();
    }
}

public static class BezierHelper
{
    private static float defaultAngleRotation = 35;

    private static float angleVariation = 5;

    private static float minDistanceFromOrigin_PercentOfTotal = 0.2f;
    private static float maxDistanceFromOrigin_PercentOfTotal = 0.45f;


    public static (Vector3, Vector3) GetTangentsForBezier(Vector3 point1, Vector3 point2)
    {
        Vector3 tangent1 = new Vector3();
        Vector3 tangent2 = new Vector3();

        float angleOfRotation = 0;
        float magnitude = 1;

        // First tangent
        Vector3 firstToSecond = point2 - point1;
        magnitude = firstToSecond.magnitude;
        firstToSecond.Normalize();
        firstToSecond *= Random.Range(minDistanceFromOrigin_PercentOfTotal, maxDistanceFromOrigin_PercentOfTotal) * magnitude;
        angleOfRotation = defaultAngleRotation + Random.Range(-angleVariation, angleVariation);

        Quaternion rotation = Quaternion.AngleAxis(angleOfRotation, Vector3.forward);
        firstToSecond = rotation * firstToSecond;

        tangent1 = firstToSecond + point1;

        // Second tangent
        Vector3 secondToFirst = point1 - point2;
        magnitude = secondToFirst.magnitude;
        secondToFirst.Normalize();
        secondToFirst *= Random.Range(minDistanceFromOrigin_PercentOfTotal, maxDistanceFromOrigin_PercentOfTotal) * magnitude;
        angleOfRotation = defaultAngleRotation + Random.Range(-angleVariation, angleVariation);

        rotation = Quaternion.AngleAxis(angleOfRotation, Vector3.up);
        secondToFirst = rotation * secondToFirst;

        tangent2 = secondToFirst + point2;

        return (tangent1, tangent2);
    }
}




