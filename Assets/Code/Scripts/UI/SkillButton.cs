using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField] private Button myButton;
    [SerializeField] private TMP_Text myText;
    
    public static Action<SkillButton> Activated;


    public void SetText(string text)
    {
        this.myText.text = text; //...
    }

    public void SetInteractionState(bool isInteractable)
    {
        myButton.interactable = isInteractable;
    }
    
    public void Activate()
    {
        Activated?.Invoke(this);
    }

    public void SetScale(bool isSelected)
    {
        transform.localScale = isSelected ? Vector3.one * 1.5f : Vector3.one;
    }

    public void SetColor(Color color)
    {
        myButton.image.color = color;
    }

    public override string ToString()
    {
        return gameObject.name;
    }
}
