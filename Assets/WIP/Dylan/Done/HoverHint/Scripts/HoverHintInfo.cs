using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Hover Hint", menuName = "Hover Hint", order = -1)]
public class HoverHintInfo : ScriptableObject
{
    [Header("Text")]
    public string title;
    public Color titleColor = Color.white;
    
    [Multiline]
    public string description;
}
