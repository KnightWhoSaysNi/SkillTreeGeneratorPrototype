using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillTreeHelper : MonoBehaviour
{
    public int numberOfTotalIterations = 20;
    public int numberOfAppliedForcesIteration = 5;
    public SkillTreeController controller;
    [Min(0)]
    public int yieldFrequency = 5;
    public bool isEnabled;
    [Space(10)]
    public float rootMaxDistanceFromCenter = 150;
    public float maxDistanceFromCenter = 1500;

    public float maxForce = 100;
    public float attractionForce = 5;
    public float attractionMinDistance = 50;
    public float attractionMaxDistance = 100;

    public float separationForce = 3;
    public float maxDistanceForSeparation = 200;

    private Coroutine generationCoroutine;

    private HashSet<SkillButton> rootButtons = new HashSet<SkillButton>();
    private Dictionary<SkillButton, List<SkillButton>> allLinkedButtons = new Dictionary<SkillButton, List<SkillButton>>();

    private int yieldCounter;

    public void OnValidate()
    {
        if (!isEnabled)
        {
            return;
        }

        if(generationCoroutine!=null)
        {
            StopCoroutine(generationCoroutine);
        }

        generationCoroutine = StartCoroutine(PositionSkillButtons());        
    }

    public void RegisterButtons(int seed, bool shouldUseSeed, HashSet<SkillButton> rootButtons, Dictionary<SkillButton, List<SkillButton>> allLinkedButtons)
    {
        if (shouldUseSeed)
        {
            UnityEngine.Random.InitState(seed);
        }
        this.rootButtons = rootButtons;
        this.allLinkedButtons = allLinkedButtons;

        OnValidate();
    }
    public void ResetHelper()
    {
        rootButtons.Clear();
        yieldCounter = 0;
        if(generationCoroutine!=null)
        {
            StopCoroutine(generationCoroutine );
        }
    }

    private IEnumerator PositionSkillButtons()
    {
        float sweetSpot = (attractionMinDistance + attractionMaxDistance) / 2;

        // Initialize velocities
        Dictionary<SkillButton, Vector3> velocities = new Dictionary<SkillButton, Vector3>();
        foreach (var entry in allLinkedButtons)
        {
            velocities.Add(entry.Key, new Vector3(0, 0, 0));
        }

        #region - Disconnected buttons -
        Dictionary<SkillButton, List<SkillButton>> allDisconnectedButtons = new Dictionary<SkillButton, List<SkillButton>>();
        foreach (var entry in allLinkedButtons)
        {
            List<SkillButton> disconnectedButtons = new List<SkillButton>();
            disconnectedButtons.AddRange(allLinkedButtons.Where(x => x.Key != entry.Key && !entry.Value.Contains(x.Key)).Select(x => x.Key));
            allDisconnectedButtons.Add(entry.Key, disconnectedButtons);
        }
        #endregion

        #region - Parameters -
        //float rootMaxDistanceFromCenter = 150;

        //float attractionForce = 5;
        //float attractionMinDistance = 50;
        //float attractionMaxDistance = 100;
        //float sweetSpot = (attractionMinDistance + attractionMaxDistance) / 2;

        //float separationForce = 3;
        //float maxDistanceForSeparation = 200; 
        #endregion

        for (int a = 0; a < numberOfTotalIterations; a++)
        {
            for (int i = 0; i < numberOfAppliedForcesIteration; i++)
            {
                foreach (var button in velocities.Keys.ToList())
                {
                    velocities[button] = Vector3.zero;
                }

                // Attraction
                foreach (var entry in allLinkedButtons)
                {
                    Vector3 center = entry.Key.transform.localPosition;

                    #region - Attraction of connected buttons to self -
                    // Go through each connected button and attract it to this(its "parent") button
                    foreach (var button in entry.Value)
                    {
                        center += button.transform.localPosition;

                        var direction = entry.Key.transform.localPosition - button.transform.localPosition;
                        float distance = direction.magnitude;
                        if (distance >= attractionMinDistance && distance <= attractionMaxDistance)
                        {
                            // No need to apply a force, already in the ideal zone
                            continue;
                        }

                        float difference = distance - sweetSpot;
                        var acceleration = difference * attractionForce * direction.normalized;

                        velocities[button] += acceleration;
                    }
                    #endregion

                    #region - Attraction to connected buttons -
                    //// Add a little bit of attraction for self towards linked buttons
                    //center /= (1 + entry.Value.Count);
                    //var directionToCenter = entry.Key.transform.localPosition - center;
                    //float distanceToCenter = directionToCenter.magnitude;
                    //if (distanceToCenter >= attractionMinDistance && distanceToCenter <= attractionMaxDistance)
                    //{
                    //    // No need to apply any more forces
                    //    continue;
                    //}
                    //float differenceToCenter = distanceToCenter - sweetSpot;
                    //var accelerationToCenter = differenceToCenter * attractionForce * 0.1f * directionToCenter.normalized;
                    //velocities[entry.Key] += accelerationToCenter;
                    #endregion
                }

                // Separation
                foreach (var entry in allDisconnectedButtons)
                {
                    Vector3 center = entry.Key.transform.localPosition;

                    #region - Separate neighbors from self -
                    // Go through each disconnected button and apply separation forces to this button
                    foreach (var button in entry.Value)
                    {
                        var direction = button.transform.localPosition - entry.Key.transform.localPosition;
                        float distance = direction.magnitude;
                        if (distance > maxDistanceForSeparation)
                        {
                            // No need to apply a force - too far away
                            continue;
                        }

                        center += button.transform.localPosition;

                        var acceleration = (separationForce / Mathf.Pow(distance, 2)) * direction.normalized;

                        velocities[button] += acceleration;
                    }
                    #endregion
                }

                // Start with root buttons and ensure that they can't go too far away from center
                foreach (var root in rootButtons)
                {
                    var velocity = velocities[root];
                    if (velocity.magnitude > maxForce)
                    {
                        velocity = velocity.normalized * maxForce;
                    }
                    root.transform.localPosition += velocity;
                    if (root.transform.localPosition.magnitude > rootMaxDistanceFromCenter)
                    {
                        root.transform.localPosition = root.transform.localPosition.normalized * rootMaxDistanceFromCenter;
                    }
                }

                // Go through all the other buttons now
                foreach (var button in allLinkedButtons.Keys)
                {
                    if (rootButtons.Contains(button))
                    {
                        continue;
                    }

                    var velocity = velocities[button];
                    if (velocity.magnitude > maxForce)
                    {
                        velocity = velocity.normalized * maxForce;
                    }

                    var directionToCenter = Vector3.zero - (button.transform.localPosition + velocity);
                    if (directionToCenter.magnitude > maxDistanceFromCenter)
                    {
                        velocity += directionToCenter.normalized * maxForce;
                    }
                    button.transform.localPosition += velocity;

                }
            }

            controller.GenerateLinks();

            yieldCounter++;
            if(yieldCounter==yieldFrequency)
            {
                yieldCounter = 0;
                yield return null;
            }
        }

    }

}
