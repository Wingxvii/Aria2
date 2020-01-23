using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RTSInput
{

    public class QueueButton : MonoBehaviour
    {
        public int queuePlace = 0;

        public void OnClick()
        {
            if (InputManager.Instance.PrimaryEntity.type == EntityType.Barracks)
            {

                if (queuePlace == 0)
                {
                    InputManager.Instance.PrimaryEntity.gameObject.GetComponent<Barracks>().currentBuildTime = 0;
                    ResourceManager.Instance.Refund(EntityType.Droid);
                }
                else {
                    InputManager.Instance.PrimaryEntity.gameObject.GetComponent<Barracks>().buildTimes.Dequeue();
                    ResourceManager.Instance.Refund(EntityType.Droid);
                }
            }
            else {
                Debug.Log("Queue Button Selection was not valid");
            }
        }
    }
}