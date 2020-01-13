using UnityEngine;

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
