using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSInput
{
    public class SelectionButton : MonoBehaviour
    {
        public EntityType prefabType;
        public Entity parentObject;

        public void OnCreate(Entity parentObj)
        {
            parentObject = parentObj;
            prefabType = parentObj.type;
        }

        public void OnClick()
        {
            SelectionUI.Instance.OnElementSelected(this);
        }

    }
}