using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Test updateable, only spins
public class SimpleUpdateable : UpdateableObj
{
    //contains only values, functions are unnecessary
    public float rotSpeed = 30f;

    //With a single function call, a function with which to update this object is stored in the lists
    //It has been added to GenericObjectList<SimpleUpdateable>, using function RotateSimple, and applies at the end of each Update()
    public override void UAwake()
    {
        MANUAL_UPDATE.GenericObjectList<SimpleUpdateable>.AddToSystem(this, NonsenseClass.RotateSimple, UpdateStages.UPDATE_END);
    }

    public override void UDestroy()
    {
        
    }
}

//Functions are stored in other class, preferably as statics
public class NonsenseClass
{
    public static void RotateSimple(SimpleUpdateable SU)
    {
        SU.transform.Rotate(0, SU.rotSpeed * Time.deltaTime, 0);
    }
}