using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSInput
{

    public enum NotificationType
    {
        NONE,
        INSUFFICIENT_CREDITS,
        INVALID_PLACEMENT,
        GATE_OPENED,
        SUPPLY_BLOCKED,
        QUEUE_FULL,
        MINUTE_MARK,
        THIRTY_MARK,
        TEN_MARK,

    }

    public class NotificationManager : MonoBehaviour
    {
        #region SingletonCode
        private static NotificationManager _instance;
        public static NotificationManager Instance { get { return _instance; } }
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }

        }
        //single pattern ends here
        #endregion

        public GameObject notification;
        public Transform notifCanvas;


        //add notification to list
        public void HitNotification(NotificationType type) {
            switch (type) {
                case NotificationType.INSUFFICIENT_CREDITS:
                    StartCoroutine(NotifPlay("Insufficient Credits"));
                    break;
                case NotificationType.INVALID_PLACEMENT:
                    StartCoroutine(NotifPlay("Invalid Placements"));
                    break;
                case NotificationType.GATE_OPENED:
                    StartCoroutine(NotifPlay("A Gate was Opened"));
                    break;
                case NotificationType.MINUTE_MARK:
                    StartCoroutine(NotifPlay("One Minute Remaining"));
                    break;
                case NotificationType.SUPPLY_BLOCKED:
                    StartCoroutine(NotifPlay("You are out of supply"));
                    break;
                case NotificationType.QUEUE_FULL:
                    StartCoroutine(NotifPlay("Your Queue is Full"));
                    break;
                case NotificationType.THIRTY_MARK:
                    StartCoroutine(NotifPlay("Thirty Seconds Remaining"));
                    break;
                case NotificationType.TEN_MARK:
                    StartCoroutine(NotifPlay("Ten Seconds Remaining"));
                    break;

            }
        }


        IEnumerator NotifPlay(string newText)
        {
            GameObject notifObj = Instantiate(notification, new Vector3(960, 365,0), Quaternion.identity);
            notifObj.transform.parent = notifCanvas;
            notifObj.GetComponent<Text>().text = newText;


            Animation anim = notifObj.GetComponent<Animation>();
            
            //play build animation
            anim.Play();

            while (anim.isPlaying)
            {
                yield return 0;
            }

            Destroy(notifObj);
        }

    }
}