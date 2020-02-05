using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New UIInfo", menuName = "RTS UIInfo", order = 0)]
public class UIInfo : ScriptableObject
{
    public string name = "New UIInfo";
    public Sprite unitPortrait;

    public bool useActionQueue;

    public ActionButton[] actions;
}
