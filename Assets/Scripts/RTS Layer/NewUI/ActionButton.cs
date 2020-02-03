using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "New ActionButton", menuName = "Action Button", order = -1)]
public class ActionButton : ScriptableObject
{
    public enum ActionType { 
        BUILD,
        DO_TRAIN,
        DO_MOVE,
        DO_ATTACK,
        DO_RALLY,
        DO_RELOAD,
    }

    public ActionType actionType;
    public int argument;
    public Sprite spriteToUse;
}
