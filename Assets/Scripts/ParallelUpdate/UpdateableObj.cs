using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base class for anything that updates via the manual updater.
//It is completely optional whether or not you use the manual updater on children of this class.
//Update() still works, only Awake and OnDestroy are overriden (with substitutes provided, UAwake and UDestroy).
public abstract class UpdateableObj : MonoBehaviour
{
    public List<MANUAL_UPDATE.BaseObjectList> functions { get; private set; }

    //Create a private list of all the ObjectLists that own this entity.
    //Used to remove itself from all objectlists once the item is destroyed.
    private void Awake()
    {
        functions = new List<MANUAL_UPDATE.BaseObjectList>();

        UAwake();
    }

    //This is where any Awake code would go
    //Also should be used to add this object to objectLists
    public abstract void UAwake();

    //Remove from the list
    protected void OnDestroy()
    {
        UDestroy();

        for (int i = functions.Count - 1; i >= 0; --i)
        {
            functions[i].RemoveFromSystem(this);
        }
    }

    public abstract void UDestroy();
}
